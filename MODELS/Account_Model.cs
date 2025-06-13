namespace FLEXIERP.MODELS
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Mvc;

    namespace AGRIMANDI.Model
    {
        public class User1
        {
            [Key]
            public string? UserName { get; set; } = "";
            public string? Name { get; set; } = "";
            public string? Role { get; set; } = "FARMER";
            public bool IsActive { get; set; } = false;
            public string? Token { get; set; } = "";
            public string? Password { get; set; } = "";
            public int Id { get; set; } = 0;
            public string? Email { get; set; } = "";
            public string? CompanyName { get; set; }
            public string? CompanyType { get; set; }


            //public User() { }

            //public User1(int id,string? userName, string? name, string? password, string? role, string? email)
            //{
            //    //Email = email;
            //    Id = id;
            //    UserName = userName;
            //    Name = name;
            //    Password = password;
            //    Role = role;
            //    Email = email;
            //}
        }

        public class LoginUser
        {
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
        }

        public class RegisterUser
        {
            public string Name { get; set; } = "";
            public string UserName { get; set; } = "";
            public string Password { get; set; } = "";
            public string Role { get; set; } = "Everyone";
            public string Email { get; set; } = "";
            public string? CompanyType { get; set; }
            public string? CompanyName { get; set; }
        }

        public class ProfileImageUploadDto
        {
            [FromForm(Name = "file")]
            public IFormFile File { get; set; }

            [FromForm(Name = "userid")]
            public int UserId { get; set; }
        }

    }

}
