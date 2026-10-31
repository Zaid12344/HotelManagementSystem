using HotelManagement.Models.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services;

public class BillingService
{
    private readonly ApplicationDbContext _db;

    public BillingService(ApplicationDbContext db) => _db = db;

    public async Task<string> GenerateReceiptNumberAsync()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _db.Payments
            .CountAsync(p => p.ReceiptNumber.StartsWith($"RCP-{today}-"));
        return $"RCP-{today}-{(count + 1):D4}";
    }

    /// <summary>
    /// Records a payment against a folio and updates folio totals.
    /// </summary>
    public async Task<Payment> RecordPaymentAsync(
        int folioId, decimal amount, PaymentMethod method,
        string? transactionReference, string? notes, string? userId)
    {
        var folio = await _db.Folios
            .Include(f => f.Lines)
            .FirstOrDefaultAsync(f => f.Id == folioId)
            ?? throw new InvalidOperationException("Folio not found.");

        if (folio.Status == FolioStatus.Settled)
            throw new InvalidOperationException("Folio is already settled.");

        if (amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero.");

        var charges = folio.Lines
            .Where(l => l.LineType != FolioLineType.Adjustment).Sum(l => l.Amount);
        var balance = charges - folio.TotalPaid;

        if (amount > balance + 0.01m)
            throw new InvalidOperationException(
                $"Amount exceeds outstanding balance of ₹{balance:N2}.");

        var payment = new Payment
        {
            FolioId = folioId,
            ReceiptNumber = await GenerateReceiptNumberAsync(),
            Amount = amount,
            Method = method,
            TransactionReference = transactionReference,
            Notes = notes,
            RecordedByUserId = userId,
            PaidAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);

        folio.TotalPaid += amount;
        if (folio.TotalPaid >= charges - 0.01m && folio.Status == FolioStatus.Open)
        {
            // Fully paid — mark for settlement at check-out
            // (folio stays Open until check-out so we can add more charges)
        }

        await _db.SaveChangesAsync();
        return payment;
    }

    /// <summary>
    /// Checks if a folio's balance is zero (or within 1 cent tolerance).
    /// </summary>
    public async Task<bool> IsFolioFullyPaidAsync(int folioId)
    {
        var folio = await _db.Folios
            .Include(f => f.Lines)
            .FirstOrDefaultAsync(f => f.Id == folioId);
        if (folio == null) return false;

        var charges = folio.Lines
            .Where(l => l.LineType != FolioLineType.Adjustment).Sum(l => l.Amount);
        return folio.TotalPaid >= charges - 0.01m;
    }
}