using Microsoft.EntityFrameworkCore;
using Endpoint.Models;

namespace Endpoint.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<ActionHistory> ActionHistory { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<PendingCommand> PendingCommands { get; set; } = null!;
        public DbSet<AgentRegistration> AgentRegistrations { get; set; } = null!;

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

            modelBuilder.Entity<PendingCommand>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Id).ValueGeneratedOnAdd();
                e.HasIndex(p => p.AgentId);
                e.HasIndex(p => p.Status);
                e.HasIndex(p => p.CreatedAt);
            });

            modelBuilder.Entity<AgentRegistration>(e =>
            {
                e.HasKey(a => a.Id);
                e.Property(a => a.Id).ValueGeneratedOnAdd();
                e.HasIndex(a => a.AgentId).IsUnique();
                e.HasIndex(a => a.LastSeenAt);
            });
        }
    }
}
