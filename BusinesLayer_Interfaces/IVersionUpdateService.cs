using Microsoft.AspNetCore.Mvc;

namespace FLEXIERP.BusinesLayer_Interfaces
{
    public interface IVersionUpdateService
    {
        public Task<int> VersionUpdate(string version);
    }
}
