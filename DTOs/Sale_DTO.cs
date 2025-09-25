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

}
