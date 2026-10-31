using HotelManagement.Models.Enums;

namespace HotelManagement.Models.ViewModels;

public class HousekeepingDashboardViewModel
{
    public List<HousekeepingTaskLine> MyTasks { get; set; } = new();
    public List<HousekeepingTaskLine> AllPending { get; set; } = new();
    public List<HousekeepingTaskLine> CompletedToday { get; set; } = new();
}

public class HousekeepingTaskLine
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public HousekeepingTaskStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}