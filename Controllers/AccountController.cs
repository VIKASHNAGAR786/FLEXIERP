using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.MODELS.AGRIMANDI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FLEXIERP.Controllers
{
    [Route("[controller]/[action]")]
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
            if (String.IsNullOrEmpty(user.Email))
            {
                return BadRequest(new { message = "Email address needs to entered" });
            }
            else if (String.IsNullOrEmpty(user.Password))
            {
                return BadRequest(new { message = "Password needs to entered" });
            }

            User1? loggedInUser = await accouuntservice.Login(user.Email, user.Password);

            if (loggedInUser != null)
            {
                return Ok(loggedInUser);
            }

            return BadRequest(new { message = "User login unsuccessful" });
        }

        // POST: auth/register
        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterUser user)
        {
            if (String.IsNullOrEmpty(user.Name))
            {
                return BadRequest(new { message = "Name needs to entered" });
            }
            else if (String.IsNullOrEmpty(user.UserName))
            {
                return BadRequest(new { message = "User name needs to entered" });
            }
            else if (String.IsNullOrEmpty(user.Password))
            {
                return BadRequest(new { message = "Password needs to entered" });
            }

            //User1 userToRegister = new(0,user.UserName, user.Name, user.Password, user.Role.ToUpper(), user.Email);
            User1 userToRegister = new User1();
            userToRegister.Id = 0;
            userToRegister.UserName = user.UserName;
            userToRegister.Name = user.Name;
            userToRegister.Password = user.Password;
            userToRegister.Role = user.Role.ToUpper();
            userToRegister.Email = user.Email;
            userToRegister.CompanyName = user.CompanyName;
            userToRegister.CompanyType = user.CompanyType;

            User1 registeredUser = await accouuntservice.Register(userToRegister);

            User1? loggedInUser = await accouuntservice.Login(registeredUser.Email, user.Password);

            if (loggedInUser != null)
            {
                return Ok(loggedInUser);
            }
            return BadRequest(new { message = "User registration unsuccessful" });
        }
        #endregion
    }
}
