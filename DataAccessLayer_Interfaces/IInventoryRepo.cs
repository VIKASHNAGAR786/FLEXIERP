using FLEXIERP.MODELS;

namespace FLEXIERP.DataAccessLayer_Interfaces
{
    public interface IInventoryRepo 
    {
        public Task<int> AddCategory(Product_Category product_Category);
        public Task<IEnumerable<ProductCategory_DTO>> GetCategories(bool onlyActive = false);
    }
}
