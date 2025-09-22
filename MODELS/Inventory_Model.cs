namespace FLEXIERP.MODELS
{
    public record Product_Category
    {
        public required string CategoryName { get; set; }
        public string? Description { get; set; }
        public int? CreatedBy { get; set; }
        
    }
}
