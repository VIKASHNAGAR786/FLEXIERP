using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.MODELS;
using FLEXIERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FLEXIERP.Controllers
{

    [Route("[controller]")]
    [ApiController]
    public class InventroryController : ControllerBase
    {
        private readonly IInventoryService inventoryService;
        public InventroryController(IInventoryService _inventoryService)
        {
            inventoryService = _inventoryService;
        }

        #region Product /category
        [HttpPost]
        [Route("AddCategory")]
        public async Task<ActionResult<Product_Category>> AddCategory([FromBody] Product_Category category)
        {
            if (category == null)
                return BadRequest("Category data is required.");

            try
            {
                int? userid = User.GetUserId();
                if(userid == null)
                    return Unauthorized("User ID not found in token.");
                category.CreatedBy = userid.Value;

                var addedCategory = await inventoryService.AddCategory(category);
                return Ok(addedCategory);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet]
        [Route("GetCategories")]
        public async Task<ActionResult<IEnumerable<Product_Category>>> GetCategories()
        {
            try
            {
                var categories = await inventoryService.GetCategories();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region Save Product
        [AllowAnonymous]
        [HttpPost("AddProduct")]
        public async Task<ActionResult<Product_Category>> AddProduct([FromBody] ProductModel productModel)
        {
            if (productModel == null)
                return BadRequest("Category data is required.");

            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");
                productModel.CreatedBy = userid.Value;

                var addedCategory = await inventoryService.AddProduct(productModel);
                return Ok(addedCategory);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region Get Products
        [AllowAnonymous]
        [HttpGet("GetProducts")]
        public async Task<ActionResult<IEnumerable<Product_DTO>>> GetProducts([FromQuery] PaginationFilter filter)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var addedCategory = await inventoryService.GetProducts(filter);
                return Ok(addedCategory);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetProductReportPdf")]
        public async Task<IActionResult> GetProductReportPdf([FromQuery]PaginationFilter filter)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var pdfBytes = await inventoryService.GetProductReportPdf(filter);
                return File(pdfBytes, "application/pdf", "SampleReport.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion


    }
}
