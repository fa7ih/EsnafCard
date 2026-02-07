using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SecureCardSystem.Models;

namespace SecureCardSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        public DbSet<Card> Cards { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Card configuration
            builder.Entity<Card>()
                .HasIndex(c => c.CardNumber)
                .IsUnique();

            builder.Entity<Card>()
                .Property(c => c.Balance)
                .HasPrecision(18, 2);

            builder.Entity<Card>()
                .Property(c => c.InitialBalance)
                .HasPrecision(18, 2);

            // User-Card relationship
            builder.Entity<Card>()
                .HasOne(c => c.User)
                .WithMany(u => u.Cards)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Transaction configuration
            builder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            builder.Entity<Transaction>()
                .Property(t => t.BalanceBefore)
                .HasPrecision(18, 2);

            builder.Entity<Transaction>()
                .Property(t => t.BalanceAfter)
                .HasPrecision(18, 2);

            // Card-Transaction relationship
            builder.Entity<Transaction>()
                .HasOne(t => t.Card)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CardId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
