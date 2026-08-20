using Microsoft.EntityFrameworkCore;
using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Data
{
    public class UserProductRepository : IUserProductRepository
    {
        private readonly AppDbContext _context;

        public UserProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserProduct>> GetAllAsync()
        {
            return await _context.UserProducts
                .Include(up => up.User)
                .Include(up => up.Product)
                .ToListAsync();
        }

        public async Task<UserProduct?> GetByIdAsync(int id)
        {
            return await _context.UserProducts.FindAsync(id);
        }

        public async Task AddAsync(UserProduct userProduct)
        {
            _context.UserProducts.Add(userProduct);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserProduct userProduct)
        {
            _context.UserProducts.Update(userProduct);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var userProduct = await _context.UserProducts.FindAsync(id);

            if (userProduct != null)
            {
                _context.UserProducts.Remove(userProduct);
                await _context.SaveChangesAsync();
            }
        }
    }
}