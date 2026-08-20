using SupportPanel.Models;

namespace SupportPanel.Interfaces
{
    public interface ISlaLevelService
    {
        Task<List<SlaLevel>> GetAllAsync();
        Task<List<SlaLevel>> GetByTenantAsync(int tenantId);
        Task<List<SlaLevel>> GetActiveByTenantAsync(int tenantId);
        Task<SlaLevel?> GetByIdAsync(int id);
        Task AddAsync(SlaLevel slaLevel);
        Task UpdateAsync(SlaLevel slaLevel);
        Task DeleteAsync(int id);
        Task EnsureDefaultLevelsAsync(int tenantId);
    }
}
