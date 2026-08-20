using SupportPanel.Data;
using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Services
{
    public class TenantService : ITenantService
    {
        private readonly ITenantRepository _repository;

        private readonly ISlaLevelService _slaLevelService;

        public TenantService(ITenantRepository repository, ISlaLevelService slaLevelService)
        {
            _repository = repository;
            _slaLevelService = slaLevelService;
        }

        public async Task<List<Tenant>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<List<Tenant>> GetActiveAsync()
        {
            return await _repository.GetActiveAsync();
        }

        public async Task<Tenant?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<bool> CompanyNameExistsAsync(string companyName, int? excludedTenantId = null)
        {
            return await _repository.CompanyNameExistsAsync(companyName, excludedTenantId);
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludedTenantId = null)
        {
            return await _repository.EmailExistsAsync(email, excludedTenantId);
        }

        public async Task<bool> PhoneExistsAsync(string phone, int? excludedTenantId = null)
        {
            return await _repository.PhoneExistsAsync(phone, excludedTenantId);
        }

        public async Task AddAsync(Tenant tenant)
        {
            await _repository.AddAsync(tenant);
            await _slaLevelService.EnsureDefaultLevelsAsync(tenant.Id);
        }

        public async Task UpdateAsync(Tenant tenant)
        {
            await _repository.UpdateAsync(tenant);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task ReactivateAsync(int id)
        {
            await _repository.ReactivateAsync(id);
        }
    }
}
