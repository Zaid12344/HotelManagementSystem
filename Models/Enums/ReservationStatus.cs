namespace HotelManagement.Models.Enums;

public enum ReservationStatus
{
    Pending = 1,        // created, awaiting confirmation/deposit
    Confirmed = 2,      // deposit paid or guaranteed
    CheckedIn = 3,      // guest currently in-house (set by Module 4)
    CheckedOut = 4,     // completed stay (set by Module 4)
    Cancelled = 5,      // cancelled by guest/staff
    NoShow = 6          // guest didn't show up
}