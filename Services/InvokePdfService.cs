using HotelManagement.Models.Data;
using HotelManagement.Models.Enums;
using HotelManagement.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HotelManagement.Services;

public class InvoicePdfService
{
    private readonly ApplicationDbContext _db;
    private readonly HotelSettings _settings;

    public InvoicePdfService(ApplicationDbContext db, IOptions<HotelSettings> settings)
    {
        _db = db;
        _settings = settings.Value;
    }
    public async Task<byte[]> GenerateInvoicePdfAsync(int folioId)
    {
        var folio = await _db.Folios
            .Include(f => f.Guest)
            .Include(f => f.Reservation)
            .Include(f => f.Lines)
            .FirstOrDefaultAsync(f => f.Id == folioId)
            ?? throw new InvalidOperationException("Folio not found.");

        var payments = await _db.Payments
            .Where(p => p.FolioId == folioId && !p.IsRefund)
            .OrderBy(p => p.PaidAt)
            .ToListAsync();

        var charges = folio.Lines
            .Where(l => l.LineType != FolioLineType.Adjustment)
            .OrderBy(l => l.PostedAt)
            .ToList();

        var totalCharges = charges.Sum(c => c.Amount);
        var totalPaid = payments.Sum(p => p.Amount);
        var balance = totalCharges - totalPaid;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontSize(10));

                // ----- Header -----
                page.Header().Column(col =>
                {
                    col.Item().Text(_settings.HotelName).FontSize(20).Bold();
                    col.Item().Text(_settings.Address);
                    col.Item().Text($"Phone: {_settings.Phone}   Email: {_settings.Email}");
                    if (!string.IsNullOrEmpty(_settings.GSTNumber))
                        col.Item().Text($"GSTIN: {_settings.GSTNumber}");

                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(8).Text("TAX INVOICE").FontSize(14).Bold();
                });

                // ----- Content -----
                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(10);

                    // Invoice meta
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Folio No: {folio.FolioNumber}").Bold();
                            c.Item().Text($"Booking Ref: {folio.Reservation.BookingReference}");
                            c.Item().Text($"Opened: {folio.OpenedAt:dd MMM yyyy HH:mm}");
                            if (folio.ClosedAt.HasValue)
                                c.Item().Text($"Closed: {folio.ClosedAt:dd MMM yyyy HH:mm}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Guest: {folio.Guest.FullName}").Bold();
                            c.Item().Text($"Phone: {folio.Guest.Phone}");
                            c.Item().Text($"Email: {folio.Guest.Email}");
                            if (!string.IsNullOrEmpty(folio.Guest.Address))
                                c.Item().Text($"Address: {folio.Guest.Address}");
                        });
                    });

                    // Charges table
                    col.Item().PaddingTop(10).Text("Charges").FontSize(12).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(80);   // date
                            cols.RelativeColumn(3);    // description
                            cols.RelativeColumn(1);    // type
                            cols.ConstantColumn(80);   // amount
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Date").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Description").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Type").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Amount").Bold();
                        });

                        foreach (var line in charges)
                        {
                            table.Cell().Padding(4).Text(line.PostedAt.ToString("dd MMM"));
                            table.Cell().Padding(4).Text(line.Description);
                            table.Cell().Padding(4).Text(line.LineType.ToString());
                            table.Cell().Padding(4).AlignRight().Text($"₹{line.Amount:N2}");
                        }
                    });

                    // Totals
                    col.Item().PaddingTop(8).AlignRight().Column(c =>
                    {
                        c.Item().Text($"Subtotal: ₹{charges.Where(x => x.LineType != FolioLineType.Tax).Sum(x => x.Amount):N2}");
                        c.Item().Text($"Tax: ₹{charges.Where(x => x.LineType == FolioLineType.Tax).Sum(x => x.Amount):N2}");
                        c.Item().Text($"Total Charges: ₹{totalCharges:N2}").Bold();
                        c.Item().Text($"Total Paid: ₹{totalPaid:N2}");
                        c.Item().Text($"Balance Due: ₹{balance:N2}").Bold()
                            .FontColor(balance > 0 ? Colors.Red.Medium : Colors.Green.Medium);
                    });

                    // Payments
                    if (payments.Any())
                    {
                        col.Item().PaddingTop(15).Text("Payments").FontSize(12).Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(80);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Date").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Receipt").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Method").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Amount").Bold();
                            });

                            foreach (var p in payments)
                            {
                                table.Cell().Padding(4).Text(p.PaidAt.ToString("dd MMM HH:mm"));
                                table.Cell().Padding(4).Text(p.ReceiptNumber);
                                table.Cell().Padding(4).Text(p.Method.ToString());
                                table.Cell().Padding(4).AlignRight().Text($"₹{p.Amount:N2}");
                            }
                        });
                    }
                });

                // ----- Footer -----
                page.Footer().AlignCenter().Column(c =>
                {
                    c.Item().Text("Thank you for staying with us!").Italic();
                    c.Item().Text($"Generated on {DateTime.Now:dd MMM yyyy HH:mm}");
                });
            });
        });

        return doc.GeneratePdf();
    }

    public async Task<byte[]> GenerateReceiptPdfAsync(int paymentId)
    {
        var payment = await _db.Payments
            .Include(p => p.Folio).ThenInclude(f => f.Guest)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new InvalidOperationException("Payment not found.");

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(25);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(_settings.HotelName).FontSize(16).Bold();
                    col.Item().Text(_settings.Address);
                    col.Item().PaddingTop(6).Text("PAYMENT RECEIPT").FontSize(12).Bold();
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"Receipt No: {payment.ReceiptNumber}").Bold();
                    col.Item().Text($"Folio: {payment.Folio.FolioNumber}");
                    col.Item().Text($"Guest: {payment.Folio.Guest.FullName}");
                    col.Item().Text($"Date: {payment.PaidAt:dd MMM yyyy HH:mm}");
                    col.Item().Text($"Method: {payment.Method}");
                    if (!string.IsNullOrEmpty(payment.TransactionReference))
                        col.Item().Text($"Ref: {payment.TransactionReference}");
                    col.Item().PaddingTop(8).Text($"Amount: ₹{payment.Amount:N2}").FontSize(14).Bold();
                });

                page.Footer().AlignCenter().Text("Thank you!").Italic();
            });
        });

        return doc.GeneratePdf();
    }
}