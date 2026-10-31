using System.ComponentModel.DataAnnotations;
using HotelManagement.Models.Enums;

namespace HotelManagement.Models.Entities;

public class HousekeepingTask
{
    public int Id { get; set; }

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    [Required]
    public HousekeepingTaskStatus Status { get; set; } = HousekeepingTaskStatus.Pending;

    [StringLength(200)]
    public string? Notes { get; set; }

    [StringLength(100)]
    [Display(Name = "Task Type")]
    public string TaskType { get; set; } = "Checkout Clean";   // Checkout Clean / Daily Clean / Deep Clean

    // Assigned housekeeper
    public string? AssignedToUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? CreatedByUserId { get; set; }
}