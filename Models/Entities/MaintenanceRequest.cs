using System.ComponentModel.DataAnnotations;
using HotelManagement.Models.Enums;

namespace HotelManagement.Models.Entities;

public class MaintenanceRequest
{
    public int Id { get; set; }

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    [Required, StringLength(200)]
    [Display(Name = "Issue Title")]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;

    [Required]
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;

    [StringLength(500)]
    [Display(Name = "Resolution Notes")]
    public string? ResolutionNotes { get; set; }

    public string? ReportedByUserId { get; set; }
    public string? AssignedToUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}