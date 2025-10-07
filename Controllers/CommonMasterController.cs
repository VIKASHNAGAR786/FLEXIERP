using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.BusinessLayer;
using FLEXIERP.MODELS;
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
        [HttpGet("GenerateDashboardPdf")]
        public async Task<IActionResult> GenerateDashboardPdf([FromQuery] string startDate, string endDate)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var pdfBytes = await commonservice.GenerateDashboardPdf(startDate,endDate);
                return File(pdfBytes, "application/pdf", "Dashboard.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet("GenerateDashboardExcel")]
        public async Task<IActionResult> GenerateDashboardExcel([FromQuery] string startDate, string endDate)
        {
            int? userid = User.GetUserId();
            if (userid == null)
                return Unauthorized("User ID not found in token.");
            // Call service to get Excel file bytes
            byte[] fileBytes = await commonservice.GenerateDashboardExcel(startDate, endDate);

            if (fileBytes == null || fileBytes.Length == 0)
                return NotFound("No data found for the given filter.");

            // Return as downloadable file
            string fileName = "Dashboard.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        #endregion
    }
}
