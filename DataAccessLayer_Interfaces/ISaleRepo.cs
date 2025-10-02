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

        #region Get Sale
        public Task<List<Sale_DTO>> GetSalesAsync(PaginationFilter pagination);
        #endregion

        #region Old customer 
        public Task<List<OldCustomerDTO>> GetOldCustomersAsync(PaginationFilter pagination);
        #endregion

        #region Get Customer with sales 
        public Task<List<CustomerWithSalesDTO>> GetCustomersWithSalesAsync(PaginationFilter pagination);

        #endregion

        #region Sale Invoice
        public Task<ReceiptDTO?> GetReceiptDetail(int saleId); 
        #endregion

    }
}
