using HotelManagement.Models.Enums;

namespace HotelManagement.Models.ViewModels;

public class RoomListViewModel
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public decimal Rate { get; set; }
    public RoomStatus Status { get; set; }
    public int Floor { get; set; }
    public string? Amenities { get; set; }
}