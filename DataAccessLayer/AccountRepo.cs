using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DATABASE;
using FLEXIERP.MODELS.AGRIMANDI.Model;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;

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

        public async Task<User1?> Login(string? email, string? UserName, string password)
        {
            try
            {
                await sqlConnection.GetConnection().OpenAsync();
                var cmd = sqlConnection.GetConnection().CreateCommand();
                cmd.CommandText = @"SELECT 
                                    UserID,
                                    Username,
                                    FullName,
                                    Email,
                                    PasswordHash,
                                    RoleID,
                                    IsActive,
                                    MobileNo,
                                    Gender,
                                    DateOfBirth,
                                    Address,
                                    City,
                                    State,
                                    Country,
                                    ProfileImageUrl,
                                    LastLoginAt,
                                    IsEmailVerified
                                FROM Tbl_Users
                                WHERE Username = @Username OR Email = @Email;
";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Username", UserName);

                var reader = await cmd.ExecuteReaderAsync();
                var user = await GetUserFromReader(reader, password);

                if (user == null)
                    return null;

                // Generate JWT Token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["JWT:SecretKey"]);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.GivenName, user.FullName),
                    new Claim(ClaimTypes.Role, user.RoleID.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim("IsActive", user.IsActive?.ToString() ?? "null"),
                    new Claim("MobileNo", user.MobileNo ?? string.Empty),
                    new Claim("Gender", user.Gender ?? string.Empty),
                    new Claim("DateOfBirth", user.DateOfBirth.ToString("yyyy-MM-dd")),
                    new Claim("Address", user.Address ?? string.Empty),
                    new Claim("City", user.City ?? string.Empty),
                    new Claim("State", user.State ?? string.Empty),
                    new Claim("Country", user.Country ?? string.Empty),
                    new Claim("ProfileImageUrl", user.ProfileImageUrl ?? string.Empty),
                    new Claim("LastLoginAt", user.LastLoginAt.ToString("o")), // ISO 8601 format
                    new Claim("IsEmailVerified", user.IsEmailVerified?.ToString() ?? "null")
                };


                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddDays(7),
                    Issuer = _configuration["JWT:Issuer"],
                    Audience = _configuration["JWT:Audience"],
                    SigningCredentials = new SigningCredentials(
         new SymmetricSecurityKey(key),
         SecurityAlgorithms.HmacSha256Signature)
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
        private async Task<User1?> GetUserFromReader(SqlDataReader reader, string plainPassword)
        {
            using (reader)
            {
                if (reader.HasRows)
                {
                    if (await reader.ReadAsync())
                    {
                        var hashedPassword = reader.GetString(reader.GetOrdinal("PasswordHash"));

                        // Use your password verification method here
                        bool isPasswordValid = VerifyPassword(plainPassword, hashedPassword);
                        if (!isPasswordValid)
                            throw new InvalidOperationException();

                        return new User1
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("UserID")),
                            Username = reader.GetString(reader.GetOrdinal("Username")),
                            FullName = reader.GetString(reader.GetOrdinal("FullName")),
                            Email = reader.GetString(reader.GetOrdinal("Email")),
                            PasswordHash = hashedPassword,
                            RoleID = reader.GetInt32(reader.GetOrdinal("RoleID")),
                            IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive"))
                               ? null
                               : reader.GetBoolean(reader.GetOrdinal("IsActive")),
                            MobileNo = reader.IsDBNull(reader.GetOrdinal("MobileNo"))
                               ? null
                               : reader.GetString(reader.GetOrdinal("MobileNo")),
                            Gender = reader.IsDBNull(reader.GetOrdinal("Gender"))
                             ? null
                             : reader.GetString(reader.GetOrdinal("Gender")),
                            DateOfBirth = reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
                            Address = reader.IsDBNull(reader.GetOrdinal("Address"))
                              ? null
                              : reader.GetString(reader.GetOrdinal("Address")),
                            City = reader.IsDBNull(reader.GetOrdinal("City"))
                           ? null
                           : reader.GetString(reader.GetOrdinal("City")),
                            State = reader.IsDBNull(reader.GetOrdinal("State"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("State")),
                            Country = reader.IsDBNull(reader.GetOrdinal("Country"))
                              ? null
                              : reader.GetString(reader.GetOrdinal("Country")),
                            ProfileImageUrl = reader.IsDBNull(reader.GetOrdinal("ProfileImageUrl"))
                                      ? null
                                      : reader.GetString(reader.GetOrdinal("ProfileImageUrl")),
                            LastLoginAt = reader.IsDBNull(reader.GetOrdinal("LastLoginAt"))
                                  ? DateTime.Today
                                  : reader.GetDateTime(reader.GetOrdinal("LastLoginAt")),
                            IsEmailVerified = reader.IsDBNull(reader.GetOrdinal("IsEmailVerified"))
                                      ? null
                                      : reader.GetBoolean(reader.GetOrdinal("IsEmailVerified")),
                            Token = null
                        };
                    }
                    return null;

                }
                return null;
            }
        }
        public async Task<User1> Register(User1 user1)
        {
            if (string.IsNullOrEmpty(user1.PasswordHash))
                throw new ArgumentException("Password cannot be null or empty.", nameof(user1.PasswordHash));

            user1.PasswordHash = HashPassword(user1.PasswordHash);

            try
            {
                await sqlConnection.GetConnection().OpenAsync();
                // Insert new user
                var insertCmd = sqlConnection.GetConnection().CreateCommand();
                insertCmd.CommandText = @"pro_Tbl_Users_insert";
                insertCmd.CommandType = CommandType.StoredProcedure;

                // insertCmd.Parameters.AddWithValue("@p_FullName", user1.FullName);
                insertCmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "@p_FullName",
                    SqlDbType = SqlDbType.VarChar,
                    SqlValue = user1.FullName,
                    Direction = ParameterDirection.Input,
                });
                insertCmd.Parameters.AddWithValue("@p_Username", user1.Username);
                insertCmd.Parameters.AddWithValue("@p_Email", user1.Email);
                insertCmd.Parameters.AddWithValue("@p_PasswordHash", user1.PasswordHash);
                insertCmd.Parameters.AddWithValue("@p_MobileNo", user1.MobileNo);
                insertCmd.Parameters.AddWithValue("@p_Gender", user1.Gender);
                insertCmd.Parameters.AddWithValue("@p_DateOfBirth", user1.DateOfBirth);
                insertCmd.Parameters.AddWithValue("@p_Address", user1.Address);
                insertCmd.Parameters.AddWithValue("@p_City", user1.City);
                insertCmd.Parameters.AddWithValue("@p_State", user1.Username);
                insertCmd.Parameters.AddWithValue("@p_Country", user1.Country);
                insertCmd.Parameters.AddWithValue("@p_ProfileImageUrl", user1.ProfileImageUrl);
                insertCmd.Parameters.AddWithValue("@p_RoleID ", user1.RoleID);
                insertCmd.Parameters.AddWithValue("@p_LastLoginAt", user1.LastLoginAt);
                insertCmd.Parameters.AddWithValue("@p_IsActive", user1.IsActive);
                insertCmd.Parameters.AddWithValue("@p_IsEmailVerified", user1.IsEmailVerified);


                int insertedUserId = await insertCmd.ExecuteNonQueryAsync();
                if (insertedUserId <= 0)
                {
                    throw new Exception("User registration failed. Please try again.");
                }
                return user1;

            }
            catch (SqlException ex)
            {
                _logger.LogError($"Error during user registration: {ex.Message}");
                throw new Exception("User registration failed. Please try again later.");
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }
        }

        public async Task<bool> StartUserSession(StartUserSession loginModel)
        {
            try
            {
                var connection = this.sqlConnection.GetConnection();
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "pro_UserLoginAttempt";
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Parameters
                    cmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 50) { Value = loginModel.Username });
                    cmd.Parameters.Add(new SqlParameter("@Password", SqlDbType.NVarChar, 256) { Value = loginModel.Password });
                    cmd.Parameters.Add(new SqlParameter("@IPAddress", SqlDbType.VarChar, 50) { Value = (object?)loginModel.IPAddress ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@DeviceInfo", SqlDbType.VarChar, 255) { Value = (object?)loginModel.DeviceInfo ?? DBNull.Value });

                    // Execute SP
                    var rows = await cmd.ExecuteNonQueryAsync();

                    // Since SP does not return success explicitly, 
                    // we return true if SP executed without exception
                    return rows > 0;
                }
            }
            catch (SqlException ex)
            {
                // Log error
                throw new Exception("Login attempt failed due to database error.", ex);
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }
        }
        public async Task<bool> LogoutUser(int userId)
        {
            try
            {
                var connection = sqlConnection.GetConnection();
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "pro_UserLogout";
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameter
                    cmd.Parameters.Add(new SqlParameter("@UserID", SqlDbType.Int) { Value = userId });

                    // Execute SP
                    var rowsAffected = await cmd.ExecuteScalarAsync();

                    // Return true if any row was updated
                    if (rowsAffected is not null)
                        return (int)rowsAffected > 0;
                    else
                    {
                        return false;
                    }
                }
            }
            catch (SqlException ex)
            {
                // Log exception if needed
                throw new Exception("Logout failed due to database error.", ex);
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }
        }

        public async Task<IEnumerable<UserLoginHistoryDTO>> GetUserLoginHistory(int pageNo = 1, int pageSize = 20)
        {
            var historyList = new List<UserLoginHistoryDTO>();

            try
            {
                using var connection = sqlConnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "usp_GetUserLoginHistory";
                cmd.CommandType = CommandType.StoredProcedure;

                // Add parameters
                cmd.Parameters.Add(new SqlParameter("@PageNo", SqlDbType.Int) { Value = pageNo });
                cmd.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize });

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    historyList.Add(new UserLoginHistoryDTO
                    {
                        HistoryID = !reader.IsDBNull(0) ? reader.GetInt32(0) : 0,
                        Username = !reader.IsDBNull(1) ? reader.GetString(1) : string.Empty,
                        Email = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty,
                        LoginTime = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty,
                        LogoutTime = !reader.IsDBNull(4) ? reader.GetString(4) : string.Empty,
                        Status = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty,
                        IPAddress = !reader.IsDBNull(6) ? reader.GetString(6) : string.Empty,
                        DeviceInfo = !reader.IsDBNull(7) ? reader.GetString(7) : string.Empty,
                        FailureReason = !reader.IsDBNull(8) ? reader.GetString(8) : string.Empty,
                        TotalRecords = !reader.IsDBNull(9) ? reader.GetInt32(9) : 0
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Failed to retrieve user login history. Please try again later.", ex);
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }

            return historyList;
        }

        public async Task<CompanyInfoDto?> GetCompanyInfoByUserAsync(int userId)
        {
            CompanyInfoDto? companyInfo = null;
            try
            {
                using var connection = sqlConnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM dbo.fun_CompanyInfoByUser_get(@UserID)";
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.Add(new SqlParameter("@UserID", SqlDbType.Int) { Value = userId });

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    companyInfo = new CompanyInfoDto
                    {
                        ComInfoId = !reader.IsDBNull(0) ? reader.GetInt32(0) : 0,
                        CompanyName = !reader.IsDBNull(1) ? reader.GetString(1) : string.Empty,
                        ContactNo = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty,
                        WhatsAppNo = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty,
                        Email = !reader.IsDBNull(4) ? reader.GetString(4) : string.Empty,
                        Address = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty,
                        FullName = !reader.IsDBNull(6) ? reader.GetString(6) : string.Empty,
                        CreatedDate = !reader.IsDBNull(7) ? reader.GetString(7) :string.Empty,
                        CompanyLogo = !reader.IsDBNull(8) ? reader.GetString(8) : string.Empty
                    };
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Failed to retrieve company info. Please try again later.", ex);
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }

            return companyInfo;
        }

        public async Task<int> UpdateCompanyInfo(UpdateCompanyInfo UpdateCompanyInfo)
        {
            try
            {
                var connection = sqlConnection.GetConnection();
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UpdateCompanyInfo";
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    cmd.Parameters.Add(new SqlParameter("@Company_Name", SqlDbType.VarChar)
                    {
                        Value = string.IsNullOrEmpty(UpdateCompanyInfo.Company_Name) ? DBNull.Value : UpdateCompanyInfo.Company_Name
                    });

                    cmd.Parameters.Add(new SqlParameter("@Contact_No", SqlDbType.VarChar)
                    {
                        Value = string.IsNullOrEmpty(UpdateCompanyInfo.Contact_No) ? DBNull.Value : UpdateCompanyInfo.Contact_No
                    });

                    cmd.Parameters.Add(new SqlParameter("@WhatsApp_No", SqlDbType.VarChar)
                    {
                        Value = string.IsNullOrEmpty(UpdateCompanyInfo.WhatsApp_No) ? DBNull.Value : UpdateCompanyInfo.WhatsApp_No
                    });

                    cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.VarChar)
                    {
                        Value = string.IsNullOrEmpty(UpdateCompanyInfo.Email) ? DBNull.Value : UpdateCompanyInfo.Email
                    });

                    cmd.Parameters.Add(new SqlParameter("@Address", SqlDbType.VarChar)
                    {
                        Value = string.IsNullOrEmpty(UpdateCompanyInfo.Address) ? DBNull.Value : UpdateCompanyInfo.Address
                    });

                    cmd.Parameters.Add(new SqlParameter("@row_id", SqlDbType.Int)
                    {
                        Value = UpdateCompanyInfo.row_id
                    });

                    cmd.Parameters.Add(new SqlParameter("@UpdatedBy", SqlDbType.Int)
                    {
                        Value = UpdateCompanyInfo.UpdatedBy
                    });

                    cmd.Parameters.Add(new SqlParameter("@CompanyLogo", SqlDbType.VarChar)
                    {
                        Value = string.IsNullOrEmpty(UpdateCompanyInfo.CompanyLogo) ? DBNull.Value : UpdateCompanyInfo.CompanyLogo
                    });

                    // Execute SP
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected;
                }
            }
            catch (SqlException ex)
            {
                // Log exception if needed
                throw new Exception("Logout failed due to database error.", ex);
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }
        }
        #region Customer Ledger
        public async Task<int> Savecustomerledger(Customerledgermodel customerledger)
        {
            try
            {
                var connection = sqlConnection.GetConnection();
                await connection.OpenAsync();

                using (var insertCmd = connection.CreateCommand())
                {
                    insertCmd.CommandText = "usp_Customer_Ledger_insert";
                    insertCmd.CommandType = CommandType.StoredProcedure;

                    insertCmd.Parameters.Add(new SqlParameter("@paid_amt", SqlDbType.Decimal)
                    {
                        Value = customerledger.paidamount
                    });
                    insertCmd.Parameters.Add(new SqlParameter("@balance_due", SqlDbType.Decimal)
                    {
                        Value = customerledger.balancedue 
                    });
                    insertCmd.Parameters.Add(new SqlParameter("@total_amt", SqlDbType.Decimal)
                    {
                        Value = customerledger.Totalamount
                    });
                    insertCmd.Parameters.Add(new SqlParameter("@payment_mode", SqlDbType.Int)
                    {
                        Value = customerledger.paymentmode
                    });
                    insertCmd.Parameters.Add(new SqlParameter("@transaction_type", SqlDbType.VarChar)
                    {
                        Value = customerledger.transactiontype
                    });
                    insertCmd.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.Int)
                    {
                        Value = customerledger.createby
                    });

                    // Execute and get inserted CategoryID
                    var result = await insertCmd.ExecuteScalarAsync();
                    if (result == null || Convert.ToInt32(result) <= 0)
                    {
                        throw new Exception("Customer Ledger insertion failed. Please try again.");
                    }

                    int lastinsertid = Convert.ToInt32(result);
                    return lastinsertid;
                }
            }
            catch (SqlException ex)
            {
                // You can log ex.Message here for debugging
                throw new Exception("Customer Ledger insertion failed. Please try again later.");
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }
        }

        public async Task<IEnumerable<CustomerledgerDto?>> GetCustomerledger(int pageNo = 1, int pageSize = 20)
        {
            var historyList = new List<CustomerledgerDto?>();

            try
            {
                using var connection = sqlConnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "usp_Customer_Ledger_Summary_get";
                cmd.CommandType = CommandType.StoredProcedure;

                // Add parameters
                cmd.Parameters.Add(new SqlParameter("@PageNo", SqlDbType.Int) { Value = pageNo });
                cmd.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize });

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    historyList.Add(new CustomerledgerDto
                    {
                        customerid = !reader.IsDBNull(0) ? reader.GetInt32(0) : 0,
                        Customername = !reader.IsDBNull(1) ? reader.GetString(1) : string.Empty,
                        ContactNo = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty,
                        CustomerAddress = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty,
                        Email = !reader.IsDBNull(4) ? reader.GetString(4) : string.Empty,
                        totalamount = !reader.IsDBNull(5) ? reader.GetDecimal(5) : decimal.MaxValue,
                        totaldue = !reader.IsDBNull(6) ? reader.GetDecimal(6) : decimal.MaxValue,
                        lasttransactiondate = !reader.IsDBNull(7) ? reader.GetString(7) : string.Empty,
                        rowid = !reader.IsDBNull(8) ? reader.GetInt32(8) : 0
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Failed to retrieve customer ledger data. Please try again later.", ex);
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }

            return historyList;
        }
        #endregion
    }
}
