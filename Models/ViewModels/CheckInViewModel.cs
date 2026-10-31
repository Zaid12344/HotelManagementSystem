using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models.ViewModels;

public class CheckInViewModel
{
    public int ReservationId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string? GuestIdProofType { get; set; }
    public string? GuestIdProofNumber { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }

    public List<CheckInRoomLine> Rooms { get; set; } = new();

    // Staff confirms at counter
    [Display(Name = "ID Verified at Counter")]
    public bool IdVerified { get; set; } = true;

    [StringLength(500), Display(Name = "Check-In Notes")]
    public string? Notes { get; set; }
}

public class CheckInRoomLine
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal NightlyRate { get; set; }
}