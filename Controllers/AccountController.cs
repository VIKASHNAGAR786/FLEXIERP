using crypto;
using DocumentFormat.OpenXml.Wordprocessing;
using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.BusinessLayer;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;
using FLEXIERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Razorpay.Api.Errors;
using System.Diagnostics;
using System.Reflection;

namespace FLEXIERP.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountServices accouuntservice;
        private readonly ICommonMasterRepo commonMasterRepo;
        public AccountController(IAccountServices _accouuntservice, ICommonMasterRepo _common)
        {
            accouuntservice = _accouuntservice;
            commonMasterRepo = _common;
        }

        #region Accounts Operations
        // POST: auth/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUser user)
        {
            try
            {
                if (string.IsNullOrEmpty(user.Email))
                    return BadRequest(new { message = "Email address needs to be entered" });

                if (string.IsNullOrEmpty(user.Password))
                    return BadRequest(new { message = "Password needs to be entered" });

                User1? loggedInUser = await accouuntservice.Login(user.Email, user.UserName, user.Password);

                if (loggedInUser != null)
                    return Ok(loggedInUser);

                return BadRequest(new { message = "Invalid login credentials" });
            }
            catch (Exception ex)
            {
                // Only log if the exception is NOT from AccountService
                if (ex.TargetSite?.DeclaringType?.Namespace != "FLEXIERP.DataAccessLayer")
                {
                    await commonMasterRepo.SaveUserErrorLogAsync(new UserErrorLogDto
                    {
                        Module = "Account",
                        ActionType = "Login",
                        ErrorMessage = ex.Message,
                        ErrorCode = ex.HResult.ToString(),
                        StackTrace = ex.StackTrace,
                        ApiName = "Login",
                        Severity = "High",
                        AdditionalInfo = $"{ex.InnerException?.Message ?? string.Empty}, An unexpected error occurred in login"
                    });
                }

                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
            }
        }

        [HttpPatch("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                int? user = User.GetUserId();
                int userid = user.Value;
                bool result = false;
                if (userid != 0)
                {
                    result = await accouuntservice.LogoutUser(userid);
                }
                else
                {
                    return BadRequest(new { message = "No active session found to logout." });
                }
                   

                if (result)
                {
                    return Ok(new { message = "User logged out successfully." });
                }

                return BadRequest(new { message = "No active session found to logout." });
            }
            catch (Exception ex)
            {
                // Log the error (e.g., ILogger)
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred.",
                    details = ex.Message
                });
            }
        }

        [HttpGet("GetUserLoginHistory")]
        public async Task<IActionResult> GetUserLoginHistory([FromQuery]PaginationFilter pagination)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var history = await accouuntservice.GetUserLoginHistory(pagination.PageNo, pagination.PageSize);

                if (history == null || !history.Any())
                    return NotFound(new { message = "No login history found." });

                return Ok(history);
            }
            catch (Exception ex)
            {
                // You can log ex.Message here using ILogger
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving login history.",
                    details = ex.Message
                });
            }
        }

        // POST: auth/register
        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterUser user)
        {
            try
            {
                // Input validation
                if (string.IsNullOrEmpty(user.FullName))
                    return BadRequest(new { message = "Name needs to be entered" });

                if (string.IsNullOrEmpty(user.Username))
                    return BadRequest(new { message = "Username needs to be entered" });

                if (string.IsNullOrEmpty(user.PasswordHash))
                    return BadRequest(new { message = "Password needs to be entered" });

                // Prepare user object for registration
                User1 userToRegister = new User1
                {
                    FullName = user.FullName,
                    Username = user.Username,
                    Email = user.Email,
                    PasswordHash = user.PasswordHash,
                    MobileNo = user.MobileNo,
                    Gender = user.Gender,
                    DateOfBirth = user.DateOfBirth,
                    Address = user.Address,
                    City = user.City,
                    State = user.State,
                    Country = user.Country,
                    ProfileImageUrl = user.ProfileImageUrl,
                    RoleID = user.RoleID,
                    LastLoginAt = DateTime.UtcNow,
                    IsActive = true,
                    IsEmailVerified = false
                };

                // Register the user
                User1 registeredUser = await accouuntservice.Register(userToRegister);

                // Automatically log in after registration
                User1? loggedInUser = await accouuntservice.Login(
                    registeredUser.Email,
                    registeredUser.Username,
                    user.PasswordHash
                );

                if (loggedInUser != null)
                    return Ok(loggedInUser);

                return BadRequest(new { message = "User registration unsuccessful" });
            }
            catch (Exception ex)
            {
                // Return safe error message to client
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred while registering the user.",
                    detail = ex.Message
                });
            }
        }

        #endregion

        #region Company Info

        [HttpGet("GetCompanyInfo")]
        public async Task<IActionResult> GetCompanyInfo()
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                CompanyInfoDto? data = await accouuntservice.GetCompanyInfoByUserAsync((int)userid);
                return Ok(data);
            }
            catch (Exception ex)
            {
                // You can log ex.Message here using ILogger
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving company data.",
                    details = ex.Message
                });
            }
        }
       
        [HttpPost("UpdateCompanyInfo")]
        public async Task<IActionResult> UpdateCompanyInfo([FromForm] UpdateCompanyInfo updateCompanyInfo)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                updateCompanyInfo.UpdatedBy = userid.Value;
                int? data = await accouuntservice.UpdateCompanyInfo(updateCompanyInfo, updateCompanyInfo.file);
                return Ok(data);
            }
            catch (Exception ex)
            {
                // You can log ex.Message here using ILogger
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving company data.",
                    details = ex.Message
                });
            }
        }
        #endregion

        #region Customer Ledger
        [HttpPost]
        [Route("Savecustomerledger")]
        public async Task<ActionResult<Customerledgermodel>> Savecustomerledger([FromBody] Customerledgermodel customerledger)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");
                customerledger.createby = userid.Value;

                var addedCategory = await accouuntservice.Savecustomerledger(customerledger);
                return Ok(addedCategory);
            }
            catch (Exception ex)
            {
                // Log ex.Message if needed
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetCustomerledger")]
        public async Task<IActionResult> GetCustomerledger([FromQuery] PaginationFilter pagination)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var data = await accouuntservice.GetCustomerledger(pagination.PageNo, pagination.PageSize);
                return Ok(data);
            }
            catch (Exception ex)
            {
                // You can log ex.Message here using ILogger
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving customer ledger data.",
                    details = ex.Message
                });
            }
        }

        [HttpGet("GetCustomerledgerdetails")]
        public async Task<IActionResult> GetCustomerledgerdetails([FromQuery] int customerid, string startDate, string endDate)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                IEnumerable<CustomerledgerdetailDto?> data = await accouuntservice.GetCustomerledgerdetails(customerid, startDate, endDate);
                return Ok(data);
            }
            catch (Exception ex)
            {
                // You can log ex.Message here using ILogger
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving customer ledger data.",
                    details = ex.Message
                });
            }
        }

        [HttpGet("GetCustomerledgerdetailspdf")]
        public async Task<IActionResult> GetCustomerledgerdetailspdf([FromQuery] int customerid, string startDate, string endDate)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                var pdfBytes = await accouuntservice.GetCustomerledgerdetailspdf(customerid, startDate, endDate, (int)userid);
                return File(pdfBytes, "application/pdf", "GetCustomerledgerdetailspdf.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region Balance Due
        [HttpGet("GetBalanceDueListAsync")]
        public async Task<IActionResult> GetBalanceDueListAsync([FromQuery] int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                    return Unauthorized("User ID not found in token.");

                IEnumerable<BalanceDueDto?> data = await accouuntservice.GetBalanceDueListAsync(pageNumber, pageSize, searchTerm);
                return Ok(data);
            }
            catch (Exception ex)
            {
                // You can log ex.Message here using ILogger
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving customer ledger data.",
                    details = ex.Message
                });
            }
        }

        [HttpPost("SaveCustomerbalancesettlement")]
        public async Task<ActionResult<Customerledgermodel>> SaveCustomerbalancesettlement([FromBody] SettleBalance settlebalance)
        {
            try
            {
                int? userid = User.GetUserId();
                if (userid == null)
                {
                    return Unauthorized("User ID not found in token.");
                }
                settlebalance.createby = userid.Value;
                var addedCategory = await accouuntservice.SaveCustomerbalancesettlement(settlebalance);
                return Ok(addedCategory);
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
