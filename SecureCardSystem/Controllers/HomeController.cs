using Microsoft.AspNetCore.Mvc;

namespace SecureCardSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (User.IsInRole("User"))
                {
                    return RedirectToAction("Index", "User");
                }
            }
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
