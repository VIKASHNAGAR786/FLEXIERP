using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer_Interfaces;

namespace FLEXIERP.BusinessLayer
{
    public class VersionUpdateService : IVersionUpdateService
    {
        private readonly IVersionUpdate _versionUpdateRepository;
        public VersionUpdateService(IVersionUpdate versionUpdateRepository)
        {
            _versionUpdateRepository = versionUpdateRepository;
        }
        public async Task<int> VersionUpdate(string version)
        {
            return await _versionUpdateRepository.UpdateVersion(version);
        }
    }
}
