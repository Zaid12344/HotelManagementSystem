using HotelManagement.Models.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services;

public class ReservationService
{
    private readonly ApplicationDbContext _db;

    public ReservationService(ApplicationDbContext db) => _db = db;

    /// <summary>
    /// Returns rooms that are NOT maintenance, and have no overlapping reservation
    /// for the given date range (excluding a given reservation if editing).
    /// </summary>
    public async Task<List<Room>> GetAvailableRoomsAsync(
        DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
    {
        checkIn = checkIn.Date;
        checkOut = checkOut.Date;

        // Overlap rule: existing.CheckIn < new.CheckOut AND existing.CheckOut > new.CheckIn
        var overlappingRoomIds = await _db.ReservationRooms
            .Where(rr =>
                rr.Reservation.Status != ReservationStatus.Cancelled &&
                rr.Reservation.Status != ReservationStatus.CheckedOut &&
                rr.Reservation.Status != ReservationStatus.NoShow &&
                (excludeReservationId == null || rr.ReservationId != excludeReservationId) &&
                rr.Reservation.CheckInDate < checkOut &&
                rr.Reservation.CheckOutDate > checkIn)
            .Select(rr => rr.RoomId)
            .Distinct()
            .ToListAsync();

        return await _db.Rooms
            .Where(r => r.Status != RoomStatus.Maintenance
                     && !overlappingRoomIds.Contains(r.Id))
            .OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber)
            .ToListAsync();
    }

    public async Task<string> GenerateBookingReferenceAsync()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var countToday = await _db.Reservations
            .CountAsync(r => r.BookingReference.StartsWith($"BK-{today}-"));

        return $"BK-{today}-{(countToday + 1):D4}";
    }
}