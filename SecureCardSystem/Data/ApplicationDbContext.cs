using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SecureCardSystem.Models;

namespace SecureCardSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Card> Cards { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Card configuration
            builder.Entity<Card>()
                .HasIndex(c => c.CardNumber)
                .IsUnique();

            // SQLite için decimal type string olarak saklanýr
            builder.Entity<Card>()
                .Property(c => c.Balance)
                .HasColumnType("TEXT");

            builder.Entity<Card>()
                .Property(c => c.InitialBalance)
                .HasColumnType("TEXT");

            // User-Card relationship
            builder.Entity<Card>()
                .HasOne(c => c.User)
                .WithMany(u => u.Cards)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Transaction configuration
            builder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasColumnType("TEXT");

            builder.Entity<Transaction>()
                .Property(t => t.BalanceBefore)
                .HasColumnType("TEXT");

            builder.Entity<Transaction>()
                .Property(t => t.BalanceAfter)
                .HasColumnType("TEXT");

            // Card-Transaction relationship
            builder.Entity<Transaction>()
                .HasOne(t => t.Card)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CardId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}