namespace HotelManagement.Models.ViewModels;

public class CheckOutViewModel
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public int FolioId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string FolioNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Nights { get; set; }

    public decimal TotalCharges { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }

    public List<FolioLineViewModel> Lines { get; set; } = new();
}

public class FolioLineViewModel
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PostedAt { get; set; }
    public string LineType { get; set; } = string.Empty;
}