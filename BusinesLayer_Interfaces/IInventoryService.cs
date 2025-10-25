using FLEXIERP.DTOs;
using FLEXIERP.MODELS;

namespace FLEXIERP.BusinesLayer_Interfaces
{
    public interface IInventoryService
    {
        public Task<int> AddCategory(Product_Category product_Category);
        public Task<IEnumerable<ProductCategory_DTO>> GetCategories();
        Task<IEnumerable<ProductCategoryListDto>> GetProductCategoryListAsync(CancellationToken cancellationToken = default);

        #region Save prodcut
        public Task<string> AddProduct(ProductModel product);
        public Task<IEnumerable<Product_DTO>> GetProducts(PaginationFilter filter);
        public Task<byte[]> GetProductReportPdf(PaginationFilter filter, int userid);
        public Task<byte[]> GetProductReportExcel(PaginationFilter filter, int userid);
        public Task<IEnumerable<Product_DTO>> GetSoldProductsList(PaginationFilter filter);
        public Task<byte[]> GetSoldProductReportPdf(PaginationFilter filter, int userid);
        public Task<byte[]> GetSoldProductReportExcel(PaginationFilter filter, int userid);
        #endregion


        #region vendors / provider
        public Task<int> AddProvider(ProviderModel provider);
        public Task<IEnumerable<Provider_DTO>> GetProviders(PaginationFilter filter);
        #endregion

        #region Warehouse Work
        public Task<int> AddWarehouse(WarehouseModel warehouse);
        public Task<IEnumerable<Warehouse_DTO>> GetWarehouses();
        #endregion
    }
}
