using System;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;

namespace FLEXIERP.DATABASE
{
    public class DataBaseOperation : IDataBaseOperation, IDisposable
    {
        //private SqlConnection Connection { get; set; }

        //public DataBaseOperation(IConfiguration configuration)
        //{
        //    // 1️⃣ Get the encrypted connection string from config
        //    string? encryptedConn = configuration.GetConnectionString("DefaultConnectionEncrypted");
        //    if (string.IsNullOrWhiteSpace(encryptedConn))
        //    {
        //        throw new ArgumentNullException(nameof(encryptedConn), "Encrypted DB connection string is not configured.");
        //    }

        //    // 2️⃣ Get AES key from environment variable
        //    string? key = Environment.GetEnvironmentVariable("FLEXIERP_AES_KEY");
        //    if (string.IsNullOrWhiteSpace(key))
        //    {
        //        throw new ArgumentNullException(nameof(key), "AES key environment variable is not set.");
        //    }

        //    // 3️⃣ Decrypt connection string at runtime
        //    string decryptedConn = Decrypt(encryptedConn, key);

        //    // 4️⃣ Initialize SQL connection
        //    Connection = new SqlConnection(decryptedConn);
        //}

        //public void Dispose()
        //{
        //    Connection.Dispose();
        //}

        //public SqlConnection GetConnection() => Connection;

        private SqliteConnection Connection { get; set; }

        public DataBaseOperation(IConfiguration configuration)
        {
            // 1️⃣ Get the encrypted connection string from config
            string? encryptedConn = configuration.GetConnectionString("DefaultConnectionEncrypted");
            if (string.IsNullOrWhiteSpace(encryptedConn))
            {
                throw new ArgumentNullException(nameof(encryptedConn), "Encrypted DB connection string is not configured.");
            }

            // 2️⃣ Get AES key from environment variable
            string? key = Environment.GetEnvironmentVariable("FLEXIERP_AES_KEY");
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key), "AES key environment variable is not set.");
            }

            // 3️⃣ Decrypt connection string at runtime
            string decryptedConn = encryptedConn; //Decrypt(encryptedConn, key);    

            // 4️⃣ Initialize SQLite connection
            Connection = new SqliteConnection(decryptedConn);
        }

        public void OpenConnection()
        {
            if (Connection.State != System.Data.ConnectionState.Open)
                Connection.Open();
        }

        public void CloseConnection()
        {
            if (Connection.State != System.Data.ConnectionState.Closed)
                Connection.Close();
        }

        public SqliteConnection GetConnection()
        {
            return Connection;
        }

        public void Dispose()
        {
            Connection.Dispose();
        }

        #region AES Encrypt/Decrypt Methods

        public static string Encrypt(string plainText, string key)
        {
            using var aes = Aes.Create();
            var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32)); // AES-256
            aes.Key = keyBytes;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = Convert.ToBase64String(aes.IV.Concat(encryptedBytes).ToArray());
            return result;
        }

        public static string Decrypt(string encryptedText, string key)
        {
            var fullCipher = Convert.FromBase64String(encryptedText);
            using var aes = Aes.Create();
            var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32)); // AES-256
            aes.Key = keyBytes;

            var iv = fullCipher.Take(16).ToArray();
            var cipher = fullCipher.Skip(16).ToArray();
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        #endregion
    }
}
