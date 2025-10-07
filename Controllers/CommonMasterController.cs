using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.BusinessLayer;
using FLEXIERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace FLEXIERP.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CommonMasterController : ControllerBase
    {
        private readonly ICommonMasterService commonservice;
        public CommonMasterController(ICommonMasterService _commonservice)
        {
            commonservice = _commonservice;
        }

        #region DashBoard
        [HttpGet("GetDashboardMetricsAsync")]
        public async Task<IActionResult> GetDashboardMetricsAsync([FromQuery] string startDate, string endDate)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var result = await commonservice.GetDashboardMetricsAsync(startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion
    }
}
