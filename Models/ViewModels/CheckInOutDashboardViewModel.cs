namespace HotelManagement.Models.ViewModels;

public class CheckInOutDashboardViewModel
{
    public List<ReservationListViewModel> TodaysArrivals { get; set; } = new();
    public List<ReservationListViewModel> TodaysDepartures { get; set; } = new();
    public List<ReservationListViewModel> InHouse { get; set; } = new();

    public int ArrivalsCount => TodaysArrivals.Count;
    public int DeparturesCount => TodaysDepartures.Count;
    public int InHouseCount => InHouse.Count;
}