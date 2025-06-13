using Microsoft.Data.SqlClient;

namespace FLEXIERP.DATABASE
{
    public interface IDataBaseOperation : IDisposable
    {
        public SqlConnection GetConnection();
    }
}
