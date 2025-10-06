using Microsoft.AspNetCore.Mvc;

namespace FLEXIERP.MODELS
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

    public record Product_Category
    {
        public required string CategoryName { get; set; }
        public string? Description { get; set; }
        public int? CreatedBy { get; set; }

    }

    public class ProductModel
    {
        public string ProductName { get; set; }
        public int ProductCategory { get; set; }
        public string? ProductType { get; set; }
        public DateTime? PackedDate { get; set; }
        public int? PackedWeight { get; set; }
        public int? PackedHeight { get; set; }
        public int? PackedDepth { get; set; }
        public int? PackedWidth { get; set; }
        public bool? IsPerishable { get; set; }
        public int CreatedBy { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? SellingPrice { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? Discount { get; set; }
        public decimal? taxpr { get; set; }
        public decimal? discounpr { get; set; }
        public required decimal productQunatity { get; set; }
    }

    public class PaginationFilter
    {
        public string StartDate { get; set; } = DateTime.UtcNow.ToString();
        public string EndDate { get; set; } = DateTime.UtcNow.ToString();
        public string? SearchTerm { get; set; }
        public int PageNo { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class WarehouseModel
    {
        public string WarehouseName { get; set; }
        public bool IsRefrigerated { get; set; }
        public int CreatedBy { get; set; }
        public string Remark { get; set; }
    }

    public record Customer
    {
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string PhoneNo { get; set; }
        public string Email { get; set; }
        public int? PaymentMode { get; set; }
        public string? Remark { get; set; }

        // customer ledger
        public decimal? PaidAmt { get; set; }         // @paid_amt
        public decimal? BalanceDue { get; set; }      // @balance_due
        public decimal? TotalAmt { get; set; }        // @total_amt
        public string? TransactionType { get; set; } = "SALE"; // @transaction_type
        public int payid { get; set; } = 1;        // @payid
    }
    public class SaleDetail
    {
        public int ProductID { get; set; }
        public decimal productquantity { get; set; }
    }

    public class Sale
    {
        public int? customerID { get; set; } // Nullable if new customer
        public Customer? Customer { get; set; }  // Optional
        public decimal TotalItems { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public DateTime? OrderDate { get; set; }
        public int? CreatedBy { get; set; }
        public List<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
    }

    public class ProductCategoryListDto
    {
        public int SrNo { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
