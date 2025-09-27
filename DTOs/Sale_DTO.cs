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
        public decimal TotalItems { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
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


}
