using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DATABASE;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.Data.Sqlite;

namespace FLEXIERP.DataAccessLayer
{
    public class SaleRepo : ISaleRepo
    {
        private readonly IDataBaseOperation sqlconnection;

        public SaleRepo(IDataBaseOperation _sqlconnection)
        {
            sqlconnection = _sqlconnection;
        }

        #region common
        private string GetPaymentModeString(int mode)
        {
            return mode switch
            {
                1 => "Cash",
                2 => "UPI",
                3 => "Card",
                4 => "Other",
                _ => "Unknown"
            };
        }

        #endregion

        #region Product By Barcode
        public async Task<ProductByBarcode_DTO?> GetProductByBarcode(string barCode)
        {
            ProductByBarcode_DTO? product = null;

            try
            {
                using var connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;

                cmd.CommandText = @"
            SELECT    
                pr.ProductID,    
                pr.ProductCode,    
                pr.BarCode,    
                pr.ProductName,    
                category.CategoryName,    
                pr.ProductType,    
                pr.PackedDate,    
                pr.PackedWeight,    
                pr.PackedHeight,    
                pr.PackedDepth,    
                pr.PackedWidth,    
                pr.IsPerishable,    
                pr.PurchasePrice,    
                pr.SellingPrice,    
                pr.TaxRate,    
                pr.Discount,
                (pr.Quantity - IFNULL(SUM(sd.Quantity), 0)) AS AvailableQuantity
            FROM product AS pr    
            LEFT JOIN flexi_erp_product_category AS category    
                ON pr.ProductCategory = category.CategoryID 
            LEFT JOIN SaleDetail AS sd 
                ON pr.ProductID = sd.ProductID
            WHERE pr.BarCode = @BarCode
            GROUP BY 
                pr.ProductID,    
                pr.ProductCode,    
                pr.BarCode,    
                pr.ProductName,    
                category.CategoryName,    
                pr.ProductType,    
                pr.PackedDate,    
                pr.PackedWeight,    
                pr.PackedHeight,    
                pr.PackedDepth,    
                pr.PackedWidth,    
                pr.IsPerishable,    
                pr.PurchasePrice,    
                pr.SellingPrice,    
                pr.TaxRate,    
                pr.Discount,
                pr.Quantity;
        ";

                cmd.Parameters.AddWithValue("@BarCode", barCode);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    product = new ProductByBarcode_DTO
                    {
                        ProductID = !reader.IsDBNull(0) ? reader.GetInt32(0) : 0,
                        ProductCode = !reader.IsDBNull(1) ? reader.GetString(1) : string.Empty,
                        BarCode = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty,
                        ProductName = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty,
                        CategoryName = !reader.IsDBNull(4) ? reader.GetString(4) : string.Empty,
                        ProductType = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty,
                        PackedDate = !reader.IsDBNull(6) ? reader.GetDateTime(6) : null,
                        PackedWeight = !reader.IsDBNull(7) ? reader.GetInt32(7) : null,
                        PackedHeight = !reader.IsDBNull(8) ? reader.GetInt32(8) : null,
                        PackedDepth = !reader.IsDBNull(9) ? reader.GetInt32(9) : null,
                        PackedWidth = !reader.IsDBNull(10) ? reader.GetInt32(10) : null,
                        IsPerishable = !reader.IsDBNull(11) ? reader.GetBoolean(11) : null,
                        PurchasePrice = !reader.IsDBNull(12) ? reader.GetDecimal(12) : null,
                        SellingPrice = !reader.IsDBNull(13) ? reader.GetDecimal(13) : null,
                        TaxRate = !reader.IsDBNull(14) ? reader.GetDecimal(14) : null,
                        Discount = !reader.IsDBNull(15) ? reader.GetDecimal(15) : null,
                        availableQuantity = !reader.IsDBNull(16) ? reader.GetDecimal(16) : null,
                    };
                }
            }
            catch (SqliteException ex)
            {
                throw new Exception("Failed to retrieve product. Please try again later.", ex);
            }
            finally
            {
                await sqlconnection.GetConnection().CloseAsync();
            }

            return product;
        }

        #endregion

        #region Make Sale 
        public async Task<int> InsertSaleAsync(Sale sale)
        {
            int saleId = 0;

            using var connection = sqlconnection.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                // 1️⃣ Insert new customer if needed
                if (sale.customerID == null)
                {
                    using var insertCustomerCmd = connection.CreateCommand();
                    insertCustomerCmd.Transaction = transaction;
                    insertCustomerCmd.CommandText = @"
                INSERT INTO Customer
                (CustomerName, CustomerAddress, PhoneNo, Email, PaymentMode, CreatedBy, CreatedDate, Remark)
                VALUES (@CustomerName, @CustomerAddress, @PhoneNo, @Email, @PaymentMode, @CreatedBy, @CreatedDate, @Remark);
                SELECT last_insert_rowid();";

                    insertCustomerCmd.Parameters.AddWithValue("@CustomerName", (object?)sale.Customer?.CustomerName ?? DBNull.Value);
                    insertCustomerCmd.Parameters.AddWithValue("@CustomerAddress", (object?)sale.Customer?.CustomerAddress ?? DBNull.Value);
                    insertCustomerCmd.Parameters.AddWithValue("@PhoneNo", (object?)sale.Customer?.PhoneNo ?? DBNull.Value);
                    insertCustomerCmd.Parameters.AddWithValue("@Email", (object?)sale.Customer?.Email ?? DBNull.Value);
                    insertCustomerCmd.Parameters.AddWithValue("@PaymentMode", (object?)sale.Customer?.PaymentMode ?? DBNull.Value);
                    insertCustomerCmd.Parameters.AddWithValue("@CreatedBy", (object?)sale.CreatedBy ?? DBNull.Value);
                    insertCustomerCmd.Parameters.AddWithValue("@Remark", (object?)sale.Customer?.Remark ?? DBNull.Value);
                    insertCustomerCmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                    sale.customerID = Convert.ToInt32(await insertCustomerCmd.ExecuteScalarAsync());
                }

                // 2️⃣ Insert Sale
                using var insertSaleCmd = connection.CreateCommand();
                insertSaleCmd.Transaction = transaction;

                insertSaleCmd.CommandText = @"
                INSERT INTO Sale
                (CustomerID, TotalItems, TotalAmount, TotalDiscount, OrderDate, CreatedBy, CreatedDate, invoice_no)
                VALUES 
                (@CustomerID, @TotalItems, @TotalAmount, @TotalDiscount, @OrderDate, @CreatedBy, @CreatedDate, @InvoiceNo);
                SELECT last_insert_rowid();
                ";

                insertSaleCmd.Parameters.AddWithValue("@CustomerID", sale.customerID);
                insertSaleCmd.Parameters.AddWithValue("@TotalItems", sale.TotalItems);
                insertSaleCmd.Parameters.AddWithValue("@TotalAmount", sale.TotalAmount);
                insertSaleCmd.Parameters.AddWithValue("@TotalDiscount", sale.TotalDiscount);
                insertSaleCmd.Parameters.AddWithValue("@CreatedBy", (object?)sale.CreatedBy ?? DBNull.Value);
                insertSaleCmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                insertSaleCmd.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                insertSaleCmd.Parameters.AddWithValue("@InvoiceNo", sale.invoiceno);

                saleId = Convert.ToInt32(await insertSaleCmd.ExecuteScalarAsync());


                // 3️⃣ Insert SaleDetails
                foreach (var detail in sale.SaleDetails)
                {
                    using var insertDetailCmd = connection.CreateCommand();
                    insertDetailCmd.Transaction = transaction;
                    insertDetailCmd.CommandText = @"
                INSERT INTO SaleDetail
                (SaleID, ProductID, CreatedBy, CreatedDate, Quantity)
                VALUES (@SaleID, @ProductID, @CreatedBy, @CreatedDate, @Quantity);";

                    insertDetailCmd.Parameters.AddWithValue("@SaleID", saleId);
                    insertDetailCmd.Parameters.AddWithValue("@ProductID", detail.ProductID);
                    insertDetailCmd.Parameters.AddWithValue("@CreatedBy", (object?)sale.CreatedBy ?? DBNull.Value);
                    insertDetailCmd.Parameters.AddWithValue("@Quantity", (object?)detail.productquantity ?? DBNull.Value);
                    insertDetailCmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                    await insertDetailCmd.ExecuteNonQueryAsync();
                }

                // 4️⃣ Insert ExtraCharges
                if (sale.extracharges != null && sale.extracharges.Any())
                {
                    foreach (var extra in sale.extracharges)
                    {
                        using var insertExtraCmd = connection.CreateCommand();
                        insertExtraCmd.Transaction = transaction;
                        insertExtraCmd.CommandText = @"
                    INSERT INTO extra_charges
                    (name, charge_amt, create_by, saleid, customerid)
                    VALUES (@Name, @ChargeAmt, @CreatedBy, @SaleID, @CustomerID);";

                        insertExtraCmd.Parameters.AddWithValue("@Name", (object?)extra.name ?? DBNull.Value);
                        insertExtraCmd.Parameters.AddWithValue("@ChargeAmt", (object?)extra.amount ?? DBNull.Value);
                        insertExtraCmd.Parameters.AddWithValue("@CreatedBy", (object?)sale.CreatedBy ?? DBNull.Value);
                        insertExtraCmd.Parameters.AddWithValue("@SaleID", saleId);
                        insertExtraCmd.Parameters.AddWithValue("@CustomerID", sale.customerID);

                        await insertExtraCmd.ExecuteNonQueryAsync();
                    }
                }

                if (sale.Customer?.BalanceDue is not null && sale.Customer?.BalanceDue > 0)
                {
                    long dueId = 0;

                    // 1️⃣ Check if the customer already has a balance_due record
                    using (var checkCmd = connection.CreateCommand())
                    {
                        checkCmd.Transaction = transaction;
                        checkCmd.CommandText = "SELECT dueid FROM balance_due WHERE customerid = @CustomerId LIMIT 1;";
                        checkCmd.Parameters.AddWithValue("@CustomerId", sale.customerID);

                        var result = await checkCmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            dueId = Convert.ToInt64(result);

                            // 2️⃣ If exists, update total_due_amount
                            using var updateCmd = connection.CreateCommand();
                            updateCmd.Transaction = transaction;
                            updateCmd.CommandText = @"
                UPDATE balance_due
                SET total_due_amount = total_due_amount + @AddAmount,
                    updateby = @UpdatedBy,
                    updateat = @UpdatedAt
                WHERE dueid = @DueId;
            ";

                            updateCmd.Parameters.AddWithValue("@AddAmount", sale.Customer?.BalanceDue ?? 0);
                            updateCmd.Parameters.AddWithValue("@UpdatedBy", (object?)sale.CreatedBy ?? DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                            updateCmd.Parameters.AddWithValue("@DueId", dueId);

                            await updateCmd.ExecuteNonQueryAsync();
                        }
                        else
                        {
                            // 3️⃣ If not exists, insert a new master record
                            using var insertCmd = connection.CreateCommand();
                            insertCmd.Transaction = transaction;
                            insertCmd.CommandText = @"
                INSERT INTO balance_due (customerid, total_due_amount, createby, create_at)
                VALUES (@CustomerId, @TotalDueAmount, @CreatedBy, @CreateAt);
            ";

                            insertCmd.Parameters.AddWithValue("@CustomerId", sale.customerID);
                            insertCmd.Parameters.AddWithValue("@TotalDueAmount", sale.Customer?.BalanceDue ?? 0);
                            insertCmd.Parameters.AddWithValue("@CreatedBy", (object?)sale.CreatedBy ?? DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@CreateAt", DateTime.Now);

                            await insertCmd.ExecuteNonQueryAsync();

                            // 4️⃣ Get the new dueid
                            using var getIdCmd = connection.CreateCommand();
                            getIdCmd.Transaction = transaction;
                            getIdCmd.CommandText = "SELECT last_insert_rowid();";
                            dueId = (long)(await getIdCmd.ExecuteScalarAsync() ?? 0);
                        }
                    }

                    // 5️⃣ Insert into detail table (always happens)
                    using var detailCmd = connection.CreateCommand();
                    detailCmd.Transaction = transaction;

                    detailCmd.CommandText = @"
        INSERT INTO balance_due_detail (dueid, saleid, due_amount, createby, create_at)
        VALUES (@DueId, @SaleId, @DueAmount, @CreatedBy, @CreateAt);
    ";

                    detailCmd.Parameters.AddWithValue("@DueId", dueId);
                    detailCmd.Parameters.AddWithValue("@SaleId", saleId);
                    detailCmd.Parameters.AddWithValue("@DueAmount", sale.Customer?.BalanceDue ?? 0);
                    detailCmd.Parameters.AddWithValue("@CreatedBy", (object?)sale.CreatedBy ?? DBNull.Value);
                    detailCmd.Parameters.AddWithValue("@CreateAt", DateTime.Now);

                    await detailCmd.ExecuteNonQueryAsync();
                }

                // 5️⃣ Insert Customer Ledger
                using var ledgerCmd = connection.CreateCommand();
                ledgerCmd.Transaction = transaction;
                ledgerCmd.CommandText = @"
            INSERT INTO Customer_ledger
            (customer_id, paid_amt, balance_due, total_amt, payment_mode, transaction_type, create_by, create_at, payid, transaction_type_id)
            VALUES (@CustomerID, @PaidAmt, @BalanceDue, @TotalAmt, @PaymentMode, @TransactionType, @CreatedBy, @create_at, @PayID, @SaleID);";

                ledgerCmd.Parameters.AddWithValue("@CustomerID", sale.customerID);
                ledgerCmd.Parameters.AddWithValue("@PaidAmt", (object?)sale.Customer?.PaidAmt ?? DBNull.Value);
                ledgerCmd.Parameters.AddWithValue("@BalanceDue", (object?)sale.Customer?.BalanceDue ?? DBNull.Value);
                ledgerCmd.Parameters.AddWithValue("@TotalAmt", (object?)sale.Customer?.TotalAmt ?? DBNull.Value);
                ledgerCmd.Parameters.AddWithValue("@PaymentMode", (object?)sale.Customer?.PaymentMode ?? DBNull.Value);
                ledgerCmd.Parameters.AddWithValue("@TransactionType", (object?)sale.Customer?.TransactionType ?? DBNull.Value);
                ledgerCmd.Parameters.AddWithValue("@CreatedBy", (object?)sale.CreatedBy ?? DBNull.Value);
                ledgerCmd.Parameters.AddWithValue("@PayID", (object?)sale.Customer?.payid ?? DBNull.Value);
                ledgerCmd.Parameters.AddWithValue("@SaleID", saleId);
                ledgerCmd.Parameters.AddWithValue("@create_at", DateTime.Now);

                await ledgerCmd.ExecuteNonQueryAsync();

                // ✅ Commit transaction
                transaction.Commit();

                return saleId;
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

        #region Get Sale
        public async Task<List<Sale_DTO>> GetSalesAsync(PaginationFilter pagination)
        {
            var salesList = new List<Sale_DTO>();
            int offset = (pagination.PageNo - 1) * pagination.PageSize;

            using var connection = sqlconnection.GetConnection();
            await connection.OpenAsync();

            try
            {
                using var cmd = connection.CreateCommand();

                cmd.CommandText = @"
            SELECT 
    sale.SaleID,
    cus.CustomerName,
    sale.TotalItems,
    sale.TotalAmount,
    sale.TotalDiscount,
    sale.OrderDate,
    us.FullName,
    IFNULL((
        SELECT SUM(charges.charge_amt)
        FROM extra_charges charges
        WHERE charges.saleid = sale.SaleID
    ), 0) AS TotalExtraCharges,
    (
        SELECT COUNT(*) 
        FROM Sale s
        LEFT JOIN Customer c ON s.CustomerID = c.CustomerID
        LEFT JOIN Tbl_Users u ON s.CreatedBy = u.UserID
        WHERE (@Search IS NULL 
               OR c.CustomerName LIKE '%' || @Search || '%' 
               OR u.FullName LIKE '%' || @Search || '%')
          AND (@StartDate IS NULL OR DATE(s.OrderDate) >= DATE(@StartDate))
          AND (@EndDate IS NULL OR DATE(s.OrderDate) <= DATE(@EndDate))
    ) AS TotalRows
FROM Sale sale
LEFT JOIN Customer cus ON sale.CustomerID = cus.CustomerID
LEFT JOIN Tbl_Users us ON sale.CreatedBy = us.UserID
WHERE (@Search IS NULL 
       OR cus.CustomerName LIKE '%' || @Search || '%' 
       OR us.FullName LIKE '%' || @Search || '%')
  AND (@StartDate IS NULL OR DATE(sale.OrderDate) >= DATE(@StartDate))
  AND (@EndDate IS NULL OR DATE(sale.OrderDate) <= DATE(@EndDate))
ORDER BY sale.OrderDate DESC
LIMIT @PageSize OFFSET @Offset;

        ";

                cmd.Parameters.AddWithValue("@PageSize", pagination.PageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);
                cmd.Parameters.AddWithValue("@Search", string.IsNullOrEmpty(pagination.SearchTerm) ? DBNull.Value : pagination.SearchTerm);
                cmd.Parameters.AddWithValue("@StartDate", (object?)pagination.StartDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EndDate", (object?)pagination.EndDate ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                int srNo = offset + 1;

                while (await reader.ReadAsync())
                {
                    salesList.Add(new Sale_DTO
                    {
                        SrNo = srNo++,
                        SaleID = !reader.IsDBNull(0) ? reader.GetInt32(0) : 0,
                        CustomerName = !reader.IsDBNull(1) ? reader.GetString(1) : string.Empty,
                        TotalItems = !reader.IsDBNull(2) ? reader.GetInt32(2) : 0,
                        TotalAmount = !reader.IsDBNull(3) ? reader.GetDecimal(3) : 0,
                        TotalDiscount = !reader.IsDBNull(4) ? reader.GetDecimal(4) : 0,
                        OrderDate = !reader.IsDBNull(5) ? reader.GetDateTime(5) : DateTime.MinValue,
                        FullName = !reader.IsDBNull(6) ? reader.GetString(6) : string.Empty,
                        extracharges = !reader.IsDBNull(7) ? reader.GetDecimal(7) : 0,
                        TotalRows = !reader.IsDBNull(8) ? reader.GetInt32(8) : 0
                    });
                }
            }
            finally
            {
                await connection.CloseAsync();
            }

            return salesList;
        }

        #endregion

        #region Old customer 
        public async Task<List<OldCustomerDTO>> GetOldCustomersAsync(PaginationFilter pagination)
        {
            var customers = new List<OldCustomerDTO>();

            try
            {
                using var connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;

                int offset = (pagination.PageNo - 1) * pagination.PageSize;

                cmd.CommandText = @"
            WITH DistinctCustomers AS (
                SELECT *
                FROM Customer AS cs
                WHERE (@SearchTerm IS NULL OR @SearchTerm = '' 
                       OR cs.CustomerName LIKE '%' || @SearchTerm || '%'
                       OR cs.PhoneNo LIKE '%' || @SearchTerm || '%'
                       OR cs.Email LIKE '%' || @SearchTerm || '%'
                       OR cs.Remark LIKE '%' || @SearchTerm || '%')
                GROUP BY cs.PhoneNo
                ORDER BY MAX(cs.CustomerID) DESC
            )
            SELECT 
                (ROW_NUMBER() OVER (ORDER BY CustomerID DESC) + @Offset) AS SrNo,
                CustomerID,
                CustomerName,
                PhoneNo,
                Email,
                Remark,
                CustomerAddress,
                (SELECT COUNT(*) FROM DistinctCustomers) AS TotalRecords
            FROM DistinctCustomers
            LIMIT @PageSize OFFSET @Offset;
        ";

                cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(pagination.SearchTerm) ? DBNull.Value : pagination.SearchTerm);
                cmd.Parameters.AddWithValue("@PageSize", pagination.PageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    customers.Add(new OldCustomerDTO
                    {
                        SrNo = !reader.IsDBNull(0) ? reader.GetInt64(0) : 0,
                        CustomerID = !reader.IsDBNull(1) ? reader.GetInt32(1) : 0,
                        CustomerName = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty,
                        PhoneNo = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty,
                        Email = !reader.IsDBNull(4) ? reader.GetString(4) : string.Empty,
                        Remark = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty,
                        CustomerAddress = !reader.IsDBNull(6) ? reader.GetString(6) : string.Empty,
                        TotalRecords = !reader.IsDBNull(7) ? reader.GetInt32(7) : 0
                    });
                }
            }
            catch (SqliteException ex)
            {
                throw new Exception("Failed to retrieve old customers. Please try again later.", ex);
            }
            finally
            {
                await sqlconnection.GetConnection().CloseAsync();
            }

            return customers;
        }

        #endregion

        #region Get Customer with sales 

        public async Task<List<CustomerWithSalesDTO>> GetCustomersWithSalesAsync(PaginationFilter pagination)
        {
            var customers = new List<CustomerWithSalesDTO>();

            try
            {
                using var connection = sqlconnection.GetConnection(); // returns SqliteConnection
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
            WITH CustomerCTE AS (
     SELECT 
         cs.CustomerID,
         cs.CustomerName,
         cs.CustomerAddress,
         cs.PhoneNo,
         cs.Email,
         cs.Remark,
         cs.CreatedDate,
         u.FullName,
         ROW_NUMBER() OVER (ORDER BY cs.CustomerID DESC) AS RowNum,
         COUNT(*) OVER() AS TotalRecords
     FROM Customer cs
     LEFT JOIN Tbl_Users u ON cs.CreatedBy = u.UserID
     WHERE 
         (@SearchTerm IS NULL OR @SearchTerm = '' 
          OR cs.CustomerName LIKE '%' || @SearchTerm || '%'
          OR cs.PhoneNo LIKE '%' || @SearchTerm || '%'
          OR cs.Email LIKE '%' || @SearchTerm || '%'
          OR cs.Remark LIKE '%' || @SearchTerm || '%')
         AND (@StartDate IS NULL OR date(cs.CreatedDate) >= date(@StartDate))
         AND (@EndDate IS NULL OR date(cs.CreatedDate) <= date(@EndDate))
 )
 SELECT 
     RowNum AS SrNo,
     CustomerID,
     CustomerName,
     CustomerAddress,
     PhoneNo,
     Email,
     Remark,
     CreatedDate,
     FullName,
     TotalRecords
 FROM CustomerCTE
 WHERE RowNum > ((@PageNo - 1) * @PageSize)
   AND RowNum <= (@PageNo * @PageSize);
        ";

                // Add parameters safely
                cmd.Parameters.AddWithValue("@SearchTerm", (object?)pagination.SearchTerm ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@StartDate", (object?)pagination.StartDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EndDate", (object?)pagination.EndDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PageNo", pagination.PageNo);
                cmd.Parameters.AddWithValue("@PageSize", pagination.PageSize);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    customers.Add(new CustomerWithSalesDTO
                    {
                        SrNo = reader.GetInt64(0),
                        CustomerID = reader.GetInt32(1),
                        CustomerName = reader.GetString(2),
                        CustomerAddress = reader.GetString(3),
                        PhoneNo = reader.GetString(4),
                        Email = reader.GetString(5),
                        Remark = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        CreatedDate = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7),
                        FullName = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                        TotalRecords = reader.IsDBNull(9) ? 0 : reader.GetInt32(9)
                    });
                }
            }
            catch (SqliteException ex)
            {
                throw new Exception("Failed to retrieve customers with sales. Please try again later.", ex);
            }

            return customers;
        }

        #endregion

        #region Sale Invoice
        public async Task<ReceiptDTO?> GetReceiptDetail(int saleId)
        {
            using var connection = sqlconnection.GetConnection();
            await connection.OpenAsync();


            var receipt = new ReceiptDTO
            {
                CustomerInfo = new ReceiptCustomerDTO(),
                SaleDetails = new List<ReceiptDetailDTO>(),
                extracharges = new List<extrachargesDTO>()
            };

            try
            {
                // 1️⃣ Sale + Customer info
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
            SELECT 
                 cs.CustomerName,
                 cs.PhoneNo,
                 cs.Email,
                 CASE cl.payment_mode
                     WHEN 1 THEN 'Cash'
                     WHEN 2 THEN 'UPI'
                     WHEN 3 THEN 'Bank Transfer'
                     ELSE 'Other'
                 END AS PaymentMode,
                 cs.Remark,
                 sale.TotalItems,
                 ROUND(sale.TotalAmount, 2) AS TotalAmount,
                 ROUND(sale.TotalDiscount, 2) AS TotalDiscount,
                 IFNULL(cl.paid_amt, 0) AS paid_amt,
                 IFNULL(cl.Balance_Due, 0) AS Balance_Due,
                 sale.invoice_no
                FROM Sale AS sale
                LEFT JOIN Customer AS cs ON sale.CustomerID = cs.CustomerID
                LEFT JOIN Customer_ledger AS cl 
                    ON cs.CustomerID = cl.Customer_ID 
                    AND sale.SaleID = cl.transaction_type_id
                WHERE sale.SaleID = @SaleID
                LIMIT 1;
            ";

                    cmd.Parameters.AddWithValue("@SaleID", saleId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        receipt.CustomerInfo = new ReceiptCustomerDTO
                        {
                            CustomerName = reader["CustomerName"]?.ToString() ?? "",
                            PhoneNo = reader["PhoneNo"]?.ToString() ?? "",
                            Email = reader["Email"]?.ToString() ?? "",
                            PaymentMode = reader["PaymentMode"]?.ToString() ?? "",
                            Remark = reader["Remark"]?.ToString() ?? "",
                            TotalItems = Convert.ToDecimal(reader["TotalItems"] ?? 0),
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"] ?? 0),
                            TotalDiscount = Convert.ToDecimal(reader["TotalDiscount"] ?? 0),
                            paidamt = Convert.ToDecimal(reader["paid_amt"] ?? 0),
                            baldue = Convert.ToDecimal(reader["Balance_Due"] ?? 0),
                            invoiceno = reader["invoice_no"].ToString() ?? null
                        };
                    }
                }

                // 2️⃣ Sale detail + Product info
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT 
                    pro.ProductName,
                    detail.Quantity,
                    ROUND(pro.SellingPrice, 2) AS Price,
                    ROUND(detail.Quantity * IFNULL(pro.Discount, 0), 2) AS TotalDiscount,
                    ROUND(detail.Quantity * IFNULL(pro.TaxRate, 0), 2) AS Tax,
                    ROUND(detail.Quantity * IFNULL(pro.SellingPrice, 0), 2) AS TotalAmount
                FROM SaleDetail AS detail
                LEFT JOIN Product AS pro ON detail.ProductID = pro.ProductID
                WHERE detail.SaleID = @SaleID;
            ";

                    cmd.Parameters.AddWithValue("@SaleID", saleId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        receipt.SaleDetails.Add(new ReceiptDetailDTO
                        {
                            ProductName = reader["ProductName"]?.ToString() ?? "",
                            Quantity = Convert.ToDecimal(reader["Quantity"] ?? 0),
                            Price = Convert.ToDecimal(reader["Price"] ?? 0),
                            TotalDiscount = Convert.ToDecimal(reader["TotalDiscount"] ?? 0),
                            Tax = Convert.ToDecimal(reader["Tax"] ?? 0),
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"] ?? 0)
                        });
                    }
                }

                // 3️⃣ Extra Charges
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT 
                    ec.name AS ChargeName,
                    ROUND(ec.charge_amt, 2) AS ChargeAmount,
                    u.FullName AS CreatedBy,
                    strftime('%d-%m-%Y %H:%M', ec.create_at) AS CreatedDate
                FROM extra_charges AS ec
                LEFT JOIN Tbl_Users AS u ON ec.create_by = u.UserID
                WHERE ec.saleid = @SaleID AND ec.status = 1;
            ";

                    cmd.Parameters.AddWithValue("@SaleID", saleId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        receipt.extracharges.Add(new extrachargesDTO
                        {
                            chargename = reader["ChargeName"]?.ToString() ?? "",
                            chargeamount = Convert.ToDecimal(reader["ChargeAmount"] ?? 0),
                            createby = reader["CreatedBy"]?.ToString() ?? "",
                            createdate = reader["CreatedDate"]?.ToString() ?? ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve receipt details (SQLite).", ex);
            }
            finally
            {
                await connection.CloseAsync();
            }

            return receipt;
        }

        #endregion
    }
}
