using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DATABASE;
using Microsoft.Data.SqlClient;

namespace FLEXIERP.DataAccessLayer
{
    public class VersionUpdate : IVersionUpdate
    {
        private readonly IDataBaseOperation sqlconnection;

        public VersionUpdate(IDataBaseOperation _sqlconnection)
        {
            sqlconnection = _sqlconnection;
        }

        #region Version Update
        public async Task<int> UpdateVersion(string version)
        {
            try
            {
                using (var connection = sqlconnection.GetConnection())
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            if (version == "1.0.0")
                            {
                                var cmd1 = connection.CreateCommand();
                                cmd1.Transaction = transaction;
                                cmd1.CommandText = @"
                            IF NOT EXISTS (
                                SELECT * 
                                FROM INFORMATION_SCHEMA.TABLES 
                                WHERE TABLE_NAME = 'Tbl_Roles' AND TABLE_TYPE = 'BASE TABLE'
                            )
                            BEGIN
                                CREATE TABLE Tbl_Roles (
                                    RoleID INT IDENTITY(1,1) PRIMARY KEY,
                                    RoleName VARCHAR(50) NOT NULL UNIQUE,
                                    Description VARCHAR(255),
                                    CreatedAt DATETIME DEFAULT GETDATE(),
                                    UpdatedAt DATETIME
                                );
                            END
                        ";
                                await cmd1.ExecuteNonQueryAsync();

                                var cmd2 = connection.CreateCommand();
                                cmd2.Transaction = transaction;
                                cmd2.CommandText = @"
                            IF NOT EXISTS (
                                SELECT * 
                                FROM INFORMATION_SCHEMA.TABLES 
                                WHERE TABLE_NAME = 'Tbl_Users' AND TABLE_TYPE = 'BASE TABLE'
                            )
                            BEGIN
                                CREATE TABLE Tbl_Users (
                                    UserID INT PRIMARY KEY IDENTITY(1,1),
                                    FullName VARCHAR(100) NOT NULL,
                                    Username VARCHAR(50) NOT NULL UNIQUE,
                                    Email VARCHAR(100) NOT NULL UNIQUE,
                                    PasswordHash VARCHAR(256) NOT NULL,
                                    MobileNo VARCHAR(15),
                                    Gender VARCHAR(10),
                                    DateOfBirth DATE,
                                    Address VARCHAR(255),
                                    City VARCHAR(50),
                                    State VARCHAR(50),
                                    Country VARCHAR(50),
                                    ProfileImageUrl VARCHAR(255),
                                    RoleID INT NOT NULL,
                                    CreatedAt DATETIME DEFAULT GETDATE(),
                                    UpdatedAt DATETIME,
                                    LastLoginAt DATETIME,
                                    IsActive BIT DEFAULT 1,
                                    IsEmailVerified BIT DEFAULT 0,

                                    CONSTRAINT FK_Users_Role FOREIGN KEY (RoleID) REFERENCES Tbl_Roles(RoleID)
                                );
                            END
                        ";
                                await cmd2.ExecuteNonQueryAsync();

                                var cmd3 = connection.CreateCommand();
                                cmd3.Transaction = transaction;
                                cmd3.CommandText = @"
                                                        CREATE OR ALTER PROCEDURE [pro_Tbl_Users_insert]  
                                                        @p_FullName VARCHAR(100),  
                                                        @p_Username VARCHAR(50),  
                                                        @p_Email VARCHAR(100),  
                                                        @p_PasswordHash VARCHAR(256),  
                                                        @p_MobileNo VARCHAR(15),  
                                                        @p_Gender VARCHAR(10) NULL,  
                                                        @p_DateOfBirth DATE ,  
                                                        @p_Address VARCHAR(255) NULL,  
                                                        @p_City VARCHAR(50) NULL,  
                                                        @p_State VARCHAR(50) NULL,  
                                                        @p_Country VARCHAR(50) NULL,  
                                                        @p_ProfileImageUrl VARCHAR(255) NULL,  
                                                        @p_RoleID INT ,   
                                                        @p_LastLoginAt DATETIME,  
                                                        @p_IsActive BIT NULL,  
                                                        @p_IsEmailVerified BIT NULL  
                                                        AS  
                                                         BEGIN  
                                                          BEGIN TRY  
                                                         BEGIN TRANSACTION;  
                                                         IF EXISTS (SELECT 1 FROM Tbl_Users WHERE Email = @p_Email OR Username = @p_Username)  
                                                                BEGIN  
                                                                    RAISERROR('Email or Username already exists.', 16, 1);  
                                                                    ROLLBACK TRANSACTION;  
                                                                    RETURN;  
                                                                END  
                                                           INSERT INTO Tbl_Users  
                                                           (  
                                                        FullName,  
                                                        Username,  
                                                        Email,  
                                                        PasswordHash ,  
                                                        MobileNo ,  
                                                        Gender ,  
                                                        DateOfBirth ,  
                                                        Address ,  
                                                        City ,  
                                                        State ,  
                                                        Country ,  
                                                        ProfileImageUrl ,  
                                                        RoleID ,  
                                                        LastLoginAt ,  
                                                        IsActive ,  
                                                        IsEmailVerified   
                                                           )  
                                                           Values  
                                                           (  
                                                        @p_FullName ,  
                                                        @p_Username ,  
                                                        @p_Email ,  
                                                        @p_PasswordHash ,  
                                                        @p_MobileNo ,  
                                                        @p_Gender ,  
                                                        @p_DateOfBirth ,  
                                                        @p_Address ,  
                                                        @p_City ,  
                                                        @p_State ,  
                                                        @p_Country ,  
                                                        @p_ProfileImageUrl ,  
                                                        @p_RoleID ,   
                                                        @p_LastLoginAt ,  
                                                        @p_IsActive ,  
                                                        @p_IsEmailVerified  
                                                           )  
                                                        COMMIT TRANSACTION;  
                                                         END TRY   
                                                         BEGIN CATCH  
                                                        IF (XACT_STATE())  <>  0  
                                                        BEGIN   
                                                        ROLLBACK TRANSACTION;  
                                                         set nocount on;  
                                                         declare @lastidentity int;  
                                                         select @lastidentity =IDENT_CURRENT('Tbl_Users') -1;  
                                                         DBCC checkident ('Tbl_Users',reseed,@lastidentity) with no_infomsgs;  
                                                        END;  
                                                        END CATCH;  
                                                        END;  
                                                        GO
                            
                        ";
                                await cmd3.ExecuteNonQueryAsync();

                                var cmd4 = connection.CreateCommand();
                                cmd4.Transaction = transaction;
                                cmd4.CommandText = @"
                                                       CREATE OR ALTER PROCEDURE get_procedure
                                                                    @ObjectName NVARCHAR(128)
                                                                AS
                                                                BEGIN
                                                                    SET NOCOUNT ON;

                                                                    IF NOT EXISTS (
                                                                        SELECT 1 
                                                                        FROM sys.objects 
                                                                        WHERE name = @ObjectName AND type IN ('P', 'FN', 'TF', 'IF')
                                                                    )
                                                                    BEGIN
                                                                        PRINT 'Procedure or Function not found.';
                                                                        RETURN;
                                                                    END

                                                                    EXEC sp_helptext @ObjectName;
                                                                END
                                                                GO
                            
                        ";
                                await cmd4.ExecuteNonQueryAsync();

                                var cmd5 = connection.CreateCommand();
                                cmd5.Transaction = transaction;
                                cmd5.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'flexi_erp_product_category')
                BEGIN
                    CREATE TABLE flexi_erp_product_category (
                        CategoryID INT IDENTITY(1,1) PRIMARY KEY,
                        CategoryName VARCHAR(100) NOT NULL,
                        Description VARCHAR(MAX) NULL,
                        Status BIT NOT NULL DEFAULT 1,
                        CreatedBy INT NOT NULL,
                        UpdatedBy INT NULL,
                        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                        UpdatedDate DATETIME NULL,
                        CONSTRAINT FK_flexi_erp_product_category_CreatedBy FOREIGN KEY (CreatedBy)
                            REFERENCES Tbl_Users(UserID),
                        CONSTRAINT FK_flexi_erp_product_category_UpdatedBy FOREIGN KEY (UpdatedBy)
                            REFERENCES Tbl_Users(UserID)
                    );
                END
            ";
                                await cmd5.ExecuteNonQueryAsync();

                                var cmd6 = connection.CreateCommand();
                                cmd6.Transaction = transaction;
                                cmd6.CommandText = @"
                CREATE OR ALTER PROCEDURE usp_Category_insert
                    @CategoryName VARCHAR(100),
                    @Description VARCHAR(MAX) = NULL,
                    @CreatedBy INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    INSERT INTO flexi_erp_product_category (CategoryName, Description, CreatedBy, CreatedDate)
                    VALUES (@CategoryName, @Description, @CreatedBy, GETDATE());

                    SELECT SCOPE_IDENTITY() AS NewCategoryID;
                END
            ";
                                await cmd6.ExecuteNonQueryAsync();

                                var cmd7 = connection.CreateCommand();
                                cmd7.Transaction = transaction;
                                cmd7.CommandText = @"
                CREATE OR ALTER PROCEDURE Get_Categories
                    @OnlyActive BIT = 0
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @OnlyActive = 1
                    BEGIN
                        SELECT CategoryID, CategoryName, Description 
                        FROM flexi_erp_product_category
                        WHERE Status = 1;
                    END
                    ELSE
                    BEGIN
                        SELECT * 
                        FROM flexi_erp_product_category;
                    END
                END
            ";
                                await cmd7.ExecuteNonQueryAsync();
                            }

                            // All commands executed successfully
                            transaction.Commit();
                            return 1;
                        }

                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception("Transaction failed: " + ex.Message, ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or rethrow with more info
                throw new Exception("Error in VersionUpdate: " + ex.Message, ex);
            }
        }

        #endregion
    }

}
