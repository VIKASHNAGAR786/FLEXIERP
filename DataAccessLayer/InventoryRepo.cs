using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DATABASE;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;
using FLEXIERP.MODELS.AGRIMANDI.Model;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace FLEXIERP.DataAccessLayer
{
    public class InventoryRepo : IInventoryRepo
    {
        private readonly IDataBaseOperation sqlconnection;

        public InventoryRepo(IDataBaseOperation _sqlconnection)
        {
            sqlconnection = _sqlconnection;
        }

        #region product category
        public async Task<int> AddCategory(Product_Category product_Category)
        {
            try
            {
                var connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using (var insertCmd = connection.CreateCommand())
                {
                    insertCmd.CommandText = "usp_Category_insert";
                    insertCmd.CommandType = CommandType.StoredProcedure;

                    insertCmd.Parameters.Add(new SqlParameter("@CategoryName", SqlDbType.VarChar, 100)
                    {
                        Value = product_Category.CategoryName
                    });
                    insertCmd.Parameters.Add(new SqlParameter("@Description", SqlDbType.VarChar, -1) // -1 for MAX
                    {
                        Value = product_Category.Description ?? (object)DBNull.Value
                    });
                    insertCmd.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.Int)
                    {
                        Value = product_Category.CreatedBy
                    });

                    // Execute and get inserted CategoryID
                    var result = await insertCmd.ExecuteScalarAsync();
                    if (result == null || Convert.ToInt32(result) <= 0)
                    {
                        throw new Exception("Category insertion failed. Please try again.");
                    }

                    int lastinsertid = Convert.ToInt32(result);
                    return lastinsertid;
                }
            }
            catch (SqlException ex)
            {
                // You can log ex.Message here for debugging
                throw new Exception("Category insertion failed. Please try again later.");
            }
            finally
            {
                await sqlconnection.GetConnection().CloseAsync();
            }
        }

        public async Task<IEnumerable<ProductCategory_DTO>> GetCategories(bool onlyActive = false)
        {
            var categories = new List<ProductCategory_DTO>();

            try
            {
                var connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "Get_Categories";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@OnlyActive", SqlDbType.Bit)
                    {
                        Value = onlyActive
                    });

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var category = new ProductCategory_DTO
                            {
                                CategoryID = !reader.IsDBNull(0) ? reader.GetInt32(0) : null,
                                CategoryName = !reader.IsDBNull(1) ? reader.GetString(1) : null,
                                Description = !reader.IsDBNull(2) ? reader.GetString(2) : null,
                            };
                            categories.Add(category);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // Log ex.Message if needed
                throw new Exception("Failed to retrieve categories. Please try again later.", ex);
            }
            finally
            {
                await sqlconnection.GetConnection().CloseAsync();
            }

            return categories;
        }
        #endregion

        #region add product
        public async Task<string> AddProduct(ProductModel product)
        {
            try
            {
                var connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "usp_Insert_Product";
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    cmd.Parameters.Add(new SqlParameter("@ProductName", SqlDbType.NVarChar, 100) { Value = product.ProductName });
                    cmd.Parameters.Add(new SqlParameter("@ProductCategory", SqlDbType.Int) { Value = product.ProductCategory });
                    cmd.Parameters.Add(new SqlParameter("@ProductType", SqlDbType.NVarChar, 100) { Value = (object)product.ProductType ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@PackedDate", SqlDbType.Date) { Value = (object)product.PackedDate ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@PackedWeight", SqlDbType.Int) { Value = (object)product.PackedWeight ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@PackedHeight", SqlDbType.Int) { Value = (object)product.PackedHeight ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@PackedDepth", SqlDbType.Int) { Value = (object)product.PackedDepth ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@PackedWidth", SqlDbType.Int) { Value = (object)product.PackedWidth ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@IsPerishable", SqlDbType.Bit) { Value = (object)product.IsPerishable ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.Int) { Value = product.CreatedBy });
                    cmd.Parameters.Add(new SqlParameter("@PurchasePrice", SqlDbType.Decimal) { Value = (object)product.PurchasePrice ?? DBNull.Value, Precision = 18, Scale = 2 });
                    cmd.Parameters.Add(new SqlParameter("@SellingPrice", SqlDbType.Decimal) { Value = (object)product.SellingPrice ?? DBNull.Value, Precision = 18, Scale = 2 });
                    cmd.Parameters.Add(new SqlParameter("@TaxRate", SqlDbType.Decimal) { Value = (object)product.TaxRate ?? DBNull.Value, Precision = 18, Scale = 2 });
                    cmd.Parameters.Add(new SqlParameter("@Discount", SqlDbType.Decimal) { Value = (object)product.Discount ?? DBNull.Value, Precision = 18, Scale = 2 });

                    // Execute and get the generated barcode
                    var result = await cmd.ExecuteScalarAsync();
                    if (result == null || string.IsNullOrEmpty(result.ToString()))
                    {
                        throw new Exception("Product insertion failed. Please try again.");
                    }

                    return result.ToString(); // Returns the GeneratedBarCode
                }
            }
            catch (SqlException ex)
            {
                // Log ex.Message if needed
                throw new Exception("Product insertion failed. Please try again later.", ex);
            }
            finally
            {
                await sqlconnection.GetConnection().CloseAsync();
            }
        }
        #endregion

        #region get product
        public async Task<IEnumerable<Product_DTO>> GetProducts(PaginationFilter filter)
        {
            var products = new List<Product_DTO>();

            try
            {
                using var connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "usp_GetProducts";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.Date)
                { Value = string.IsNullOrEmpty(filter.StartDate) ? DBNull.Value : filter.StartDate });
                cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.Date)
                { Value = string.IsNullOrEmpty(filter.EndDate) ? DBNull.Value : filter.EndDate });
                cmd.Parameters.Add(new SqlParameter("@SearchTerm", SqlDbType.NVarChar, 100)
                { Value = string.IsNullOrEmpty(filter.SearchTerm) ? DBNull.Value : filter.SearchTerm });
                cmd.Parameters.Add(new SqlParameter("@PageNo", SqlDbType.Int) { Value = filter.PageNo });
                cmd.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = filter.PageSize });

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    products.Add(new Product_DTO
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
                        CreatedDate = !reader.IsDBNull(12) ? reader.GetDateTime(12) : null,
                        PurchasePrice = !reader.IsDBNull(13) ? reader.GetDecimal(13) : null,
                        SellingPrice = !reader.IsDBNull(14) ? reader.GetDecimal(14) : null,
                        TaxRate = !reader.IsDBNull(15) ? reader.GetDecimal(15) : null,
                        Discount = !reader.IsDBNull(16) ? reader.GetDecimal(16) : null,
                        FullName = !reader.IsDBNull(17) ? reader.GetString(17) : string.Empty,
                        TotalRecords = !reader.IsDBNull(18) ? reader.GetInt32(18) : 0
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Failed to retrieve products. Please try again later.", ex);
            }
            finally
            {
                await sqlconnection.GetConnection().CloseAsync();
            }

            return products;
        }

        #endregion

        #region vendors / provider
        public async Task<int> AddProvider(ProviderModel provider)
        {
            SqlConnection connection = null;
            try
            {
                connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "usp_InsertProvider";
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Input parameters
                    cmd.Parameters.Add(new SqlParameter("@ProviderName", SqlDbType.VarChar, 150) { Value = provider.ProviderName });
                    cmd.Parameters.Add(new SqlParameter("@ProviderType", SqlDbType.VarChar, 50) { Value = (object)provider.ProviderType ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@ContactPerson", SqlDbType.VarChar, 100) { Value = (object)provider.ContactPerson ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@ContactEmail", SqlDbType.VarChar, 100) { Value = (object)provider.ContactEmail ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@ContactPhone", SqlDbType.VarChar, 20) { Value = (object)provider.ContactPhone ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@ProviderAddress", SqlDbType.VarChar, 200) { Value = (object)provider.ProviderAddress ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@City", SqlDbType.VarChar, 50) { Value = (object)provider.City ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@State", SqlDbType.VarChar, 50) { Value = (object)provider.State ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@Country", SqlDbType.VarChar, 50) { Value = (object)provider.Country ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@PaymentTerms", SqlDbType.VarChar, 50) { Value = (object)provider.PaymentTerms ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.Int) { Value = provider.CreatedBy });

                    // Output parameter
                    var outputId = new SqlParameter("@NewProviderID", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(outputId);

                    await cmd.ExecuteNonQueryAsync();

                    return (int)outputId.Value; // return the newly inserted ProviderID
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Provider insertion failed. Please try again later.", ex);
            }
            finally
            {
                if (connection != null)
                    await connection.CloseAsync();
            }
        }
        public async Task<IEnumerable<Provider_DTO>> GetProviders(PaginationFilter filter)
        {
            var providers = new List<Provider_DTO>();

            try
            {
                using var connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "usp_GetProviders";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.NVarChar, 50)
                { Value = string.IsNullOrEmpty(filter.StartDate) ? DBNull.Value : filter.StartDate });

                cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.NVarChar, 50)
                { Value = string.IsNullOrEmpty(filter.EndDate) ? DBNull.Value : filter.EndDate });

                cmd.Parameters.Add(new SqlParameter("@SearchTerm", SqlDbType.NVarChar, 100)
                { Value = string.IsNullOrEmpty(filter.SearchTerm) ? DBNull.Value : filter.SearchTerm });

                cmd.Parameters.Add(new SqlParameter("@PageNo", SqlDbType.Int) { Value = filter.PageNo });
                cmd.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = filter.PageSize });

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    providers.Add(new Provider_DTO
                    {
                        SrNo = !reader.IsDBNull(0) ? reader.GetInt64(0) : 0,
                        ProviderID = !reader.IsDBNull(1) ? reader.GetInt32(1) : 0,
                        ProviderName = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty,
                        ProviderType = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty,
                        ContactPerson = !reader.IsDBNull(4) ? reader.GetString(4) : string.Empty,
                        ContactEmail = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty,
                        ContactPhone = !reader.IsDBNull(6) ? reader.GetString(6) : string.Empty,
                        ProviderAddress = !reader.IsDBNull(7) ? reader.GetString(7) : string.Empty,
                        City = !reader.IsDBNull(8) ? reader.GetString(8) : string.Empty,
                        State = !reader.IsDBNull(9) ? reader.GetString(9) : string.Empty,
                        Country = !reader.IsDBNull(10) ? reader.GetString(10) : string.Empty,
                        PaymentTerms = !reader.IsDBNull(11) ? reader.GetString(11) : string.Empty,
                        CreatedBy = !reader.IsDBNull(12) ? reader.GetInt32(12) : 0,
                        CreatedDate = !reader.IsDBNull(13) ? reader.GetDateTime(13) : null,
                        CreatedByName = !reader.IsDBNull(14) ? reader.GetString(14) : string.Empty,
                        TotalRows = !reader.IsDBNull(15) ? reader.GetInt32(15) : 0
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Failed to retrieve providers. Please try again later.", ex);
            }
            finally
            {
                await sqlconnection.GetConnection().CloseAsync();
            }

            return providers;
        }

        #endregion

        #region Warehouse Work
        public async Task<int> AddWarehouse(WarehouseModel warehouse)
        {
            SqlConnection connection = null;
            try
            {
                connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "usp_Insert_Warehouse";
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Input parameters
                    cmd.Parameters.Add(new SqlParameter("@WarehouseName", SqlDbType.NVarChar, 200)
                    {
                        Value = warehouse.WarehouseName
                    });

                    cmd.Parameters.Add(new SqlParameter("@IsRefrigerated", SqlDbType.Bit)
                    {
                        Value = warehouse.IsRefrigerated
                    });

                    cmd.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.Int)
                    {
                        Value = warehouse.CreatedBy
                    });

                    cmd.Parameters.Add(new SqlParameter("@Remark", SqlDbType.NVarChar, 255)
                    {
                        Value = (object)warehouse.Remark ?? DBNull.Value
                    });

                    // Execute & fetch new ID
                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Warehouse insertion failed. Please try again later.", ex);
            }
            finally
            {
                if (connection != null)
                    await connection.CloseAsync();
            }
        }
        public async Task<IEnumerable<Warehouse_DTO>> GetWarehouses()
        {
            var warehouses = new List<Warehouse_DTO>();

            try
            {
                using var connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "usp_Get_Warehouses";
                cmd.CommandType = CommandType.StoredProcedure;

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    warehouses.Add(new Warehouse_DTO
                    {
                        WarehouseID = !reader.IsDBNull(0) ? reader.GetInt32(0) : 0,
                        WarehouseName = !reader.IsDBNull(1) ? reader.GetString(1) : string.Empty,
                        IsRefrigerated = !reader.IsDBNull(2) && reader.GetBoolean(2),
                        CreatedBy = !reader.IsDBNull(3) ? reader.GetInt32(3) : 0,
                        Remark = !reader.IsDBNull(4) ? reader.GetString(4) : string.Empty,
                        CreatedDate = !reader.IsDBNull(5) ? reader.GetDateTime(5) : DateTime.MinValue
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Failed to retrieve warehouses. Please try again later.", ex);
            }
            finally
            {
                await sqlconnection.GetConnection().CloseAsync();
            }

            return warehouses;
        }

        #endregion
    }
}
