using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DATABASE;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;

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
                    insertCmd.CommandType = CommandType.Text;

                    insertCmd.CommandText = @"
                INSERT INTO flexi_erp_product_category 
                    (CategoryName, Description, CreatedBy, CreatedDate)
                VALUES 
                    (@CategoryName, @Description, @CreatedBy, @createdate);

                SELECT last_insert_rowid();  -- Get last inserted CategoryID
            ";

                    // Add parameters
                    insertCmd.Parameters.AddWithValue("@CategoryName", product_Category.CategoryName);
                    insertCmd.Parameters.AddWithValue("@Description", product_Category.Description ?? ""); // SQLite accepts empty string for NULL
                    insertCmd.Parameters.AddWithValue("@CreatedBy", product_Category.CreatedBy);
                    insertCmd.Parameters.AddWithValue("@createdate", DateTime.Now);

                    // Execute and get inserted CategoryID
                    var result = await insertCmd.ExecuteScalarAsync();
                    if (result == null || Convert.ToInt32(result) <= 0)
                    {
                        throw new Exception("Category insertion failed. Please try again.");
                    }

                    int lastInsertId = Convert.ToInt32(result);
                    return lastInsertId;
                }
            }
            catch (SqliteException ex)
            {
                // You can log ex.Message here for debugging
                throw new Exception("Category insertion failed. Please try again later.", ex);
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
                    cmd.CommandType = CommandType.Text;

                    if (onlyActive)
                    {
                        cmd.CommandText = @"
                    SELECT CategoryID, CategoryName, Description
                    FROM flexi_erp_product_category
                    WHERE Status = 1;
                ";
                    }
                    else
                    {
                        cmd.CommandText = @"
                    SELECT CategoryID, CategoryName, Description
                    FROM flexi_erp_product_category;
                ";
                    }

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
            catch (SqliteException ex)
            {
                throw new Exception("Failed to retrieve categories. Please try again later.", ex);
            }
            finally
            {
                await sqlconnection.GetConnection().CloseAsync();
            }

            return categories;
        }

        public async Task<IEnumerable<ProductCategoryListDto>> GetProductCategoryList(bool onlyactive = false)
        {
            var categories = new List<ProductCategoryListDto>();
            var connection = sqlconnection.GetConnection();

            try
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"
                SELECT 
                    CategoryID AS SrNo,
                    CategoryName,
                    Description,
                    CreatedDate
                FROM 
                    flexi_erp_product_category
                ORDER BY 
                    CategoryID;
            ";

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var category = new ProductCategoryListDto
                            {
                                SrNo = !reader.IsDBNull(0) ? reader.GetInt32(0) : 0,
                                CategoryName = !reader.IsDBNull(1) ? reader.GetString(1) : string.Empty,
                                Description = !reader.IsDBNull(2) ? reader.GetString(2) : null,
                                CreatedDate = !reader.IsDBNull(3) ? reader.GetDateTime(3) : DateTime.MinValue
                            };

                            categories.Add(category);
                        }
                    }
                }
            }
            catch (SqliteException ex)
            {
                throw new Exception("Failed to retrieve categories. Please try again later.", ex);
            }
            finally
            {
                await connection.CloseAsync();
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

                // Step 1: Generate Next ProductID for the category
                int nextProductId = 1;
                using (var seqCmd = connection.CreateCommand())
                {
                    seqCmd.CommandText = @"
                SELECT IFNULL(MAX(ProductID), 0) + 1
                FROM Product
                WHERE ProductCategory = @ProductCategory;
            ";
                    seqCmd.Parameters.AddWithValue("@ProductCategory", product.ProductCategory);

                    var seqResult = await seqCmd.ExecuteScalarAsync();
                    if (seqResult != null)
                        nextProductId = Convert.ToInt32(seqResult);
                }

                // Step 2: Generate BarCode YY0CC0PPP
                string currentYear = DateTime.UtcNow.ToString("yy");
                string categoryCode = product.ProductCategory.ToString().PadLeft(2, '0');
                string sequenceNumber = nextProductId.ToString().PadLeft(3, '0');
                string barcode = $"{currentYear}0{categoryCode}0{sequenceNumber}";

                // Step 3: Insert into Product table
                using (var insertCmd = connection.CreateCommand())
                {
                    insertCmd.CommandText = @"
                INSERT INTO Product
                (
                    ProductCode,
                    BarCode,
                    ProductName,
                    ProductCategory,
                    ProductType,
                    PackedDate,
                    PackedWeight,
                    PackedHeight,
                    PackedDepth,
                    PackedWidth,
                    IsPerishable,
                    CreatedBy,
                    CreatedDate,
                    PurchasePrice,
                    SellingPrice,
                    TaxRate,
                    Discount,
                    Quantity,
                    TaxPr,
                    DiscountPr
                )
                VALUES
                (
                    @ProductCode,
                    @BarCode,
                    @ProductName,
                    @ProductCategory,
                    @ProductType,
                    @PackedDate,
                    @PackedWeight,
                    @PackedHeight,
                    @PackedDepth,
                    @PackedWidth,
                    @IsPerishable,
                    @CreatedBy,
                    @CreateDate,
                    @PurchasePrice,
                    @SellingPrice,
                    @TaxRate,
                    @Discount,
                    @Quantity,
                    @TaxPr,
                    @DiscountPr
                );
            ";

                    insertCmd.Parameters.AddWithValue("@ProductCode", product.ProductName); // or generate separate code
                    insertCmd.Parameters.AddWithValue("@BarCode", barcode);
                    insertCmd.Parameters.AddWithValue("@ProductName", product.ProductName);
                    insertCmd.Parameters.AddWithValue("@ProductCategory", product.ProductCategory);
                    insertCmd.Parameters.AddWithValue("@ProductType", (object?)product.ProductType ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@PackedDate", (object?)product.PackedDate ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@PackedWeight", (object?)product.PackedWeight ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@PackedHeight", (object?)product.PackedHeight ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@PackedDepth", (object?)product.PackedDepth ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@PackedWidth", (object?)product.PackedWidth ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@IsPerishable", (object?)product.IsPerishable ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@CreatedBy", product.CreatedBy);
                    insertCmd.Parameters.AddWithValue("@PurchasePrice", (object?)product.PurchasePrice ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@SellingPrice", (object?)product.SellingPrice ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@TaxRate", (object?)product.TaxRate ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Discount", (object?)product.Discount ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Quantity", (object?)product.productQunatity ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@TaxPr", (object?)product.taxpr ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@DiscountPr", (object?)product.discounpr ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("CreateDate", DateTime.Now);

                    int rows = await insertCmd.ExecuteNonQueryAsync();
                    if (rows <= 0)
                        throw new Exception("Product insertion failed. Please try again.");
                }

                return barcode; // return generated barcode
            }
            catch (SqliteException ex)
            {
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
                cmd.CommandType = CommandType.Text;

                // Build query dynamically
                var sql = @"
            SELECT 
                p.ProductID,
                p.ProductCode,
                p.BarCode,
                p.ProductName,
                c.CategoryName,
                p.ProductType,
                p.PackedDate,
                p.PackedWeight,
                p.PackedHeight,
                p.PackedDepth,
                p.PackedWidth,
                p.IsPerishable,
                p.CreatedDate,
                p.PurchasePrice,
                p.SellingPrice,
                p.TaxRate,
                p.Discount,
                u.FullName,
                (SELECT COUNT(*) FROM Product p2
                 LEFT JOIN flexi_erp_product_category c2 ON p2.ProductCategory = c2.CategoryID
                 LEFT JOIN Tbl_Users u2 ON p2.CreatedBy = u2.UserID
                 WHERE (@StartDate IS NULL OR DATE(p2.CreatedDate) >= DATE(@StartDate))
                   AND (@EndDate IS NULL OR DATE(p2.CreatedDate) <= DATE(@EndDate))
                   AND (@SearchTerm IS NULL OR p2.ProductName LIKE '%' || @SearchTerm || '%')
                ) AS TotalRecords,
                p.TaxPr,
                p.DiscountPr
            FROM Product p
            LEFT JOIN flexi_erp_product_category c ON p.ProductCategory = c.CategoryID
            LEFT JOIN Tbl_Users u ON p.CreatedBy = u.UserID
            WHERE (@StartDate IS NULL OR DATE(p.CreatedDate) >= DATE(@StartDate))
              AND (@EndDate IS NULL OR DATE(p.CreatedDate) <= DATE(@EndDate))
              AND (@SearchTerm IS NULL OR p.ProductName LIKE '%' || @SearchTerm || '%')
            ORDER BY p.CreatedDate DESC
            LIMIT @PageSize OFFSET @Offset;
        ";

                cmd.CommandText = sql;

                // Normalize pagination
                int pageNo = filter.PageNo < 1 ? 1 : filter.PageNo;
                int pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;
                int offset = (pageNo - 1) * pageSize;

                cmd.Parameters.AddWithValue("@StartDate", string.IsNullOrEmpty(filter.StartDate) ? DBNull.Value : filter.StartDate);
                cmd.Parameters.AddWithValue("@EndDate", string.IsNullOrEmpty(filter.EndDate) ? DBNull.Value : filter.EndDate);
                cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(filter.SearchTerm) ? DBNull.Value : filter.SearchTerm);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);

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
                        TotalRecords = !reader.IsDBNull(18) ? reader.GetInt32(18) : 0,
                        taxpr = !reader.IsDBNull(19) ? reader.GetDecimal(19) : 0,
                        discounpr = !reader.IsDBNull(20) ? reader.GetDecimal(20) : 0,
                    });
                }
            }
            catch (SqliteException ex)
            {
                throw new Exception("Failed to retrieve products. Please try again later.", ex);
            }
            finally
            {
                await sqlconnection.GetConnection().CloseAsync();
            }

            return products;
        }

        public async Task<IEnumerable<Product_DTO>> GetSoldProductsList(PaginationFilter filter)
        {
            var products = new List<Product_DTO>();

            try
            {
                using var connection = sqlconnection.GetConnection(); // your ISqlConnectionService for SQLite
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();

                cmd.CommandText = @"
           WITH ProductCTE AS (
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
        pr.CreatedDate,
        pr.PurchasePrice,
        pr.SellingPrice,
        pr.TaxRate,
        pr.Discount,
        fuser.FullName,
        ROW_NUMBER() OVER (ORDER BY pr.CreatedDate DESC) AS RowNum,
		sale.CreatedDate,
		sale.Quantity as soldquantity,
		(pr.Quantity - sale.Quantity) as availableQuantity
    FROM product AS pr
    INNER JOIN saledetail AS sale ON pr.ProductID = sale.ProductID
    LEFT JOIN flexi_erp_product_category AS category ON pr.ProductCategory = category.CategoryID
    LEFT JOIN Tbl_Users AS fuser ON pr.CreatedBy = fuser.UserID
    WHERE
        (@StartDate IS NULL OR DATE(pr.CreatedDate) >= DATE(@StartDate))
        AND (@EndDate IS NULL OR DATE(pr.CreatedDate) <= DATE(@EndDate))
        AND (
            @SearchTerm IS NULL OR
            pr.ProductName LIKE '%' || @SearchTerm || '%' OR
            pr.ProductCode LIKE '%' || @SearchTerm || '%' OR
            pr.BarCode LIKE '%' || @SearchTerm || '%' OR
            category.CategoryName LIKE '%' || @SearchTerm || '%' OR
            fuser.FullName LIKE '%' || @SearchTerm || '%'
        )
    GROUP BY
        pr.ProductID, pr.ProductCode, pr.BarCode, pr.ProductName,
        category.CategoryName, pr.ProductType, pr.PackedDate,
        pr.PackedWeight, pr.PackedHeight, pr.PackedDepth, pr.PackedWidth,
        pr.IsPerishable, pr.CreatedDate, pr.PurchasePrice,
        pr.SellingPrice, pr.TaxRate, pr.Discount, fuser.FullName
),
TotalCount AS (
    SELECT COUNT(*) AS TotalRecords FROM ProductCTE
)
SELECT
    p.ProductID,
    p.ProductCode,
    p.BarCode,
    p.ProductName,
    p.CategoryName,
    p.ProductType,
    p.PackedDate,
    p.PackedWeight,
    p.PackedHeight,
    p.PackedDepth,
    p.PackedWidth,
    p.IsPerishable,
    p.CreatedDate,
    p.PurchasePrice,
    p.SellingPrice,
    p.TaxRate,
    p.Discount,
    p.FullName,
    t.TotalRecords,
	p.CreatedDate,
	p.soldquantity,
	p.availableQuantity
FROM ProductCTE p, TotalCount t
WHERE p.RowNum > @Offset AND p.RowNum <= (@Offset + @PageSize);
        ";

                // Pagination setup
                int pageNo = filter.PageNo <= 0 ? 1 : filter.PageNo;
                int pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
                int offset = (pageNo - 1) * pageSize;

                // Parameters
                cmd.Parameters.Add(new SqliteParameter("@StartDate", string.IsNullOrEmpty(filter.StartDate) ? (object)DBNull.Value : filter.StartDate));
                cmd.Parameters.Add(new SqliteParameter("@EndDate", string.IsNullOrEmpty(filter.EndDate) ? (object)DBNull.Value : filter.EndDate));
                cmd.Parameters.Add(new SqliteParameter("@SearchTerm", string.IsNullOrEmpty(filter.SearchTerm) ? (object)DBNull.Value : filter.SearchTerm));
                cmd.Parameters.Add(new SqliteParameter("@PageNo", pageNo));
                cmd.Parameters.Add(new SqliteParameter("@PageSize", pageSize));
                cmd.Parameters.Add(new SqliteParameter("@Offset", offset));

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
                        TotalRecords = !reader.IsDBNull(18) ? reader.GetInt32(18) : 0,
                        solddate = !reader.IsDBNull(19) ? reader.GetDateTime(19) : null,
                        soldquantity = !reader.IsDBNull(20) ? reader.GetInt32(20) : 0,
                        availablequantity = !reader.IsDBNull(21) ? reader.GetInt32(21) : 0,

                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve sold products (SQLite).", ex);
            }

            return products;
        }

        #endregion

        #region vendors / provider
        public async Task<int> AddProvider(ProviderModel provider)
        {
            SqliteConnection connection = null;
            try
            {
                connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                INSERT INTO Providers
                (
                    ProviderName,
                    ProviderType,
                    ContactPerson,
                    ContactEmail,
                    ContactPhone,
                    ProviderAddress,
                    City,
                    State,
                    Country,
                    PaymentTerms,
                    CreatedBy,
                    CreatedDate
                )
                VALUES
                (
                    @ProviderName,
                    @ProviderType,
                    @ContactPerson,
                    @ContactEmail,
                    @ContactPhone,
                    @ProviderAddress,
                    @City,
                    @State,
                    @Country,
                    @PaymentTerms,
                    @CreatedBy,
                    @CreatedDate
                );
                SELECT last_insert_rowid();"; // Get the last inserted ProviderID

                    cmd.Parameters.Add(new SqliteParameter("@ProviderName", provider.ProviderName));
                    cmd.Parameters.Add(new SqliteParameter("@ProviderType", (object)provider.ProviderType ?? DBNull.Value));
                    cmd.Parameters.Add(new SqliteParameter("@ContactPerson", (object)provider.ContactPerson ?? DBNull.Value));
                    cmd.Parameters.Add(new SqliteParameter("@ContactEmail", (object)provider.ContactEmail ?? DBNull.Value));
                    cmd.Parameters.Add(new SqliteParameter("@ContactPhone", (object)provider.ContactPhone ?? DBNull.Value));
                    cmd.Parameters.Add(new SqliteParameter("@ProviderAddress", (object)provider.ProviderAddress ?? DBNull.Value));
                    cmd.Parameters.Add(new SqliteParameter("@City", (object)provider.City ?? DBNull.Value));
                    cmd.Parameters.Add(new SqliteParameter("@State", (object)provider.State ?? DBNull.Value));
                    cmd.Parameters.Add(new SqliteParameter("@Country", (object)provider.Country ?? DBNull.Value));
                    cmd.Parameters.Add(new SqliteParameter("@PaymentTerms", (object)provider.PaymentTerms ?? DBNull.Value));
                    cmd.Parameters.Add(new SqliteParameter("@CreatedBy", provider.CreatedBy));
                    cmd.Parameters.Add(new SqliteParameter("@CreatedDate", DateTime.Now));

                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
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
                using var connection = sqlconnection.GetConnection(); // returns SqliteConnection
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
            WITH ProviderCTE AS (
                SELECT
                    p.ProviderID,
                    p.ProviderName,
                    p.ProviderType,
                    p.ContactPerson,
                    p.ContactEmail,
                    p.ContactPhone,
                    p.ProviderAddress,
                    p.City,
                    p.State,
                    p.Country,
                    p.PaymentTerms,
                    p.CreatedBy,
                    p.CreatedDate,
                    u.FullName AS CreatedByName,
                    ROW_NUMBER() OVER (ORDER BY p.CreatedDate DESC) AS RowNum,
                    COUNT(*) OVER() AS TotalRows
                FROM Providers p
                LEFT JOIN Tbl_Users u ON p.CreatedBy = u.UserID
                WHERE 
                    (@StartDate IS NULL OR date(p.CreatedDate) >= date(@StartDate))
                    AND (@EndDate IS NULL OR date(p.CreatedDate) <= date(@EndDate))
                    AND (
                        @SearchTerm IS NULL 
                        OR p.ProviderName LIKE '%' || @SearchTerm || '%'
                        OR p.ContactPerson LIKE '%' || @SearchTerm || '%'
                        OR p.ContactEmail LIKE '%' || @SearchTerm || '%'
                        OR p.City LIKE '%' || @SearchTerm || '%'
                        OR p.State LIKE '%' || @SearchTerm || '%'
                        OR p.Country LIKE '%' || @SearchTerm || '%'
                    )
            )
            SELECT
                RowNum AS SrNo,
                ProviderID,
                ProviderName,
                ProviderType,
                ContactPerson,
                ContactEmail,
                ContactPhone,
                ProviderAddress,
                City,
                State,
                Country,
                PaymentTerms,
                CreatedBy,
                CreatedDate,
                CreatedByName,
                TotalRows
            FROM ProviderCTE
            WHERE RowNum > ((@PageNo - 1) * @PageSize)
              AND RowNum <= (@PageNo * @PageSize)
            ORDER BY CreatedDate DESC;
        ";

                // Add parameters
                cmd.Parameters.AddWithValue("@StartDate", string.IsNullOrEmpty(filter.StartDate) ? DBNull.Value : filter.StartDate);
                cmd.Parameters.AddWithValue("@EndDate", string.IsNullOrEmpty(filter.EndDate) ? DBNull.Value : filter.EndDate);
                cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(filter.SearchTerm) ? DBNull.Value : filter.SearchTerm);
                cmd.Parameters.AddWithValue("@PageNo", filter.PageNo);
                cmd.Parameters.AddWithValue("@PageSize", filter.PageSize);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    providers.Add(new Provider_DTO
                    {
                        SrNo = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                        ProviderID = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        ProviderName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        ProviderType = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        ContactPerson = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        ContactEmail = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        ContactPhone = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        ProviderAddress = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        City = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                        State = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                        Country = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                        PaymentTerms = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                        CreatedBy = reader.IsDBNull(12) ? 0 : reader.GetInt32(12),
                        CreatedDate = reader.IsDBNull(13) ? DateTime.MinValue : reader.GetDateTime(13),
                        CreatedByName = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                        TotalRows = reader.IsDBNull(15) ? 0 : reader.GetInt32(15)
                    });
                }
            }
            catch (SqliteException ex)
            {
                throw new Exception("Failed to retrieve providers. Please try again later.", ex);
            }

            return providers;
        }
        #endregion

        #region Warehouse Work
        public async Task<int> AddWarehouse(WarehouseModel warehouse)
        {
            SqliteConnection connection = null;
            try
            {
                connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    // Use local system datetime from C#
                    cmd.CommandText = @"
                INSERT INTO Warehouse
                (
                    WarehouseName,
                    IsRefrigerated,
                    CreatedBy,
                    Remark,
                    CreatedDate
                )
                VALUES
                (
                    @WarehouseName,
                    @IsRefrigerated,
                    @CreatedBy,
                    @Remark,
                    @CreatedDate
                );
                SELECT last_insert_rowid();"; // Return new inserted WarehouseID

                    // Parameters
                    cmd.Parameters.Add(new SqliteParameter("@WarehouseName", warehouse.WarehouseName));
                    cmd.Parameters.Add(new SqliteParameter("@IsRefrigerated", warehouse.IsRefrigerated ? 1 : 0));
                    cmd.Parameters.Add(new SqliteParameter("@CreatedBy", warehouse.CreatedBy));
                    cmd.Parameters.Add(new SqliteParameter("@Remark", (object)warehouse.Remark ?? DBNull.Value));
                    cmd.Parameters.Add(new SqliteParameter("@CreatedDate", DateTime.Now)); // ✅ local date and time

                    // Execute and return the new ID
                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
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
            SqliteConnection connection = null;

            try
            {
                connection = sqlconnection.GetConnection();
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                // Run direct SQL instead of stored procedure
                cmd.CommandText = @"
            SELECT 
                WarehouseID,
                WarehouseName,
                IsRefrigerated,
                CreatedBy,
                Remark,
                CreatedDate
            FROM Warehouse
            ORDER BY datetime(CreatedDate) DESC;";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    warehouses.Add(new Warehouse_DTO
                    {
                        WarehouseID = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        WarehouseName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        IsRefrigerated = !reader.IsDBNull(2) && reader.GetInt32(2) == 1, // SQLite stores bool as 0/1
                        CreatedBy = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        Remark = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        CreatedDate = reader.IsDBNull(5)
                            ? DateTime.MinValue
                            : Convert.ToDateTime(reader.GetValue(5)) // SQLite stores text datetime
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve warehouses. Please try again later.", ex);
            }
            finally
            {
                if (connection != null)
                    await connection.CloseAsync();
            }

            return warehouses;
        }

        #endregion
    }
}
