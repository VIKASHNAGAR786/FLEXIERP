namespace FLEXIERP.DataAccessLayer_Interfaces
{
    public interface IVersionUpdate
    {
        public Task<int> UpdateVersion(string version);
    }
}
