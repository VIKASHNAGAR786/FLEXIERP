using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.MODELS;
using FLEXIERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace FLEXIERP.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BackupController : ControllerBase
    {
        private readonly IBackupService backupservice;
        private readonly ICommonMasterRepo commonMasterRepo;
        public BackupController(IBackupService _backupservice, ICommonMasterRepo _common)
        {
            backupservice = _backupservice;
            commonMasterRepo = _common;
        }

        [HttpPost("backup")]
        public async Task<IActionResult> BackupDatabase([FromBody] BackupRequest request)
        {
            try
            {
                // Validate input
                int userId = (int)User.GetUserId();

                if(string.IsNullOrWhiteSpace(request.BackupFolderPath))
                    return BadRequest("Invalid backup folder path.");
                if(userId <= 0)
                    return BadRequest("Invalid user.");

                int result = await backupservice.BackupDatabaseAsync(request);

                if (result == 0)
                {
                    return BadRequest("Invalid backup folder path.");
                }
                else if (result == -1)
                {
                    return NotFound("Database file not found.");
                }
                else if (result == -2)
                {
                    return StatusCode(500, "An error occurred during backup.");
                }

                return Ok(new { message = "Backup successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Backup failed: " + ex.Message);
            }
        }


    }
}
