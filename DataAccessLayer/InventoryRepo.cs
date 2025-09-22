using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DATABASE;
using FLEXIERP.MODELS;
using FLEXIERP.MODELS.AGRIMANDI.Model;
using Microsoft.Data.SqlClient;
using System.Data;

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
    }
}
