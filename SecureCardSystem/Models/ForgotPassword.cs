using System.ComponentModel.DataAnnotations;

namespace SecureCardSystem.Models
{
    public class ForgotPassword
    {

        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        public string Email { get; set; }
    }
}
