//using Microsoft.AspNetCore.Identity;
//using SecureCardSystem.Models;

//namespace SecureCardSystem.Services
//{
//    public class IpRestrictionMiddleware
//    {
//        private readonly RequestDelegate _next;

//        public IpRestrictionMiddleware(RequestDelegate next)
//        {
//            _next = next;
//        }

//        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
//        {
//            // Skip for login and public pages
//            var path = context.Request.Path.Value?.ToLower() ?? "";
//            if (path.Contains("/account/login") || 
//                path.Contains("/account/logout") ||
//                path.Contains("/home") ||
//                path == "/")
//            {
//                await _next(context);
//                return;
//            }

//            if (context.User.Identity?.IsAuthenticated == true)
//            {
//                var user = await userManager.GetUserAsync(context.User);
                
//                if (user != null && !string.IsNullOrEmpty(user.AllowedIpAddress))
//                {
//                    var clientIp = GetClientIpAddress(context);
                    
//                    // Strict IP check - must match exactly
//                    if (clientIp != user.AllowedIpAddress)
//                    {
//                        // Sign out the user
//                        context.Response.StatusCode = 403;
//                        context.Response.ContentType = "text/html; charset=utf-8";
//                        await context.Response.WriteAsync(@"
//<!DOCTYPE html>
//<html>
//<head>
//    <title>Erişim Engellendi</title>
//    <style>
//        body { font-family: Arial, sans-serif; text-align: center; padding: 50px; background: #f5f5f5; }
//        .error-box { background: white; padding: 40px; border-radius: 10px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); max-width: 500px; margin: 0 auto; }
//        .error-icon { font-size: 64px; color: #dc3545; }
//        h1 { color: #dc3545; }
//        p { color: #666; }
//        .ip-info { background: #f8f9fa; padding: 10px; border-radius: 5px; margin: 20px 0; }
//    </style>
//</head>
//<body>
//    <div class='error-box'>
//        <div class='error-icon'>🚫</div>
//        <h1>Erişim Engellendi</h1>
//        <p>Bu cihazdan giriş yapma yetkiniz bulunmamaktadır.</p>
//        <div class='ip-info'>
//            <strong>Mevcut IP Adresiniz:</strong> " + clientIp + @"
//        </div>
//        <p><small>Lütfen sistem yöneticisi ile iletişime geçin.</small></p>
//        <a href='/Account/Logout' style='display: inline-block; margin-top: 20px; padding: 10px 20px; background: #667eea; color: white; text-decoration: none; border-radius: 5px;'>Çıkış Yap</a>
//    </div>
//</body>
//</html>");
//                        return;
//                    }
//                }
//            }

//            await _next(context);
//        }

//        private string GetClientIpAddress(HttpContext context)
//        {
//            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
//            if (string.IsNullOrEmpty(ipAddress))
//            {
//                ipAddress = context.Connection.RemoteIpAddress?.ToString();
//            }
            
//            // Normalize IPv6 localhost to IPv4
//            if (ipAddress == "::1")
//            {
//                ipAddress = "127.0.0.1";
//            }
            
//            return ipAddress ?? "Unknown";
//        }
//    }
//}
