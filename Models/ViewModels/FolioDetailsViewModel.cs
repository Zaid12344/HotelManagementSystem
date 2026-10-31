namespace HotelManagement.Models.ViewModels;

public class FolioDetailsViewModel
{
    public int Id { get; set; }
    public string FolioNumber { get; set; } = string.Empty;
    public string BookingReference { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public decimal TotalCharges { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }

    public List<FolioLineViewModel> Lines { get; set; } = new();
}