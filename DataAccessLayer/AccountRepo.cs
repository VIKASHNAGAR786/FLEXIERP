using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DATABASE;
using FLEXIERP.MODELS.AGRIMANDI.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FLEXIERP.DataAccessLayer
{
    public class AccountRepo : IAccountRepo
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountRepo> _logger;
        private readonly IDataBaseOperation sqlConnection; 

        public AccountRepo(IConfiguration configuration, ILogger<AccountRepo> logger, IDataBaseOperation _sqlConnection)
        {
            _configuration = configuration;
            this._logger = logger;
            this.sqlConnection = _sqlConnection;
        }


        #region common properties
        private const int SaltSize = 16; // 128-bit
        private const int KeySize = 32;  // 256-bit
        private const int Iterations = 10000;

        public string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(KeySize);

            // Format: iterations.salt.hash (Base64)
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            var parts = hashedPassword.Split('.');
            if (parts.Length != 3)
                return false;

            int iterations = int.Parse(parts[0]);
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] storedHash = Convert.FromBase64String(parts[2]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            byte[] computedHash = pbkdf2.GetBytes(KeySize);

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }

        #endregion
        public async Task<User1?> Login(string email, string password)
        {
            try
            {
                await sqlConnection.GetConnection().OpenAsync();
                var cmd = sqlConnection.GetConnection().CreateCommand();
                cmd.CommandText = @"";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@email", email);

                var reader = await cmd.ExecuteReaderAsync();
                var user = await GetUserFromReader(reader, password);

                if (user == null)
                    return null;

                // Generate JWT Token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["JWT:SecretKey"]);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.GivenName, user.Name),
                new Claim(ClaimTypes.Role, user.Role)
            }),
                    Expires = DateTime.UtcNow.AddDays(7),
                    Issuer = _configuration["JWT:Issuer"],
                    Audience = _configuration["JWT:Audience"],
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                user.Token = tokenHandler.WriteToken(token);
                user.IsActive = true;

                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }
        }
        public async Task<User1?> GetUserFromReader(SqlDataReader reader, string plainPassword)
        {
            using (reader)
            {
                if (reader.HasRows)
                {
                    if (await reader.ReadAsync())
                    {
                        var hashedPassword = reader.GetString(reader.GetOrdinal("Password"));

                        // Use your password verification method here
                        bool isPasswordValid = VerifyPassword(plainPassword, hashedPassword);
                        if (!isPasswordValid)
                            return null;

                        return new User1
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            UserName = reader.GetString(reader.GetOrdinal("UserName")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Email = reader.GetString(reader.GetOrdinal("Email")),
                            Password = hashedPassword,
                            Role = reader.GetString(reader.GetOrdinal("Role")),
                            IsActive = false // Will be updated after token generation
                        };
                    }
                }
            }
            return null;
        }

        public async Task<User1> Register(User1 user1)
        {
            if (string.IsNullOrEmpty(user1.Password))
                throw new ArgumentException("Password cannot be null or empty.", nameof(user1.Password));

            user1.Password = HashPassword(user1.Password);

            try
            {
                await sqlConnection.GetConnection().OpenAsync();

                // Check for existing user
                var checkCmd = sqlConnection.GetConnection().CreateCommand();
                checkCmd.CommandText = "";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("", user1.UserName);
                checkCmd.Parameters.AddWithValue("", user1.Email);

                var existingCount = (int)await checkCmd.ExecuteScalarAsync();
                if (existingCount > 0)
                    throw new Exception("");

                // Insert new user
                var insertCmd = sqlConnection.GetConnection().CreateCommand();
                insertCmd.CommandText = @"
            ";
                insertCmd.CommandType = CommandType.Text;

                insertCmd.Parameters.AddWithValue("", user1.Name);
                insertCmd.Parameters.AddWithValue("", user1.UserName);
                insertCmd.Parameters.AddWithValue("", user1.Email);
                insertCmd.Parameters.AddWithValue("", user1.Password);
                insertCmd.Parameters.AddWithValue("", user1.Role);

                var insertedUserId = (int)await insertCmd.ExecuteScalarAsync();

                // Insert into role-specific table
                if (user1.Role == "")
                {
                    var farmerCmd = sqlConnection.GetConnection().CreateCommand();
                    farmerCmd.CommandText = @"
               ";
                    farmerCmd.CommandType = CommandType.Text;

                    farmerCmd.Parameters.AddWithValue("", insertedUserId);
                    farmerCmd.Parameters.AddWithValue("", user1.Name);
                    farmerCmd.Parameters.AddWithValue("", user1.Email);
                    farmerCmd.Parameters.AddWithValue("", insertedUserId);

                    await farmerCmd.ExecuteNonQueryAsync();
                }
                else if (user1.Role == "")
                {
                    var buyerCmd = sqlConnection.GetConnection().CreateCommand();
                    buyerCmd.CommandText = @"";
                    buyerCmd.CommandType = CommandType.Text;

                    buyerCmd.Parameters.AddWithValue("", insertedUserId);
                    buyerCmd.Parameters.AddWithValue("", user1.CompanyName);
                    buyerCmd.Parameters.AddWithValue("", user1.CompanyName); 

                    await buyerCmd.ExecuteNonQueryAsync();
                }

                user1.Id = insertedUserId;
                return user1;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during user registration: {ex.Message}");
                throw new Exception("User registration failed. Please try again later.");
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }
        }


    }
}
