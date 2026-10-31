using HotelManagement.Models.Enums;

namespace HotelManagement.Models.Helpers;

public static class BadgeHelper
{
    // ----- Room Status -----
    public static string RoomStatus(RoomStatus status) => status switch
    {
        Enums.RoomStatus.Available => "badge-status badge-status-success",
        Enums.RoomStatus.Occupied => "badge-status badge-status-danger",
        Enums.RoomStatus.Reserved => "badge-status badge-status-warning",
        Enums.RoomStatus.Cleaning => "badge-status badge-status-info",
        Enums.RoomStatus.Maintenance => "badge-status badge-status-neutral",
        _ => "badge-status badge-status-neutral"
    };

    // ----- Reservation Status -----
    public static string ReservationStatus(Enums.ReservationStatus status) => status switch
    {
        Enums.ReservationStatus.Pending => "badge-status badge-status-warning",
        Enums.ReservationStatus.Confirmed => "badge-status badge-status-info",
        Enums.ReservationStatus.CheckedIn => "badge-status badge-status-success",
        Enums.ReservationStatus.CheckedOut => "badge-status badge-status-neutral",
        Enums.ReservationStatus.Cancelled => "badge-status badge-status-danger",
        Enums.ReservationStatus.NoShow => "badge-status badge-status-neutral",
        _ => "badge-status badge-status-neutral"
    };

    // ----- Housekeeping Task Status -----
    public static string HousekeepingTask(HousekeepingTaskStatus status) => status switch
    {
        HousekeepingTaskStatus.Pending => "badge-status badge-status-warning",
        HousekeepingTaskStatus.InProgress => "badge-status badge-status-info",
        HousekeepingTaskStatus.Completed => "badge-status badge-status-success",
        _ => "badge-status badge-status-neutral"
    };

    // ----- Maintenance Priority -----
    public static string MaintenancePriority(Enums.MaintenancePriority priority) => priority switch
    {
        Enums.MaintenancePriority.Low => "badge-status badge-status-neutral",
        Enums.MaintenancePriority.Medium => "badge-status badge-status-info",
        Enums.MaintenancePriority.High => "badge-status badge-status-warning",
        Enums.MaintenancePriority.Urgent => "badge-status badge-status-danger",
        _ => "badge-status badge-status-neutral"
    };

    // ----- Maintenance Status -----
    public static string MaintenanceStatus(Enums.MaintenanceStatus status) => status switch
    {
        Enums.MaintenanceStatus.Open => "badge-status badge-status-warning",
        Enums.MaintenanceStatus.InProgress => "badge-status badge-status-info",
        Enums.MaintenanceStatus.Resolved => "badge-status badge-status-success",
        _ => "badge-status badge-status-neutral"
    };

    // ----- Service Request Status -----
    public static string ServiceRequestStatus(Enums.ServiceRequestStatus status) => status switch
    {
        Enums.ServiceRequestStatus.Requested => "badge-status badge-status-warning",
        Enums.ServiceRequestStatus.InProgress => "badge-status badge-status-info",
        Enums.ServiceRequestStatus.Fulfilled => "badge-status badge-status-success",
        Enums.ServiceRequestStatus.Cancelled => "badge-status badge-status-neutral",
        _ => "badge-status badge-status-neutral"
    };

    // ----- Notification Status -----
    public static string NotificationStatus(Enums.NotificationStatus status) => status switch
    {
        Enums.NotificationStatus.Pending => "badge-status badge-status-warning",
        Enums.NotificationStatus.Sent => "badge-status badge-status-success",
        Enums.NotificationStatus.Failed => "badge-status badge-status-danger",
        Enums.NotificationStatus.Skipped => "badge-status badge-status-neutral",
        _ => "badge-status badge-status-neutral"
    };

    // ----- Folio Status -----
    public static string FolioStatus(Enums.FolioStatus status) => status switch
    {
        Enums.FolioStatus.Open => "badge-status badge-status-info",
        Enums.FolioStatus.Settled => "badge-status badge-status-success",
        Enums.FolioStatus.Void => "badge-status badge-status-neutral",
        _ => "badge-status badge-status-neutral"
    };

    // ----- Generic Active/Inactive -----
    public static string Active(bool isActive) =>
        isActive ? "badge-status badge-status-success" : "badge-status badge-status-neutral";
}