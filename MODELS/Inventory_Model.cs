namespace FLEXIERP.MODELS
{
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


}
