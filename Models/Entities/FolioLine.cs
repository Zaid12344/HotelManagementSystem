using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelManagement.Models.Enums;

namespace HotelManagement.Models.Entities;

public class FolioLine
{
    public int Id { get; set; }

    public int FolioId { get; set; }
    public Folio Folio { get; set; } = null!;

    [Required]
    public FolioLineType LineType { get; set; }

    [Required, StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }       // positive for charges, negative for discounts

    public DateTime PostedAt { get; set; } = DateTime.UtcNow;

    // Optional link to a room (for room-charge lines)
    public int? RoomId { get; set; }
    public Room? Room { get; set; }
}