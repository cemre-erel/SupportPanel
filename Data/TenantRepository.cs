using Microsoft.EntityFrameworkCore;
using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Data
{
    public class TenantRepository : ITenantRepository
    {
        private readonly AppDbContext _context;

        public TenantRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Tenant>> GetAllAsync()
        {
            return await _context.Tenants.ToListAsync();
        }

        public async Task<List<Tenant>> GetActiveAsync()
        {
            return await _context.Tenants
                .Where(t => t.IsActive)
                .ToListAsync();
        }

        public async Task<Tenant?> GetByIdAsync(int id)
        {
            return await _context.Tenants.FindAsync(id);
        }

        public async Task<bool> CompanyNameExistsAsync(string companyName, int? excludedTenantId = null)
        {
            var normalizedName = companyName.Trim().ToUpper();

            return await _context.Tenants.AnyAsync(t =>
                (!excludedTenantId.HasValue || t.Id != excludedTenantId.Value) &&
                t.CompanyName.Trim().ToUpper() == normalizedName);
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludedTenantId = null)
        {
            var normalizedEmail = email.Trim().ToUpper();

            return await _context.Tenants.AnyAsync(t =>
                (!excludedTenantId.HasValue || t.Id != excludedTenantId.Value) &&
                t.Email.Trim().ToUpper() == normalizedEmail);
        }

        public async Task<bool> PhoneExistsAsync(string phone, int? excludedTenantId = null)
        {
            var normalizedPhone = NormalizePhone(phone);
            var phones = await _context.Tenants
                .Where(t => !excludedTenantId.HasValue || t.Id != excludedTenantId.Value)
                .Select(t => t.Phone)
                .ToListAsync();

            return phones.Any(existingPhone => NormalizePhone(existingPhone) == normalizedPhone);
        }

        private static string NormalizePhone(string phone)
        {
            var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

            if (digits.StartsWith("90") && digits.Length == 12)
            {
                digits = digits[2..];
            }

            if (digits.StartsWith('0') && digits.Length == 11)
            {
                digits = digits[1..];
            }

            return digits;
        }

        public async Task AddAsync(Tenant tenant)
        {
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Tenant tenant)
        {
            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var tenant = await _context.Tenants.FindAsync(id);

            if (tenant != null)
            {
                tenant.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReactivateAsync(int id)
        {
            var tenant = await _context.Tenants.FindAsync(id);

            if (tenant != null)
            {
                tenant.IsActive = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
