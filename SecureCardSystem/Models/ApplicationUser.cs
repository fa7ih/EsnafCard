using Microsoft.AspNetCore.Identity;

namespace SecureCardSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public bool IsYilmaz { get; set; }
        public string? AllowedIpAddress { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation property for user's cards
        public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
    }
}
