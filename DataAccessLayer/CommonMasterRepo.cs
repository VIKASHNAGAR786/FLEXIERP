using DocumentFormat.OpenXml.Spreadsheet;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DATABASE;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
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

        #region Number System
        public async Task<string> GetInvoiceNumber()
        {
            using var connection = sqlConnection.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                string prefix = "";
                int lastInvoice = 0;

                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"SELECT prefix, last_invoice_number 
                                FROM InvoiceNumberSystem 
                                WHERE id = 1;";

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        prefix = reader["prefix"]?.ToString() ?? "";
                        lastInvoice = Convert.ToInt32(reader["last_invoice_number"]);
                    }
                    else
                    {
                        throw new Exception("InvoiceNumberSystem table not initialized.");
                    }
                }

                // build invoice number ex: 2025/1
                string invoiceNo = $"{prefix}{lastInvoice}";

                transaction.Commit();
                return invoiceNo;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> UpdateInvoiceNumber(int updatedBy)
        {
            using var connection = sqlConnection.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                int rowsAffected = 0;

                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;

                cmd.CommandText = @"
                    UPDATE InvoiceNumberSystem
                    SET 
                        last_invoice_number = last_invoice_number + 1,
                        updated_at = CURRENT_TIMESTAMP,
                        updated_by = @updated_by
                    WHERE id = 1;

                    SELECT changes();
                    ";

                cmd.Parameters.AddWithValue("@updated_by", updatedBy);

                var result = await cmd.ExecuteScalarAsync();
                rowsAffected = Convert.ToInt32(result);

                transaction.Commit();
                return rowsAffected;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        #endregion

        #region payment methods
        public async Task<int> SaveChequePaymentAsync(SaveChequePaymentDto chequePayment)
        {
            int insertedId = 0;

            using var connection = sqlConnection.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
            INSERT INTO ChequePayments
            (chequenumber, bankname, branchname, chequedate, amount, ifsc_code, create_by)
            VALUES (@chequenumber, @bankname, @branchname, @chequedate, @amount, @ifsc_code, @create_by);
            SELECT last_insert_rowid();";

                // Parameters
                cmd.Parameters.AddWithValue("@chequenumber", (object?)chequePayment.ChequeNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@bankname", (object?)chequePayment.BankName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@branchname", (object?)chequePayment.BranchName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@chequedate", (object?)chequePayment.ChequeDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@amount", chequePayment.Amount);
                cmd.Parameters.AddWithValue("@ifsc_code", (object?)chequePayment.IFSC_Code ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@create_by", chequePayment.CreatedBy);

                // Execute and get the inserted ID
                var result = await cmd.ExecuteScalarAsync();
                insertedId = result != null ? Convert.ToInt32(result) : 0;

                transaction.Commit();
                return insertedId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<int> SaveCashPaymentAsync(SaveCashPaymentDto cashPayment)
        {
            int insertedId = 0;

            using var connection = sqlConnection.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
            INSERT INTO CashPayments
            (amount, payment_date, create_by)
            VALUES (@amount, COALESCE(@payment_date, DATE('now')), @create_by);
            SELECT last_insert_rowid();";

                // Parameters
                cmd.Parameters.AddWithValue("@amount", cashPayment.Amount);
                cmd.Parameters.AddWithValue("@payment_date", (object?)cashPayment.PaymentDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@create_by", cashPayment.CreatedBy);

                // Execute and get the inserted ID
                var result = await cmd.ExecuteScalarAsync();
                insertedId = result != null ? Convert.ToInt32(result) : 0;

                transaction.Commit();
                return insertedId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<int> SaveBankTransferPaymentAsync(SaveBankTransferPaymentDto payment)
        {
            int insertedId = 0;

            using var connection = sqlConnection.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
            INSERT INTO BankTransferPayments
            (
                company_bank_id,
                transfer_type,
                amount,
                currency,
                charges,
                final_amount_received,
                transaction_date,
                received_date,
                posted_date,
                status,
                is_reconciled,
                reconciliation_date,
                customer_bank_name,
                customer_account_number,
                customer_ifsc,
                customer_branch,
                utr_number,
                reference_number,
                payment_description,
                remarks,
                create_by
            )
            VALUES
            (
                @company_bank_id,
                @transfer_type,
                @amount,
                @currency,
                @charges,
                @final_amount_received,
                @transaction_date,
                @received_date,
                @posted_date,
                @status,
                @is_reconciled,
                @reconciliation_date,
                @customer_bank_name,
                @customer_account_number,
                @customer_ifsc,
                @customer_branch,
                @utr_number,
                @reference_number,
                @payment_description,
                @remarks,
                @create_by
            );
            SELECT last_insert_rowid();";

                // Parameters with explicit type and direction
                cmd.Parameters.Add(new SqliteParameter("@company_bank_id", DbType.Int32) { Value = payment.company_bank_id });
                cmd.Parameters.Add(new SqliteParameter("@transfer_type", DbType.String) { Value = payment.transfer_type });
                cmd.Parameters.Add(new SqliteParameter("@amount", DbType.Decimal) { Value = payment.amount });
                cmd.Parameters.Add(new SqliteParameter("@currency", DbType.String) { Value = string.IsNullOrEmpty(payment.currency) ? "INR" : payment.currency });
                cmd.Parameters.Add(new SqliteParameter("@charges", DbType.Decimal) { Value = payment.charges ?? 0 });
                cmd.Parameters.Add(new SqliteParameter("@final_amount_received", DbType.Decimal) { Value = (object?)payment.final_amount_received ?? DBNull.Value });
                cmd.Parameters.Add(new SqliteParameter("@transaction_date", DbType.DateTime) { Value = payment.transaction_date });
                cmd.Parameters.Add(new SqliteParameter("@received_date", DbType.DateTime) { Value = (object?)payment.received_date ?? DBNull.Value });
                cmd.Parameters.Add(new SqliteParameter("@posted_date", DbType.DateTime) { Value = (object?)payment.posted_date ?? DBNull.Value });
                cmd.Parameters.Add(new SqliteParameter("@status", DbType.String) { Value = string.IsNullOrEmpty(payment.status) ? "Pending" : payment.status });
                cmd.Parameters.Add(new SqliteParameter("@is_reconciled", DbType.Int32) { Value = payment.is_reconciled ? 1 : 0 });
                cmd.Parameters.Add(new SqliteParameter("@reconciliation_date", DbType.DateTime) { Value = (object?)payment.reconciliation_date ?? DBNull.Value });
                cmd.Parameters.Add(new SqliteParameter("@customer_bank_name", DbType.String) { Value = (object?)payment.customer_bank_name ?? DBNull.Value });
                cmd.Parameters.Add(new SqliteParameter("@customer_account_number", DbType.String) { Value = (object?)payment.customer_account_number ?? DBNull.Value });
                cmd.Parameters.Add(new SqliteParameter("@customer_ifsc", DbType.String) { Value = (object?)payment.customer_ifsc ?? DBNull.Value });
                cmd.Parameters.Add(new SqliteParameter("@customer_branch", DbType.String) { Value = (object?)payment.customer_branch ?? DBNull.Value });
                cmd.Parameters.Add(new SqliteParameter("@utr_number", DbType.String) { Value = payment.utr_number });
                cmd.Parameters.Add(new SqliteParameter("@reference_number", DbType.String) { Value = (object?)payment.reference_number ?? DBNull.Value });
                cmd.Parameters.Add(new SqliteParameter("@payment_description", DbType.String) { Value = (object?)payment.payment_description ?? DBNull.Value });
                cmd.Parameters.Add(new SqliteParameter("@remarks", DbType.String) { Value = (object?)payment.remarks ?? DBNull.Value });
                cmd.Parameters.Add(new SqliteParameter("@create_by", DbType.Int32) { Value = payment.create_by });

                // Execute and get the inserted ID
                var result = await cmd.ExecuteScalarAsync();
                insertedId = result != null ? Convert.ToInt32(result) : 0;

                transaction.Commit();
                return insertedId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
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
                cmd.CommandText = @$"
                                                -- Parameters: @StartDate, @EndDate, @PageSize
                                                -- If dates are NULL, use last 7 days
                                                -- Replace parameters in C# using cmd.Parameters.AddWithValue

                                                -- Set default dates in C# if not provided:
                                                -- startDate = startDate ?? DateTime.UtcNow.AddDays(-7);
                                                -- endDate = endDate ?? DateTime.UtcNow;

                                                -------------------------
                                                -- 1. Dashboard Metrics
                                                -------------------------

                                                SELECT 
                                                    COALESCE((SELECT SUM(amount) FROM CashPayments 
                                                              WHERE payment_date >= @StartDate 
                                                                AND payment_date < date(@EndDate, '+1 day')), 0) AS TotalCashReceived,

                                                    COALESCE((SELECT SUM(amount) FROM ChequePayments 
                                                              WHERE chequedate >= @StartDate 
                                                                AND chequedate < date(@EndDate, '+1 day')), 0) AS TotalChequeReceived,

                                                    CASE 
                                                        WHEN (SELECT SUM(amount) FROM CashPayments 
                                                              WHERE payment_date >= date(@StartDate, '-1 month') 
                                                                AND payment_date < date(@EndDate, '-1 month', '+1 day')) = 0 
                                                        THEN NULL
                                                        ELSE ROUND(
                                                            ((COALESCE((SELECT SUM(amount) FROM CashPayments 
                                                                        WHERE payment_date >= @StartDate 
                                                                          AND payment_date < date(@EndDate, '+1 day')), 0) -
                                                              COALESCE((SELECT SUM(amount) FROM CashPayments 
                                                                        WHERE payment_date >= date(@StartDate, '-1 month') 
                                                                          AND payment_date < date(@EndDate, '-1 month', '+1 day')), 0)) 
                                                             / COALESCE((SELECT SUM(amount) FROM CashPayments 
                                                                         WHERE payment_date >= date(@StartDate, '-1 month') 
                                                                           AND payment_date < date(@EndDate, '-1 month', '+1 day')), 1)) * 100, 2)
                                                    END AS CashGrowthPercent,

                                                    CASE 
                                                        WHEN (SELECT SUM(amount) FROM ChequePayments 
                                                              WHERE chequedate >= date(@StartDate, '-1 month') 
                                                                AND chequedate < date(@EndDate, '-1 month', '+1 day')) = 0 
                                                        THEN NULL
                                                        ELSE ROUND(
                                                            ((COALESCE((SELECT SUM(amount) FROM ChequePayments 
                                                                        WHERE chequedate >= @StartDate 
                                                                          AND chequedate < date(@EndDate, '+1 day')), 0) -
                                                              COALESCE((SELECT SUM(amount) FROM ChequePayments 
                                                                        WHERE chequedate >= date(@StartDate, '-1 month') 
                                                                          AND chequedate < date(@EndDate, '-1 month', '+1 day')), 0)) 
                                                             / COALESCE((SELECT SUM(amount) FROM ChequePayments 
                                                                         WHERE chequedate >= date(@StartDate, '-1 month') 
                                                                           AND chequedate < date(@EndDate, '-1 month', '+1 day')), 1)) * 100, 2)
                                                    END AS ChequeGrowthPercent,

                                                    COALESCE((SELECT SUM(balance_due) FROM Customer_Ledger 
                                                              WHERE create_at >= @StartDate 
                                                                AND create_at < date(@EndDate, '+1 day')), 0) AS TotalBalanceDue;

                                                -------------------------
                                                -- 2. Recent Transactions
                                                -------------------------

                                                SELECT
                                                    date(datetime(substr(cl.create_at, 1, 19))) AS Date,
                                                    time(datetime(substr(cl.create_at, 1, 19))) AS Time,
                                                    cs.CustomerName,
                                                    CASE 
                                                        WHEN cl.payment_mode = 1 THEN cash.amount
                                                        WHEN cl.payment_mode = 2 THEN cheque.amount
                                                        ELSE 0
                                                    END AS ReceivedAmount,
                                                    cl.balance_due,
                                                    cl.total_amt,
                                                    CASE 
                                                        WHEN cl.payment_mode = 1 THEN 'Cash'
                                                        WHEN cl.payment_mode = 2 THEN 'Cheque'
                                                        WHEN cl.payment_mode = 3 THEN 'BANK TRANSFER'
                                                        ELSE 'Unknown'
                                                    END AS PaymentType,
                                                    cl.transaction_type
                                                FROM Customer_Ledger cl
                                                LEFT JOIN Customer cs ON cl.customer_id = cs.CustomerID
                                                LEFT JOIN CashPayments cash ON cl.payid = cash.id AND cl.payment_mode = 1
                                                LEFT JOIN ChequePayments cheque ON cl.payid = cheque.id AND cl.payment_mode = 2
                                                WHERE cl.create_at >= @StartDate
                                                  AND cl.create_at < date(@EndDate, '+1 day')
                                                ORDER BY cl.create_at DESC
                                                LIMIT @PageSize;

";
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.Add(new SqliteParameter("@StartDate", SqlDbType.VarChar) { Value = startDate });
                cmd.Parameters.Add(new SqliteParameter("@EndDate", SqlDbType.VarChar) { Value = endDate });
                cmd.Parameters.Add(new SqliteParameter("@PageSize", SqlDbType.Int) { Value = 50 }); // optional page size

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

                using (var connection = sqlConnection.GetConnection()) // SQLiteConnection
                {
                    await connection.OpenAsync();

                    string sql = @"INSERT INTO UserErrorLog
                (userid, module, actiontype, errormessage, errorcode, stacktrace, apiname, CreatedAt, Severity, AdditionalInfo)
                VALUES
                (@UserId, @Module, @ActionType, @ErrorMessage, @ErrorCode, @StackTrace, @ApiName, @CreatedAt, @Severity, @AdditionalInfo);";

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        cmd.CommandType = CommandType.Text; // Not StoredProcedure

                        // Parameters
                        cmd.Parameters.AddWithValue("@UserId", userId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Module", string.IsNullOrEmpty(errorLog.Module) ? DBNull.Value : errorLog.Module);
                        cmd.Parameters.AddWithValue("@ActionType", string.IsNullOrEmpty(errorLog.ActionType) ? DBNull.Value : errorLog.ActionType);
                        cmd.Parameters.AddWithValue("@ErrorMessage", errorLog.ErrorMessage ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ErrorCode", string.IsNullOrEmpty(errorLog.ErrorCode) ? DBNull.Value : errorLog.ErrorCode);
                        cmd.Parameters.AddWithValue("@StackTrace", string.IsNullOrEmpty(errorLog.StackTrace) ? DBNull.Value : errorLog.StackTrace);
                        cmd.Parameters.AddWithValue("@ApiName", string.IsNullOrEmpty(errorLog.ApiName) ? DBNull.Value : errorLog.ApiName);
                        cmd.Parameters.AddWithValue("@Severity", string.IsNullOrEmpty(errorLog.Severity) ? "ERROR" : errorLog.Severity);
                        cmd.Parameters.AddWithValue("@AdditionalInfo", string.IsNullOrEmpty(errorLog.AdditionalInfo) ? DBNull.Value : errorLog.AdditionalInfo);
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                        // Execute and return affected rows
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected;
                    }
                }
            }
            catch (Exception ex)
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
            string query = @"
        WITH ChequeCTE AS (
            SELECT 
                cs.CustomerName,
                cs.CustomerAddress, 
                cs.PhoneNo,
                cheque.chequenumber,
                cheque.bankname,
                cheque.branchname,
                cheque.chequedate,
                cheque.amount,
                cheque.ifsc_code,
                cheque.createat,
                userdata.FullName,
                ROW_NUMBER() OVER (ORDER BY cheque.createat DESC) AS RowNum
            FROM Customer_Ledger AS cl
            LEFT JOIN Customer AS cs ON cl.customer_id = cs.CustomerID
            LEFT JOIN ChequePayments AS cheque ON cl.payid = cheque.id AND cl.payment_mode = 2
            LEFT JOIN Tbl_Users AS userdata ON cl.create_by = userdata.UserID
            WHERE cl.payment_mode = 2
              AND (
                    @SearchText = '' OR
                    cs.CustomerName LIKE '%' || @SearchText || '%' OR
                    cheque.chequenumber LIKE '%' || @SearchText || '%' OR
                    cheque.bankname LIKE '%' || @SearchText || '%' OR
                    cheque.branchname LIKE '%' || @SearchText || '%' OR
                    userdata.FullName LIKE '%' || @SearchText || '%'
                  )
        )
        SELECT 
            ((@PageNo - 1) * @PageSize) + ROW_NUMBER() OVER (ORDER BY RowNum DESC) AS SrNo,
            CustomerName,
            CustomerAddress,
            PhoneNo,
            chequenumber,
            bankname,
            branchname,
            STRFTIME('%d/%m/%Y, %I:%M %p', chequedate) AS ChequeDate,
            amount AS Amount,
            ifsc_code AS IFSC_Code,
            STRFTIME('%d/%m/%Y, %I:%M %p', createat) AS CreatedAt,
            FullName,
            (SELECT COUNT(*) FROM ChequeCTE) AS TotalRecords
        FROM ChequeCTE
        WHERE RowNum BETWEEN ((@PageNo - 1) * @PageSize + 1) AND (@PageNo * @PageSize)
        ORDER BY RowNum DESC;
    ";

            try
            {
                using var connection = sqlConnection.GetConnection(); // returns SqliteConnection
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.AddWithValue("@PageNo", pagination.PageNo);
                cmd.Parameters.AddWithValue("@PageSize", pagination.PageSize);
                cmd.Parameters.AddWithValue("@SearchText", pagination.SearchTerm ?? string.Empty);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    cheques.Add(new ReceivedChequeDto
                    {
                        SrNo = reader["SrNo"] != DBNull.Value ? Convert.ToInt64(reader["SrNo"]) : 0,
                        CustomerName = reader["CustomerName"]?.ToString() ?? string.Empty,
                        CustomerAddress = reader["CustomerAddress"]?.ToString() ?? string.Empty,
                        PhoneNo = reader["PhoneNo"]?.ToString() ?? string.Empty,
                        ChequeNumber = reader["chequenumber"]?.ToString() ?? string.Empty,
                        BankName = reader["bankname"]?.ToString() ?? string.Empty,
                        BranchName = reader["branchname"]?.ToString() ?? string.Empty,
                        ChequeDate = reader["ChequeDate"]?.ToString() ?? string.Empty,
                        Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0,
                        ifsc_Code = reader["IFSC_Code"]?.ToString() ?? string.Empty,
                        CreatedAt = reader["CreatedAt"]?.ToString() ?? string.Empty,
                        FullName = reader["FullName"]?.ToString() ?? string.Empty,
                        TotalRecords = reader["TotalRecords"] != DBNull.Value ? Convert.ToInt32(reader["TotalRecords"]) : 0
                    });
                }

                return cheques;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve received cheques from SQLite. Please try again later.", ex);
            }
        }

        #endregion

        #region Notes
        public async Task<int> SaveNoteAsync(SaveNotes note)
        {
            int insertedId = 0;

            using var connection = sqlConnection.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                                INSERT INTO flexi_notes (
                                    id, title, content, created_at, updated_at, author_id, is_pinned, is_archived, CreatedBy, UpdatedBy
                                )
                                VALUES (
                                    NULLIF(@id, 0), @title, @content, @created_at, @updated_at, @author_id, @is_pinned, @is_archived, @CreatedBy, NULL
                                )
                                ON CONFLICT(id) DO UPDATE SET
                                    title = excluded.title,
                                    content = excluded.content,
                                    updated_at = excluded.updated_at,
                                    author_id = excluded.author_id,
                                    is_pinned = excluded.is_pinned,
                                    is_archived = excluded.is_archived,
                                    UpdatedBy = @UpdatedBy;
                                ";

                // Parameters
                cmd.Parameters.AddWithValue("@title", (object?)note.Title ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@content", (object?)note.Content ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@created_at", DateTime.Now);
                cmd.Parameters.AddWithValue("@updated_at", DateTime.Now);
                cmd.Parameters.AddWithValue("@author_id", note.AuthorId);
                cmd.Parameters.AddWithValue("@is_pinned", note.IsPinned);
                cmd.Parameters.AddWithValue("@is_archived", note.IsArchived);
                cmd.Parameters.AddWithValue("@CreatedBy", note.CreatedBy);
                cmd.Parameters.AddWithValue("@id", note.notesid);
                cmd.Parameters.AddWithValue("@UpdatedBy", note.CreatedBy);

                // Execute and get the inserted ID
                var result = await cmd.ExecuteScalarAsync();
                insertedId = result != null ? Convert.ToInt32(result) : 0;

                transaction.Commit();
                return insertedId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<List<NoteDto>> GetAllNotesAsync()
        {
            var notes = new List<NoteDto>();
            var connection = sqlConnection.GetConnection();

            try
            {
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                                      SELECT 
  notes.id,
  notes.title,
  substr(notes.content, 1, 15) || '....' AS content,
  notes.created_at,
  notes.updated_at,
  user.FullName,
  notes.is_pinned,
  notes.is_archived,
  user.FullName,
  user.FullName,
  notes.status
FROM flexi_notes AS notes
LEFT JOIN Tbl_Users AS user ON notes.author_id = user.userid
WHERE notes.status = 1
ORDER BY notes.is_pinned DESC, notes.created_at DESC;
                            ";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var note = new NoteDto
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Content = reader.GetString(2),
                        CreatedAt = reader.GetDateTime(3),
                        UpdatedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                        AuthorId = reader.GetString(5),
                        IsPinned = reader.GetBoolean(6),
                        IsArchived = reader.GetBoolean(7),
                        CreatedBy = reader.GetString(8),
                        UpdatedBy = reader.IsDBNull(9) ? null : reader.GetString(9),
                        Status = reader.GetBoolean(10)
                    };
                    notes.Add(note);
                }

                return notes;
            }
            catch (Exception ex)
            {
                // Optional: log the exception here
                throw new Exception("Error retrieving notes", ex);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<NoteDetailsDto> GetNoteDetailsByIdAsync(int rowid)
        {
            var connection = sqlConnection.GetConnection();

            try
            {
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
            SELECT 
                notes.title,
                notes.content AS content,
                strftime('%Y-%m-%d %I:%M:%S %p', notes.created_at) AS created_at,
                strftime('%Y-%m-%d %I:%M:%S %p', notes.updated_at) AS updated_at,
                user.FullName AS author_name,
                notes.is_pinned,
                notes.is_archived,
                user.FullName AS created_by_name,
                user.FullName AS updated_by_name,
                notes.status
            FROM flexi_notes AS notes
            LEFT JOIN Tbl_Users AS user ON notes.author_id = user.userid
            WHERE notes.status = 1 AND notes.id = @rowid
            ORDER BY notes.created_at DESC;
        ";

                cmd.Parameters.AddWithValue("@rowid", rowid);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var note = new NoteDetailsDto
                    {
                        title = reader.GetString(0),
                        content = reader.GetString(1),
                        createdat = reader.GetString(2),
                        updatedat = reader.IsDBNull(3) ? null : reader.GetString(3),
                        authorname = reader.GetString(4),
                        ispinned = reader.GetBoolean(5),
                        isarchived = reader.GetBoolean(6),
                        createdbyname = reader.GetString(7),
                        updatedbyname = reader.GetString(8),
                        status = reader.GetBoolean(9)
                    };
                    return note;
                }

                return new NoteDetailsDto();
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving note details", ex);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<int> DeleteNotesById(int deletednotsid)
        {

            int insertedId = 0;

            using var connection = sqlConnection.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                               UPDATE flexi_notes
SET status = 0
WHERE id = @id;

SELECT CASE WHEN changes() > 0 THEN 1 ELSE 0 END AS is_deleted;
                                ";

                // Parameters
                cmd.Parameters.AddWithValue("@id", (object?)deletednotsid);

                // Execute and get the inserted ID
                var result = await cmd.ExecuteScalarAsync();
                insertedId = result != null ? Convert.ToInt32(result) : 0;

                transaction.Commit();
                return insertedId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<int> MarkPinned(int notesid)
        {

            int insertedId = 0;

            using var connection = sqlConnection.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
UPDATE flexi_notes
SET is_pinned = CASE WHEN is_pinned = 1 THEN 0 ELSE 1 END
WHERE id = @id;
SELECT CASE WHEN changes() > 0 THEN 1 ELSE 0 END AS is_updated;
                                ";

                // Parameters
                cmd.Parameters.AddWithValue("@id", (object?)notesid);

                // Execute and get the inserted ID
                var result = await cmd.ExecuteScalarAsync();
                insertedId = result != null ? Convert.ToInt32(result) : 0;

                transaction.Commit();
                return insertedId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        #endregion

        #region Bank Accounts
        public async Task<int> SaveCompanyBankAccounts(SaveCompanyBankAccounts bankAccounts)
        {
            int affectedId = 0;

            using var connection = sqlConnection.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;

                if (bankAccounts.rowid == 0)
                {
                    // INSERT
                    cmd.CommandText = @"
                INSERT INTO CompanyBankAccounts (
                    account_name, bank_name, account_number,
                    ifsc_code, branch_name, account_type,
                    create_by, create_at, use_on_print
                )
                VALUES (
                    @account_name, @bank_name, @account_number,
                    @ifsc_code, @branch_name, @account_type,
                    @created_by, @create_at, @use_on_print
                );
                SELECT last_insert_rowid();";
                }
                else
                {
                    // UPDATE
                    cmd.CommandText = @"
                UPDATE CompanyBankAccounts
                SET account_name   = COALESCE(@account_name, account_name),
                    bank_name      = COALESCE(@bank_name, bank_name),
                    account_number = COALESCE(@account_number, account_number),
                    ifsc_code      = COALESCE(@ifsc_code, ifsc_code),
                    branch_name    = COALESCE(@branch_name, branch_name),
                    account_type   = COALESCE(@account_type, account_type),
                    create_by      = COALESCE(@created_by, create_by),
                    create_at      = COALESCE(@create_at, create_at),
                    use_on_print   = COALESCE(@use_on_print, use_on_print)
                WHERE company_bank_id = @company_bank_id;
                SELECT @company_bank_id;";
                }

                // Parameters
                cmd.Parameters.AddWithValue("@account_name", bankAccounts.accountname ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@bank_name", bankAccounts.bankname ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@account_number", bankAccounts.accountnumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ifsc_code", bankAccounts.ifsccode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@branch_name", bankAccounts.branchname ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@account_type", bankAccounts.accounttype ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@created_by", bankAccounts.CreateBy ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@create_at", DateTime.Now);
                cmd.Parameters.AddWithValue("@company_bank_id", bankAccounts.rowid);
                cmd.Parameters.AddWithValue("@use_on_print", bankAccounts.useonprint);

                var result = await cmd.ExecuteScalarAsync();
                affectedId = result != null ? Convert.ToInt32(result) : 0;

                // 🔑 Reset other rows if this one is marked for print
                if (bankAccounts.useonprint == 1)
                {
                    using var resetCmd = connection.CreateCommand();
                    resetCmd.Transaction = transaction;
                    resetCmd.CommandText = @"
                UPDATE CompanyBankAccounts
                SET use_on_print = 0
                WHERE company_bank_id <> @company_bank_id;";
                    resetCmd.Parameters.AddWithValue("@company_bank_id", affectedId);
                    await resetCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return affectedId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<IEnumerable<CompanyBankAccountDto>> GetCompanyBankAccounts()
        {
            var connection = sqlConnection.GetConnection();
            List<CompanyBankAccountDto> result = new List<CompanyBankAccountDto>();
            try
            {
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                                     SELECT 
    c.company_bank_id,
    c.account_name,
    c.bank_name,
    c.account_number,
    c.ifsc_code,
    c.branch_name,
    c.account_type,
    c.create_by,
    u.Username AS created_by_name,
    c.status,
    strftime('%Y-%m-%d %H:%M:%S', c.create_at) AS created_at,
	c.use_on_print as useonprint
FROM CompanyBankAccounts c
LEFT JOIN Tbl_Users u ON u.userid = c.create_by
WHERE c.status = 1;
                    ";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var note = new CompanyBankAccountDto
                    {
                        company_bank_id = reader.GetInt32(0),
                        account_name = reader.GetString(1),
                        bank_name = reader.GetString(2),
                        account_number = reader.IsDBNull(3) ? null : reader.GetString(3),
                        ifsc_code = reader.GetString(4),
                        branch_name = reader.GetString(5),
                        account_type = reader.GetString(6),
                        created_by = reader.GetInt32(7),
                        created_by_name = reader.GetString(8),
                        status = reader.GetInt32(9),
                        created_at = reader.GetString(10),
                        useonprint = reader.GetInt32(11),
                    };
                    result.Add(note);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving note details", ex);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<BankAccountforprintDTO> GetCompanyBankAccountsForPrint()
        {
            var connection = sqlConnection.GetConnection();

            try
            {
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
            SELECT 
                company_bank_id,
                account_name,
                bank_name,
                account_number,
                ifsc_code,
                branch_name
            FROM CompanyBankAccounts
            WHERE use_on_print = 1;
        ";

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new BankAccountforprintDTO
                    {
                        company_bank_id = reader.GetInt32(0),
                        account_name = reader.GetString(1),
                        bank_name = reader.GetString(2),
                        account_number = reader.IsDBNull(3) ? null : reader.GetString(3),
                        ifsc_code = reader.GetString(4),
                        branch_name = reader.GetString(5),
                    };

                }
                else
                {
                    return new BankAccountforprintDTO();
                }

                
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving company bank accounts for print", ex);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        #endregion

        #region Formate Editor
        public async Task<List<TemplateOption>> GetTemplates()
        {
            var templates = new List<TemplateOption>
    {
        new TemplateOption { id = 1, key = "receipt",  name = "Receipt" },
        //new TemplateOption { id = 2, key = "orderreceipt", name = "Order" },
        //new TemplateOption { id = 3, key = "purchase", name = "Purchase" }
    };

            return await Task.FromResult(templates);
        }

        public async Task<int> SaveTemplateAsync(SaveTemplate template)
        {
            int insertedId = 0;

            using var connection = sqlConnection.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
            -- Case 1 & 2: Insert
INSERT INTO TemplateMaster (
    Id, CategoryId, Name, Description, HtmlContent, CssContent, JsContent, SchemaJson,
    IsDefault, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt, Version
)
SELECT 
    NULLIF(@Id, 0), @CategoryId, @Name, @Description, @HtmlContent, @CssContent, @JsContent, @SchemaJson,
    CASE 
        WHEN NOT EXISTS (SELECT 1 FROM TemplateMaster WHERE CategoryId = @CategoryId) 
            THEN 1   -- First row for category → default
        WHEN EXISTS (SELECT 1 FROM TemplateMaster WHERE CategoryId = @CategoryId AND IsDefault = 1) 
            THEN 0   -- Default already exists → insert non-default
    END,
    @CreatedBy, @UpdatedBy, @CreatedAt, @UpdatedAt, @Version
WHERE NOT EXISTS (
    -- Prevent insert if a non-default already exists → we’ll update instead
    SELECT 1 FROM TemplateMaster WHERE CategoryId = @CategoryId AND IsDefault = 0
);

-- Case 3: Update existing non-default row
UPDATE TemplateMaster
SET Name = @Name,
    Description = @Description,
    HtmlContent = @HtmlContent,
    CssContent = @CssContent,
    JsContent = @JsContent,
    SchemaJson = @SchemaJson,
    UpdatedBy = @UpdatedBy,
    UpdatedAt = @UpdatedAt,
    Version = @Version
WHERE CategoryId = @CategoryId AND IsDefault = 0;

SELECT last_insert_rowid();
        ";

                // Parameters
                cmd.Parameters.AddWithValue("@Id", (object?)template.id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CategoryId", template.categoryid);
                cmd.Parameters.AddWithValue("@Name", (object?)template.name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", (object?)template.description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@HtmlContent", (object?)template.htmlcontent ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CssContent", (object?)template.csscontent ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@JsContent", (object?)template.jscontent ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SchemaJson", (object?)template.schemajson ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsDefault", template.isdefault ? 1 : 0);
                cmd.Parameters.AddWithValue("@CreatedBy", (object?)template.createdby ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UpdatedBy", (object?)template.updatedby ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedAt", template.createdat);
                cmd.Parameters.AddWithValue("@UpdatedAt", template.updatedat);
                cmd.Parameters.AddWithValue("@Version", template.version);

                // Execute and get the inserted ID
                var result = await cmd.ExecuteScalarAsync();
                insertedId = result != null ? Convert.ToInt32(result) : 0;

                transaction.Commit();
                return insertedId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<TemplateData?> GetTemplateAsync(int categoryId, int isDefault)
        {
            using var connection = sqlConnection.GetConnection();
            await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
        SELECT HtmlContent, CssContent, JsContent, SchemaJson, IsDefault
        FROM TemplateMaster
        WHERE CategoryId = @CategoryId AND IsDefault = @IsDefault
        LIMIT 1;
    ";

            cmd.Parameters.AddWithValue("@CategoryId", categoryId);
            cmd.Parameters.AddWithValue("@IsDefault", isDefault);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new TemplateData
                {
                    // numbering (ordinal index)
                    htmlcontent = reader.IsDBNull(0) ? null : reader.GetString(0),   // HtmlContent
                    csscontent = reader.IsDBNull(1) ? null : reader.GetString(1),   // CssContent
                    jscontent = reader.IsDBNull(2) ? null : reader.GetString(2),   // JsContent
                    schemajson = reader.IsDBNull(3) ? null : reader.GetString(3),   // SchemaJson
                    isdefault = reader.GetInt32(4)                                 // IsDefault
                };

            }

            return null; // no row found
        }

        public async Task<HtmlTemplate?> GethtmlContent(int categoryId)
        {
            using var connection = sqlConnection.GetConnection();
            await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
       SELECT HtmlContent, CssContent
 FROM TemplateMaster
 WHERE CategoryId = @CategoryId AND IsDefault = 0
    ";

            cmd.Parameters.AddWithValue("@CategoryId", categoryId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new HtmlTemplate
                {
                    // numbering (ordinal index)
                    htmlcontent = reader.IsDBNull(0) ? null : reader.GetString(0),   // HtmlContent
                    csscontent = reader.IsDBNull(1) ? null : reader.GetString(1),   // CssContent
                };

            }

            return null; // no row found
        }
        #endregion
    }
}
