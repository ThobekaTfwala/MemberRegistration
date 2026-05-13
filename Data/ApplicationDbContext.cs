using Microsoft.EntityFrameworkCore;
using MeetingRegister.Models;

namespace MeetingRegister.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<MeetingAttendee> MeetingAttendees { get; set; }
        public DbSet<AttendanceSession> AttendanceSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Unique index for Meeting Code
            modelBuilder.Entity<Meeting>()
                .HasIndex(m => m.MeetingCode)
                .IsUnique();

            // GLOBAL Unique index for QRCode
            modelBuilder.Entity<MeetingAttendee>()
                .HasIndex(a => a.QRCode)
                .IsUnique();

            // Per-Meeting Unique Index for Cellphone
            modelBuilder.Entity<MeetingAttendee>()
                .HasIndex(a => new { a.MeetingId, a.Cellphone })
                .IsUnique();

            // Per-Meeting Unique Index for Email
            modelBuilder.Entity<MeetingAttendee>()
                .HasIndex(a => new { a.MeetingId, a.Email })
                .IsUnique();

            // Setup relationships
            modelBuilder.Entity<MeetingAttendee>()
                .HasOne(a => a.Meeting)
                .WithMany(m => m.Attendees)
                .HasForeignKey(a => a.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AttendanceSession>()
                .HasOne(s => s.Attendee)
                .WithMany(a => a.Sessions)
                .HasForeignKey(s => s.AttendeeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
