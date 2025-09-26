using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;

namespace FLEXIERP.BusinessLayer
{
    public class SaleService : ISaleService
    {
        private readonly ISaleRepo _saleRepo;
        public SaleService(ISaleRepo saleRepo)
        {
            _saleRepo = saleRepo;
        }

        #region Product By Barcode
        public async Task<ProductByBarcode_DTO?> GetProductByBarcode(string barCode)
        {
            return await _saleRepo.GetProductByBarcode(barCode);
        }
        #endregion

        #region make Sale
        public async Task<int> InsertSaleAsync(Sale sale)
        {
            return await _saleRepo.InsertSaleAsync(sale);
        }
        #endregion

        #region Get Sale
        public async Task<List<Sale_DTO>> GetSalesAsync(PaginationFilter pagination)
        {
            return await _saleRepo.GetSalesAsync(pagination);
        }
        #endregion

        #region Old customer 
        public Task<List<OldCustomerDTO>> GetOldCustomersAsync(PaginationFilter pagination)
        {
            return _saleRepo.GetOldCustomersAsync(pagination);
        }
        #endregion

        #region Get Customer with sales 
        public  async Task<List<CustomerWithSalesDTO>> GetCustomersWithSalesAsync(PaginationFilter pagination)
        {
            return await _saleRepo.GetCustomersWithSalesAsync(pagination);
        }
        #endregion
    }
}
