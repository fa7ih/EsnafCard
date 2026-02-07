using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureCardSystem.Models;
using SecureCardSystem.Data;

namespace SecureCardSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<dynamic>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var cardCount = await _context.Cards.CountAsync(c => c.UserId == user.Id);
                
                userViewModels.Add(new
                {
                    User = user,
                    Roles = string.Join(", ", roles),
                    CardCount = cardCount
                });
            }

            ViewBag.Users = userViewModels;
            ViewBag.TotalCards = await _context.Cards.CountAsync();
            ViewBag.TotalTransactions = await _context.Transactions.CountAsync();
            ViewBag.CurrentIp = GetClientIpAddress();
            
            return View();
        }

        public IActionResult CreateUser()
        {
            ViewBag.CurrentIp = GetClientIpAddress();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(string email, string fullName, string password, bool isYilmaz, string? allowedIp)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                IsYilmaz = isYilmaz,
                AllowedIpAddress = allowedIp,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Rol ata
                var roleName = isYilmaz ? "User" : "User";
                
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }

                await _userManager.AddToRoleAsync(user, roleName);

                TempData["Success"] = "Kullanıcı başarıyla oluşturuldu!";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateIpRestriction(string userId, string? ipAddress)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.AllowedIpAddress = ipAddress;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "IP kısıtlaması güncellendi!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = $"Kullanıcı {(user.IsActive ? "aktif" : "pasif")} hale getirildi!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == userId)
            {
                TempData["Error"] = "Kendi hesabınızı silemezsiniz!";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.DeleteAsync(user);

            TempData["Success"] = "Kullanıcı silindi!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> SystemStats()
        {
            var stats = new
            {
                TotalCards = await _context.Cards.CountAsync(),
                ActiveCards = await _context.Cards.CountAsync(c => c.IsActive),
                TotalTransactions = await _context.Transactions.CountAsync(),
                TotalBalance = await _context.Cards.SumAsync(c => c.Balance),
                TotalUsers = await _userManager.Users.CountAsync(),
                TodayTransactions = await _context.Transactions.CountAsync(t => t.TransactionDate.Date == DateTime.Today)
            };

            return View(stats);
        }

        private string GetClientIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "Unknown";

            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString();
            }
            
            // Normalize IPv6 localhost to IPv4
            if (ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }
            
            return ipAddress ?? "Unknown";
        }
    }
}
