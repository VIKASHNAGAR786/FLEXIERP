using FLEXIERP.MODELS;

namespace FLEXIERP.BusinesLayer_Interfaces
{
    public interface IInventoryService
    {
        public Task<int> AddCategory(Product_Category product_Category);
        public Task<IEnumerable<ProductCategory_DTO>> GetCategories();

        #region Save prodcut
        public Task<string> AddProduct(ProductModel product);
        public Task<IEnumerable<Product_DTO>> GetProducts(PaginationFilter filter);
        #endregion

        public byte[] GenerateSamplePdf();
    }
}
