using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.BusinessLayer;
using FLEXIERP.DTOs;
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

        #region cheque details 
        [HttpGet("GetReceivedChequesAsync")]
        public async Task<IActionResult> GetReceivedChequesAsync([FromQuery] PaginationFilter pagination)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var result = await commonservice.GetReceivedChequesAsync(pagination);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region notes
        [HttpPost("SaveNote")]
        public async Task<ActionResult<int>> SaveNote([FromBody] SaveNotes notes)
        {
            try
            {
                int? userid = User.GetUserId();
                notes.CreatedBy = (int)userid!;
                if (userid == null)
                    return Unauthorized("User ID not found in token.");
                var result = await commonservice.SaveNoteAsync(notes);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetAllNotes")]
        public async Task<IActionResult> GetAllNotes()
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var result = await commonservice.GetAllNotesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetNoteDetailsByIdAsync")]
        public async Task<IActionResult> GetNoteDetailsByIdAsync([FromQuery]int rowid)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var result = await commonservice.GetNoteDetailsByIdAsync(rowid);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("DeleteNotesById")]
        public async Task<IActionResult> DeleteNotesById([FromQuery] int deletednotsid)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var result = await commonservice.DeleteNotesById(deletednotsid);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPut("MarkPinned")]
        public async Task<IActionResult> MarkPinned([FromQuery] int notesid)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var result = await commonservice.MarkPinned(notesid);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region Bank Accounts
        [HttpPost("SaveCompanyBankAccounts")]
        public async Task<ActionResult<int>> SaveCompanyBankAccounts([FromBody] SaveCompanyBankAccounts bankAccounts)
        {
            try
            {
                int? userid = User.GetUserId();
                bankAccounts.CreateBy = (int)userid!;
                if (userid == null)
                    return Unauthorized("User ID not found in token.");
                var result = await commonservice.SaveCompanyBankAccounts(bankAccounts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetCompanyBankAccounts")]
        public async Task<IActionResult> GetCompanyBankAccounts()
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                IEnumerable<CompanyBankAccountDto> result = await commonservice.GetCompanyBankAccounts();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region Formate Editor
        [HttpGet("GetTemplates")]
        public async Task<IActionResult> GetTemplates()
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                List<TemplateOption> result = await commonservice.GetTemplates();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("SaveTemplateAsync")]
        public async Task<ActionResult<int>> SaveTemplateAsync([FromBody] SaveTemplate template)
        {
            try
            {
                int? userid = User.GetUserId();
                template.createdby = (int)userid!;
                template.updatedby = (int)userid!;

                if (userid == null)
                    return Unauthorized("User ID not found in token.");
                var result = await commonservice.SaveTemplateAsync(template);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetTemplateAsync")]
        public async Task<IActionResult> GetTemplateAsync([FromQuery] int categorid, int isdefault)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var result = await commonservice.GetTemplateAsync(categorid, isdefault);
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
