using FLEXIERP.MODELS;

namespace FLEXIERP.BusinesLayer_Interfaces
{
    public interface IBackupService
    {
        public Task<int> BackupDatabaseAsync(BackupRequest request);
    }
}
