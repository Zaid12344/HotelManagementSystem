using HotelManagement.Models.Enums;

namespace HotelManagement.Models.ViewModels;

public class ReservationListViewModel
{
    public int Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Nights { get; set; }
    public BookingSource Source { get; set; }
    public ReservationStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public string RoomsText { get; set; } = string.Empty;   // "101, 102"
}