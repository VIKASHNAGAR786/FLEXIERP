using FLEXIERP.DTOs;
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
        public Task<byte[]> GetProductReportPdf(PaginationFilter filter);
        public Task<byte[]> GetProductReportExcel(PaginationFilter filter);
        #endregion


        #region vendors / provider
        public Task<int> AddProvider(ProviderModel provider);
        public Task<IEnumerable<Provider_DTO>> GetProviders(PaginationFilter filter);
        #endregion
    }
}
