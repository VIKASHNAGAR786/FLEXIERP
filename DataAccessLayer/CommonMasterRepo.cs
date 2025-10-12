using DocumentFormat.OpenXml.Spreadsheet;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DATABASE;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace FLEXIERP.DataAccessLayer
{
    public class CommonMasterRepo : ICommonMasterRepo
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountRepo> _logger;
        private readonly IDataBaseOperation sqlConnection;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CommonMasterRepo(IConfiguration configuration, ILogger<AccountRepo> logger, IDataBaseOperation _sqlConnection, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            this._logger = logger;
            this.sqlConnection = _sqlConnection;
            _httpContextAccessor = httpContextAccessor;
        }

        #region payment methods
        public async Task<int> SaveChequePaymentAsync(SaveChequePaymentDto chequePayment)
        {
            try
            {
                using (var connection = sqlConnection.GetConnection())
                {
                    await connection.OpenAsync();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SaveChequePayment";
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parameters
                        cmd.Parameters.Add(new SqlParameter("@chequenumber", SqlDbType.NVarChar)
                        {
                            Value = string.IsNullOrEmpty(chequePayment.ChequeNumber) ? DBNull.Value : chequePayment.ChequeNumber
                        });

                        cmd.Parameters.Add(new SqlParameter("@bankname", SqlDbType.NVarChar)
                        {
                            Value = string.IsNullOrEmpty(chequePayment.BankName) ? DBNull.Value : chequePayment.BankName
                        });

                        cmd.Parameters.Add(new SqlParameter("@branchname", SqlDbType.NVarChar)
                        {
                            Value = string.IsNullOrEmpty(chequePayment.BranchName) ? DBNull.Value : chequePayment.BranchName
                        });

                        cmd.Parameters.Add(new SqlParameter("@chequedate", SqlDbType.Date)
                        {
                            Value = chequePayment.ChequeDate == null ? DBNull.Value : chequePayment.ChequeDate
                        });

                        cmd.Parameters.Add(new SqlParameter("@amount", SqlDbType.Decimal)
                        {
                            Value = chequePayment.Amount
                        });

                        cmd.Parameters.Add(new SqlParameter("@ifsc_code", SqlDbType.NVarChar)
                        {
                            Value = string.IsNullOrEmpty(chequePayment.IFSC_Code) ? DBNull.Value : chequePayment.IFSC_Code
                        });

                        cmd.Parameters.Add(new SqlParameter("@create_by", SqlDbType.Int)
                        {
                            Value = chequePayment.CreatedBy
                        });

                        // Execute stored procedure and get inserted ID
                        var insertedIdObj = await cmd.ExecuteScalarAsync();

                        int insertedId = insertedIdObj != null ? Convert.ToInt32(insertedIdObj) : 0;

                        return insertedId;
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Error while saving cheque payment.", ex);
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }
        }

        public async Task<int> SaveCashPaymentAsync(SaveCashPaymentDto cashPayment)
        {
            try
            {
                using (var connection = sqlConnection.GetConnection())
                {
                    await connection.OpenAsync();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SaveCashPayment";
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parameters
                        cmd.Parameters.Add(new SqlParameter("@amount", SqlDbType.Decimal)
                        {
                            Value = cashPayment.Amount
                        });

                        cmd.Parameters.Add(new SqlParameter("@payment_date", SqlDbType.Date)
                        {
                            Value = cashPayment.PaymentDate == null ? DBNull.Value : cashPayment.PaymentDate
                        });

                        cmd.Parameters.Add(new SqlParameter("@create_by", SqlDbType.Int)
                        {
                            Value = cashPayment.CreatedBy
                        });

                        // Execute SP and get inserted ID
                        var insertedIdObj = await cmd.ExecuteScalarAsync();
                        int insertedId = insertedIdObj != null ? Convert.ToInt32(insertedIdObj) : 0;

                        return insertedId;
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Error while saving cash payment.", ex);
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }
        }

        #endregion

        #region DashBoard
        public async Task<DashboardMetricsDto?> GetDashboardMetricsAsync(string startDate, string endDate)
        {
            DashboardMetricsDto? metrics = null;
            try
            {
                using var connection = sqlConnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "GetDashboardMetrics";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.VarChar) { Value = startDate });
                cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.VarChar) { Value = endDate });
                cmd.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = 50 }); // optional page size

                using var reader = await cmd.ExecuteReaderAsync();

                // First result set: metrics
                if (await reader.ReadAsync())
                {
                    metrics = new DashboardMetricsDto
                    {
                        TotalCashReceived = !reader.IsDBNull(0) ? reader.GetDecimal(0) : 0,
                        TotalChequeReceived = !reader.IsDBNull(1) ? reader.GetDecimal(1) : 0,
                        CashGrowthPercent = !reader.IsDBNull(2) ? reader.GetDecimal(2) : 0,
                        ChequeGrowthPercent = !reader.IsDBNull(3) ? reader.GetDecimal(3) : 0,
                        TotalBalanceDue = !reader.IsDBNull(4) ? reader.GetDecimal(4) : 0,
                    };
                }

                // Move to second result set: recent transactions

                List<TransactionDto?> transactionDtos = new List<TransactionDto?>();
                if (await reader.NextResultAsync() && metrics != null)
                {
                    while (await reader.ReadAsync())
                    {
                        var data = new TransactionDto
                        {
                            Date = !reader.IsDBNull(0) ? reader.GetString(0) : "",
                            Time = !reader.IsDBNull(1) ? reader.GetString(1) : "",
                            CustomerName = !reader.IsDBNull(2) ? reader.GetString(2) : "",
                            ReceivedAmount = !reader.IsDBNull(3) ? reader.GetDecimal(3) : 0,
                            BalanceDue = !reader.IsDBNull(4) ? reader.GetDecimal(4) : 0,
                            TotalAmount = !reader.IsDBNull(5) ? reader.GetDecimal(5) : 0,
                            PaymentType = !reader.IsDBNull(6) ? reader.GetString(6) : "",
                            TransactionType = !reader.IsDBNull(7) ? reader.GetString(7) : ""
                        };
                        transactionDtos.Add(data);
                    }
                }
                metrics.recenttransaction = transactionDtos;
                return metrics;
            }
            catch (SqlException ex)
            {
                throw new Exception("Failed to retrieve dashboard metrics. Please try again later.", ex);
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }
        }
        #endregion

        #region error log
        public async Task<int> SaveUserErrorLogAsync(UserErrorLogDto errorLog)
        {
            try
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? userId = null;

                if (int.TryParse(userIdClaim, out int parsedId))
                {
                    userId = parsedId;
                }

                using (var connection = sqlConnection.GetConnection())
                {
                    await connection.OpenAsync();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "InsertUserErrorLog";
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parameters
                        cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int)
                        {
                            Value = userId
                        });

                        cmd.Parameters.Add(new SqlParameter("@Module", SqlDbType.VarChar, 100)
                        {
                            Value = string.IsNullOrEmpty(errorLog.Module) ? DBNull.Value : errorLog.Module
                        });

                        cmd.Parameters.Add(new SqlParameter("@ActionType", SqlDbType.VarChar, 100)
                        {
                            Value = string.IsNullOrEmpty(errorLog.ActionType) ? DBNull.Value : errorLog.ActionType
                        });

                        cmd.Parameters.Add(new SqlParameter("@ErrorMessage", SqlDbType.NVarChar)
                        {
                            Value = errorLog.ErrorMessage ?? (object)DBNull.Value
                        });

                        cmd.Parameters.Add(new SqlParameter("@ErrorCode", SqlDbType.VarChar, 50)
                        {
                            Value = string.IsNullOrEmpty(errorLog.ErrorCode) ? DBNull.Value : errorLog.ErrorCode
                        });

                        cmd.Parameters.Add(new SqlParameter("@StackTrace", SqlDbType.NVarChar)
                        {
                            Value = string.IsNullOrEmpty(errorLog.StackTrace) ? DBNull.Value : errorLog.StackTrace
                        });

                        cmd.Parameters.Add(new SqlParameter("@ApiName", SqlDbType.VarChar, 200)
                        {
                            Value = string.IsNullOrEmpty(errorLog.ApiName) ? DBNull.Value : errorLog.ApiName
                        });

                        cmd.Parameters.Add(new SqlParameter("@Severity", SqlDbType.VarChar, 20)
                        {
                            Value = string.IsNullOrEmpty(errorLog.Severity) ? "ERROR" : errorLog.Severity
                        });

                        cmd.Parameters.Add(new SqlParameter("@AdditionalInfo", SqlDbType.NVarChar)
                        {
                            Value = string.IsNullOrEmpty(errorLog.AdditionalInfo) ? DBNull.Value : errorLog.AdditionalInfo
                        });

                        cmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime)
                        {
                            Value = DateTime.Now
                        });


                        // Execute SP and return affected rows (should be 1 if success)
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected;
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Error while saving user error log.", ex);
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }
        }

        #endregion

        #region Cheque Details
        public async Task<List<ReceivedChequeDto>> GetReceivedChequesAsync(PaginationFilter pagination)
        {
            var cheques = new List<ReceivedChequeDto>();

            try
            {
                using var connection = sqlConnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "sp_GetReceivedCheques";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@PageNo", SqlDbType.Int) { Value = pagination.PageNo });
                cmd.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pagination.PageSize });
                cmd.Parameters.Add(new SqlParameter("@SearchText", SqlDbType.NVarChar, 100) { Value = pagination.SearchTerm ?? string.Empty });

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var cheque = new ReceivedChequeDto
                    {
                        SrNo = !reader.IsDBNull(0) ? reader.GetInt64(0) : 0,
                        CustomerName = !reader.IsDBNull(1) ? reader.GetString(1) : string.Empty,
                        CustomerAddress = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty,
                        PhoneNo = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty,
                        ChequeNumber = !reader.IsDBNull(4) ? reader.GetString(4) : string.Empty,
                        BankName = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty,
                        BranchName = !reader.IsDBNull(6) ? reader.GetString(6) : string.Empty,
                        ChequeDate = !reader.IsDBNull(7) ? reader.GetString(7) : string.Empty,
                        Amount = !reader.IsDBNull(8) ? reader.GetDecimal(8) : 0,
                        ifsc_Code = !reader.IsDBNull(9) ? reader.GetString(9) : string.Empty,
                        CreatedAt = !reader.IsDBNull(10) ? reader.GetString(10) : string.Empty,
                        FullName = !reader.IsDBNull(11) ? reader.GetString(11) : string.Empty,
                        TotalRecords = !reader.IsDBNull(12) ? reader.GetInt32(12) : 0
                    };

                    cheques.Add(cheque);
                }

                return cheques;
            }
            catch (SqlException ex)
            {
                throw new Exception("Failed to retrieve received cheques. Please try again later.", ex);
            }
            finally
            {
                await sqlConnection.GetConnection().CloseAsync();
            }
        }

        #endregion
    }
}
