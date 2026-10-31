using HotelManagement.Models.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services;

public class ServiceRequestService
{
    private readonly ApplicationDbContext _db;

    public ServiceRequestService(ApplicationDbContext db) => _db = db;

    /// <summary>
    /// Marks a request as Fulfilled and posts the charge to the reservation's open folio.
    /// Returns the created FolioLine (or null if no open folio).
    /// </summary>
    public async Task<FolioLine?> FulfillAndPostAsync(int requestId, string? userId)
    {
        var req = await _db.ServiceRequests
            .Include(r => r.Service)
            .Include(r => r.Reservation)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Service request not found.");

        if (req.Status == ServiceRequestStatus.Fulfilled)
            throw new InvalidOperationException("Request already fulfilled.");

        if (req.Status == ServiceRequestStatus.Cancelled)
            throw new InvalidOperationException("Cannot fulfill a cancelled request.");

        req.Status = ServiceRequestStatus.Fulfilled;
        req.FulfilledAt = DateTime.UtcNow;

        // Find the open folio
        var folio = await _db.Folios
            .FirstOrDefaultAsync(f => f.ReservationId == req.ReservationId
                                   && f.Status == FolioStatus.Open);

        FolioLine? line = null;
        if (folio != null)
        {
            var amount = req.Service.Price * req.Quantity;
            line = new FolioLine
            {
                FolioId = folio.Id,
                LineType = FolioLineType.ServiceCharge,
                Description = $"{req.Service.Name} × {req.Quantity}",
                Amount = amount,
                RoomId = req.RoomId
            };
            _db.FolioLines.Add(line);
            folio.TotalCharges += amount;
            req.FolioLineId = line.Id;   // will be set after SaveChanges
        }

        await _db.SaveChangesAsync();

        // Update FK after line was saved
        if (line != null && req.FolioLineId == null)
        {
            req.FolioLineId = line.Id;
            await _db.SaveChangesAsync();
        }

        return line;
    }
}