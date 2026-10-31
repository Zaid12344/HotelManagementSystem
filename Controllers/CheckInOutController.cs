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
public class CheckInOutController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly CheckInOutService _service;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CheckInOutController> _logger;
    private readonly NotificationService _notifications;

    public CheckInOutController(
        ApplicationDbContext db,
        CheckInOutService service,
        UserManager<ApplicationUser> userManager,
        ILogger<CheckInOutController> logger,
        NotificationService notifications)
    {
        _db = db;
        _service = service;
        _userManager = userManager;
        _logger = logger;
        _notifications = notifications;
    }

    // GET: /CheckInOut  — dashboard
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;

        var arrivals = await _db.Reservations
            .Include(r => r.Guest).Include(r => r.ReservationRooms).ThenInclude(rr => rr.Room)
            .Where(r => r.CheckInDate.Date == today
                     && (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Pending))
            .ToListAsync();

        var departures = await _db.Reservations
            .Include(r => r.Guest).Include(r => r.ReservationRooms).ThenInclude(rr => rr.Room)
            .Where(r => r.Status == ReservationStatus.CheckedIn && r.CheckOutDate.Date == today)
            .ToListAsync();

        var inHouse = await _db.Reservations
            .Include(r => r.Guest).Include(r => r.ReservationRooms).ThenInclude(rr => rr.Room)
            .Where(r => r.Status == ReservationStatus.CheckedIn)
            .OrderBy(r => r.CheckOutDate)
            .ToListAsync();

        var vm = new CheckInOutDashboardViewModel
        {
            TodaysArrivals = arrivals.Select(ToListItem).ToList(),
            TodaysDepartures = departures.Select(ToListItem).ToList(),
            InHouse = inHouse.Select(ToListItem).ToList()
        };
        return View(vm);
    }

    // GET: /CheckInOut/CheckIn/5
    [Authorize(Roles = "Admin,Manager,FrontDesk")]
    public async Task<IActionResult> CheckIn(int id)
    {
        var r = await _db.Reservations
            .Include(x => x.Guest)
            .Include(x => x.ReservationRooms).ThenInclude(rr => rr.Room)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();

        if (r.Status != ReservationStatus.Confirmed && r.Status != ReservationStatus.Pending)
        {
            TempData["Error"] = $"Cannot check in — current status is {r.Status}.";
            return RedirectToAction("Details", "Reservation", new { id });
        }

        var vm = new CheckInViewModel
        {
            ReservationId = r.Id,
            BookingReference = r.BookingReference,
            GuestName = r.Guest.FullName,
            GuestEmail = r.Guest.Email,
            GuestPhone = r.Guest.Phone,
            GuestIdProofType = r.Guest.IdProofType,
            GuestIdProofNumber = r.Guest.IdProofNumber,
            CheckInDate = r.CheckInDate,
            CheckOutDate = r.CheckOutDate,
            Rooms = r.ReservationRooms.Select(rr => new CheckInRoomLine
            {
                RoomId = rr.RoomId,
                RoomNumber = rr.Room.RoomNumber,
                Type = rr.Room.Type.ToString(),
                NightlyRate = rr.NightlyRate
            }).ToList()
        };
        return View(vm);
    }

    // POST: /CheckInOut/CheckIn/5
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,FrontDesk")]
    public async Task<IActionResult> CheckIn(CheckInViewModel model)
    {
        if (!model.IdVerified)
        {
            ModelState.AddModelError(nameof(model.IdVerified),
                "You must confirm the guest's ID has been verified.");
            return View(model);
        }

        try
        {
            var folio = await _service.CheckInAsync(model.ReservationId, model.Notes);

            await LogActivity("CheckIn",
                $"Reservation {model.BookingReference} → Folio {folio.FolioNumber}");

            _logger.LogInformation(
                "[CHECK-IN] Reservation {Ref} checked in, folio {Folio}",
                model.BookingReference, folio.FolioNumber);

            TempData["Success"] = $"Checked in. Folio {folio.FolioNumber} created.";
            return RedirectToAction(nameof(Folio), new { id = folio.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    // GET: /CheckInOut/CheckOut/5  — pre-check-out review
    [Authorize(Roles = "Admin,Manager,FrontDesk")]
    public async Task<IActionResult> CheckOut(int id)
    {
        var r = await _db.Reservations
            .Include(x => x.Guest).Include(x => x.ReservationRooms).ThenInclude(rr => rr.Room)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();

        if (r.Status != ReservationStatus.CheckedIn)
        {
            TempData["Error"] = "Guest is not currently checked in.";
            return RedirectToAction("Details", "Reservation", new { id });
        }

        var folio = await _db.Folios
            .Include(f => f.Lines)
            .FirstOrDefaultAsync(f => f.ReservationId == id && f.Status == FolioStatus.Open);
        if (folio == null)
        {
            TempData["Error"] = "No open folio found.";
            return RedirectToAction("Details", "Reservation", new { id });
        }

        var vm = new CheckOutViewModel
        {
            Id = r.Id,                          
            ReservationId = r.Id,
            FolioId = folio.Id,
            BookingReference = r.BookingReference,
            FolioNumber = folio.FolioNumber,
            GuestName = r.Guest.FullName,
            CheckInDate = r.CheckInDate,
            CheckOutDate = r.CheckOutDate,
            Nights = r.Nights,
            TotalCharges = folio.Lines.Where(l => l.LineType != FolioLineType.Adjustment).Sum(l => l.Amount),
            TotalPaid = folio.TotalPaid,
            Lines = folio.Lines.OrderBy(l => l.PostedAt).Select(l => new FolioLineViewModel
            {
                Description = l.Description,
                Amount = l.Amount,
                PostedAt = l.PostedAt,
                LineType = l.LineType.ToString()
            }).ToList()
        };
        vm.Balance = vm.TotalCharges - vm.TotalPaid;

        return View(vm);
    }

    // POST: /CheckInOut/CheckOut/5  — finalize
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,FrontDesk")]
    public async Task<IActionResult> CheckOutConfirmed(int reservationId, bool allowUnpaid = false, string? overrideReason = null)
    {
        try
        {
            var folio = await _service.CheckOutAsync(reservationId, allowUnpaid , overrideReason);

            await LogActivity("CheckOut", $"Folio {folio.FolioNumber} closed");

            var res = await _db.Reservations
                    .Include(r => r.Guest)
                    .Include(r => r.ReservationRooms).ThenInclude(rr => rr.Room)
                    .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (res != null)
            {
                var roomNums = res.ReservationRooms.Select(rr => rr.Room.RoomNumber);
                await _notifications.SendCheckoutHousekeepingAlertAsync(res, roomNums);
            }

            _logger.LogInformation(
                "[CHECK-OUT] Reservation {Id} checked out, folio {Folio} closed",
                reservationId, folio.FolioNumber);

            TempData["Success"] = $"Checked out. Folio {folio.FolioNumber} closed. Rooms marked for cleaning.";
            return RedirectToAction("Details", "Reservation", new { id = reservationId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: /CheckInOut/Folio/5
    public async Task<IActionResult> Folio(int id)
    {
        var folio = await _db.Folios
            .Include(f => f.Guest)
            .Include(f => f.Reservation)
            .Include(f => f.Lines)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (folio == null) return NotFound();

        var vm = new FolioDetailsViewModel
        {
            Id = folio.Id,
            FolioNumber = folio.FolioNumber,
            BookingReference = folio.Reservation.BookingReference,
            GuestName = folio.Guest.FullName,
            GuestEmail = folio.Guest.Email,
            GuestPhone = folio.Guest.Phone,
            Status = folio.Status.ToString(),
            OpenedAt = folio.OpenedAt,
            ClosedAt = folio.ClosedAt,
            TotalCharges = folio.Lines
                .Where(l => l.LineType != FolioLineType.Adjustment).Sum(l => l.Amount),
            TotalPaid = folio.TotalPaid,
            Lines = folio.Lines.OrderBy(l => l.PostedAt).Select(l => new FolioLineViewModel
            {
                Description = l.Description,
                Amount = l.Amount,
                PostedAt = l.PostedAt,
                LineType = l.LineType.ToString()
            }).ToList()
        };
        vm.Balance = vm.TotalCharges - vm.TotalPaid;

        return View(vm);
    }

    // GET: /CheckInOut/ExpressCheckIn/5  — one-click for simple cases
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,FrontDesk")]
    public async Task<IActionResult> ExpressCheckIn(int reservationId)
    {
        try
        {
            var folio = await _service.CheckInAsync(reservationId, "Express check-in (ID verified at counter)");
            await LogActivity("CheckIn", $"Express check-in for reservation {reservationId}");
            TempData["Success"] = $"Express check-in complete. Folio {folio.FolioNumber} created.";
            return RedirectToAction(nameof(Folio), new { id = folio.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // ----- helpers -----

    private static ReservationListViewModel ToListItem(Reservation r) => new()
    {
        Id = r.Id,
        BookingReference = r.BookingReference,
        GuestName = r.Guest.FullName,
        GuestPhone = r.Guest.Phone,
        CheckInDate = r.CheckInDate,
        CheckOutDate = r.CheckOutDate,
        Nights = r.Nights,
        Source = r.Source,
        Status = r.Status,
        TotalAmount = r.TotalAmount,
        DepositAmount = r.DepositAmount,
        RoomsText = string.Join(", ", r.ReservationRooms.Select(rr => rr.Room.RoomNumber))
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