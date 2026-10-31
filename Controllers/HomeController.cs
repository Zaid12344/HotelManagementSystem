using HotelManagement.Models.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Models.Enums;
using HotelManagement.Models.Identity;
using HotelManagement.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace HotelManagement.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(nameof(Landing));
        }

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        // ----- Room counts -----
        var rooms = await _db.Rooms.ToListAsync();
        var totalRooms = rooms.Count;
        var occupied = rooms.Count(r => r.Status == RoomStatus.Occupied);
        var available = rooms.Count(r => r.Status == RoomStatus.Available);
        var cleaning = rooms.Count(r => r.Status == RoomStatus.Cleaning);
        var maintenance = rooms.Count(r => r.Status == RoomStatus.Maintenance);
        var reserved = rooms.Count(r => r.Status == RoomStatus.Reserved);

        var sellable = totalRooms - maintenance;
        var occupancyPercent = sellable > 0
            ? Math.Round((decimal)occupied / sellable * 100m, 1)
            : 0m;

        // ----- Reservations -----
        var arrivalsToday = await _db.Reservations
            .Include(r => r.Guest).Include(r => r.ReservationRooms).ThenInclude(rr => rr.Room)
            .Where(r => r.CheckInDate.Date == today
                     && (r.Status == ReservationStatus.Confirmed
                         || r.Status == ReservationStatus.Pending))
            .ToListAsync();

        var departuresToday = await _db.Reservations
            .Include(r => r.Guest).Include(r => r.ReservationRooms).ThenInclude(rr => rr.Room)
            .Where(r => r.Status == ReservationStatus.CheckedIn
                     && r.CheckOutDate.Date == today)
            .ToListAsync();

        var inHouse = await _db.Reservations
            .CountAsync(r => r.Status == ReservationStatus.CheckedIn);

        var upcoming = await _db.Reservations
            .CountAsync(r => r.CheckInDate.Date > today
                          && r.Status != ReservationStatus.Cancelled
                          && r.Status != ReservationStatus.CheckedOut);

        // ----- Operations -----
        var pendingTasks = await _db.HousekeepingTasks
            .CountAsync(t => t.Status != HousekeepingTaskStatus.Completed);

        var openMaintenance = await _db.MaintenanceRequests
            .CountAsync(m => m.Status != MaintenanceStatus.Resolved);

        // ----- Money -----
        var revenueToday = await _db.Payments
            .Where(p => !p.IsRefund && p.PaidAt.Date == today)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        // ----- Trend data -----
        var yesterday = today.AddDays(-1);

        var revenueYesterday = await _db.Payments
            .Where(p => !p.IsRefund && p.PaidAt.Date == yesterday)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var arrivalsYesterday = await _db.Reservations
            .CountAsync(r => r.CheckInDate.Date == yesterday
                          && (r.Status == ReservationStatus.Confirmed
                              || r.Status == ReservationStatus.Pending));

        var departuresYesterday = await _db.Reservations
            .CountAsync(r => r.Status == ReservationStatus.CheckedIn
                          && r.CheckOutDate.Date == yesterday);

        // Compute percentage changes (with safe divide)
        decimal revenueChange = revenueYesterday > 0
            ? Math.Round((revenueToday - revenueYesterday) / revenueYesterday * 100m, 1)
            : (revenueToday > 0 ? 100m : 0m);

        decimal occupancyChange = 0m;   // occupancy is a live snapshot, so no "yesterday" value; skip for now
        var openFolios = await _db.Folios
            .Include(f => f.Lines)
            .Where(f => f.Status == FolioStatus.Open)
            .ToListAsync();

        decimal outstanding = 0m;
        foreach (var f in openFolios)
        {
            var charges = f.Lines
                .Where(l => l.LineType != FolioLineType.Adjustment)
                .Sum(l => l.Amount);
            outstanding += Math.Max(0, charges - f.TotalPaid);
        }

        // ----- Recent activity (last 8) -----
        var recentLogs = await _db.UserActivityLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(8)
            .ToListAsync();

        // ----- Lists for dashboard -----
        var arrivals = arrivalsToday.Select(r => new DashboardListItem
        {
            Primary = r.Guest.FullName,
            Secondary = r.BookingReference,
            Tertiary = string.Join(", ", r.ReservationRooms.Select(rr => rr.Room.RoomNumber)),
            Badge = "Arrival",
            BadgeColor = "bg-primary",
            LinkUrl = $"/Reservation/Details/{r.Id}"
        }).ToList();

        var departures = departuresToday.Select(r => new DashboardListItem
        {
            Primary = r.Guest.FullName,
            Secondary = r.BookingReference,
            Tertiary = string.Join(", ", r.ReservationRooms.Select(rr => rr.Room.RoomNumber)),
            Badge = "Departure",
            BadgeColor = "bg-warning text-dark",
            LinkUrl = $"/Reservation/Details/{r.Id}"
        }).ToList();

        var activity = recentLogs.Select(l => new DashboardListItem
        {
            Primary = l.Activity,
            Secondary = l.Details ?? "",
            Tertiary = l.Timestamp.ToLocalTime().ToString("dd MMM HH:mm"),
            Badge = "Log",
            BadgeColor = "bg-secondary"
        }).ToList();

        var vm = new HomeDashboardViewModel
        {
            TotalRooms = totalRooms,
            AvailableRooms = available,
            OccupiedRooms = occupied,
            CleaningRooms = cleaning,
            MaintenanceRooms = maintenance,
            ReservedRooms = reserved,
            OccupancyPercent = occupancyPercent,

            TodaysArrivals = arrivalsToday.Count,
            TodaysDepartures = departuresToday.Count,
            InHouseGuests = inHouse,
            UpcomingReservations = upcoming,

            PendingHousekeepingTasks = pendingTasks,
            OpenMaintenanceRequests = openMaintenance,

            RevenueToday = revenueToday,
            OutstandingBalance = outstanding,

            ArrivalsList = arrivals,
            DeparturesList = departures,
            RecentActivity = activity,

            RevenueYesterday = revenueYesterday,
            RevenueChangePercent = revenueChange,
            ArrivalsYesterday = arrivalsYesterday,
            DeparturesYesterday = departuresYesterday,
            OccupancyChangePercent = occupancyChange,
        };

        return View(vm);
    }

    [AllowAnonymous]
    public IActionResult Landing()
    {
        // If already logged in, skip the landing page
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Index));
        }
        return View();
    }


    [AllowAnonymous]
    public IActionResult About() => View();
    
    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    public IActionResult Error() => View();
}