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

        #region Get Sale
        public Task<List<Sale_DTO>> GetSalesAsync(PaginationFilter pagination);
        public Task<byte[]> GetSalesReportPdf(PaginationFilter filter);
        public Task<byte[]> GetSalesReportExcel(PaginationFilter filter);
        #endregion

        #region Old customer 
        public Task<List<OldCustomerDTO>> GetOldCustomersAsync(PaginationFilter pagination);
        #endregion

        #region Get Customer with sales 
        public Task<List<CustomerWithSalesDTO>> GetCustomersWithSalesAsync(PaginationFilter pagination);

        #endregion
    }
}
