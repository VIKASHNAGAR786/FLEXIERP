using FLEXIERP.MODELS.AGRIMANDI.Model;
using Microsoft.AspNetCore.Mvc;

namespace FLEXIERP.BusinesLayer_Interfaces
{
    public interface IAccountServices
    {
        public Task<User1?> Login(string? email,string? username, string password);
        public Task<User1> Register(User1 user);
        public Task<bool> LogoutUser(int userId);
        public Task<IEnumerable<UserLoginHistoryDTO>> GetUserLoginHistory(int pageNo = 1, int pageSize = 20);
        public Task<CompanyInfoDto?> GetCompanyInfoByUserAsync(int userId);
        public Task<int> UpdateCompanyInfo(UpdateCompanyInfo UpdateCompanyInfo, IFormFile? file);


    }
}
