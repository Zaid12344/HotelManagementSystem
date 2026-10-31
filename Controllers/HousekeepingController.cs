using HotelManagement.Models.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Models.Enums;
using HotelManagement.Models.Identity;
using HotelManagement.Models.ViewModels;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers;

[Authorize]
public class HousekeepingController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly HousekeepingService _service;
    private readonly UserManager<ApplicationUser> _userManager;

    public HousekeepingController(
        ApplicationDbContext db,
        HousekeepingService service,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _service = service;
        _userManager = userManager;
    }

    // GET: /Housekeeping  — dashboard
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var isManager = User.IsInRole("Admin") || User.IsInRole("Manager");

        // Build a name lookup from AspNetUsers
        var userIds = await _db.HousekeepingTasks
            .Where(t => t.AssignedToUserId != null)
            .Select(t => t.AssignedToUserId!)
            .Distinct()
            .ToListAsync();

        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var allTasks = await _db.HousekeepingTasks
            .Include(t => t.Room)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var vm = new HousekeepingDashboardViewModel
        {
            MyTasks = allTasks
                .Where(t => t.AssignedToUserId == currentUser?.Id
                         && t.Status != HousekeepingTaskStatus.Completed)
                .Select(t => ToLine(t, users))
                .ToList(),

            AllPending = allTasks
                .Where(t => t.Status != HousekeepingTaskStatus.Completed)
                .Select(t => ToLine(t, users))
                .ToList(),

            CompletedToday = allTasks
                .Where(t => t.Status == HousekeepingTaskStatus.Completed
                         && t.CompletedAt.HasValue
                         && t.CompletedAt.Value.Date == DateTime.Today)
                .Select(t => ToLine(t, users))
                .ToList()
        };
        return View(vm);
    }

    // POST: /Housekeeping/Claim/5
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,Housekeeping")]
    public async Task<IActionResult> Claim(int id)
    {
        var task = await _db.HousekeepingTasks.FindAsync(id);
        if (task == null) return NotFound();

        var currentUser = await _userManager.GetUserAsync(User);
        task.AssignedToUserId = currentUser?.Id;
        task.Status = HousekeepingTaskStatus.InProgress;
        task.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await LogActivity("ClaimHousekeepingTask", $"Task {task.Id} (room {task.RoomId})");
        TempData["Success"] = "Task claimed.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Housekeeping/Assign
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Assign(int taskId, string userId)
    {
        var task = await _db.HousekeepingTasks.FindAsync(taskId);
        if (task == null) return NotFound();

        task.AssignedToUserId = userId;
        task.Status = HousekeepingTaskStatus.InProgress;
        task.StartedAt ??= DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await LogActivity("AssignHousekeepingTask", $"Task {taskId} → user {userId}");
        TempData["Success"] = "Task assigned.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Housekeeping/Complete/5
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,Housekeeping")]
    public async Task<IActionResult> Complete(int id, string? notes)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            await _service.CompleteTaskAsync(id, notes, currentUser?.Id);
            await LogActivity("CompleteHousekeepingTask", $"Task {id} completed");
            TempData["Success"] = "Task completed. Room is now available (unless under maintenance).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    // ----- helpers -----

    private static HousekeepingTaskLine ToLine(HousekeepingTask t, Dictionary<string, string> users) => new()
    {
        Id = t.Id,
        RoomNumber = t.Room.RoomNumber,
        Floor = t.Room.Floor,
        TaskType = t.TaskType,
        Status = t.Status,
        Notes = t.Notes,
        AssignedToUserId = t.AssignedToUserId,
        AssignedToName = t.AssignedToUserId != null && users.TryGetValue(t.AssignedToUserId, out var n) ? n : null,
        CreatedAt = t.CreatedAt,
        CompletedAt = t.CompletedAt
    };

    private async Task LogActivity(string activity, string? details = null)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        _db.UserActivityLogs.Add(new UserActivityLog
        {
            UserId = currentUser?.Id ?? "system",
            Activity = activity,
            Details = details
        });
        await _db.SaveChangesAsync();
    }
}