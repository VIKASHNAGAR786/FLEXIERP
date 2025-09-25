using FLEXIERP.DTOs;
using FLEXIERP.MODELS;

namespace FLEXIERP.BusinesLayer_Interfaces
{
    public interface ISaleService
    {
        #region Product By Barcode
        public Task<ProductByBarcode_DTO?> GetProductByBarcode(string barCode);
        #endregion

        #region make Sale
        public Task<int> InsertSaleAsync(Sale sale);
        #endregion
    }
}
