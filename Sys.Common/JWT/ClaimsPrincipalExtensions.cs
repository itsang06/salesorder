using SysAdmin.Models.StaticValue;
using System.Linq;
using System.Security.Claims;


namespace Sys.Common.Utils
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetName(this ClaimsPrincipal user) =>
            user.Claims.FirstOrDefault(c => c.Type == CustomClaimType.UserName)?.Value;
    }
}
