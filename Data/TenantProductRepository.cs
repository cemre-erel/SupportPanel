using Microsoft.EntityFrameworkCore;
using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Data
{
    public class TenantProductRepository : ITenantProductRepository
    {
        private readonly AppDbContext _context;

        public TenantProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TenantProduct>> GetAllAsync()
        {
            return await _context.TenantProducts
                .Include(tp => tp.Tenant)
                .Include(tp => tp.Product)
                .ToListAsync();
        }

        public async Task<TenantProduct?> GetByIdAsync(int id)
        {
            return await _context.TenantProducts.FindAsync(id);
        }

        public async Task AddAsync(TenantProduct tenantProduct)
        {
            _context.TenantProducts.Add(tenantProduct);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TenantProduct tenantProduct)
        {
            _context.TenantProducts.Update(tenantProduct);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var tenantProduct = await _context.TenantProducts.FindAsync(id);

            if (tenantProduct != null)
            {
                _context.TenantProducts.Remove(tenantProduct);
                await _context.SaveChangesAsync();
            }
        }
    }
}