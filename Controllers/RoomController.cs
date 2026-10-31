using HotelManagement.Models.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Models.Enums;
using HotelManagement.Models.Identity;
using HotelManagement.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers;

[Authorize]
public class RoomController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoomController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // GET: /Room  — list all rooms
    public async Task<IActionResult> Index()
    {
        var rooms = await _db.Rooms
            .OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber)
            .Select(r => new RoomListViewModel
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                Type = r.Type,
                Rate = r.Rate,
                Status = r.Status,
                Floor = r.Floor,
                Amenities = r.Amenities
            })
            .ToListAsync();

        return View(rooms);
    }

    // GET: /Room/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var allRooms = await _db.Rooms.ToListAsync();

        var vm = new RoomDashboardViewModel
        {
            TotalRooms = allRooms.Count,
            StatusCounts = allRooms
                .GroupBy(r => r.Status)
                .ToDictionary(g => g.Key, g => g.Count()),
            TypeCounts = allRooms
                .GroupBy(r => r.Type)
                .ToDictionary(g => g.Key, g => g.Count()),
            Rooms = allRooms
                .OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber)
                .Select(r => new RoomListViewModel
                {
                    Id = r.Id,
                    RoomNumber = r.RoomNumber,
                    Type = r.Type,
                    Rate = r.Rate,
                    Status = r.Status,
                    Floor = r.Floor,
                    Amenities = r.Amenities
                }).ToList()
        };

        return View(vm);
    }

    // GET: /Room/Create
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create() => View(new RoomFormViewModel());

    // POST: /Room/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(RoomFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Ensure room number is unique
        if (await _db.Rooms.AnyAsync(r => r.RoomNumber == model.RoomNumber))
        {
            ModelState.AddModelError(nameof(model.RoomNumber), "Room number already exists.");
            return View(model);
        }

        var room = new Room
        {
            RoomNumber = model.RoomNumber,
            Type = model.Type,
            Rate = model.Rate,
            Floor = model.Floor,
            Amenities = model.Amenities,
            Status = model.Status
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        await LogActivity("CreateRoom", $"Created room {room.RoomNumber}");

        return RedirectToAction(nameof(Index));
    }

    // GET: /Room/Edit/5
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();

        var model = new RoomFormViewModel
        {
            Id = room.Id,
            RoomNumber = room.RoomNumber,
            Type = room.Type,
            Rate = room.Rate,
            Floor = room.Floor,
            Amenities = room.Amenities,
            Status = room.Status
        };
        return View(model);
    }

    // POST: /Room/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int id, RoomFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();

        // Unique room number check (excluding this room)
        if (await _db.Rooms.AnyAsync(r => r.RoomNumber == model.RoomNumber && r.Id != id))
        {
            ModelState.AddModelError(nameof(model.RoomNumber), "Room number already exists.");
            return View(model);
        }

        room.RoomNumber = model.RoomNumber;
        room.Type = model.Type;
        room.Rate = model.Rate;
        room.Floor = model.Floor;
        room.Amenities = model.Amenities;
        room.Status = model.Status;
        room.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await LogActivity("EditRoom", $"Updated room {room.RoomNumber}");

        return RedirectToAction(nameof(Index));
    }

    // GET: /Room/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();
        return View(room);
    }

    // POST: /Room/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();

        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync();
        await LogActivity("DeleteRoom", $"Deleted room {room.RoomNumber}");

        return RedirectToAction(nameof(Index));
    }

    // POST: /Room/UpdateStatus  (housekeeping + all staff can use)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, RoomStatus status)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();

        room.Status = status;
        room.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await LogActivity("UpdateRoomStatus", $"Room {room.RoomNumber} → {status}");
        return RedirectToAction(nameof(Dashboard));
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