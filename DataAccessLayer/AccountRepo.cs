using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DATABASE;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;

namespace FLEXIERP.DataAccessLayer
{
    public class AccountRepo : IAccountRepo
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountRepo> _logger;
        private readonly IDataBaseOperation sqlConnection;
        private readonly ICommonMasterRepo commonMasterRepo;

        public AccountRepo(IConfiguration configuration, ILogger<AccountRepo> logger, IDataBaseOperation _sqlConnection, ICommonMasterRepo commonMasterRepo)
        {
            _configuration = configuration;
            this._logger = logger;
            this.sqlConnection = _sqlConnection;
            this.commonMasterRepo = commonMasterRepo;
        }


        #region payment mode string
        private string GetPaymentModeName(int paymentMode)
        {
            return paymentMode switch
            {
                1 => "Cash",
                2 => "Cheque",
                3 => "UPI",
                4 => "Bank Transfer",
                5 => "Card",
                _ => "Unknown"
            };
        }

        #endregion

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
                {
                    await commonMasterRepo.SaveUserErrorLogAsync(new UserErrorLogDto
                    {
                        Module = "Account",
                        ActionType = "Login",
                        ErrorMessage = "USER NOT FOUND",
                        ErrorCode = "500",
                        StackTrace = "FBGNB",
                        ApiName = "Login",
                        Severity = "High",
                        AdditionalInfo = $" An unexpected error occurred in login"
                    });
                    return null;
                }

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
                Console.WriteLine(ex.ToString());
                throw;
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }
        }
        private async Task<User1?> GetUserFromReader(SqliteDataReader reader, string plainPassword)
        {
            using (reader)
            {
                if (reader.HasRows && await reader.ReadAsync())
                {
                    var hashedPassword = reader["PasswordHash"]?.ToString() ?? "";

                    bool isPasswordValid = VerifyPassword(plainPassword, hashedPassword);
                    if (!isPasswordValid)
                        throw new InvalidOperationException("Invalid password");

                    return new User1
                    {
                        Id = Convert.ToInt32(reader["UserID"]),
                        Username = reader["Username"]?.ToString() ?? "",
                        FullName = reader["FullName"]?.ToString() ?? "",
                        Email = reader["Email"]?.ToString() ?? "",
                        PasswordHash = hashedPassword,
                        RoleID = Convert.ToInt32(reader["RoleID"]),
                        IsActive = reader["IsActive"] != DBNull.Value ? Convert.ToBoolean(reader["IsActive"]) : null,
                        MobileNo = reader["MobileNo"]?.ToString(),
                        Gender = reader["Gender"]?.ToString(),
                        DateOfBirth = reader["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(reader["DateOfBirth"]) : DateTime.MinValue,
                        Address = reader["Address"]?.ToString(),
                        City = reader["City"]?.ToString(),
                        State = reader["State"]?.ToString(),
                        Country = reader["Country"]?.ToString(),
                        ProfileImageUrl = reader["ProfileImageUrl"]?.ToString(),
                        LastLoginAt = reader["LastLoginAt"] != DBNull.Value ? Convert.ToDateTime(reader["LastLoginAt"]) : DateTime.Now,
                        IsEmailVerified = reader["IsEmailVerified"] != DBNull.Value ? Convert.ToBoolean(reader["IsEmailVerified"]) : null,
                        Token = null
                    };
                }
                return null;
            }
        }

        public async Task<User1> Register(User1 user1)
        {
            if (string.IsNullOrEmpty(user1.PasswordHash))
                throw new ArgumentException("Password cannot be null or empty.", nameof(user1.PasswordHash));

            user1.PasswordHash = HashPassword(user1.PasswordHash);

            var connection = sqlConnection.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1️⃣ Check if user exists
                using (var checkCmd = connection.CreateCommand())
                {
                    checkCmd.CommandText = @"SELECT 1 FROM Tbl_Users WHERE Email = @p_Email OR Username = @p_Username LIMIT 1;";
                    checkCmd.Parameters.AddWithValue("@p_Email", user1.Email);
                    checkCmd.Parameters.AddWithValue("@p_Username", user1.Username);

                    var exists = await checkCmd.ExecuteScalarAsync();
                    if (exists != null)
                    {
                        throw new Exception("Email or Username already exists.");
                    }
                }

                // 2️⃣ Insert into Tbl_Users
                using (var insertUserCmd = connection.CreateCommand())
                {
                    insertUserCmd.CommandText = @"
                INSERT INTO Tbl_Users (
                    FullName, Username, Email, PasswordHash, MobileNo, Gender, DateOfBirth,
                    Address, City, State, Country, ProfileImageUrl, RoleID, LastLoginAt,
                    IsActive, IsEmailVerified
                ) VALUES (
                    @p_FullName, @p_Username, @p_Email, @p_PasswordHash, @p_MobileNo, @p_Gender, @p_DateOfBirth,
                    @p_Address, @p_City, @p_State, @p_Country, @p_ProfileImageUrl, @p_RoleID, @p_LastLoginAt,
                    @p_IsActive, @p_IsEmailVerified
                );";

                    insertUserCmd.Parameters.AddWithValue("@p_FullName", user1.FullName);
                    insertUserCmd.Parameters.AddWithValue("@p_Username", user1.Username);
                    insertUserCmd.Parameters.AddWithValue("@p_Email", user1.Email);
                    insertUserCmd.Parameters.AddWithValue("@p_PasswordHash", user1.PasswordHash);
                    insertUserCmd.Parameters.AddWithValue("@p_MobileNo", user1.MobileNo ?? "0000000000");
                    insertUserCmd.Parameters.AddWithValue("@p_Gender", user1.Gender ?? "NotSpecified");
                    insertUserCmd.Parameters.AddWithValue("@p_DateOfBirth", user1.DateOfBirth);
                    insertUserCmd.Parameters.AddWithValue("@p_Address", user1.Address ?? "N/A");
                    insertUserCmd.Parameters.AddWithValue("@p_City", user1.City ?? "N/A");
                    insertUserCmd.Parameters.AddWithValue("@p_State", user1.State ?? "N/A");
                    insertUserCmd.Parameters.AddWithValue("@p_Country", user1.Country ?? "N/A");
                    insertUserCmd.Parameters.AddWithValue("@p_ProfileImageUrl", user1.ProfileImageUrl ?? "default.png");
                    insertUserCmd.Parameters.AddWithValue("@p_RoleID", user1.RoleID);
                    insertUserCmd.Parameters.AddWithValue("@p_LastLoginAt", user1.LastLoginAt);
                    insertUserCmd.Parameters.AddWithValue("@p_IsActive", user1.IsActive ?? true);
                    insertUserCmd.Parameters.AddWithValue("@p_IsEmailVerified", user1.IsEmailVerified ?? false);

                    await insertUserCmd.ExecuteNonQueryAsync();
                }

                // 3️⃣ Get last inserted user ID
                long lastUserId;
                using (var idCmd = connection.CreateCommand())
                {
                    idCmd.CommandText = "SELECT last_insert_rowid();";
                    lastUserId = (long)await idCmd.ExecuteScalarAsync();
                }

                // 4️⃣ Insert into Company_info with dummy values
                using (var insertCompanyCmd = connection.CreateCommand())
                {
                    insertCompanyCmd.CommandText = @"
                INSERT INTO Company_info (
                    Company_Name, Contact_No, WhatsApp_No, Email, Address, CreatedBy, CompanyLogo
                ) VALUES (
                    @Company_Name, @Contact_No, @WhatsApp_No, @Email, @Address, @CreatedBy, @CompanyLogo
                );";

                    insertCompanyCmd.Parameters.AddWithValue("@Company_Name", "Dummy Company");
                    insertCompanyCmd.Parameters.AddWithValue("@Contact_No", user1.MobileNo ?? "0000000000");
                    insertCompanyCmd.Parameters.AddWithValue("@WhatsApp_No", "0000000000");
                    insertCompanyCmd.Parameters.AddWithValue("@Email", user1.Email);
                    insertCompanyCmd.Parameters.AddWithValue("@Address", user1.Address ?? "N/A");
                    insertCompanyCmd.Parameters.AddWithValue("@CreatedBy", lastUserId);
                    insertCompanyCmd.Parameters.AddWithValue("@CompanyLogo", "default_logo.png");

                    await insertCompanyCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return user1;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError($"SQLite error during registration: {ex.Message}");
                throw new Exception("User registration failed. Please try again later.", ex);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<bool> StartUserSession(StartUserSession loginModel)
        {
            try
            {
                var connection = sqlConnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;

                cmd.CommandText = @"
            -- Get user ID and password from Tbl_Users
            SELECT UserID, PasswordHash
            FROM Tbl_Users
            WHERE Username = @Username;
        ";

                cmd.Parameters.AddWithValue("@Username", loginModel.Username);

                int? userId = null;
                string? dbPassword = null;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        userId = reader["UserID"] != DBNull.Value ? Convert.ToInt32(reader["UserID"]) : (int?)null;
                        dbPassword = reader["PasswordHash"]?.ToString();
                    }
                }

                // Prepare insert into UserLoginHistory
                using var insertCmd = connection.CreateCommand();
                insertCmd.CommandType = CommandType.Text;

                if (userId == null)
                {
                    // User not found
                    insertCmd.CommandText = @"
                INSERT INTO UserLoginHistory (UserID, IsSuccess, IPAddress, DeviceInfo, FailureReason)
                VALUES (2, 0, @IPAddress, @DeviceInfo, 'User not found');
            ";
                }
                else if (dbPassword == loginModel.Password)
                {
                    // Successful login
                    insertCmd.CommandText = @"
                INSERT INTO UserLoginHistory (UserID, IsSuccess, IPAddress, DeviceInfo)
                VALUES (@UserID, 1, @IPAddress, @DeviceInfo);
            ";
                    insertCmd.Parameters.AddWithValue("@UserID", userId);
                }
                else
                {
                    // Invalid password
                    insertCmd.CommandText = @"
                INSERT INTO UserLoginHistory (UserID, IsSuccess, IPAddress, DeviceInfo, FailureReason)
                VALUES (@UserID, 0, @IPAddress, @DeviceInfo, 'Invalid Password');
            ";
                    insertCmd.Parameters.AddWithValue("@UserID", userId);
                }

                insertCmd.Parameters.AddWithValue("@IPAddress", loginModel.IPAddress ?? "Unknown");
                insertCmd.Parameters.AddWithValue("@DeviceInfo", loginModel.DeviceInfo ?? "Unknown");

                int rows = await insertCmd.ExecuteNonQueryAsync();

                return rows > 0;
            }
            catch (SqliteException ex)
            {
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

                using var cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
            UPDATE UserLoginHistory
            SET LogoutTime = CURRENT_TIMESTAMP
            WHERE UserID = @UserID
              AND IsSuccess = 1
              AND LogoutTime IS NULL;

            SELECT changes() AS RowsUpdated;
        ";

                cmd.Parameters.AddWithValue("@UserID", userId);

                var result = await cmd.ExecuteScalarAsync();
                return result is not null && Convert.ToInt32(result) > 0;
            }
            catch (SqliteException ex)
            {
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
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
            SELECT
                h.HistoryID,
                u.Username,
                u.Email,
                strftime('%Y-%m-%d %H:%M:%S', h.LoginTime) AS LoginTime,
                COALESCE(strftime('%Y-%m-%d %H:%M:%S', h.LogoutTime), 'N/A') AS LogoutTime,
                CASE WHEN h.IsSuccess = 1 THEN 'Success' ELSE 'Failed' END AS Status,
                h.IPAddress,
                h.DeviceInfo,
                COALESCE(h.FailureReason, '-') AS FailureReason,
                (SELECT COUNT(*) FROM UserLoginHistory) AS TotalRecords
            FROM UserLoginHistory h
            LEFT JOIN Tbl_Users u ON h.UserID = u.UserID
            ORDER BY h.LoginTime DESC
            LIMIT @PageSize OFFSET (@PageNo - 1) * @PageSize;
        ";

                cmd.Parameters.AddWithValue("@PageNo", pageNo);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    historyList.Add(new UserLoginHistoryDTO
                    {
                        HistoryID = reader.GetInt32(0),
                        Username = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        LoginTime = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        LogoutTime = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        Status = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        IPAddress = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        DeviceInfo = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        FailureReason = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                        TotalRecords = reader.IsDBNull(9) ? 0 : reader.GetInt32(9)
                    });
                }
            }
            catch (SqliteException ex)
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
                cmd.CommandType = CommandType.Text;

                // SQLite query (no table-valued function)
                cmd.CommandText = @"
                           SELECT 
                    comp.Com_info_id,  
                    comp.Company_Name,  
                    comp.Contact_No,  
                    comp.WhatsApp_No,  
                    comp.Email,  
                    comp.Address,  
                    users.FullName AS UserFullName,  
                    strftime('%Y-%m-%d %H:%M:%S', comp.CreatedDate) AS CreatedDate,
                    comp.CompanyLogo
                FROM Company_info AS comp
                LEFT JOIN Tbl_Users AS users ON comp.CreatedBy = users.UserID
                WHERE comp.CreatedBy = @UserID
                LIMIT 1;

                ";

                cmd.Parameters.AddWithValue("@UserID", userId);

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
                        CreatedDate = !reader.IsDBNull(7) ? reader.GetString(7) : string.Empty,
                        CompanyLogo = !reader.IsDBNull(8) ? reader.GetString(8) : string.Empty
                    };
                }
            }
            catch (SqliteException ex)
            {
                throw new Exception("Failed to retrieve company info. Please try again later.", ex);
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }

            return companyInfo;
        }

        public async Task<int> UpdateCompanyInfo(UpdateCompanyInfo info)
        {
            try
            {
                using var connection = sqlConnection.GetConnection(); // your SQLite connection service
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
            UPDATE Company_info
            SET 
                Company_Name = COALESCE(@Company_Name, Company_Name),
                Contact_No   = COALESCE(@Contact_No, Contact_No),
                WhatsApp_No  = COALESCE(@WhatsApp_No, WhatsApp_No),
                Email        = COALESCE(@Email, Email),
                Address      = COALESCE(@Address, Address),
                UpdatedBy    = COALESCE(@UpdatedBy, UpdatedBy),
                UpdatedDate  = CURRENT_TIMESTAMP,
                CompanyLogo  = COALESCE(@CompanyLogo, CompanyLogo)
            WHERE Com_info_id = @row_id;
        ";

                // Add parameters
                cmd.Parameters.Add(new SqliteParameter("@Company_Name", string.IsNullOrEmpty(info.Company_Name) ? DBNull.Value : info.Company_Name));
                cmd.Parameters.Add(new SqliteParameter("@Contact_No", string.IsNullOrEmpty(info.Contact_No) ? DBNull.Value : info.Contact_No));
                cmd.Parameters.Add(new SqliteParameter("@WhatsApp_No", string.IsNullOrEmpty(info.WhatsApp_No) ? DBNull.Value : info.WhatsApp_No));
                cmd.Parameters.Add(new SqliteParameter("@Email", string.IsNullOrEmpty(info.Email) ? DBNull.Value : info.Email));
                cmd.Parameters.Add(new SqliteParameter("@Address", string.IsNullOrEmpty(info.Address) ? DBNull.Value : info.Address));
                cmd.Parameters.Add(new SqliteParameter("@row_id", info.row_id));
                cmd.Parameters.Add(new SqliteParameter("@UpdatedBy", info.UpdatedBy == 0 ? DBNull.Value : info.UpdatedBy));
                cmd.Parameters.Add(new SqliteParameter("@CompanyLogo", string.IsNullOrEmpty(info.CompanyLogo) ? DBNull.Value : info.CompanyLogo));

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to update company info (SQLite).", ex);
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
            string query = @"
        SELECT 
     C.CustomerID,
     C.CustomerName,
     C.PhoneNo,
     C.CustomerAddress,
     C.Email,
     IFNULL(SUM(L.paid_amt), 0) AS TotalPaidAmount_Yet,
     IFNULL(due.total_due_amount, 0) AS TotalDue_Yet,
     STRFTIME('%Y-%m-%d %H:%M', MAX(L.create_at)) AS LastTransactionDate,
     MAX(L.customer_id) AS ROWID
 FROM Customer C
 LEFT JOIN Customer_Ledger L ON C.CustomerID = L.customer_id
 LEFT JOIN balance_due as due on L.customer_id = due.customerid
 GROUP BY C.CustomerID
 ORDER BY C.CustomerName
LIMIT @PageSize OFFSET ((@PageNo - 1) * @PageSize);
    ";

            try
            {
                using var connection = sqlConnection.GetConnection(); // Should return SQLiteConnection
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.AddWithValue("@PageNo", pageNo);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    historyList.Add(new CustomerledgerDto
                    {
                        customerid = !reader.IsDBNull(0) ? reader.GetInt32(0) : 0,                          // CustomerID
                        Customername = !reader.IsDBNull(1) ? reader.GetString(1) : string.Empty,            // CustomerName
                        ContactNo = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty,               // PhoneNo
                        CustomerAddress = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty,         // CustomerAddress
                        Email = !reader.IsDBNull(4) ? reader.GetString(4) : string.Empty,                   // Email
                        totalamount = !reader.IsDBNull(5) ? Convert.ToDecimal(reader.GetValue(5)) : 0,      // TotalPaidAmount_Yet
                        totaldue = !reader.IsDBNull(6) ? Convert.ToDecimal(reader.GetValue(6)) : 0,         // TotalDue_Yet
                        lasttransactiondate = !reader.IsDBNull(7) ? reader.GetString(7) : string.Empty,     // LastTransactionDate
                        rowid = !reader.IsDBNull(8) ? reader.GetInt32(8) : 0                                // ROWID
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve customer ledger data from SQLite. Please try again later.", ex);
            }

            return historyList;
        }

        public async Task<IEnumerable<CustomerledgerdetailDto?>> GetCustomerledgerdetails(int customerid, string StartDate, string EndDate)
        {
            var ledgerDetails = new List<CustomerledgerdetailDto?>();
            string query = @"
       -- Step 1: Calculate total tax per Sale
WITH SaleTax AS (
    SELECT 
        sd.SaleID,
        SUM(IFNULL(p.TaxRate, 0)) AS TotalTax
    FROM saledetail AS sd
    INNER JOIN product AS p ON p.productid = sd.productid
    GROUP BY sd.SaleID
),
-- Step 2: Get the latest Sale per Customer
LatestSale AS (
    SELECT
        s.CustomerID,
        MAX(s.SaleID) AS LastSaleID
    FROM Sale s
    GROUP BY s.CustomerID
)
-- Step 3: Ledger Details
SELECT
    cl.ledger_id,
    cl.Customer_ID,
    cl.paid_amt,
    cl.Balance_Due,
    cl.Total_Amt,
    cl.Payment_Mode,
    cl.Transaction_Type,
    STRFTIME('%d-%m-%Y %I:%M %p', cl.create_at) AS TransactionDate,
    COALESCE(STRFTIME('%d-%m-%Y %I:%M %p', s.createddate),
             STRFTIME('%d-%m-%Y %I:%M %p', cl.create_at)) AS SaleDate,
    COALESCE(s.TotalItems, 0) AS TotalItems,
    COALESCE(s.TotalDiscount, 0) AS TotalDiscount,
    COALESCE(st.TotalTax, 0) AS TotalTax,
    cs.CustomerName,
    cs.PhoneNo
FROM Customer_Ledger AS cl
LEFT JOIN LatestSale ls ON cl.Customer_ID = ls.CustomerID
LEFT JOIN Customer AS cs ON ls.CustomerID = cs.CustomerID
LEFT JOIN Sale AS s ON s.SaleID = ls.LastSaleID
LEFT JOIN SaleTax AS st ON s.SaleID = st.SaleID
WHERE 
    cl.Customer_ID = @CustomerID
    AND (
        (@StartDate IS NULL AND @EndDate IS NULL)
        OR (DATE(cl.create_at) BETWEEN DATE(@StartDate) AND DATE(@EndDate))
    )
ORDER BY cl.create_at DESC;

    ";

            try
            {
                using var connection = sqlConnection.GetConnection(); // returns SQLiteConnection
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.AddWithValue("@CustomerID", customerid);
                cmd.Parameters.AddWithValue("@StartDate", string.IsNullOrWhiteSpace(StartDate) ? DBNull.Value : StartDate);
                cmd.Parameters.AddWithValue("@EndDate", string.IsNullOrWhiteSpace(EndDate) ? DBNull.Value : EndDate);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    ledgerDetails.Add(new CustomerledgerdetailDto
                    {
                        rowid = !reader.IsDBNull(0) ? reader.GetInt32(0) : 0,
                        customerid = !reader.IsDBNull(1) ? reader.GetInt32(1) : 0,
                        paidamt = !reader.IsDBNull(2) ? Convert.ToDecimal(reader.GetValue(2)) : 0,
                        balancedue = !reader.IsDBNull(3) ? Convert.ToDecimal(reader.GetValue(3)) : 0,
                        totalamount = !reader.IsDBNull(4) ? Convert.ToDecimal(reader.GetValue(4)) : 0,
                        paymentmode = !reader.IsDBNull(5) ? this.GetPaymentModeName(reader.GetInt32(5)) : null,
                        transactiontype = !reader.IsDBNull(6) ? reader.GetString(6) : string.Empty,
                        transactiondate = !reader.IsDBNull(7) ? reader.GetString(7) : string.Empty,
                        saledate = !reader.IsDBNull(8) ? reader.GetString(8) : string.Empty,
                        totalitems = !reader.IsDBNull(9) ? Convert.ToDecimal(reader.GetValue(9)) : 0,
                        totaldiscount = !reader.IsDBNull(10) ? Convert.ToDecimal(reader.GetValue(10)) : 0,
                        tax = !reader.IsDBNull(11) ? Convert.ToDecimal(reader.GetValue(11)) : 0,
                        customername = !reader.IsDBNull(12) ? reader.GetString(12) : string.Empty,
                        contactno = !reader.IsDBNull(13) ? reader.GetString(13) : string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve customer ledger details from SQLite. Please try again later.", ex);
            }

            return ledgerDetails;
        }

        #endregion

        #region Balance Due
        public async Task<IEnumerable<BalanceDueDto?>> GetBalanceDueListAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            var result = new List<BalanceDueDto?>();

            string query = @"
 WITH FilteredData AS (
     SELECT 
         due.customerid,
         cstmr.CustomerName,
         cstmr.CustomerAddress,
         cstmr.PhoneNo,
         cstmr.Email,
         due.total_due_amount,
         due.dueid,
         strftime('%d-%m-%Y %I:%M %p', MAX(ledger.create_at)) AS last_transaction_date
     FROM balance_due AS due
     LEFT JOIN Customer AS cstmr 
         ON due.customerid = cstmr.customerid
     INNER JOIN Customer_ledger AS ledger 
         ON due.customerid = ledger.customer_id
     WHERE 
         (@SearchTerm IS NULL OR @SearchTerm = '' OR
          cstmr.CustomerName LIKE '%' || @SearchTerm || '%' OR
          cstmr.CustomerAddress LIKE '%' || @SearchTerm || '%' OR
          cstmr.PhoneNo LIKE '%' || @SearchTerm || '%' OR
          cstmr.Email LIKE '%' || @SearchTerm || '%')
     GROUP BY 
         due.customerid,
         cstmr.CustomerName,
         cstmr.CustomerAddress,
         cstmr.PhoneNo,
         cstmr.Email,
         due.total_due_amount,
         due.dueid
 )
 SELECT 
     f.*,
     COUNT(*) OVER() AS TotalRecords
 FROM FilteredData AS f
 ORDER BY f.last_transaction_date DESC
 LIMIT @PageSize OFFSET @Offset;
    ";

            try
            {
                using var connection = sqlConnection.GetConnection(); // returns SQLiteConnection
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;

                int offset = (pageNumber - 1) * pageSize;

                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);
                cmd.Parameters.AddWithValue("@SearchTerm", searchTerm ?? string.Empty);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new BalanceDueDto
                    {
                        CustomerId = !reader.IsDBNull(0) ? reader.GetInt32(0) : 0,
                        CustomerName = !reader.IsDBNull(1) ? reader.GetString(1) : string.Empty,
                        CustomerAddress = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty,
                        PhoneNo = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty,
                        Email = !reader.IsDBNull(4) ? reader.GetString(4) : string.Empty,
                        TotalDueAmount = !reader.IsDBNull(5) ? Convert.ToDecimal(reader.GetValue(5)) : 0,
                        DueId = !reader.IsDBNull(6) ? reader.GetInt32(6) : 0,
                        LastTransactionDate = !reader.IsDBNull(7) ? reader.GetString(7) : string.Empty,
                        totalrecords = !reader.IsDBNull(8) ? reader.GetInt32(8) : 0
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve balance due list from SQLite. Please try again later.", ex);
            }

            return result;
        }

        public async Task<int> SaveCustomerbalancesettlement(SettleBalance settlebalance)
        {
            try
            {
                using (var connection = sqlConnection.GetConnection())
                {
                    await connection.OpenAsync();
                    using var transaction = connection.BeginTransaction();

                    try
                    {
                        // 1️⃣ Insert into Customer_ledger
                        using (var insertCmd = connection.CreateCommand())
                        {
                            insertCmd.Transaction = transaction;
                            insertCmd.CommandText = @"
INSERT INTO Customer_ledger
    (customer_id, paid_amt, balance_due, total_amt, payment_mode, transaction_type, create_by, create_at, payid, transaction_type_id)
VALUES
    (@CustomerID, @PaidAmt, @BalanceDue, @TotalAmt, @PaymentMode, @TransactionType, @CreatedBy, @CreatedAt, @PayID, @DueID);
SELECT last_insert_rowid();";

                            insertCmd.CommandType = CommandType.Text;

                            insertCmd.Parameters.Add(new SqliteParameter("@CustomerID", DbType.Int32) { Value = settlebalance.customerid });
                            insertCmd.Parameters.Add(new SqliteParameter("@PaidAmt", DbType.Decimal) { Value = settlebalance.settledamount });
                            insertCmd.Parameters.Add(new SqliteParameter("@BalanceDue", DbType.Decimal) { Value = settlebalance.remainingamount });
                            insertCmd.Parameters.Add(new SqliteParameter("@TotalAmt", DbType.Decimal) { Value = settlebalance.settledamount + settlebalance.remainingamount });
                            insertCmd.Parameters.Add(new SqliteParameter("@PaymentMode", DbType.Int32) { Value = settlebalance.paymode });
                            insertCmd.Parameters.Add(new SqliteParameter("@TransactionType", DbType.String) { Value = "DUE SETTLEMENT" });
                            insertCmd.Parameters.Add(new SqliteParameter("@CreatedBy", DbType.Int32) { Value = settlebalance.createby });
                            insertCmd.Parameters.Add(new SqliteParameter("@CreatedAt", DbType.DateTime) { Value = DateTime.Now });
                            insertCmd.Parameters.Add(new SqliteParameter("@PayID", DbType.Int32) { Value = settlebalance.payid });
                            insertCmd.Parameters.Add(new SqliteParameter("@DueID", DbType.Int32) { Value = settlebalance.dueid });

                            var ledgerResult = await insertCmd.ExecuteScalarAsync();
                            if (ledgerResult == null || Convert.ToInt32(ledgerResult) <= 0)
                            {
                                throw new Exception("Customer Ledger insertion failed. Please try again.");
                            }

                            int insertedId = Convert.ToInt32(ledgerResult);

                            // 2️⃣ Update balance_due
                            using (var updateCmd = connection.CreateCommand())
                            {
                                updateCmd.Transaction = transaction;
                                updateCmd.CommandText = @"
UPDATE balance_due
SET total_due_amount = @RemainingAmount,
    updateby = @UpdatedBy,
    updateat = @UpdatedAt
WHERE dueid = @DueID AND status = 1;";

                                updateCmd.Parameters.AddWithValue("@RemainingAmount", settlebalance.remainingamount);
                                updateCmd.Parameters.AddWithValue("@UpdatedBy", settlebalance.createby);
                                updateCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                                updateCmd.Parameters.AddWithValue("@DueID", settlebalance.dueid);

                                await updateCmd.ExecuteNonQueryAsync();
                            }

                            // ✅ Commit transaction
                            transaction.Commit();
                            return insertedId;
                        }
                    }
                    catch
                    {
                        // ❌ Rollback on error
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (SqliteException)
            {
                throw new Exception("Database operation failed. Please try again later.");
            }
        }

        #endregion
    }
}
