namespace HotelManagement.Models.Enums;

public enum NotificationType
{
    BookingConfirmation = 1,
    BookingReminder = 2,
    CheckoutHousekeeping = 3,
    PaymentReceipt = 4,
    InvoiceEmail = 5,
    LowOccupancyAlert = 6,
    PendingPaymentAlert = 7,
    ReservationCancelled = 8,
    Custom = 99
}