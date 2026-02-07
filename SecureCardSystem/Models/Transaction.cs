using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureCardSystem.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int CardId { get; set; }
        
        [ForeignKey("CardId")]
        public virtual Card Card { get; set; } = null!;
        
        [Required]
        public string CardNumber { get; set; } = string.Empty;
        
        [Required]
        public decimal Amount { get; set; }
        
        public decimal BalanceBefore { get; set; }
        
        public decimal BalanceAfter { get; set; }
        
        public string TransactionType { get; set; } = "Payment"; // Payment, BalanceUpdate, etc.
        
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        
        public string ProcessedBy { get; set; } = string.Empty;
        
        public string? Notes { get; set; }
        
        public string IpAddress { get; set; } = string.Empty;
    }
}
