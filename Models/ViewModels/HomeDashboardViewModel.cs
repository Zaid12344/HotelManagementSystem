namespace HotelManagement.Models.ViewModels;

public class HomeDashboardViewModel
{
    // Rooms
    public int TotalRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int CleaningRooms { get; set; }
    public int MaintenanceRooms { get; set; }
    public int ReservedRooms { get; set; }
    public decimal OccupancyPercent { get; set; }

    // Front desk
    public int TodaysArrivals { get; set; }
    public int TodaysDepartures { get; set; }
    public int InHouseGuests { get; set; }
    public int UpcomingReservations { get; set; }

    // Operations
    public int PendingHousekeepingTasks { get; set; }
    public int OpenMaintenanceRequests { get; set; }

    // Money
    public decimal RevenueToday { get; set; }
    public decimal OutstandingBalance { get; set; }

    // Trend data (for KPI cards)
    public decimal RevenueYesterday { get; set; }
    public decimal RevenueChangePercent { get; set; }   // vs yesterday

    public int OccupancyYesterday { get; set; }
    public decimal OccupancyChangePercent { get; set; } // vs yesterday

    public int ArrivalsYesterday { get; set; }
    public int DeparturesYesterday { get; set; }

    // Quick lists
    public List<DashboardListItem> ArrivalsList { get; set; } = new();
    public List<DashboardListItem> DeparturesList { get; set; } = new();
    public List<DashboardListItem> RecentActivity { get; set; } = new();
}

public class DashboardListItem
{
    public string Primary { get; set; } = string.Empty;
    public string Secondary { get; set; } = string.Empty;
    public string? Tertiary { get; set; }
    public string? Badge { get; set; }
    public string? BadgeColor { get; set; }
    public string? LinkUrl { get; set; }
}