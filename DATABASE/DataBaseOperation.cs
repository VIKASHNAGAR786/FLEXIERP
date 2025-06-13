using Microsoft.Data.SqlClient;

namespace FLEXIERP.DATABASE
{
    public class DataBaseOperation : IDataBaseOperation, IDisposable
    {
        private SqlConnection Connection { get; set; }

        public DataBaseOperation(IConfiguration configuration)
        {
            // Get connection string from configuration (which loads from user secrets, environment, appsettings, etc.)
            string? connectionString = configuration.GetConnectionString("DefaultConnection");

            // Optional: Check null or throw exception
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Database connection string is not configured.");
            }

            Connection = new SqlConnection(connectionString);
        }

        public void Dispose()
        {
            Connection.Dispose();
        }

        public SqlConnection GetConnection() => Connection;
    }
}
