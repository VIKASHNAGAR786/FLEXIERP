using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.MODELS.AGRIMANDI.Model;
using FLEXIERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FLEXIERP.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountServices accouuntservice;
        public AccountController(IAccountServices _accouuntservice)
        {
            accouuntservice = _accouuntservice;
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
                {
                    return BadRequest(new { message = "Email address needs to be entered" });
                }
                else if (string.IsNullOrEmpty(user.Password))
                {
                    return BadRequest(new { message = "Password needs to be entered" });
                }

                User1? loggedInUser = await accouuntservice.Login(user.Email, user.UserName, user.Password);

                if (loggedInUser != null)
                {
                    return Ok(loggedInUser);
                }

                return BadRequest(new { message = "Invalid login credentials" });
            }
            catch (Exception ex)
            {
                // log the error here (e.g., using ILogger or any logging framework)
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


        // POST: auth/register
        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterUser user)
        {
            if (String.IsNullOrEmpty(user.FullName))
            {
                return BadRequest(new { message = "Name needs to entered" });
            }
            else if (String.IsNullOrEmpty(user.Username))
            {
                return BadRequest(new { message = "User name needs to entered" });
            }
            else if (String.IsNullOrEmpty(user.PasswordHash))
            {
                return BadRequest(new { message = "Password needs to entered" });
            }

            // Fix for CS9035: Initialize all required members in the object initializer
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

            User1 registeredUser = await accouuntservice.Register(userToRegister);

            User1? loggedInUser = await accouuntservice.Login(registeredUser.Email,registeredUser.Username, user.PasswordHash);

            if (loggedInUser != null)
            {
                return Ok(loggedInUser);
            }
            return BadRequest(new { message = "User registration unsuccessful" });
        }
        #endregion
    }
}
