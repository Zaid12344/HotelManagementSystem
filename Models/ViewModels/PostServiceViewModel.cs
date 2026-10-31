using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models.ViewModels;

public class PostServiceViewModel
{
    public int ReservationId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public int? RoomId { get; set; }

    [Required, Display(Name = "Service")]
    public int ServiceId { get; set; }

    [Required, Range(1, 50), Display(Name = "Quantity")]
    public int Quantity { get; set; } = 1;

    [StringLength(500), Display(Name = "Notes")]
    public string? Notes { get; set; }

    public List<ServiceListViewModel> AvailableServices { get; set; } = new();
}