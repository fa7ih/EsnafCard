using Microsoft.AspNetCore.Authorization;
using System.Net;
using SecureCardSystem.Authorization.Requirements;

namespace SecureCardSystem.Authorization.Handlers
{
    public class IpRangeHandler : AuthorizationHandler<IpRangeRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IpRangeHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            IpRangeRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return Task.CompletedTask;

            var claim = context.User.FindFirst("AllowedIpRange");

            // Claim yoksa kısıtlama yok
            if (claim == null)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var clientIp = httpContext.Connection.RemoteIpAddress;
            if (clientIp == null)
                return Task.CompletedTask;

            if (IsInRange(clientIp, claim.Value))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }

        private bool IsInRange(IPAddress ip, string cidr)
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2)
                return false;

            var baseIp = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);

            var ipBytes = ip.GetAddressBytes();
            var baseBytes = baseIp.GetAddressBytes();

            if (ipBytes.Length != baseBytes.Length)
                return false;

            var bits = prefixLength;
            for (int i = 0; i < ipBytes.Length && bits > 0; i++)
            {
                var mask = bits >= 8 ? 255 : (byte)(~(255 >> bits));
                if ((ipBytes[i] & mask) != (baseBytes[i] & mask))
                    return false;

                bits -= 8;
            }

            return true;
        }
    }
}
