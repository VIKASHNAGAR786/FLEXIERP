using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.MODELS.AGRIMANDI.Model;

namespace FLEXIERP.BusinessLayer
{
    public class AccountService : IAccountServices
    {
        private readonly IAccountRepo accountRepo; 
        public AccountService(IAccountRepo _accountRepo)
        {
            accountRepo = _accountRepo;
        }
        public async Task<User1?> Login(string email, string password)
        {
            return await this.accountRepo.Login(email, password);
        }
        public Task<User1> Register(User1 user)
        {
            return this.accountRepo.Register(user);
        }
    }
}
