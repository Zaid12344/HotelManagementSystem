using HotelManagement.Models.Data;
using HotelManagement.Models.Enums;
using HotelManagement.Models.Identity;
using HotelManagement.Models.ViewModels;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using HotelManagement.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers;

[Authorize]
public class MaintenanceController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly MaintenanceService _service;
    private readonly UserManager<ApplicationUser> _userManager;

    public MaintenanceController(
        ApplicationDbContext db,
        MaintenanceService service,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _service = service;
        _userManager = userManager;
    }

    // GET: /Maintenance
    public async Task<IActionResult> Index()
    {
        var requests = await _db.MaintenanceRequests
            .Include(m => m.Room)
            .OrderByDescending(m => m.Status == MaintenanceStatus.Resolved ? 0 : 1)
            .ThenByDescending(m => m.Priority)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();

        var reporterIds = requests.Where(r => r.ReportedByUserId != null)
            .Select(r => r.ReportedByUserId!).Distinct().ToList();
        var reporters = await _userManager.Users
            .Where(u => reporterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var vm = requests.Select(r => new MaintenanceListViewModel
        {
            Id = r.Id,
            RoomNumber = r.Room.RoomNumber,
            Title = r.Title,
            Description = r.Description,
            Priority = r.Priority,
            Status = r.Status,
            ReportedByName = r.ReportedByUserId != null && reporters.TryGetValue(r.ReportedByUserId, out var n) ? n : null,
            CreatedAt = r.CreatedAt,
            ResolvedAt = r.ResolvedAt,
            ResolutionNotes = r.ResolutionNotes
        }).ToList();

        return View(vm);
    }

    // GET: /Maintenance/Create
    [Authorize(Roles = "Admin,Manager,FrontDesk,Housekeeping")]
    public async Task<IActionResult> Create()
    {
        var vm = new MaintenanceFormViewModel();
        await PopulateRoomsAsync(vm);
        return View(vm);
    }

    // POST: /Maintenance/Create
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,FrontDesk,Housekeeping")]
    public async Task<IActionResult> Create(MaintenanceFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateRoomsAsync(model);
            return View(model);
        }

        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var req = await _service.CreateAsync(
                model.RoomId, model.Title, model.Description,
                model.Priority, currentUser?.Id);

            await LogActivity("CreateMaintenance",
                $"Room {req.RoomId}: {req.Title} ({req.Priority})");

            TempData["Success"] = "Maintenance request created. Room is now blocked.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateRoomsAsync(model);
            return View(model);
        }
    }

    // GET: /Maintenance/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var req = await _db.MaintenanceRequests
            .Include(m => m.Room)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (req == null) return NotFound();

        var reporter = req.ReportedByUserId != null
            ? await _userManager.FindByIdAsync(req.ReportedByUserId) : null;

        var vm = new MaintenanceListViewModel
        {
            Id = req.Id,
            RoomNumber = req.Room.RoomNumber,
            Title = req.Title,
            Description = req.Description,
            Priority = req.Priority,
            Status = req.Status,
            ReportedByName = reporter?.FullName,
            CreatedAt = req.CreatedAt,
            ResolvedAt = req.ResolvedAt,
            ResolutionNotes = req.ResolutionNotes
        };
        return View(vm);
    }

    // POST: /Maintenance/UpdateStatus
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,Housekeeping")]
    public async Task<IActionResult> UpdateStatus(int id, MaintenanceStatus status, string? resolutionNotes)
    {
        try
        {
            await _service.UpdateStatusAsync(id, status, resolutionNotes);
            await LogActivity("UpdateMaintenanceStatus", $"Request {id} → {status}");
            TempData["Success"] = "Status updated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ----- helpers -----

    private async Task PopulateRoomsAsync(MaintenanceFormViewModel vm)
    {
        var rooms = await _db.Rooms
            .OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber)
            .Select(r => new { r.Id, Label = $"{r.RoomNumber} — {r.Type} (Floor {r.Floor})" })
            .ToListAsync();

        vm.Rooms = rooms.Select(r => new SelectListItem
        {
            Value = r.Id.ToString(),
            Text = r.Label
        }).ToList();
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