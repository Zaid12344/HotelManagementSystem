using System.ComponentModel.DataAnnotations;
using HotelManagement.Models.Enums;

namespace HotelManagement.Models.Entities;

public class ServiceRequest
{
    public int Id { get; set; }

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public int ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public int? RoomId { get; set; }
    public Room? Room { get; set; }

    [Range(1, 50)]
    public int Quantity { get; set; } = 1;

    [Required]
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Requested;

    [StringLength(500)]
    public string? Notes { get; set; }

    // Set when the charge is posted to the folio
    public int? FolioLineId { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FulfilledAt { get; set; }

    public string? RequestedByUserId { get; set; }   // staff-created vs guest portal
}