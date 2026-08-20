using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Services
{
    public class SlaLevelService : ISlaLevelService
    {
        private readonly ISlaLevelRepository _repository;

        public SlaLevelService(ISlaLevelRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<SlaLevel>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<List<SlaLevel>> GetByTenantAsync(int tenantId)
        {
            return await _repository.GetByTenantAsync(tenantId);
        }

        public async Task<List<SlaLevel>> GetActiveByTenantAsync(int tenantId)
        {
            return await _repository.GetActiveByTenantAsync(tenantId);
        }

        public async Task<SlaLevel?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task AddAsync(SlaLevel slaLevel)
        {
            await _repository.AddAsync(slaLevel);
        }

        public async Task UpdateAsync(SlaLevel slaLevel)
        {
            await _repository.UpdateAsync(slaLevel);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task EnsureDefaultLevelsAsync(int tenantId)
        {
            var existing = await _repository.GetByTenantAsync(tenantId);

            if (existing.Count > 0)
            {
                return;
            }

            var defaults = new (string Name, int Response, int Resolution)[]
            {
                ("Standart", 480, 2880),
                ("Yüksek", 240, 1440),
                ("Kritik", 120, 480)
            };

            foreach (var d in defaults)
            {
                await _repository.AddAsync(new SlaLevel
                {
                    TenantId = tenantId,
                    Name = d.Name,
                    IsActive = true,
                    ResponseTargetMinutes = d.Response,
                    ResolutionTargetMinutes = d.Resolution
                });
            }
        }
    }
}
