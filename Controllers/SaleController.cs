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
                int? userid = 2;//User.GetUserId();
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
    }
}
