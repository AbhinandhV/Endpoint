using Microsoft.EntityFrameworkCore;
using Endpoint.Models;

namespace Endpoint.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<ActionHistory> ActionHistory { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ActionHistory>(e =>
            {
                e.HasKey(h => h.Id);
                e.Property(h => h.Id).ValueGeneratedOnAdd();
                e.HasIndex(h => h.StartedAt);
                e.HasIndex(h => h.CategoryId);
            });

            modelBuilder.Entity<AuditLog>(e =>
            {
                e.HasKey(a => a.Id);
                e.Property(a => a.Id).ValueGeneratedOnAdd();
                e.HasIndex(a => a.Timestamp);
                e.HasIndex(a => a.User);
            });
        }
    }
}
