using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.MODELS;
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
    }
}
