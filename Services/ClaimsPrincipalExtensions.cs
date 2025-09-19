using System.Security.Claims;

namespace FLEXIERP.Services
{
    public static class ClaimsPrincipalExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : (int?)null;
        }


        public static string? GetUsername(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Name);
        }

        public static string? GetFullName(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.GivenName);
        }

        public static string? GetRoleId(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Role);
        }

        public static string? GetEmail(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Email);
        }

        public static bool? IsActive(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue("IsActive");
            return value != null && bool.TryParse(value, out var result) ? result : (bool?)null;
        }

        public static string? GetMobileNo(this ClaimsPrincipal user)
        {
            return user.FindFirstValue("MobileNo");
        }

        public static string? GetGender(this ClaimsPrincipal user)
        {
            return user.FindFirstValue("Gender");
        }

        public static DateTime? GetDateOfBirth(this ClaimsPrincipal user)
        {
            var dob = user.FindFirstValue("DateOfBirth");
            return DateTime.TryParse(dob, out var result) ? result : (DateTime?)null;
        }

        public static string? GetAddress(this ClaimsPrincipal user)
        {
            return user.FindFirstValue("Address");
        }

        public static string? GetCity(this ClaimsPrincipal user)
        {
            return user.FindFirstValue("City");
        }

        public static string? GetState(this ClaimsPrincipal user)
        {
            return user.FindFirstValue("State");
        }

        public static string? GetCountry(this ClaimsPrincipal user)
        {
            return user.FindFirstValue("Country");
        }

        public static string? GetProfileImageUrl(this ClaimsPrincipal user)
        {
            return user.FindFirstValue("ProfileImageUrl");
        }

        public static DateTime? GetLastLoginAt(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue("LastLoginAt");
            return DateTime.TryParse(value, out var result) ? result : (DateTime?)null;
        }

        public static bool? IsEmailVerified(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue("IsEmailVerified");
            return value != null && bool.TryParse(value, out var result) ? result : (bool?)null;
        }
    }
}
