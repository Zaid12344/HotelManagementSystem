using System.ComponentModel.DataAnnotations;
using HotelManagement.Models.Enums;

namespace HotelManagement.Models.ViewModels;

public class RoomFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(10), Display(Name = "Room Number")]
    public string RoomNumber { get; set; } = string.Empty;

    [Required, Display(Name = "Room Type")]
    public RoomType Type { get; set; }

    [Required, Range(0, 100000), Display(Name = "Rate per Night")]
    public decimal Rate { get; set; }

    [Display(Name = "Floor")]
    [Range(1, 50)]
    public int Floor { get; set; } = 1;

    [Display(Name = "Amenities (comma-separated)")]
    [StringLength(500)]
    public string? Amenities { get; set; }

    [Required, Display(Name = "Status")]
    public RoomStatus Status { get; set; } = RoomStatus.Available;
}