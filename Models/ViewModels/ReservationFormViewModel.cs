using System.ComponentModel.DataAnnotations;
using HotelManagement.Models.Enums;

namespace HotelManagement.Models.ViewModels;

public class ReservationFormViewModel
{
    public int Id { get; set; }

    // ----- Guest info -----
    [Required, StringLength(100), Display(Name = "Guest Full Name")]
    public string GuestFullName { get; set; } = string.Empty;

    [Required, EmailAddress, Display(Name = "Guest Email")]
    public string GuestEmail { get; set; } = string.Empty;

    [Required, Phone, Display(Name = "Guest Phone")]
    public string GuestPhone { get; set; } = string.Empty;

    [StringLength(300), Display(Name = "Address")]
    public string? GuestAddress { get; set; }

    [StringLength(50), Display(Name = "ID Proof Type")]
    public string? GuestIdProofType { get; set; }

    [StringLength(50), Display(Name = "ID Proof Number")]
    public string? GuestIdProofNumber { get; set; }

    // ----- Reservation info -----
    [Required, DataType(DataType.Date), Display(Name = "Check-In Date")]
    public DateTime CheckInDate { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date), Display(Name = "Check-Out Date")]
    public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(1);

    [Required, Range(1, 20), Display(Name = "Adults")]
    public int Adults { get; set; } = 1;

    [Range(0, 20), Display(Name = "Children")]
    public int Children { get; set; } = 0;

    [Required, Display(Name = "Booking Source")]
    public BookingSource Source { get; set; } = BookingSource.WalkIn;

    [Required, Display(Name = "Status")]
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    [Display(Name = "Advance Deposit")]
    [Range(0, 1000000)]
    public decimal DepositAmount { get; set; }

    [StringLength(100), Display(Name = "Group Name (optional)")]
    public string? GroupName { get; set; }

    [StringLength(500), Display(Name = "Notes")]
    public string? Notes { get; set; }

    // ----- Room selection -----
    [Required(ErrorMessage = "Select at least one room.")]
    [Display(Name = "Rooms")]
    public List<int> SelectedRoomIds { get; set; } = new();

    // Populated by controller for the view (available rooms only)
    public List<AvailableRoomOption> AvailableRooms { get; set; } = new();
}

public class AvailableRoomOption
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public int Floor { get; set; }
    public bool IsSelected { get; set; }
}