namespace FLEXIERP.MODELS
{
    public record Customer
    {
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string PhoneNo { get; set; }
        public string Email { get; set; }
        public int? PaymentMode { get; set; }
    }
    public class SaleDetail
    {
        public int ProductID { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class Sale
    {
        public int? CustomerID { get; set; } // Nullable if new customer
        public Customer Customer { get; set; }  // Optional
        public decimal TotalItems { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public DateTime? OrderDate { get; set; }
        public int? CreatedBy { get; set; }
        public List<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
    }



}
