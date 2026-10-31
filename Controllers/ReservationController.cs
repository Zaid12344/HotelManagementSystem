using HotelManagement.Models.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Models.Enums;
using HotelManagement.Models.Identity;
using HotelManagement.Models.ViewModels;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers;

[Authorize]
public class ReservationController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ReservationService _reservationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ReservationController> _logger;

    public ReservationController(
        ApplicationDbContext db,
        ReservationService reservationService,
        UserManager<ApplicationUser> userManager,
        ILogger<ReservationController> logger)
    {
        _db = db;
        _reservationService = reservationService;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: /Reservation?filter=all|arrivals|departures|inhouse|upcoming
    public async Task<IActionResult> Index(string filter = "all")
    {
        var query = _db.Reservations
            .Include(r => r.Guest)
            .Include(r => r.ReservationRooms).ThenInclude(rr => rr.Room)
            .AsQueryable();

        var today = DateTime.Today;

        query = filter switch
        {
            "arrivals" => query.Where(r => r.CheckInDate.Date == today
                                          && r.Status != ReservationStatus.Cancelled
                                          && r.Status != ReservationStatus.CheckedOut),
            "departures" => query.Where(r => r.CheckOutDate.Date == today
                                          && r.Status == ReservationStatus.CheckedIn),
            "inhouse" => query.Where(r => r.Status == ReservationStatus.CheckedIn),
            "upcoming" => query.Where(r => r.CheckInDate.Date > today
                                          && r.Status != ReservationStatus.Cancelled
                                          && r.Status != ReservationStatus.CheckedOut),
            _ => query
        };

        var list = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReservationListViewModel
            {
                Id = r.Id,
                BookingReference = r.BookingReference,
                GuestName = r.Guest.FullName,
                GuestPhone = r.Guest.Phone,
                CheckInDate = r.CheckInDate,
                CheckOutDate = r.CheckOutDate,
                Nights = EF.Functions.DateDiffDay(r.CheckInDate, r.CheckOutDate),
                Source = r.Source,
                Status = r.Status,
                TotalAmount = r.TotalAmount,
                DepositAmount = r.DepositAmount,
                RoomsText = string.Join(", ", r.ReservationRooms.Select(rr => rr.Room.RoomNumber))
            })
            .ToListAsync();

        ViewBag.Filter = filter;
        return View(list);
    }

    // GET: /Reservation/Create
    [Authorize(Roles = "Admin,Manager,FrontDesk")]
    public async Task<IActionResult> Create()
    {
        var model = new ReservationFormViewModel();
        await PopulateAvailableRoomsAsync(model);
        return View(model);
    }

    // POST: /Reservation/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,FrontDesk")]
    public async Task<IActionResult> Create(ReservationFormViewModel model)
    {
        ValidateDates(model);

        if (model.SelectedRoomIds == null || !model.SelectedRoomIds.Any())
            ModelState.AddModelError(nameof(model.SelectedRoomIds), "Select at least one room.");

        if (!ModelState.IsValid)
        {
            await PopulateAvailableRoomsAsync(model);
            return View(model);
        }

        // Re-check availability at save time (race condition protection)
        var available = await _reservationService.GetAvailableRoomsAsync(
            model.CheckInDate, model.CheckOutDate);
        var availableIds = available.Select(r => r.Id).ToHashSet();

        var selectedIds = model.SelectedRoomIds ?? new List<int>();
        var chosen = selectedIds.Where(id => availableIds.Contains(id)).ToList();
        if (chosen.Count != selectedIds.Count)
        {
            ModelState.AddModelError(string.Empty,
                "One or more selected rooms are no longer available for those dates. Please re-pick.");
            await PopulateAvailableRoomsAsync(model);
            return View(model);
        }

        // Get or create guest by email
        var guest = await _db.Guests.FirstOrDefaultAsync(g => g.Email == model.GuestEmail);
        if (guest == null)
        {
            guest = new Guest
            {
                FullName = model.GuestFullName,
                Email = model.GuestEmail,
                Phone = model.GuestPhone,
                Address = model.GuestAddress,
                IdProofType = model.GuestIdProofType,
                IdProofNumber = model.GuestIdProofNumber
            };
            _db.Guests.Add(guest);
            await _db.SaveChangesAsync();
        }
        else
        {
            guest.FullName = model.GuestFullName;
            guest.Phone = model.GuestPhone;
            guest.Address = model.GuestAddress;
            guest.IdProofType = model.GuestIdProofType;
            guest.IdProofNumber = model.GuestIdProofNumber;
        }

        // Build reservation
        var rooms = await _db.Rooms.Where(r => chosen.Contains(r.Id)).ToListAsync();
        var nights = (model.CheckOutDate.Date - model.CheckInDate.Date).Days;
        var total = rooms.Sum(r => r.Rate) * nights;

        var currentUser = await _userManager.GetUserAsync(User);

        var reservation = new Reservation
        {
            BookingReference = await _reservationService.GenerateBookingReferenceAsync(),
            GuestId = guest.Id,
            CheckInDate = model.CheckInDate.Date,
            CheckOutDate = model.CheckOutDate.Date,
            Adults = model.Adults,
            Children = model.Children,
            Source = model.Source,
            Status = model.Status,
            TotalAmount = total,
            DepositAmount = model.DepositAmount,
            Notes = model.Notes,
            GroupName = model.GroupName,
            CreatedByUserId = currentUser?.Id
        };

        foreach (var room in rooms)
        {
            reservation.ReservationRooms.Add(new ReservationRoom
            {
                RoomId = room.Id,
                NightlyRate = room.Rate
            });
            // Mark room as Reserved
            room.Status = RoomStatus.Reserved;
            room.UpdatedAt = DateTime.UtcNow;
        }

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        await LogActivity("CreateReservation",
            $"Booking {reservation.BookingReference} — {guest.FullName} — {nights} night(s) — {rooms.Count} room(s)");

        // 📧 Notification stub — real email/SMS to be added in Notification module
        _logger.LogInformation(
            "[NOTIFY] Booking confirmation would be sent to {Email} / {Phone} for {Ref}",
            guest.Email, guest.Phone, reservation.BookingReference);

        TempData["Success"] = $"Reservation {reservation.BookingReference} created successfully.";
        return RedirectToAction(nameof(Details), new { id = reservation.Id });
    }

    // GET: /Reservation/Edit/5
    [Authorize(Roles = "Admin,Manager,FrontDesk")]
    public async Task<IActionResult> Edit(int id)
    {
        var r = await _db.Reservations
            .Include(x => x.Guest)
            .Include(x => x.ReservationRooms)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r == null) return NotFound();

        if (r.Status == ReservationStatus.CheckedOut || r.Status == ReservationStatus.Cancelled)
        {
            TempData["Error"] = "Cannot edit a completed or cancelled reservation.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new ReservationFormViewModel
        {
            Id = r.Id,
            GuestFullName = r.Guest.FullName,
            GuestEmail = r.Guest.Email,
            GuestPhone = r.Guest.Phone,
            GuestAddress = r.Guest.Address,
            GuestIdProofType = r.Guest.IdProofType,
            GuestIdProofNumber = r.Guest.IdProofNumber,
            CheckInDate = r.CheckInDate,
            CheckOutDate = r.CheckOutDate,
            Adults = r.Adults,
            Children = r.Children,
            Source = r.Source,
            Status = r.Status,
            DepositAmount = r.DepositAmount,
            GroupName = r.GroupName,
            Notes = r.Notes,
            SelectedRoomIds = r.ReservationRooms.Select(rr => rr.RoomId).ToList()
        };

        await PopulateAvailableRoomsAsync(model, r.Id);
        return View(model);
    }

    // POST: /Reservation/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,FrontDesk")]
    public async Task<IActionResult> Edit(int id, ReservationFormViewModel model)
    {
        if (id != model.Id) return NotFound();

        ValidateDates(model);

        if (model.SelectedRoomIds == null || !model.SelectedRoomIds.Any())
            ModelState.AddModelError(nameof(model.SelectedRoomIds), "Select at least one room.");

        if (!ModelState.IsValid)
        {
            await PopulateAvailableRoomsAsync(model, id);
            return View(model);
        }

        var r = await _db.Reservations
            .Include(x => x.Guest)
            .Include(x => x.ReservationRooms)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r == null) return NotFound();

        // Get available rooms excluding this reservation
        var available = await _reservationService.GetAvailableRoomsAsync(
            model.CheckInDate, model.CheckOutDate, excludeReservationId: id);
        var availableIds = available.Select(x => x.Id).ToHashSet();

        var selectedIds = model.SelectedRoomIds ?? new List<int>();
        var chosen = selectedIds.Where(x => availableIds.Contains(x)).ToList();
        if (chosen.Count != selectedIds.Count)
        {
            ModelState.AddModelError(string.Empty,
                "One or more selected rooms are no longer available. Please re-pick.");
            await PopulateAvailableRoomsAsync(model, id);
            return View(model);
        }

        // Update guest
        r.Guest.FullName = model.GuestFullName;
        r.Guest.Phone = model.GuestPhone;
        r.Guest.Address = model.GuestAddress;
        r.Guest.IdProofType = model.GuestIdProofType;
        r.Guest.IdProofNumber = model.GuestIdProofNumber;

        // Free previously reserved rooms (back to Available)
        var oldRoomIds = r.ReservationRooms.Select(rr => rr.RoomId).ToList();
        var oldRooms = await _db.Rooms.Where(x => oldRoomIds.Contains(x.Id)).ToListAsync();
        foreach (var oldRoom in oldRooms.Where(x => x.Status == RoomStatus.Reserved))
        {
            oldRoom.Status = RoomStatus.Available;
            oldRoom.UpdatedAt = DateTime.UtcNow;
        }

        // Clear old lines
        _db.ReservationRooms.RemoveRange(r.ReservationRooms);

        // Add new lines
        var newRooms = await _db.Rooms.Where(x => chosen.Contains(x.Id)).ToListAsync();
        var nights = (model.CheckOutDate.Date - model.CheckInDate.Date).Days;
        var total = newRooms.Sum(x => x.Rate) * nights;

        r.CheckInDate = model.CheckInDate.Date;
        r.CheckOutDate = model.CheckOutDate.Date;
        r.Adults = model.Adults;
        r.Children = model.Children;
        r.Source = model.Source;
        r.Status = model.Status;
        r.DepositAmount = model.DepositAmount;
        r.Notes = model.Notes;
        r.GroupName = model.GroupName;
        r.TotalAmount = total;
        r.UpdatedAt = DateTime.UtcNow;

        foreach (var room in newRooms)
        {
            r.ReservationRooms.Add(new ReservationRoom
            {
                ReservationId = r.Id,
                RoomId = room.Id,
                NightlyRate = room.Rate
            });
            room.Status = RoomStatus.Reserved;
            room.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await LogActivity("EditReservation", $"Updated booking {r.BookingReference}");

        TempData["Success"] = "Reservation updated.";
        return RedirectToAction(nameof(Details), new { id = r.Id });
    }

    // GET: /Reservation/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var r = await _db.Reservations
            .Include(x => x.Guest)
            .Include(x => x.ReservationRooms).ThenInclude(rr => rr.Room)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r == null) return NotFound();

        var vm = new ReservationDetailsViewModel
        {
            Id = r.Id,
            BookingReference = r.BookingReference,
            GuestName = r.Guest.FullName,
            GuestEmail = r.Guest.Email,
            GuestPhone = r.Guest.Phone,
            GuestAddress = r.Guest.Address,
            GuestIdProofType = r.Guest.IdProofType,
            GuestIdProofNumber = r.Guest.IdProofNumber,
            CheckInDate = r.CheckInDate,
            CheckOutDate = r.CheckOutDate,
            Nights = r.Nights,
            Adults = r.Adults,
            Children = r.Children,
            Source = r.Source,
            Status = r.Status,
            TotalAmount = r.TotalAmount,
            DepositAmount = r.DepositAmount,
            GroupName = r.GroupName,
            Notes = r.Notes,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            Rooms = r.ReservationRooms.Select(rr => new ReservationRoomLine
            {
                RoomId = rr.RoomId,
                RoomNumber = rr.Room.RoomNumber,
                Type = rr.Room.Type.ToString(),
                NightlyRate = rr.NightlyRate,
                LineTotal = rr.NightlyRate * r.Nights
            }).ToList()
        };

        return View(vm);
    }

    // POST: /Reservation/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,FrontDesk")]
    public async Task<IActionResult> Cancel(int id)
    {
        var r = await _db.Reservations
            .Include(x => x.ReservationRooms)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();

        if (r.Status == ReservationStatus.CheckedIn || r.Status == ReservationStatus.CheckedOut)
        {
            TempData["Error"] = "Cannot cancel an in-house or completed reservation.";
            return RedirectToAction(nameof(Details), new { id });
        }

        r.Status = ReservationStatus.Cancelled;
        r.UpdatedAt = DateTime.UtcNow;

        // Free rooms that were Reserved
        var roomIds = r.ReservationRooms.Select(rr => rr.RoomId).ToList();
        var rooms = await _db.Rooms.Where(x => roomIds.Contains(x.Id)).ToListAsync();
        foreach (var room in rooms.Where(x => x.Status == RoomStatus.Reserved))
        {
            room.Status = RoomStatus.Available;
            room.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await LogActivity("CancelReservation", $"Cancelled {r.BookingReference}");

        TempData["Success"] = "Reservation cancelled.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: /Reservation/AvailableRooms  (AJAX helper used by Create/Edit form)
    [HttpGet]
    public async Task<IActionResult> AvailableRooms(DateTime checkIn, DateTime checkOut, int? excludeId = null)
    {
        if (checkOut <= checkIn)
            return Json(new { rooms = new List<object>() });

        var rooms = await _reservationService.GetAvailableRoomsAsync(checkIn, checkOut, excludeId);
        return Json(new
        {
            rooms = rooms.Select(r => new
            {
                id = r.Id,
                roomNumber = r.RoomNumber,
                type = r.Type.ToString(),
                rate = r.Rate,
                floor = r.Floor
            })
        });
    }

    // ----- helpers -----

    private void ValidateDates(ReservationFormViewModel model)
    {
        if (model.CheckOutDate.Date <= model.CheckInDate.Date)
            ModelState.AddModelError(nameof(model.CheckOutDate),
                "Check-out date must be after check-in date.");

        if (model.CheckInDate.Date < DateTime.Today && model.Id == 0)
            ModelState.AddModelError(nameof(model.CheckInDate),
                "Check-in date cannot be in the past for new bookings.");
    }

    private async Task PopulateAvailableRoomsAsync(ReservationFormViewModel model, int? excludeReservationId = null)
    {
        var rooms = await _reservationService.GetAvailableRoomsAsync(
            model.CheckInDate, model.CheckOutDate, excludeReservationId);

        model.AvailableRooms = rooms.Select(r => new AvailableRoomOption
        {
            RoomId = r.Id,
            RoomNumber = r.RoomNumber,
            Type = r.Type.ToString(),
            Rate = r.Rate,
            Floor = r.Floor,
            IsSelected = model.SelectedRoomIds.Contains(r.Id)
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