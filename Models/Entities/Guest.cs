using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models.Entities;

public class Guest
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(50)]
    [Display(Name = "ID Proof Type")]
    public string? IdProofType { get; set; }        // Passport, Aadhaar, Driver License

    [StringLength(50)]
    [Display(Name = "ID Proof Number")]
    public string? IdProofNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}