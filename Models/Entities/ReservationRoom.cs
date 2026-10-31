namespace HotelManagement.Models.Entities;

public class ReservationRoom
{
    public int Id { get; set; }

    public int ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    // Snapshot of nightly rate at booking time (in case Room.Rate changes later)
    public decimal NightlyRate { get; set; }
}