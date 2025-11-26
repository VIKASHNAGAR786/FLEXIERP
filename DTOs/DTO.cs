namespace FLEXIERP.DTOs
{
    public record ProductByBarcode_DTO
    {
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string BarCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public DateTime? PackedDate { get; set; }
        public int? PackedWeight { get; set; }
        public int? PackedHeight { get; set; }
        public int? PackedDepth { get; set; }
        public int? PackedWidth { get; set; }
        public bool? IsPerishable { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? SellingPrice { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? Discount { get; set; }
        public decimal? availableQuantity { get; set; }
    }

    public class Sale_DTO
    {
        public int SrNo { get; set; }
        public int SaleID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public DateTime OrderDate { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public decimal extracharges { get; set; }
    }

    public class OldCustomerDTO
    {
        public long SrNo { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
    }

    public class CustomerWithSalesDTO
    {
        public long SrNo { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
    }

    public record ProviderModel
    {
        public required string ProviderName { get; set; }
        public required string ProviderType { get; set; }
        public string ContactPerson { get; set; }
        public required string ContactEmail { get; set; }
        public required string ContactPhone { get; set; }
        public required string ProviderAddress { get; set; }
        public required string City { get; set; }
        public required string State { get; set; }
        public required string Country { get; set; }
        public string PaymentTerms { get; set; }
        public int CreatedBy { get; set; }

    }

    public record Provider_DTO
    {
        public long? SrNo { get; set; }
        public int ProviderID { get; set; }
        public string? ProviderName { get; set; }
        public string? ProviderType { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? ProviderAddress { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PaymentTerms { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedByName { get; set; }
        public int TotalRows { get; set; }
    }
    public class ReceiptCustomerDTO
    {
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PaymentMode { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public decimal TotalItems { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal paidamt { get; set; }
        public decimal baldue { get; set; }
        public string? invoiceno { get; set; }
    }

    public class ReceiptDetailDTO
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal Tax { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class extrachargesDTO
    {
        public string chargename { get; set; } = string.Empty;
        public decimal chargeamount { get; set; }
        public string? createby { get; set; }
        public string? createdate { get; set; }
    }

    public class ReceiptDTO
    {
        public ReceiptCustomerDTO? CustomerInfo { get; set; }
        public List<ReceiptDetailDTO> SaleDetails { get; set; } = new List<ReceiptDetailDTO>();
        public List<extrachargesDTO> extracharges { get; set; } = new List<extrachargesDTO>();
    }

    public record ProductCategory_DTO
    {
        public int? CategoryID { get; set; }
        public string? CategoryName { get; set; }
        public string? Description { get; set; }
    }
    public class Product_DTO
    {
        public int? ProductID { get; set; }
        public string? ProductCode { get; set; }
        public string? BarCode { get; set; }
        public string? ProductName { get; set; }
        public string? CategoryName { get; set; }
        public string? ProductType { get; set; }
        public DateTime? PackedDate { get; set; }
        public int? PackedWeight { get; set; }
        public int? PackedHeight { get; set; }
        public int? PackedDepth { get; set; }
        public int? PackedWidth { get; set; }
        public bool? IsPerishable { get; set; }
        public DateTime? CreatedDate { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? SellingPrice { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? Discount { get; set; }
        public string? FullName { get; set; }
        public int? TotalRecords { get; set; }
        public decimal? taxpr { get; set; }
        public decimal? discounpr { get; set; }
        public DateTime? solddate { get; set; }
        public decimal? soldquantity { get; set; }
        public decimal? availablequantity { get; set; }
    }
    public class Warehouse_DTO
    {
        public int? WarehouseID { get; set; }
        public string? WarehouseName { get; set; }
        public bool? IsRefrigerated { get; set; }
        public int? CreatedBy { get; set; }
        public string? Remark { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class DashboardMetricsDto
    {
        public decimal TotalCashReceived { get; set; }
        public decimal TotalChequeReceived { get; set; }
        public decimal? CashGrowthPercent { get; set; }
        public decimal? ChequeGrowthPercent { get; set; }
        public decimal? TotalBalanceDue { get; set; }
        public List<TransactionDto?>? recenttransaction { get; set; }
    }
    public class TransactionDto
    {
        public string Date { get; set; } = "";
        public string Time { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public decimal ReceivedAmount { get; set; }
        public decimal BalanceDue { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentType { get; set; } = "";
        public string TransactionType { get; set; } = "";
    }

    public class ReceivedChequeDto
    {
        public long SrNo { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string ChequeNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string ChequeDate { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string ifsc_Code { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
    }

    public class NoteDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string AuthorId { get; set; }
        public bool IsPinned { get; set; }
        public bool IsArchived { get; set; }
        public string CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public bool Status { get; set; }
    }

    public class NoteDetailsDto
    {
        public string title { get; set; }
        public string content { get; set; }
        public string createdat { get; set; }       // AM/PM formatted string
        public string? updatedat { get; set; }      // optional
        public string authorname { get; set; }
        public bool ispinned { get; set; }
        public bool isarchived { get; set; }
        public string createdbyname { get; set; }
        public string updatedbyname { get; set; }
        public bool status { get; set; }
    }

    public class BalanceDueDto
    {
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? PhoneNo { get; set; }
        public string? Email { get; set; }
        public decimal TotalDueAmount { get; set; }
        public int DueId { get; set; }
        public string? LastTransactionDate { get; set; }
        public int? totalrecords { get; set; }
    }

    public class CompanyBankAccountDto
    {
        public int company_bank_id { get; set; }
        public string? account_name { get; set; }
        public string? bank_name { get; set; }
        public string? account_number { get; set; }
        public string? ifsc_code { get; set; }
        public string? branch_name { get; set; }
        public string? account_type { get; set; }
        public int created_by { get; set; }
        public string? created_by_name { get; set; }
        public int status { get; set; }
        public string? created_at { get; set; }
    }
}
