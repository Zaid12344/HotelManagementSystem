using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelManagement.Models.Enums;

namespace HotelManagement.Models.Entities;

public class Folio
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Folio Number")]
    public string FolioNumber { get; set; } = string.Empty;   // FOL-20260913-0001

    public int ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public int GuestId { get; set; }
    public Guest Guest { get; set; } = null!;

    [Required]
    public FolioStatus Status { get; set; } = FolioStatus.Open;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalCharges { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPaid { get; set; }

    [NotMapped]
    public decimal Balance => TotalCharges - TotalPaid;

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    [StringLength(200)]
    public string? Notes { get; set; }

    public ICollection<FolioLine> Lines { get; set; } = new List<FolioLine>();
}