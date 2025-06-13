using FLEXIERP.MODELS.AGRIMANDI.Model;

namespace FLEXIERP.DataAccessLayer_Interfaces
{
    public interface IAccountRepo
    {
        public Task<User1?> Login(string email, string password);
        public Task<User1> Register(User1 user);
    }
}
