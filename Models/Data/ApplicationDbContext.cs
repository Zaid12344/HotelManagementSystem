using HotelManagement.Models.Entities;
using HotelManagement.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Models.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<UserActivityLog> UserActivityLogs { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ReservationRoom> ReservationRooms { get; set; }
    public DbSet<Guest> Guests { get; set; }
    public DbSet<Folio> Folios { get; set; }
    public DbSet<FolioLine> FolioLines { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<HousekeepingTask> HousekeepingTasks { get; set; }
    public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<ServiceRequest> ServiceRequests { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Room>(entity =>
        {
            entity.Property(r => r.Rate)
                  .HasPrecision(10, 2);   // up to 99,999,999.99

            entity.HasIndex(r => r.RoomNumber).IsUnique();
        });

        builder.Entity<Reservation>(entity =>
        {
            entity.HasIndex(r => r.BookingReference).IsUnique();

            entity.HasOne(r => r.Guest)
                  .WithMany(g => g.Reservations)
                  .HasForeignKey(r => r.GuestId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(r => r.TotalAmount).HasPrecision(10, 2);
            entity.Property(r => r.DepositAmount).HasPrecision(10, 2);
        });

        builder.Entity<ReservationRoom>(entity =>
        {
            entity.HasOne(rr => rr.Reservation)
                  .WithMany(r => r.ReservationRooms)
                  .HasForeignKey(rr => rr.ReservationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rr => rr.Room)
                  .WithMany()
                  .HasForeignKey(rr => rr.RoomId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(rr => rr.NightlyRate).HasPrecision(10, 2);

            // Prevent the same room being booked twice in the same reservation
            entity.HasIndex(rr => new { rr.ReservationId, rr.RoomId }).IsUnique();
        });

        builder.Entity<Folio>(entity =>
        {
            entity.HasIndex(f => f.FolioNumber).IsUnique();

            entity.HasOne(f => f.Reservation)
                  .WithMany()
                  .HasForeignKey(f => f.ReservationId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Guest)
                  .WithMany()
                  .HasForeignKey(f => f.GuestId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(f => f.TotalCharges).HasPrecision(10, 2);
            entity.Property(f => f.TotalPaid).HasPrecision(10, 2);
        });

        builder.Entity<FolioLine>(entity =>
        {
            entity.HasOne(l => l.Folio)
                  .WithMany(f => f.Lines)
                  .HasForeignKey(l => l.FolioId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.Room)
                  .WithMany()
                  .HasForeignKey(l => l.RoomId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.Property(l => l.Amount).HasPrecision(10, 2);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.HasIndex(p => p.ReceiptNumber).IsUnique();

            entity.HasOne(p => p.Folio)
                  .WithMany()
                  .HasForeignKey(p => p.FolioId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.RefundOfPayment)
                  .WithMany()
                  .HasForeignKey(p => p.RefundOfPaymentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(p => p.Amount).HasPrecision(10, 2);
        });

        builder.Entity<HousekeepingTask>(entity =>
        {
            entity.HasOne(t => t.Room)
                  .WithMany()
                  .HasForeignKey(t => t.RoomId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MaintenanceRequest>(entity =>
        {
            entity.HasOne(m => m.Room)
                  .WithMany()
                  .HasForeignKey(m => m.RoomId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Service>(entity =>
        {
            entity.Property(s => s.Price).HasPrecision(10, 2);
        });

        builder.Entity<ServiceRequest>(entity =>
        {
            entity.HasOne(sr => sr.Service)
                  .WithMany()
                  .HasForeignKey(sr => sr.ServiceId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sr => sr.Reservation)
                  .WithMany()
                  .HasForeignKey(sr => sr.ReservationId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sr => sr.Room)
                  .WithMany()
                  .HasForeignKey(sr => sr.RoomId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<Notification>(entity =>
        {
            entity.HasIndex(n => n.CreatedAt);
            entity.HasIndex(n => n.Status);
        });

        // Future module configurations go here
    }
}