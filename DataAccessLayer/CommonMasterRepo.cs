using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DATABASE;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FLEXIERP.DataAccessLayer
{
    public class CommonMasterRepo : ICommonMasterRepo
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountRepo> _logger;
        private readonly IDataBaseOperation sqlConnection;

        public CommonMasterRepo(IConfiguration configuration, ILogger<AccountRepo> logger, IDataBaseOperation _sqlConnection)
        {
            _configuration = configuration;
            this._logger = logger;
            this.sqlConnection = _sqlConnection;
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
    }
}
