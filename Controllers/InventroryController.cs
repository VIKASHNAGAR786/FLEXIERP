using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.BusinessLayer;
using FLEXIERP.DataAccessLayer;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DTOs;
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

        [HttpGet("GetProductReportExcel")]
        public async Task<IActionResult> ExportProductReportExcel([FromQuery] PaginationFilter filter)
        {
            int? userid = User.GetUserId();
            if (userid == null)
                return Unauthorized("User ID not found in token.");
            // Call service to get Excel file bytes
            byte[] fileBytes = await inventoryService.GetProductReportExcel(filter);

            if (fileBytes == null || fileBytes.Length == 0)
                return NotFound("No data found for the given filter.");

            // Return as downloadable file
            string fileName = "ProductReport.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("GetSoldProductsList")]
        public async Task<ActionResult<IEnumerable<Product_DTO>>> GetSoldProductsList([FromQuery] PaginationFilter filter)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var addedCategory = await inventoryService.GetSoldProductsList(filter);
                return Ok(addedCategory);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }
        #endregion


        #region vendors / provider
        [HttpPost("AddProvider")]
        public async Task<ActionResult<int>> AddProvider([FromBody] ProviderModel provider)
        {
            if (provider == null)
                return BadRequest("Provider data is required.");
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");
                provider.CreatedBy = userid.Value;
                var addedProviderId = await inventoryService.AddProvider(provider);
                return Ok(addedProviderId);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetProviders")]
        public async Task<ActionResult<IEnumerable<Provider_DTO>>> GetProviders([FromQuery] PaginationFilter filter)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");
                var providers = await inventoryService.GetProviders(filter);
                return Ok(providers);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region Warehouse Work
        [HttpPost("AddWarehouse")]
        public async Task<ActionResult<int>> AddWarehouse([FromBody] WarehouseModel warehouse)
        {
            if (warehouse == null)
                return BadRequest("Warehouse data is required.");
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");
                warehouse.CreatedBy = userid.Value;
                var addedWarehouseId = await inventoryService.AddWarehouse(warehouse);
                return Ok(addedWarehouseId);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet("GetWarehouses")]
        public async Task<ActionResult<IEnumerable<Warehouse_DTO>>> GetWarehouses()
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");
                var warehouses = await inventoryService.GetWarehouses();
                return Ok(warehouses);
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
