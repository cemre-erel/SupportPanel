using SupportPanel.Models;

namespace SupportPanel.Interfaces
{
    public interface ITenantRepository
    {
        Task<List<Tenant>> GetAllAsync();
        Task<List<Tenant>> GetActiveAsync();
        Task<Tenant?> GetByIdAsync(int id);
        Task<bool> CompanyNameExistsAsync(string companyName, int? excludedTenantId = null);
        Task<bool> EmailExistsAsync(string email, int? excludedTenantId = null);
        Task<bool> PhoneExistsAsync(string phone, int? excludedTenantId = null);
        Task AddAsync(Tenant tenant);
        Task UpdateAsync(Tenant tenant);
        Task DeleteAsync(int id);
        Task ReactivateAsync(int id);
    }
}
