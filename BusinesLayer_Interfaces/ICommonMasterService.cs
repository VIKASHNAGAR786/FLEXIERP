using FLEXIERP.DTOs;

namespace FLEXIERP.BusinesLayer_Interfaces
{
    public interface ICommonMasterService
    {
        public Task<DashboardMetricsDto?> GetDashboardMetricsAsync(string startDate, string endDate);
        public Task<byte[]> GenerateDashboardPdf(string startDate, string endDate);
        public Task<byte[]> GenerateDashboardExcel(string startDate, string endDate);

    }
}
