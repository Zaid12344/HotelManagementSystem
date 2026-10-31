using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelManagement.Models.Enums;

namespace HotelManagement.Models.Entities;

public class Reservation
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Booking Reference")]
    public string BookingReference { get; set; } = string.Empty;   // e.g. "BK-20260913-0001"

    public int GuestId { get; set; }
    public Guest Guest { get; set; } = null!;

    [Required]
    [Display(Name = "Check-In")]
    [DataType(DataType.Date)]
    public DateTime CheckInDate { get; set; }

    [Required]
    [Display(Name = "Check-Out")]
    [DataType(DataType.Date)]
    public DateTime CheckOutDate { get; set; }

    [Required]
    [Display(Name = "Adults")]
    [Range(1, 20)]
    public int Adults { get; set; } = 1;

    [Display(Name = "Children")]
    [Range(0, 20)]
    public int Children { get; set; } = 0;

    [Required]
    public BookingSource Source { get; set; } = BookingSource.WalkIn;

    [Required]
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    [Column(TypeName = "decimal(10,2)")]
    [Display(Name = "Total Amount")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Display(Name = "Advance Deposit")]
    public decimal DepositAmount { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public string? CreatedByUserId { get; set; }    // who booked it

    // Group booking (optional)
    [Display(Name = "Group Name")]
    [StringLength(100)]
    public string? GroupName { get; set; }

    // Navigation
    public ICollection<ReservationRoom> ReservationRooms { get; set; } = new List<ReservationRoom>();

    [NotMapped]
    public int Nights => (CheckOutDate.Date - CheckInDate.Date).Days;
}