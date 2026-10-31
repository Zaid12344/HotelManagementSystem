using HotelManagement.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models.ViewModels;

public class MaintenanceListViewModel
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; }
    public string? ReportedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class MaintenanceFormViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Room")]
    public int RoomId { get; set; }

    [Required, StringLength(200), Display(Name = "Issue Title")]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1000), Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required, Display(Name = "Priority")]
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Rooms { get; set; } = new();
}