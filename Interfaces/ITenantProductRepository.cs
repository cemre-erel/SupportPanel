using SupportPanel.Models;

namespace SupportPanel.Interfaces
{
    public interface ITenantProductRepository
    {
        Task<List<TenantProduct>> GetAllAsync();
        Task<TenantProduct?> GetByIdAsync(int id);
        Task AddAsync(TenantProduct tenantProduct);
        Task UpdateAsync(TenantProduct tenantProduct);
        Task DeleteAsync(int id);
    }
}