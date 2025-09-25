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
    }
}
