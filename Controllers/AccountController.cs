using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.MODELS.AGRIMANDI.Model;
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
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUser user)
        {
            if (String.IsNullOrEmpty(user.Email))
            {
                return BadRequest(new { message = "Email address needs to entered" });
            }
            else if (String.IsNullOrEmpty(user.Password))
            {
                return BadRequest(new { message = "Password needs to entered" });
            }

            User1? loggedInUser = await accouuntservice.Login(user.Email, user.UserName, user.Password);

            if (loggedInUser != null)
            {
                return Ok(loggedInUser);
            }

            return BadRequest(new { message = "User login unsuccessful" });
        }

        // POST: auth/register
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
