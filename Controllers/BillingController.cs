using HotelManagement.Models.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Models.Enums;
using HotelManagement.Models.Identity;
using HotelManagement.Models.ViewModels;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers;

[Authorize]
public class BillingController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly BillingService _billing;
    private readonly InvoicePdfService _pdf;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<BillingController> _logger;
    private readonly NotificationService _notifications;

    public BillingController(
        ApplicationDbContext db,
        BillingService billing,
        InvoicePdfService pdf,
        UserManager<ApplicationUser> userManager,
        ILogger<BillingController> logger,
        NotificationService notifications)
    {
        _db = db;
        _billing = billing;
        _pdf = pdf;
        _userManager = userManager;
        _logger = logger;
        _notifications = notifications;
    }

    // GET: /Billing/Pay/5  (folio id)
    [Authorize(Roles = "Admin,Manager,FrontDesk,Accountant")]
    public async Task<IActionResult> Pay(int id)
    {
        var folio = await _db.Folios
            .Include(f => f.Guest).Include(f => f.Lines)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (folio == null) return NotFound();

        return View(await BuildPaymentVmAsync(folio));
    }

    // POST: /Billing/Pay
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,FrontDesk,Accountant")]
    public async Task<IActionResult> Pay(PaymentViewModel model)
    {
        var folio = await _db.Folios
            .Include(f => f.Guest).Include(f => f.Lines)
            .FirstOrDefaultAsync(f => f.Id == model.FolioId);
        if (folio == null) return NotFound();

        if (!ModelState.IsValid)
            return View(await BuildPaymentVmAsync(folio, model));

        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var payment = await _billing.RecordPaymentAsync(
                model.FolioId, model.Amount, model.Method,
                model.TransactionReference, model.Notes, currentUser?.Id);

            var guest = await _db.Guests.FirstOrDefaultAsync(g => g.Id == folio.GuestId);
            if (guest != null)
                await _notifications.SendPaymentReceiptAsync(payment, guest.Email);

            await LogActivity("RecordPayment",
                $"Folio {folio.FolioNumber}: {payment.ReceiptNumber} ₹{payment.Amount:N2} via {payment.Method}");

            TempData["Success"] = $"Payment {payment.ReceiptNumber} recorded.";
            return RedirectToAction(nameof(Receipt), new { id = payment.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(await BuildPaymentVmAsync(folio, model));
        }
    }

    // GET: /Billing/Receipt/5  (payment id)
    public async Task<IActionResult> Receipt(int id)
    {
        var payment = await _db.Payments
            .Include(p => p.Folio).ThenInclude(f => f.Guest)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (payment == null) return NotFound();
        return View(payment);
    }

    // GET: /Billing/InvoicePdf/5  (folio id)
    public async Task<IActionResult> InvoicePdf(int id)
    {
        try
        {
            var bytes = await _pdf.GenerateInvoicePdfAsync(id);
            var folio = await _db.Folios.FindAsync(id);
            return File(bytes, "application/pdf", $"Invoice-{folio!.FolioNumber}.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Folio", "CheckInOut", new { id });
        }
    }

    // GET: /Billing/ReceiptPdf/5  (payment id)
    public async Task<IActionResult> ReceiptPdf(int id)
    {
        var payment = await _db.Payments.FindAsync(id);
        if (payment == null) return NotFound();

        var bytes = await _pdf.GenerateReceiptPdfAsync(id);
        return File(bytes, "application/pdf", $"Receipt-{payment.ReceiptNumber}.pdf");
    }

    // GET: /Billing/Split/5  (folio id)
    [Authorize(Roles = "Admin,Manager,Accountant")]
    public async Task<IActionResult> Split(int id)
    {
        var folio = await _db.Folios
            .Include(f => f.Lines)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (folio == null) return NotFound();

        var charges = folio.Lines
            .Where(l => l.LineType != FolioLineType.Adjustment).Sum(l => l.Amount);

        return View(new SplitBillingViewModel
        {
            FolioId = folio.Id,
            FolioNumber = folio.FolioNumber,
            TotalCharges = charges,
            AlreadyPaid = folio.TotalPaid,
            Balance = charges - folio.TotalPaid
        });
    }

    // POST: /Billing/RecordSplit
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,Accountant")]
    public async Task<IActionResult> RecordSplit(SplitBillingViewModel model)
    {
        var folio = await _db.Folios
            .Include(f => f.Lines)
            .FirstOrDefaultAsync(f => f.Id == model.FolioId);
        if (folio == null) return NotFound();

        if (model.Parts < 2)
        {
            ModelState.AddModelError(nameof(model.Parts), "Split must be 2 or more parts.");
            return View("Split", model);
        }

        var charges = folio.Lines
            .Where(l => l.LineType != FolioLineType.Adjustment).Sum(l => l.Amount);
        var balance = charges - folio.TotalPaid;
        var perPart = Math.Round(balance / model.Parts, 2);

        // Post N payments (last one absorbs rounding remainder)
        var currentUser = await _userManager.GetUserAsync(User);
        for (int i = 0; i < model.Parts; i++)
        {
            var amount = (i == model.Parts - 1)
                ? balance - (perPart * (model.Parts - 1))
                : perPart;

            await _billing.RecordPaymentAsync(
                folio.Id, amount, PaymentMethod.Cash,
                transactionReference: null,
                notes: $"Split payment {i + 1} of {model.Parts}",
                userId: currentUser?.Id);
        }

        await LogActivity("SplitPayment",
            $"Folio {folio.FolioNumber}: split into {model.Parts} parts");

        TempData["Success"] = $"Split into {model.Parts} payments recorded.";
        return RedirectToAction("Folio", "CheckInOut", new { id = folio.Id });
    }

    // ----- helpers -----

    private async Task<PaymentViewModel> BuildPaymentVmAsync(Folio folio, PaymentViewModel? model = null)
    {
        var charges = folio.Lines
            .Where(l => l.LineType != FolioLineType.Adjustment).Sum(l => l.Amount);

        var history = await _db.Payments
            .Where(p => p.FolioId == folio.Id)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new PaymentListLine
            {
                Id = p.Id,
                ReceiptNumber = p.ReceiptNumber,
                PaidAt = p.PaidAt,
                Method = p.Method,
                Amount = p.Amount,
                TransactionReference = p.TransactionReference,
                IsRefund = p.IsRefund
            })
            .ToListAsync();

        var vm = model ?? new PaymentViewModel();
        vm.FolioId = folio.Id;
        vm.FolioNumber = folio.FolioNumber;
        vm.GuestName = folio.Guest.FullName;
        vm.TotalCharges = charges;
        vm.TotalPaid = folio.TotalPaid;
        vm.Balance = charges - folio.TotalPaid;
        vm.History = history;

        if (vm.Amount == 0) vm.Amount = vm.Balance;
        return vm;
    }

    private async Task LogActivity(string activity, string? details = null)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        _db.UserActivityLogs.Add(new UserActivityLog
        {
            UserId = currentUser?.Id ?? "system",
            Activity = activity,
            Details = details
        });
        await _db.SaveChangesAsync();
    }
}