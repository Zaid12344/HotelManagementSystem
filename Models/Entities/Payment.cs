using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelManagement.Models.Enums;

namespace HotelManagement.Models.Entities;

public class Payment
{
    public int Id { get; set; }

    public int FolioId { get; set; }
    public Folio Folio { get; set; } = null!;

    [Required, StringLength(20)]
    [Display(Name = "Receipt Number")]
    public string ReceiptNumber { get; set; } = string.Empty;   // RCP-20260913-0001

    [Required]
    public PaymentMethod Method { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    [StringLength(100)]
    [Display(Name = "Transaction Reference")]
    public string? TransactionReference { get; set; }   // e.g. UPI txn ID, card auth code

    [StringLength(200)]
    public string? Notes { get; set; }

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    public string? RecordedByUserId { get; set; }

    // Refund tracking
    public bool IsRefund { get; set; }
    public int? RefundOfPaymentId { get; set; }
    public Payment? RefundOfPayment { get; set; }
}