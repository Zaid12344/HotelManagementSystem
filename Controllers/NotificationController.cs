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

[Authorize(Roles = "Admin,Manager")]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly NotificationService _notifications;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(
        ApplicationDbContext db,
        NotificationService notifications,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _notifications = notifications;
        _userManager = userManager;
    }

    // GET: /Notifications?filter=all|failed|today|alerts
    public async Task<IActionResult> Index(string filter = "all")
    {
        var q = _db.Notifications.AsQueryable();

        q = filter switch
        {
            "failed" => q.Where(n => n.Status == NotificationStatus.Failed),
            "today" => q.Where(n => n.CreatedAt.Date == DateTime.Today),
            "alerts" => q.Where(n => n.Channel == NotificationChannel.System),
            _ => q
        };

        var list = await q
            .OrderByDescending(n => n.CreatedAt)
            .Take(500)
            .Select(n => new NotificationListViewModel
            {
                Id = n.Id,
                Type = n.Type,
                Channel = n.Channel,
                Status = n.Status,
                Recipient = n.Recipient,
                Subject = n.Subject,
                ErrorMessage = n.ErrorMessage,
                CreatedAt = n.CreatedAt,
                SentAt = n.SentAt,
                ReservationId = n.ReservationId
            })
            .ToListAsync();

        ViewBag.Filter = filter;
        return View(list);
    }

    // GET: /Notifications/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var n = await _db.Notifications.FindAsync(id);
        if (n == null) return NotFound();
        return View(n);
    }

    // POST: /Notifications/RunDailyJobs
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RunDailyJobs()
    {
        try
        {
            await _notifications.SendArrivalRemindersAsync();
            await _notifications.CheckLowOccupancyAsync();
            await _notifications.CheckPendingPaymentsAsync();

            await LogActivity("RunDailyJobs",
                "Arrival reminders + occupancy + pending payments checks run");
            TempData["Success"] = "Daily notification jobs completed. Check the log below.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

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