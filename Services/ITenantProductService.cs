using SupportPanel.Models;

namespace SupportPanel.Services
{
    public interface ITenantProductService
    {
        Task<List<TenantProduct>> GetAllAsync();
        Task<TenantProduct?> GetByIdAsync(int id);
        Task AddAsync(TenantProduct tenantProduct);
        Task UpdateAsync(TenantProduct tenantProduct);
        Task DeleteAsync(int id);
    }
}