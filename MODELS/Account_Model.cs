namespace FLEXIERP.MODELS
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Mvc;

    namespace AGRIMANDI.Model
    {
        public class User1
        {
            public required string FullName { get; set; }
            public required string Username { get; set; }
            public required string Email { get; set; }
            public required string PasswordHash { get; set; }
            public required string MobileNo { get; set; }
            public string? Gender { get; set; }
            public DateTime DateOfBirth { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
            public string? State { get; set; }
            public string? Country { get; set; }
            public string? ProfileImageUrl { get; set; }
            public int RoleID { get; set; }
            public DateTime LastLoginAt { get; set; }
            public bool? IsActive { get; set; }
            public bool? IsEmailVerified { get; set; }
            public string? Token { get; set; } = "";
            public int Id { get; set; } = 0;
            
        }

        public class LoginUser
        {
            public string? Email { get; set; }
            public required string Password { get; set; }
            public string? UserName { get; set; }
        }

        public class RegisterUser
        {
            public required string FullName { get; set; }
            public required string Username { get; set; }
            public required string Email { get; set; }
            public required string PasswordHash { get; set; }
            public required string MobileNo { get; set; }
            public string? Gender { get; set; }
            public DateTime DateOfBirth { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
            public string? State { get; set; }
            public string? Country { get; set; }
            public string? ProfileImageUrl { get; set; }
            public int RoleID { get; set; }
            public DateTime LastLoginAt { get; set; }
            public bool? IsActive { get; set; }
            public bool? IsEmailVerified { get; set; }
        }

        public class ProfileImageUploadDto
        {
            [FromForm(Name = "file")]
            public IFormFile File { get; set; }

            [FromForm(Name = "userid")]
            public int UserId { get; set; }
        }

        public record StartUserSession
        {
            public string Username { get; set; } = string.Empty;

            public string Password { get; set; } = string.Empty;

            public string? IPAddress { get; set; }  

            public string? DeviceInfo { get; set; } 
        }

        public class UserLoginHistoryDTO
        {
            public int HistoryID { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string LoginTime { get; set; } = string.Empty;
            public string LogoutTime { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string IPAddress { get; set; } = string.Empty;
            public string DeviceInfo { get; set; } = string.Empty;
            public string FailureReason { get; set; } = string.Empty;
            public int TotalRecords { get; set; }
        }

    }

}
