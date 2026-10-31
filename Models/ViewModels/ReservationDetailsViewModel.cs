using HotelManagement.Models.Enums;

namespace HotelManagement.Models.ViewModels;

public class ReservationDetailsViewModel
{
    public int Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string? GuestAddress { get; set; }
    public string? GuestIdProofType { get; set; }
    public string? GuestIdProofNumber { get; set; }

    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Nights { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }

    public BookingSource Source { get; set; }
    public ReservationStatus Status { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal BalanceDue => TotalAmount - DepositAmount;

    public string? GroupName { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<ReservationRoomLine> Rooms { get; set; } = new();
}

public class ReservationRoomLine
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal NightlyRate { get; set; }
    public decimal LineTotal { get; set; }
}