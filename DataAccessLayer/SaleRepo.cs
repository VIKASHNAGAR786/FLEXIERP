using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DATABASE;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;
using Microsoft.Data.SqlClient;
using System.Data;

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
                cmd.CommandText = "usp_GetProductByBarcode";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@BarCode", SqlDbType.BigInt) { Value = barCode });

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
                        Discount = !reader.IsDBNull(15) ? reader.GetDecimal(15) : null
                    };
                }
            }
            catch (SqlException ex)
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
            try
            {
                using var connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "usp_InsertSale";

                // 1️⃣ Sale parameters
                // 1️⃣ Sale parameters
                cmd.Parameters.Add(new SqlParameter("@CustomerID", SqlDbType.Int)
                {
                    Value = (object?)sale.CustomerID ?? DBNull.Value,
                    Direction = ParameterDirection.Input
                });

                cmd.Parameters.Add(new SqlParameter("@CustomerName", SqlDbType.VarChar, 100)
                {
                    Value = (object?)sale.Customer?.CustomerName ?? DBNull.Value,
                    Direction = ParameterDirection.Input
                });

                cmd.Parameters.Add(new SqlParameter("@CustomerAddress", SqlDbType.VarChar, 255)
                {
                    Value = (object?)sale.Customer?.CustomerAddress ?? DBNull.Value,
                    Direction = ParameterDirection.Input
                });

                cmd.Parameters.Add(new SqlParameter("@remark", SqlDbType.VarChar, 255)
                {
                    Value = (object?)sale.Customer?.Remark ?? DBNull.Value,
                    Direction = ParameterDirection.Input
                });

                cmd.Parameters.Add(new SqlParameter("@PhoneNo", SqlDbType.VarChar, 50)
                {
                    Value = (object?)sale.Customer?.PhoneNo ?? DBNull.Value,
                    Direction = ParameterDirection.Input
                });

                cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.VarChar, 100)
                {
                    Value = (object?)sale.Customer?.Email ?? DBNull.Value,
                    Direction = ParameterDirection.Input
                });

                cmd.Parameters.Add(new SqlParameter("@PaymentMode", SqlDbType.Int)
                {
                    Value = (object?)sale.Customer?.PaymentMode ?? DBNull.Value,
                    Direction = ParameterDirection.Input
                });

                cmd.Parameters.Add(new SqlParameter("@TotalItems", SqlDbType.Decimal)
                {
                    Precision = 10,
                    Scale = 2,
                    Value = sale.TotalItems,
                    Direction = ParameterDirection.Input
                });

                cmd.Parameters.Add(new SqlParameter("@TotalAmount", SqlDbType.Decimal)
                {
                    Precision = 18,
                    Scale = 2,
                    Value = sale.TotalAmount,
                    Direction = ParameterDirection.Input
                });

                cmd.Parameters.Add(new SqlParameter("@TotalDiscount", SqlDbType.Decimal)
                {
                    Precision = 18,
                    Scale = 2,
                    Value = sale.TotalDiscount,
                    Direction = ParameterDirection.Input
                });

                cmd.Parameters.Add(new SqlParameter("@OrderDate", SqlDbType.Date)
                {
                    Value = (object?)sale.OrderDate ?? DBNull.Value,
                    Direction = ParameterDirection.Input
                });

                cmd.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.Int)
                {
                    Value = (object?)sale.CreatedBy ?? DBNull.Value,
                    Direction = ParameterDirection.Input
                });

                // 2️⃣ Output SaleID
                var saleIdParam = new SqlParameter("@SaleID", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(saleIdParam);

                // 3️⃣ TVP for SaleDetails
                var tvp = new DataTable();
                tvp.Columns.Add("ProductID", typeof(int));
                tvp.Columns.Add("CreatedBy", typeof(int));

                foreach (var detail in sale.SaleDetails)
                {
                    tvp.Rows.Add(detail.ProductID, sale.CreatedBy ?? (object)DBNull.Value);
                }

                var tvpParam = cmd.Parameters.AddWithValue("@SaleDetails", tvp);
                tvpParam.SqlDbType = SqlDbType.Structured;
                tvpParam.TypeName = "SaleDetailType";

                // Execute
                await cmd.ExecuteNonQueryAsync();

                return (int)saleIdParam.Value;
            }
            catch (Exception ex)
            {
                // Log the error or rethrow
                Console.WriteLine("Error inserting sale: " + ex.Message);
                throw;
            }
            finally
            {
                await sqlconnection.GetConnection().CloseAsync();
            }
        }

        #endregion

        #region Get Sale
        public async Task<List<Sale_DTO>> GetSalesAsync(PaginationFilter pagination)
        {
            var salesList = new List<Sale_DTO>();

            try
            {
                using var connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "usp_GetSales";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@Page", SqlDbType.Int) { Value = pagination.PageNo });
                cmd.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pagination.PageSize });
                cmd.Parameters.Add(new SqlParameter("@Search", SqlDbType.NVarChar, 100) { Value = (object?)pagination.SearchTerm ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.Date) { Value = pagination.StartDate });
                cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.Date) { Value = pagination.EndDate });

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    salesList.Add(new Sale_DTO
                    {
                        SrNo = !reader.IsDBNull(0) ? reader.GetInt32(0) : 0,
                        SaleID = !reader.IsDBNull(1) ? reader.GetInt32(1) : 0,
                        CustomerName = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty,
                        TotalItems = !reader.IsDBNull(3) ? reader.GetInt32(3) : 0,
                        TotalAmount = !reader.IsDBNull(4) ? reader.GetDecimal(4) : 0,
                        TotalDiscount = !reader.IsDBNull(5) ? reader.GetDecimal(5) : 0,
                        OrderDate = !reader.IsDBNull(6) ? reader.GetDateTime(6) : DateTime.MinValue,
                        FullName = !reader.IsDBNull(7) ? reader.GetString(7) : string.Empty,
                        TotalRows = !reader.IsDBNull(8) ? reader.GetInt32(8) : 0
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Failed to retrieve sales. Please try again later.", ex);
            }
            finally
            {
                await sqlconnection.GetConnection().CloseAsync();
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
                cmd.CommandText = "usp_sale_old_Get_Customers";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@SearchTerm", SqlDbType.NVarChar, 100)
                {
                    Value = (object?)pagination.SearchTerm ?? DBNull.Value
                });
                cmd.Parameters.Add(new SqlParameter("@PageNo", SqlDbType.Int) { Value = pagination.PageNo });
                cmd.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pagination.PageSize });

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
            catch (SqlException ex)
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
                using var connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "usp_Get_Customers_WithSales";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@SearchTerm", SqlDbType.NVarChar, 100)
                {
                    Value = (object?)pagination.SearchTerm ?? DBNull.Value
                });
                cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.Date) { Value = pagination.StartDate });
                cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.Date) { Value = pagination.EndDate});
                cmd.Parameters.Add(new SqlParameter("@PageNo", SqlDbType.Int) { Value = pagination.PageNo });
                cmd.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pagination.PageSize });

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    customers.Add(new CustomerWithSalesDTO
                    {
                        SrNo = !reader.IsDBNull(0) ? reader.GetInt64(0) : 0,
                        CustomerID = !reader.IsDBNull(1) ? reader.GetInt32(1) : 0,
                        CustomerName = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty,
                        CustomerAddress = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty,
                        PhoneNo = !reader.IsDBNull(4) ? reader.GetString(4) : string.Empty,
                        Email = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty,
                        TotalItems = !reader.IsDBNull(6) ? reader.GetDecimal(6) : 0,
                        PaymentMode = !reader.IsDBNull(7) ? this.GetPaymentModeString(reader.GetInt32(7)) : string.Empty,
                        Remark = !reader.IsDBNull(8) ? reader.GetString(8) : string.Empty,
                        CreatedDate = !reader.IsDBNull(9) ? reader.GetDateTime(9) : DateTime.MinValue,
                        FullName = !reader.IsDBNull(10) ? reader.GetString(10) : string.Empty,
                        TotalRecords = !reader.IsDBNull(11) ? reader.GetInt32(11) : 0
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Failed to retrieve customers with sales. Please try again later.", ex);
            }
            finally
            {
                await sqlconnection.GetConnection().CloseAsync();
            }

            return customers;
        }

        #endregion
    }
}
