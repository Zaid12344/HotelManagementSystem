using HotelManagement.Models.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services;

public class MaintenanceService
{
    private readonly ApplicationDbContext _db;

    public MaintenanceService(ApplicationDbContext db) => _db = db;

    public async Task<MaintenanceRequest> CreateAsync(
        int roomId, string title, string description,
        MaintenancePriority priority, string? userId)
    {
        var room = await _db.Rooms.FindAsync(roomId)
            ?? throw new InvalidOperationException("Room not found.");

        var request = new MaintenanceRequest
        {
            RoomId = roomId,
            Title = title,
            Description = description,
            Priority = priority,
            Status = MaintenanceStatus.Open,
            ReportedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _db.MaintenanceRequests.Add(request);

        // Block the room
        room.Status = RoomStatus.Maintenance;
        room.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return request;
    }

    public async Task<MaintenanceRequest> UpdateStatusAsync(
        int requestId, MaintenanceStatus newStatus, string? resolutionNotes)
    {
        var req = await _db.MaintenanceRequests
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Request not found.");

        req.Status = newStatus;
        if (!string.IsNullOrWhiteSpace(resolutionNotes)) req.ResolutionNotes = resolutionNotes;

        if (newStatus == MaintenanceStatus.Resolved)
        {
            req.ResolvedAt = DateTime.UtcNow;

            // Any other unresolved maintenance for this room?
            var otherOpen = await _db.MaintenanceRequests
                .AnyAsync(m => m.RoomId == req.RoomId
                            && m.Id != req.Id
                            && m.Status != MaintenanceStatus.Resolved);

            if (!otherOpen && req.Room.Status == RoomStatus.Maintenance)
            {
                // Move to Cleaning if there's an open task, else Available
                var hasOpenCleaning = await _db.HousekeepingTasks
                    .AnyAsync(t => t.RoomId == req.RoomId && t.Status != HousekeepingTaskStatus.Completed);

                req.Room.Status = hasOpenCleaning ? RoomStatus.Cleaning : RoomStatus.Available;
                req.Room.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (newStatus == MaintenanceStatus.InProgress && req.Room.Status != RoomStatus.Maintenance)
        {
            // Re-block if it wasn't already
            req.Room.Status = RoomStatus.Maintenance;
            req.Room.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return req;
    }
}