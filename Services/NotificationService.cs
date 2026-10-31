using HotelManagement.Models.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Models.Enums;
using HotelManagement.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotelManagement.Services;

public class NotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly HotelSettings _hotel;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext db,
        IOptions<HotelSettings> hotel,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _hotel = hotel.Value;
        _logger = logger;
    }

    // -------- Generic send (log-only) --------

    public async Task<Notification> SendEmailAsync(
        NotificationType type, string recipient, string subject, string body,
        int? reservationId = null, int? folioId = null, int? paymentId = null)
    {
        var n = new Notification
        {
            Type = type,
            Channel = NotificationChannel.Email,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            ReservationId = reservationId,
            FolioId = folioId,
            PaymentId = paymentId,
            Status = NotificationStatus.Skipped,
            SentAt = DateTime.UtcNow,
            ErrorMessage = "Email delivery disabled — logged only."
        };

        _db.Notifications.Add(n);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "[NOTIFY-EMAIL] Type={Type} To={To} Subject={Subject}",
            type, recipient, subject);

        return n;
    }

    public async Task<Notification> SendSmsAsync(
        NotificationType type, string phone, string body,
        int? reservationId = null)
    {
        var n = new Notification
        {
            Type = type,
            Channel = NotificationChannel.Sms,
            Recipient = phone,
            Subject = "(SMS)",
            Body = body,
            ReservationId = reservationId,
            Status = NotificationStatus.Skipped,
            SentAt = DateTime.UtcNow,
            ErrorMessage = "SMS gateway not configured — logged only."
        };

        _db.Notifications.Add(n);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "[NOTIFY-SMS] Type={Type} To={Phone} Body={Body}",
            type, phone, body);

        return n;
    }

    public async Task<Notification> SendSystemAlertAsync(
        NotificationType type, string subject, string body)
    {
        var n = new Notification
        {
            Type = type,
            Channel = NotificationChannel.System,
            Recipient = "management",
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Sent,
            SentAt = DateTime.UtcNow
        };

        _db.Notifications.Add(n);
        await _db.SaveChangesAsync();

        _logger.LogWarning("[SYSTEM-ALERT] {Subject} | {Body}", subject, body);
        return n;
    }

    // -------- Concrete triggers --------

    public async Task SendBookingConfirmationAsync(Reservation r)
    {
        var subject = $"Booking Confirmed — {r.BookingReference}";
        var body = $@"
            <h3>Dear {r.Guest.FullName},</h3>
            <p>Thank you for your booking at <strong>{_hotel.HotelName}</strong>.</p>
            <p><strong>Reference:</strong> {r.BookingReference}<br/>
               <strong>Check-In:</strong> {r.CheckInDate:dd MMM yyyy}<br/>
               <strong>Check-Out:</strong> {r.CheckOutDate:dd MMM yyyy}<br/>
               <strong>Guests:</strong> {r.Adults} adult(s), {r.Children} child(ren)<br/>
               <strong>Total:</strong> ₹{r.TotalAmount:N2}<br/>
               <strong>Deposit Received:</strong> ₹{r.DepositAmount:N2}</p>
            <p>We look forward to hosting you!</p>
            <p>— {_hotel.HotelName}<br/>{_hotel.Phone} · {_hotel.Email}</p>";

        await SendEmailAsync(NotificationType.BookingConfirmation,
            r.Guest.Email, subject, body, reservationId: r.Id);

        await SendSmsAsync(NotificationType.BookingConfirmation,
            r.Guest.Phone,
            $"{_hotel.HotelName}: Booking {r.BookingReference} confirmed for {r.CheckInDate:dd MMM}. Total ₹{r.TotalAmount:N0}.",
            reservationId: r.Id);
    }

    public async Task SendBookingCancellationAsync(Reservation r)
    {
        var subject = $"Booking Cancelled — {r.BookingReference}";
        var body = $@"
            <h3>Dear {r.Guest.FullName},</h3>
            <p>Your booking <strong>{r.BookingReference}</strong> has been cancelled.</p>
            <p>If you did not request this cancellation, please contact us immediately.</p>
            <p>— {_hotel.HotelName}</p>";

        await SendEmailAsync(NotificationType.ReservationCancelled,
            r.Guest.Email, subject, body, reservationId: r.Id);
    }

    public async Task SendCheckoutHousekeepingAlertAsync(Reservation r, IEnumerable<string> roomNumbers)
    {
        var subject = $"Room(s) need cleaning — {r.BookingReference}";
        var body = $@"
            <p>Guest <strong>{r.Guest.FullName}</strong> has checked out.</p>
            <p>Rooms requiring cleaning: <strong>{string.Join(", ", roomNumbers)}</strong></p>
            <p>Please check the Housekeeping dashboard.</p>";

        await SendSystemAlertAsync(NotificationType.CheckoutHousekeeping, subject, body);
    }

    public async Task SendPaymentReceiptAsync(Payment payment, string guestEmail)
    {
        var subject = $"Payment Received — {payment.ReceiptNumber}";
        var body = $@"
            <h3>Payment Confirmation</h3>
            <p>We've received your payment of <strong>₹{payment.Amount:N2}</strong> via {payment.Method}.</p>
            <p><strong>Receipt No:</strong> {payment.ReceiptNumber}<br/>
               <strong>Date:</strong> {payment.PaidAt:dd MMM yyyy HH:mm}</p>
            <p>Thank you!</p>
            <p>— {_hotel.HotelName}</p>";

        await SendEmailAsync(NotificationType.PaymentReceipt,
            guestEmail, subject, body, folioId: payment.FolioId, paymentId: payment.Id);
    }

    public async Task SendArrivalRemindersAsync()
    {
        var tomorrow = DateTime.Today.AddDays(1);
        var reservations = await _db.Reservations
            .Include(r => r.Guest)
            .Where(r => r.CheckInDate.Date == tomorrow
                     && (r.Status == ReservationStatus.Confirmed
                         || r.Status == ReservationStatus.Pending))
            .ToListAsync();

        foreach (var r in reservations)
        {
            var subject = $"Reminder: Your stay begins tomorrow — {r.BookingReference}";
            var body = $@"
                <h3>Dear {r.Guest.FullName},</h3>
                <p>Just a friendly reminder that your stay at <strong>{_hotel.HotelName}</strong> begins tomorrow.</p>
                <p><strong>Reference:</strong> {r.BookingReference}<br/>
                   <strong>Check-In:</strong> {r.CheckInDate:dd MMM yyyy}</p>
                <p>See you soon!</p>";

            await SendEmailAsync(NotificationType.BookingReminder,
                r.Guest.Email, subject, body, reservationId: r.Id);
        }
    }

    public async Task CheckLowOccupancyAsync()
    {
        var totalRooms = await _db.Rooms.CountAsync(r => r.Status != RoomStatus.Maintenance);
        if (totalRooms == 0) return;

        var occupied = await _db.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied);
        var occupancyPercent = (decimal)occupied / totalRooms * 100m;

        if (occupancyPercent < 30m)
        {
            await SendSystemAlertAsync(
                NotificationType.LowOccupancyAlert,
                $"Low occupancy alert — {occupancyPercent:0.#}%",
                $"Occupancy is {occupancyPercent:0.#}% ({occupied}/{totalRooms} rooms). Consider promotions.");
        }
    }

    public async Task CheckPendingPaymentsAsync()
    {
        var pending = await _db.Folios
            .Include(f => f.Guest)
            .Where(f => f.Status == FolioStatus.Open)
            .ToListAsync();

        var withBalance = new List<(Folio folio, decimal balance)>();
        foreach (var f in pending)
        {
            var charges = await _db.FolioLines
                .Where(l => l.FolioId == f.Id && l.LineType != FolioLineType.Adjustment)
                .SumAsync(l => l.Amount);
            var balance = charges - f.TotalPaid;
            if (balance > 0) withBalance.Add((f, balance));
        }

        if (withBalance.Count > 0)
        {
            var total = withBalance.Sum(x => x.balance);
            await SendSystemAlertAsync(
                NotificationType.PendingPaymentAlert,
                $"{withBalance.Count} folio(s) with pending payments",
                $"Total outstanding: ₹{total:N2} across {withBalance.Count} open folio(s).");
        }
    }
}