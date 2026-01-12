using System.Security.Claims;
namespace BankTransfer.Api.Auth;

public static class ClaimsExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal user, out Guid userId)
    {
        var userIdStr =
            user.FindFirst("userId")?.Value ??
            user.FindFirst("UserId")?.Value ??
            user.FindFirst("sub")?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdStr, out userId);
    }
}