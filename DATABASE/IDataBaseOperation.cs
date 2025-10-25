using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace FLEXIERP.DATABASE
{
    public interface IDataBaseOperation : IDisposable
    {
        public SqliteConnection GetConnection();
        public void OpenConnection();
        public void CloseConnection();
    }
}
