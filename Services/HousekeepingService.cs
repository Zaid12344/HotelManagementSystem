using HotelManagement.Models.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services;

public class HousekeepingService
{
    private readonly ApplicationDbContext _db;

    public HousekeepingService(ApplicationDbContext db) => _db = db;

    public async Task<HousekeepingTask> CompleteTaskAsync(int taskId, string? notes, string? userId)
    {
        var task = await _db.HousekeepingTasks
            .Include(t => t.Room)
            .FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new InvalidOperationException("Task not found.");

        task.Status = HousekeepingTaskStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(notes)) task.Notes = notes;

        // Room → Available unless under maintenance
        var inMaintenance = await _db.MaintenanceRequests
            .AnyAsync(m => m.RoomId == task.RoomId && m.Status != MaintenanceStatus.Resolved);

        if (!inMaintenance && task.Room.Status == RoomStatus.Cleaning)
        {
            task.Room.Status = RoomStatus.Available;
            task.Room.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return task;
    }
}