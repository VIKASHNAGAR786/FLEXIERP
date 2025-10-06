using FLEXIERP.MODELS;
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


        #region Customer Ledger
        public Task<int> Savecustomerledger(Customerledgermodel customerLedger);
        public Task<IEnumerable<CustomerledgerDto?>> GetCustomerledger(int pageNo = 1, int pageSize = 20);
        public Task<IEnumerable<CustomerledgerdetailDto?>> GetCustomerledgerdetails(int customerid, string StartDate, string EndDate);
        public Task<byte[]> GetCustomerledgerdetailspdf(int customerid, string StartDate, string EndDate);
        #endregion
    }
}
