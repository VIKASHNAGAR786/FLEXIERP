namespace FLEXIERP.MODELS
{
    public record ProductCategory_DTO
    {
        public int? CategoryID { get; set; }
        public string? CategoryName  { get; set; }
        public string? Description  { get; set; }
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



}
