using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.MODELS;
using Microsoft.AspNetCore.Mvc;

namespace FLEXIERP.BusinessLayer
{
    public class BackupService : IBackupService
    {
        public async Task<int> BackupDatabaseAsync(BackupRequest request)
        {
            try
            {
                // 0 = invalid input
                if (string.IsNullOrWhiteSpace(request.BackupFolderPath))
                    return 0;

                // Ensure folder exists
                if (!Directory.Exists(request.BackupFolderPath))
                    Directory.CreateDirectory(request.BackupFolderPath);

                // ✅ Get app base directory (where .exe is running)
                string appBaseDir = AppContext.BaseDirectory;

                // ✅ Build relative path to the database
                string dbPath = Path.Combine(appBaseDir, "app_data", "mydatabase.db");

                if (!File.Exists(dbPath))
                    return -1; // Database file not found

                // ✅ Create backup filename
                string backupFile = Path.Combine(request.BackupFolderPath, $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

                // ✅ Copy DB file (async-safe)
                await Task.Run(() => File.Copy(dbPath, backupFile, true));

                return 1; // Success
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Backup failed: {ex.Message}");
                return -2; // Error
            }
        }

    }

}
