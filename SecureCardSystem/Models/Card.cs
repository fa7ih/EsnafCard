using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureCardSystem.Models
{
    public class Card
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(8, MinimumLength = 8)]
        public string CardNumber { get; set; } = string.Empty;
        
        [Required]
        public decimal Balance { get; set; }
        
        public decimal InitialBalance { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public string CreatedBy { get; set; } = string.Empty;
        
        // User relationship
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
        
        // Navigation property
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
