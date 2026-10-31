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
public class ServiceController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ServiceRequestService _requestService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ServiceController(
        ApplicationDbContext db,
        ServiceRequestService requestService,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _requestService = requestService;
        _userManager = userManager;
    }

    // ----- Catalog -----

    // GET: /Service
    public async Task<IActionResult> Index()
    {
        var services = await _db.Services
            .OrderBy(s => s.Category).ThenBy(s => s.Name)
            .Select(s => new ServiceListViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Category = s.Category,
                Price = s.Price,
                IsActive = s.IsActive
            })
            .ToListAsync();
        return View(services);
    }

    // GET: /Service/Create
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create() => View(new ServiceFormViewModel());

    // POST: /Service/Create
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(ServiceFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        _db.Services.Add(new Service
        {
            Name = model.Name,
            Description = model.Description,
            Category = model.Category,
            Price = model.Price,
            IsActive = model.IsActive
        });
        await _db.SaveChangesAsync();
        await LogActivity("CreateService", $"Service: {model.Name}");

        TempData["Success"] = "Service created.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Service/Edit/5
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int id)
    {
        var s = await _db.Services.FindAsync(id);
        if (s == null) return NotFound();

        return View(new ServiceFormViewModel
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            Category = s.Category,
            Price = s.Price,
            IsActive = s.IsActive
        });
    }

    // POST: /Service/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int id, ServiceFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var s = await _db.Services.FindAsync(id);
        if (s == null) return NotFound();

        s.Name = model.Name;
        s.Description = model.Description;
        s.Category = model.Category;
        s.Price = model.Price;
        s.IsActive = model.IsActive;
        s.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await LogActivity("EditService", $"Service {s.Name}");

        TempData["Success"] = "Service updated.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Service/ToggleActive/5
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var s = await _db.Services.FindAsync(id);
        if (s == null) return NotFound();

        s.IsActive = !s.IsActive;
        s.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Service {(s.IsActive ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(Index));
    }

    // ----- Requests -----

    // GET: /Service/Requests
    public async Task<IActionResult> Requests(string filter = "all")
    {
        var q = _db.ServiceRequests
            .Include(r => r.Service)
            .Include(r => r.Reservation).ThenInclude(res => res.Guest)
            .Include(r => r.Room)
            .AsQueryable();

        q = filter switch
        {
            "open" => q.Where(r => r.Status == ServiceRequestStatus.Requested
                                    || r.Status == ServiceRequestStatus.InProgress),
            "today" => q.Where(r => r.RequestedAt.Date == DateTime.Today),
            "fulfilled" => q.Where(r => r.Status == ServiceRequestStatus.Fulfilled),
            _ => q
        };

        var list = await q
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => new ServiceRequestsListViewModel
            {
                Id = r.Id,
                ServiceName = r.Service.Name,
                Category = r.Service.Category.ToString(),
                Quantity = r.Quantity,
                UnitPrice = r.Service.Price,
                TotalPrice = r.Service.Price * r.Quantity,
                BookingReference = r.Reservation.BookingReference,
                GuestName = r.Reservation.Guest.FullName,
                RoomNumber = r.Room != null ? r.Room.RoomNumber : null,
                Status = r.Status,
                RequestedAt = r.RequestedAt,
                FulfilledAt = r.FulfilledAt,
                PostedToFolio = r.FolioLineId != null,
                Notes = r.Notes
            })
            .ToListAsync();

        ViewBag.Filter = filter;
        return View(list);
    }

    // GET: /Service/PostToFolio/5   (reservationId)
    [Authorize(Roles = "Admin,Manager,FrontDesk,Accountant")]
    public async Task<IActionResult> PostToFolio(int id)
    {
        var r = await _db.Reservations
            .Include(x => x.Guest)
            .Include(x => x.ReservationRooms).ThenInclude(rr => rr.Room)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();

        if (r.Status != ReservationStatus.CheckedIn)
        {
            TempData["Error"] = "Service charges can only be posted for an in-house guest.";
            return RedirectToAction("Details", "Reservation", new { id });
        }

        var services = await _db.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.Category).ThenBy(s => s.Name)
            .Select(s => new ServiceListViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Category = s.Category,
                Price = s.Price,
                IsActive = s.IsActive
            })
            .ToListAsync();

        var firstRoom = r.ReservationRooms.FirstOrDefault();

        return View(new PostServiceViewModel
        {
            ReservationId = r.Id,
            BookingReference = r.BookingReference,
            GuestName = r.Guest.FullName,
            RoomNumber = firstRoom?.Room.RoomNumber ?? "—",
            RoomId = firstRoom?.RoomId,
            AvailableServices = services
        });
    }

    // POST: /Service/PostToFolio
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,FrontDesk,Accountant")]
    public async Task<IActionResult> PostToFolio(PostServiceViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableServices = await _db.Services
                .Where(s => s.IsActive)
                .Select(s => new ServiceListViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Category = s.Category,
                    Price = s.Price,
                    IsActive = s.IsActive
                }).ToListAsync();
            return View(model);
        }

        try
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var req = new ServiceRequest
            {
                ServiceId = model.ServiceId,
                ReservationId = model.ReservationId,
                RoomId = model.RoomId,
                Quantity = model.Quantity,
                Notes = model.Notes,
                RequestedByUserId = currentUser?.Id,
                Status = ServiceRequestStatus.Requested
            };
            _db.ServiceRequests.Add(req);
            await _db.SaveChangesAsync();

            var line = await _requestService.FulfillAndPostAsync(req.Id, currentUser?.Id);

            await LogActivity("PostService",
                $"Reservation {model.ReservationId}: {line?.Description} ₹{line?.Amount:N2}");

            TempData["Success"] = line != null
                ? $"Service charge posted: {line.Description} — ₹{line.Amount:N2}"
                : "Request created but no open folio found.";
            return RedirectToAction("Details", "Reservation", new { id = model.ReservationId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.AvailableServices = await _db.Services
                .Where(s => s.IsActive)
                .Select(s => new ServiceListViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Category = s.Category,
                    Price = s.Price,
                    IsActive = s.IsActive
                }).ToListAsync();
            return View(model);
        }
    }

    // POST: /Service/FulfillRequest/5
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,FrontDesk")]
    public async Task<IActionResult> FulfillRequest(int id)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var line = await _requestService.FulfillAndPostAsync(id, currentUser?.Id);
            await LogActivity("FulfillServiceRequest", $"Request {id} fulfilled");
            TempData["Success"] = line != null
                ? $"Fulfilled and posted ₹{line.Amount:N2} to folio."
                : "Fulfilled (no open folio to post).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Requests));
    }

    // POST: /Service/CancelRequest/5
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,FrontDesk")]
    public async Task<IActionResult> CancelRequest(int id)
    {
        var req = await _db.ServiceRequests.FindAsync(id);
        if (req == null) return NotFound();

        if (req.Status == ServiceRequestStatus.Fulfilled)
        {
            TempData["Error"] = "Cannot cancel a fulfilled request.";
            return RedirectToAction(nameof(Requests));
        }

        req.Status = ServiceRequestStatus.Cancelled;
        await _db.SaveChangesAsync();
        await LogActivity("CancelServiceRequest", $"Request {id} cancelled");

        TempData["Success"] = "Request cancelled.";
        return RedirectToAction(nameof(Requests));
    }

    // ----- helpers -----

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