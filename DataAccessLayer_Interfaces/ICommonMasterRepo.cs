using FLEXIERP.DTOs;
using FLEXIERP.MODELS;

namespace FLEXIERP.DataAccessLayer_Interfaces
{
    public interface ICommonMasterRepo
    {
        public Task<int> SaveChequePaymentAsync(SaveChequePaymentDto chequePayment);
        public Task<int> SaveCashPaymentAsync(SaveCashPaymentDto cashPayment);
        public Task<DashboardMetricsDto?> GetDashboardMetricsAsync(string startDate, string endDate);
        public Task<int> SaveUserErrorLogAsync(UserErrorLogDto errorLog);

        #region cheque details 
        public Task<List<ReceivedChequeDto>> GetReceivedChequesAsync(PaginationFilter pagination);
        #endregion
    }
}
