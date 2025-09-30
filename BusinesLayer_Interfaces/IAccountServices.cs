using FLEXIERP.MODELS.AGRIMANDI.Model;

namespace FLEXIERP.BusinesLayer_Interfaces
{
    public interface IAccountServices
    {
        public Task<User1?> Login(string? email,string? username, string password);
        public Task<User1> Register(User1 user);
        public Task<bool> LogoutUser(int userId);
    }
}
