using HotelManagement.Models.Enums;

namespace HotelManagement.Models.ViewModels;

public class ServiceRequestsListViewModel
{
    public int Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public ServiceRequestStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public bool PostedToFolio { get; set; }
    public string? Notes { get; set; }
}