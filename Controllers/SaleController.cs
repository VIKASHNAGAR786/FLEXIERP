using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.BusinessLayer;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;
using FLEXIERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FLEXIERP.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SaleController : ControllerBase
    {
        private readonly ISaleService _saleService;

        public SaleController(ISaleService saleService)
        {
            _saleService = saleService;
        }
        #region Product By Barcode
        [AllowAnonymous]
        [HttpGet("GetProductByBarcode")]
        public async Task<ActionResult<ProductByBarcode_DTO?>> GetProductByBarcode([FromQuery] string barcode)
        {
            try
            {
                int? userid = 1;//User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var productbybarcode = await _saleService.GetProductByBarcode(barcode);
                return Ok(productbybarcode);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region make Sale
        [AllowAnonymous]
        [HttpPost("InsertSale")]
        public async Task<ActionResult<int>> InsertSale([FromBody] Sale sale)
        {
            try
            {
                int? userid = User.GetUserId();
                sale.CreatedBy = userid;
                if (userid == null)
                    return Unauthorized("User ID not found in token.");
                var result = await _saleService.InsertSaleAsync(sale);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region Get Sale
        [HttpGet("GetSales")]
        public async Task<ActionResult<List<Sale_DTO>>> GetSales([FromQuery] PaginationFilter pagination)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");
                var sales = await _saleService.GetSalesAsync(pagination);
                return Ok(sales);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet("GetSalesReportPdf")]
        public async Task<IActionResult> GetSalesReportPdf([FromQuery] PaginationFilter filter)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var pdfBytes = await _saleService.GetSalesReportPdf(filter, (int)userid);
                return File(pdfBytes, "application/pdf", "SampleReport.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetSalesReportExcel")]
        public async Task<IActionResult> GetSalesReportExcel([FromQuery] PaginationFilter filter)
        {
            int? userid = User.GetUserId();
            if (userid == null)
                return Unauthorized("User ID not found in token.");
            // Call service to get Excel file bytes
            byte[] fileBytes = await _saleService.GetSalesReportExcel(filter, (int)userid);

            if (fileBytes == null || fileBytes.Length == 0)
                return NotFound("No data found for the given filter.");

            // Return as downloadable file
            string fileName = "GetSalesReport.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        #endregion

        #region Old customer 
        [AllowAnonymous]
        [HttpGet("GetOldCustomers")]
        public async Task<ActionResult<List<OldCustomerDTO>>> GetOldCustomers([FromQuery] PaginationFilter pagination)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");
                var oldCustomers = await _saleService.GetOldCustomersAsync(pagination);
                return Ok(oldCustomers);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region Get Customer with sales 
        [HttpGet("GetCustomersWithSales")]
        public async Task<ActionResult<List<CustomerWithSalesDTO>>> GetCustomersWithSales([FromQuery] PaginationFilter pagination)
        {
            try
            {
                int? userid = 1;//User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");
                var customersWithSales = await _saleService.GetCustomersWithSalesAsync(pagination);
                return Ok(customersWithSales);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region Invoice bill
        [HttpGet("GetReceiptPdf")]
        public async Task<IActionResult> GetReceiptPdf([FromQuery] int saleid)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var pdfBytes = await _saleService.GetReceiptPdf(saleid, (int)userid);
                return File(pdfBytes, "application/pdf", "GetReceiptPdf.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion
    }
}
