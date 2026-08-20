using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Services
{
    public class UserProductService : IUserProductService
    {
        private readonly IUserProductRepository _repository;

        public UserProductService(IUserProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<UserProduct>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<UserProduct?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task AddAsync(UserProduct userProduct)
        {
            await _repository.AddAsync(userProduct);
        }

        public async Task UpdateAsync(UserProduct userProduct)
        {
            await _repository.UpdateAsync(userProduct);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}