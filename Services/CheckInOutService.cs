using HotelManagement.Models.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Models.Enums;
using HotelManagement.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotelManagement.Services;

public class CheckInOutService
{
    private readonly ApplicationDbContext _db;
    private readonly HotelSettings? _settings;

    public CheckInOutService(ApplicationDbContext db, IOptions<HotelSettings> settings)
    {
        _db = db;
        _settings = settings.Value ?? new HotelSettings();
    }
    public CheckInOutService(ApplicationDbContext db) => _db = db;

    /// <summary>
    /// Performs check-in: creates folio, posts room charges, marks rooms Occupied,
    /// and updates reservation status.
    /// </summary>
    public async Task<Folio> CheckInAsync(int reservationId, string? notes = null)
    {
        var reservation = await _db.Reservations
            .Include(r => r.Guest)
            .Include(r => r.ReservationRooms).ThenInclude(rr => rr.Room)
            .FirstOrDefaultAsync(r => r.Id == reservationId)
            ?? throw new InvalidOperationException("Reservation not found.");

        if (reservation.Status == ReservationStatus.CheckedIn)
            throw new InvalidOperationException("Guest is already checked in.");

        if (reservation.Status == ReservationStatus.CheckedOut ||
            reservation.Status == ReservationStatus.Cancelled ||
            reservation.Status == ReservationStatus.NoShow)
            throw new InvalidOperationException(
                $"Cannot check in a reservation with status '{reservation.Status}'.");

        var folioNumber = await GenerateFolioNumberAsync();

        var folio = new Folio
        {
            FolioNumber = folioNumber,
            ReservationId = reservation.Id,
            GuestId = reservation.GuestId,
            Status = FolioStatus.Open,
            Notes = notes,
            OpenedAt = DateTime.UtcNow
        };

        var nights = reservation.Nights;
        if (nights <= 0) nights = 1;

        // Post room charges
        foreach (var rr in reservation.ReservationRooms)
        {
            folio.Lines.Add(new FolioLine
            {
                LineType = FolioLineType.RoomCharge,
                Description = $"Room {rr.Room.RoomNumber} — {nights} night(s) @ ₹{rr.NightlyRate:N0}",
                Amount = rr.NightlyRate * nights,
                RoomId = rr.RoomId
            });

            // Mark room Occupied
            rr.Room.Status = RoomStatus.Occupied;
            rr.Room.UpdatedAt = DateTime.UtcNow;
        }

        // Compute subtotal
        var subtotal = folio.Lines
            .Where(l => l.LineType == FolioLineType.RoomCharge || l.LineType == FolioLineType.ServiceCharge)
            .Sum(l => l.Amount);

        // Post tax
        if (_settings?.TaxRatePercent > 0)
        {
            var tax = Math.Round(subtotal * (_settings.TaxRatePercent / 100m), 2);
            folio.Lines.Add(new FolioLine
            {
                LineType = FolioLineType.Tax,
                Description = $"GST @ {_settings.TaxRatePercent:0.##}%",
                Amount = tax
            });
        }

        // Apply the reservation deposit as an initial payment credit
        if (reservation.DepositAmount > 0)
        {
            folio.Lines.Add(new FolioLine
            {
                LineType = FolioLineType.Adjustment,
                Description = "Advance deposit applied",
                Amount = -reservation.DepositAmount
            });
            folio.TotalPaid = reservation.DepositAmount;
        }


        // Wait — total charges should sum all positive lines:
        folio.TotalCharges = folio.Lines
            .Where(l => l.LineType != FolioLineType.Adjustment)
            .Sum(l => l.Amount);


        reservation.Status = ReservationStatus.CheckedIn;
        reservation.UpdatedAt = DateTime.UtcNow;

        _db.Folios.Add(folio);
        await _db.SaveChangesAsync();

        return folio;
    }

    /// <summary>
    /// Performs check-out: closes folio, sets reservation CheckedOut, marks rooms Cleaning.
    /// </summary>
    public async Task<Folio> CheckOutAsync(int reservationId , bool allowUnpaid = false , string? overrideReason = null)
    {
        var reservation = await _db.Reservations
            .Include(r => r.ReservationRooms).ThenInclude(rr => rr.Room)
            .FirstOrDefaultAsync(r => r.Id == reservationId)
            ?? throw new InvalidOperationException("Reservation not found.");

        if (reservation.Status != ReservationStatus.CheckedIn)
            throw new InvalidOperationException("Guest is not currently checked in.");

        var folio = await _db.Folios
            .Include(f => f.Lines)
            .FirstOrDefaultAsync(f => f.ReservationId == reservationId && f.Status == FolioStatus.Open)
            ?? throw new InvalidOperationException("No open folio found for this reservation.");

        var charges = folio.Lines
        .Where(l => l.LineType != FolioLineType.Adjustment).Sum(l => l.Amount);
        var balance = charges - folio.TotalPaid;

        if (balance > 0.01m && !allowUnpaid)
            throw new InvalidOperationException(
                $"Outstanding balance ₹{balance:N2}. Record payment first, or override with a reason.");

        if (allowUnpaid && string.IsNullOrWhiteSpace(overrideReason))
            throw new InvalidOperationException("Override reason is required.");

        if (allowUnpaid)
        {
            folio.Notes = (folio.Notes ?? "") + $"\n[UNPAID CHECKOUT] {overrideReason}";
        }


        // (Payment collection handled in Module 5; here we just close the folio.)
        folio.Status = FolioStatus.Settled;
        folio.ClosedAt = DateTime.UtcNow;

        reservation.Status = ReservationStatus.CheckedOut;
        reservation.UpdatedAt = DateTime.UtcNow;

        foreach (var rr in reservation.ReservationRooms)
        {
            rr.Room.Status = RoomStatus.Cleaning;
            rr.Room.UpdatedAt = DateTime.UtcNow;

            // Auto-create a housekeeping task
            _db.HousekeepingTasks.Add(new HousekeepingTask
            {
                RoomId = rr.RoomId,
                TaskType = "Checkout Clean",
                Status = HousekeepingTaskStatus.Pending,
                Notes = $"Guest check-out — reservation {reservation.BookingReference}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return folio;
    }

    public async Task<string> GenerateFolioNumberAsync()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var countToday = await _db.Folios
            .CountAsync(f => f.FolioNumber.StartsWith($"FOL-{today}-"));
        return $"FOL-{today}-{(countToday + 1):D4}";
    }
}