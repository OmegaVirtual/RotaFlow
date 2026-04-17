using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rota.Models;
using Rota.Models.Requests;
using Rota.Models.Notifications;

namespace Rota.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Shift> Shifts { get; set; }
        public DbSet<ShiftAssignment> ShiftAssignments { get; set; }
        public DbSet<ShiftRequest> ShiftRequests { get; set; }
        public DbSet<HolidayRequest> HolidayRequests { get; set; }
        public DbSet<UserHolidayBalance> UserHolidayBalances { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; } // ✅ Add UserProfiles table

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<HolidayRequest>()
                .Property(h => h.UserId)
                .IsRequired();

            builder.Entity<HolidayRequest>()
                .Property(h => h.StartDate)
                .IsRequired();

            builder.Entity<HolidayRequest>()
                .Property(h => h.EndDate)
                .IsRequired();
        }
    }
}
