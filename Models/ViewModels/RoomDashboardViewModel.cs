using HotelManagement.Models.Enums;

namespace HotelManagement.Models.ViewModels;

public class RoomDashboardViewModel
{
    public int TotalRooms { get; set; }
    public Dictionary<RoomStatus, int> StatusCounts { get; set; } = new();
    public Dictionary<RoomType, int> TypeCounts { get; set; } = new();
    public List<RoomListViewModel> Rooms { get; set; } = new();
}