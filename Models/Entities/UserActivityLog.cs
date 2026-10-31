namespace HotelManagement.Models.Entities;

public class UserActivityLog
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}