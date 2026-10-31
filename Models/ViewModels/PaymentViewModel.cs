using System.ComponentModel.DataAnnotations;
using HotelManagement.Models.Enums;

namespace HotelManagement.Models.ViewModels;

public class PaymentViewModel
{
    public int FolioId { get; set; }
    public string FolioNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;

    public decimal TotalCharges { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }

    [Required, Range(0.01, 1000000), Display(Name = "Amount")]
    public decimal Amount { get; set; }

    [Required, Display(Name = "Payment Method")]
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    [StringLength(100), Display(Name = "Transaction Reference")]
    public string? TransactionReference { get; set; }

    [StringLength(200), Display(Name = "Notes")]
    public string? Notes { get; set; }

    public List<PaymentListLine> History { get; set; } = new();
}

public class PaymentListLine
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string? TransactionReference { get; set; }
    public bool IsRefund { get; set; }
}