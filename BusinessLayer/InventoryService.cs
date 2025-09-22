using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.MODELS;

namespace FLEXIERP.BusinessLayer
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepo inventoryRepo;
        public InventoryService(IInventoryRepo _inventoryRepo) 
        {
            this.inventoryRepo = _inventoryRepo;
        }

        public async Task<int> AddCategory(Product_Category product_Category)
        {
            return await inventoryRepo.AddCategory(product_Category);
        }
        public async Task<IEnumerable<ProductCategory_DTO>> GetCategories()
        {
            return await inventoryRepo.GetCategories();
        }
    }
}
