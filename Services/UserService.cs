using Microsoft.AspNetCore.Identity;
using SupportPanel.Data;
using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly PasswordHasher<User> _hasher = new();

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task AddAsync(User user)
        {
            user.PasswordHash = _hasher.HashPassword(user, user.PasswordHash);
            await _repository.AddAsync(user);
        }

        /// <summary>
        /// Kullanıcıyı olduğu gibi kaydeder. <see cref="User.PasswordHash"/> alanının
        /// ÇAĞIRAN tarafından hash'lenmiş olarak verilmesi gerekir; burada hash'leme yapılmaz.
        /// Yeni bir şifre atamak için <see cref="HashPassword"/> kullanın.
        /// </summary>
        public async Task UpdateAsync(User user)
        {
            await _repository.UpdateAsync(user);
        }

        /// <summary>
        /// Düz metin şifreyi hash'ler. Hash'leme için tek giriş noktasıdır.
        /// </summary>
        public string HashPassword(User user, string plainPassword)
        {
            return _hasher.HashPassword(user, plainPassword);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _repository.GetByUsernameAsync(username);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _repository.GetByEmailAsync(email);
        }

        public async Task<bool> VerifyPasswordAsync(User user, string password)
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash ?? "", password);
            return result != PasswordVerificationResult.Failed;
        }
    }
}
