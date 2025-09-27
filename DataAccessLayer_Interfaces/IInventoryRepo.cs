using FLEXIERP.DTOs;
using FLEXIERP.MODELS;

namespace FLEXIERP.DataAccessLayer_Interfaces
{
    public interface IInventoryRepo 
    {
        public Task<int> AddCategory(Product_Category product_Category);
        public Task<IEnumerable<ProductCategory_DTO>> GetCategories(bool onlyActive = false);

        #region save product
        public Task<string> AddProduct(ProductModel product);
        public Task<IEnumerable<Product_DTO>> GetProducts(PaginationFilter filter);
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
