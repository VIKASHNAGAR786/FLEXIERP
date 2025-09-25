using FLEXIERP.DTOs;
using FLEXIERP.MODELS;

namespace FLEXIERP.DataAccessLayer_Interfaces
{
    public interface ISaleRepo
    {
        #region Product By Barcode
        public Task<ProductByBarcode_DTO?> GetProductByBarcode(string barCode);
        #endregion

        #region make Sale
        public Task<int> InsertSaleAsync(Sale sale);
        #endregion

    }
}
