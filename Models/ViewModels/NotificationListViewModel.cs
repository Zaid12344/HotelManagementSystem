using HotelManagement.Models.Enums;

namespace HotelManagement.Models.ViewModels;

public class NotificationListViewModel
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int? ReservationId { get; set; }
}