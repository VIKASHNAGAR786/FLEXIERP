using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.BusinessLayer;
using FLEXIERP.MODELS.AGRIMANDI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FLEXIERP.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class VersionUpdateController : ControllerBase
    {
        private readonly IVersionUpdateService versionupdate;
        public VersionUpdateController(IVersionUpdateService _versionupdate)
        {
            versionupdate = _versionupdate;
        }

        [HttpPost("VersionUpdate")]
        public async Task<IActionResult> VersionUpdate([FromBody] string version)
        {
            
            if (String.IsNullOrEmpty(version))
            {
                return BadRequest(new { message = "version no is empty" });
            }

            int result = await versionupdate.VersionUpdate(version);
            if(result > 0)
            {
                return Ok(new { message = "Version updated successfully", version = version });
            }
            return BadRequest(new { message = "User login unsuccessful" });
        }
    }
}
