using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models.ViewModels;

public class SplitBillingViewModel
{
    public int FolioId { get; set; }
    public string FolioNumber { get; set; } = string.Empty;
    public decimal TotalCharges { get; set; }
    public decimal AlreadyPaid { get; set; }
    public decimal Balance { get; set; }

    [Required, Range(2, 20), Display(Name = "Split into (number of parts)")]
    public int Parts { get; set; } = 2;

    // Computed preview
    public decimal PerPart => Parts > 0 ? Math.Round(Balance / Parts, 2) : 0;
}