using Microsoft.EntityFrameworkCore;
using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Tenant)
                .Include(u => u.Role)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task AddAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            var existing = await _context.Users.FindAsync(user.Id);
            if (existing == null)
                return;
            _context.Entry(existing).CurrentValues.SetValues(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE Tickets SET CreatedByUserId = NULL WHERE CreatedByUserId = {0}", id);
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE Tickets SET AssignedUserId = NULL WHERE AssignedUserId = {0}", id);
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE TicketHistories SET UserId = NULL WHERE UserId = {0}", id);
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE Notifications SET UserId = NULL WHERE UserId = {0}", id);
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE TicketAttachments SET UploadedByUserId = NULL WHERE UploadedByUserId = {0}", id);
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            await tx.CommitAsync();
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            // Login sırasında kullanıcı henüz kimliklendirilmediği için
            // CurrentTenantId/IsSystemAdmin set edilmemiş olur; tenant query filter
            // burada devrede kalırsa hiçbir kullanıcı bulunamaz. Bu yüzden bilerek IgnoreQueryFilters.
            return await _context.Users
                .IgnoreQueryFilters()
                .Include(u => u.Role)
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .IgnoreQueryFilters()
                .Include(u => u.Role)
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Email == email.Trim());
        }
    }
}