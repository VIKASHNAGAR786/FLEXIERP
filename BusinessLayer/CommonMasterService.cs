using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DTOs;

namespace FLEXIERP.BusinessLayer
{
    public class CommonMasterService : ICommonMasterService
    {
        private readonly ICommonMasterRepo commonmaster;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CommonMasterService(ICommonMasterRepo commonmaster, IHttpContextAccessor httpContextAccessor)
        {
            this.commonmaster = commonmaster;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<DashboardMetricsDto?> GetDashboardMetricsAsync(string startDate, string endDate)
        {
            return await this.commonmaster.GetDashboardMetricsAsync(startDate, endDate);
        }

    }
}
