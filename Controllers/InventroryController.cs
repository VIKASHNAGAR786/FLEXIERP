using FLEXIERP.BusinesLayer_Interfaces;
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
    }
}
