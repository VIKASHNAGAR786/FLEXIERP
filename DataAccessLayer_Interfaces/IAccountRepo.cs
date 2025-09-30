using FLEXIERP.MODELS.AGRIMANDI.Model;

namespace FLEXIERP.DataAccessLayer_Interfaces
{
    public interface IAccountRepo
    {
        public Task<User1?> Login(string? email, string? UserName, string password);
        public Task<User1> Register(User1 user);
        public Task<bool> StartUserSession(StartUserSession loginModel);
        public Task<bool> LogoutUser(int userId);

        public Task<IEnumerable<UserLoginHistoryDTO>> GetUserLoginHistory(int pageNo = 1, int pageSize = 20);
    }
}
