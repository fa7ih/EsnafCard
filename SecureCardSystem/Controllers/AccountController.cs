using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using SecureCardSystem.Models;
using SecureCardSystem.Services;
using System.Security.Claims;

namespace SecureCardSystem.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {

        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            var user = await _userManager.FindByEmailAsync(email);
            
            if (user == null)
            {
                TempData["Error"] = "Geçersiz email veya şifre!";
                return View();
            }

            if (!user.IsActive)
            {
                TempData["Error"] = "Hesabınız pasif durumda!";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);
            if (result.Succeeded)
            {
            //await SyncIpClaim(user);

                // Rol kontrolü ve yönlendirme
                var roles = await _userManager.GetRolesAsync(user);
                
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                if (roles.Contains("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (roles.Contains("User"))
                {
                    return RedirectToAction("Index", "User");
                }
                
                return RedirectToAction("Index", "Home");
            }

            TempData["Error"] = "Geçersiz email veya şifre!";
            return View();
        }
        //private async Task SyncIpClaim(ApplicationUser user)
        //{
        //    if (string.IsNullOrWhiteSpace(user.AllowedIpAddress))
        //        return;

        //    var claims = await _userManager.GetClaimsAsync(user);

        //    var existing = claims.FirstOrDefault(c => c.Type == "AllowedIpAddress");
        //    if (existing != null)
        //        await _userManager.RemoveClaimAsync(user, existing);

        //    await _userManager.AddClaimAsync(
        //        user,
        //        new Claim("AllowedIpRange", user.AllowedIpAddress)
        //    );
        //}


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Yeni şifreler eşleşmiyor!";
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Şifreniz başarıyla değiştirildi!";
                
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "User");
                }
            }

            foreach (var error in result.Errors)
            {
                TempData["Error"] = error.Description;
                break;
            }

            return View();
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(string fullName, string email)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            user.FullName = fullName;
            
            // Email değişikliği kontrolü
            if (user.Email != email)
            {
                var emailUser = await _userManager.FindByEmailAsync(email);
                if (emailUser != null && emailUser.Id != user.Id)
                {
                    TempData["Error"] = "Bu email adresi başka bir kullanıcı tarafından kullanılıyor!";
                    return RedirectToAction("Profile");
                }
                
                user.Email = email;
                user.UserName = email;
                user.NormalizedEmail = email.ToUpper();
                user.NormalizedUserName = email.ToUpper();
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Profil bilgileriniz güncellendi!";
            }
            else
            {
                TempData["Error"] = "Bir hata oluştu!";
            }

            return RedirectToAction("Profile");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(ForgotPassword forgotPassword)
        {
            var user = await _userManager.FindByEmailAsync(forgotPassword.Email);
            if (user == null)
            {
                TempData["Error"] = "Kullanıcı Bulunamadı";
                return View();
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            TempData["Success"] = "Şifre yenileme linki başarıyla gönderilmiştir. Mailinizi Kontrol Ediniz";

            var callbackUrl = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);
            var emailContent = $@"Lütfen şifrenizi yenilemek için linke <a href='{callbackUrl}'>tıklayınız</a>";

            await _emailSender.SendEmailAsync(forgotPassword.Email, "Reset Password", emailContent);


            return View();
        }

        public IActionResult ResetPassword(string token, string email)
        {
            var model = new ResetPassword { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPassword model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

                    if (result.Succeeded)
                    {
                        TempData["Success"] = "Şifreniz başarıyla değiştirilmiştir. Lütfen giriş yapınız!";
                        return RedirectToAction("Login", "Account");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            return View(model);
        }

        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}

