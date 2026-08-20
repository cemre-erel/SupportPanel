using Microsoft.EntityFrameworkCore;
using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Data
{
    public class SlaLevelRepository : ISlaLevelRepository
    {
        private readonly AppDbContext _context;

        public SlaLevelRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SlaLevel>> GetAllAsync()
        {
            return await _context.SlaLevels
                .Include(s => s.Tenant)
                .ToListAsync();
        }

        public async Task<List<SlaLevel>> GetByTenantAsync(int tenantId)
        {
            return await _context.SlaLevels
                .Where(s => s.TenantId == tenantId)
                .ToListAsync();
        }

        public async Task<List<SlaLevel>> GetActiveByTenantAsync(int tenantId)
        {
            return await _context.SlaLevels
                .Where(s => s.TenantId == tenantId && s.IsActive)
                .ToListAsync();
        }

        public async Task<SlaLevel?> GetByIdAsync(int id)
        {
            return await _context.SlaLevels.FindAsync(id);
        }

        public async Task AddAsync(SlaLevel slaLevel)
        {
            _context.SlaLevels.Add(slaLevel);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SlaLevel slaLevel)
        {
            _context.SlaLevels.Update(slaLevel);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var slaLevel = await _context.SlaLevels.FindAsync(id);

            if (slaLevel != null)
            {
                _context.SlaLevels.Remove(slaLevel);
                await _context.SaveChangesAsync();
            }
        }
    }
}
