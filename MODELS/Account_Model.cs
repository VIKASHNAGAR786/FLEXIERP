namespace FLEXIERP.MODELS
{
    using DocumentFormat.OpenXml.Drawing.Spreadsheet;
    using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
    using DocumentFormat.OpenXml.Spreadsheet;
    using Microsoft.AspNetCore.Mvc;
    using System.ComponentModel.DataAnnotations;

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

        public record CompanyInfoDto
        {
            public int ComInfoId { get; init; }
            public string CompanyName { get; init; }
            public string ContactNo { get; init; }
            public string WhatsAppNo { get; init; }
            public string Email { get; init; }
            public string Address { get; init; }
            public string FullName { get; init; }
            public string CreatedDate { get; init; }
            public string CompanyLogo { get; init; }
        }
        public record UpdateCompanyInfo
        {
            public string? Company_Name { get; set; }
            public string? Contact_No { get; set; }
            public string? WhatsApp_No { get; set; }
            public string? Email { get; set; }
            public string? Address { get; set; }
            public int? row_id { get; set; }
            public int? UpdatedBy { get; set; }
            public string? CompanyLogo { get; set; }
            public IFormFile? file { get; set; }
        }

        public class Customerledgermodel
        {
            public decimal? paidamount { get; set; }
            public decimal? balancedue { get; set; }
            public decimal Totalamount { get; set; }
            public int paymentmode { get; set; }
            public string? transactiontype { get; set; }
            public int createby { get; set; }
        }

        public record CustomerledgerDto
        {
            public int customerid { get; set; }
            public string? Customername { get; set; }
            public string? ContactNo { get; set; }
            public string? Email { get; set; }
            public string? CustomerAddress { get; set; }
            public decimal totalamount { get; set; }
            public decimal totaldue { get; set; }
            public string? lasttransactiondate { get; set; }
            public int? rowid { get; set; }
        }

        public record CustomerledgerdetailDto
        {
            public int customerid { get; set; }
            public decimal paidamt { get; set; }
            public string? transactiontype { get; set; }
            public decimal totalamount { get; set; }
            public decimal balancedue { get; set; }
            public string? transactiondate { get; set; }
            public string? saledate { get; set; }
            public decimal totalitems { get; set; }
            public decimal totaldiscount { get; set; }
            public int paymentmode { get; set; }
            public int? rowid { get; set; }
            public decimal? tax { get; set; }
            public string? customername { get; set; }
            public string? contactno { get; set; }
        }

    }

}
