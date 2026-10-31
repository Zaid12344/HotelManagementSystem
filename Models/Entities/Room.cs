using System.ComponentModel.DataAnnotations;
using HotelManagement.Models.Enums;

namespace HotelManagement.Models.Entities;

public class Room
{
    public int Id { get; set; }

    [Required, StringLength(10)]
    [Display(Name = "Room Number")]
    public string RoomNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Room Type")]
    public RoomType Type { get; set; }

    [Required]
    [Range(0, 100000, ErrorMessage = "Rate must be between 0 and 100000.")]
    [Display(Name = "Rate per Night")]
    public decimal Rate { get; set; }

    [Display(Name = "Amenities")]
    [StringLength(500)]
    public string? Amenities { get; set; }   // comma-separated: "WiFi, AC, TV"

    [Required]
    [Display(Name = "Status")]
    public RoomStatus Status { get; set; } = RoomStatus.Available;

    [Display(Name = "Floor")]
    public int Floor { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}