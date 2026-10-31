using System.ComponentModel.DataAnnotations;
using HotelManagement.Models.Enums;

namespace HotelManagement.Models.Entities;

public class Notification
{
    public int Id { get; set; }

    [Required]
    public NotificationType Type { get; set; }

    [Required]
    public NotificationChannel Channel { get; set; }

    [Required]
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    [Required, StringLength(200)]
    public string Recipient { get; set; } = string.Empty;

    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    public int? ReservationId { get; set; }
    public int? FolioId { get; set; }
    public int? PaymentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}