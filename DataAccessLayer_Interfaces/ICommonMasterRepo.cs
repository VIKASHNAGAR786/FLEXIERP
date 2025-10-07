using FLEXIERP.DTOs;
using FLEXIERP.MODELS;

namespace FLEXIERP.DataAccessLayer_Interfaces
{
    public interface ICommonMasterRepo
    {
        public Task<int> SaveChequePaymentAsync(SaveChequePaymentDto chequePayment);
        public Task<int> SaveCashPaymentAsync(SaveCashPaymentDto cashPayment);
        public Task<DashboardMetricsDto?> GetDashboardMetricsAsync(string startDate, string endDate);
    }
}
