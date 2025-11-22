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
                            if (version == "1.0.1")
                            {
                                using var cmd = connection.CreateCommand();
                                cmd.Transaction = transaction;

                                // -----------------------------------------------
                                // 1️⃣ CREATE TABLE IF NOT EXISTS
                                // -----------------------------------------------
                                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS InvoiceNumberSystem (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    prefix TEXT NULL,
    last_invoice_number INTEGER NOT NULL,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_by INTEGER NULL
);
";
                                await cmd.ExecuteNonQueryAsync();

                                // -----------------------------------------------
                                // 2️⃣ INSERT FIRST ROW ONLY IF NO RECORD EXISTS
                                // -----------------------------------------------
                                cmd.CommandText = @"
INSERT INTO InvoiceNumberSystem (prefix, last_invoice_number)
SELECT '2025/', 1
WHERE NOT EXISTS (SELECT 1 FROM InvoiceNumberSystem LIMIT 1);
";
                                await cmd.ExecuteNonQueryAsync();

                                // -----------------------------------------------
                                // 3️⃣ ADD COLUMN invoice_no TO Sale TABLE IF NOT EXISTS
                                // -----------------------------------------------
                                // SQLite does not support IF NOT EXISTS for columns, so we manually check
                                cmd.CommandText = @"
SELECT COUNT(*) 
FROM pragma_table_info('Sale') 
WHERE name = 'invoice_no';
";

                                var exists = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                                if (exists == 0)
                                {
                                    cmd.CommandText = @"
ALTER TABLE Sale 
ADD COLUMN invoice_no TEXT;
";
                                    await cmd.ExecuteNonQueryAsync();
                                }

                                // Migration 1.0.1 done safely
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
