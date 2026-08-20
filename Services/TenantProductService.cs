using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Services
{
    public class TenantProductService : ITenantProductService
    {
        private readonly ITenantProductRepository _repository;

        public TenantProductService(ITenantProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<TenantProduct>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<TenantProduct?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task AddAsync(TenantProduct tenantProduct)
        {
            await _repository.AddAsync(tenantProduct);
        }

        public async Task UpdateAsync(TenantProduct tenantProduct)
        {
            await _repository.UpdateAsync(tenantProduct);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}