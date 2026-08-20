using SupportPanel.Models;

namespace SupportPanel.Interfaces
{
    public interface IUserProductRepository
    {
        Task<List<UserProduct>> GetAllAsync();
        Task<UserProduct?> GetByIdAsync(int id);
        Task AddAsync(UserProduct userProduct);
        Task UpdateAsync(UserProduct userProduct);
        Task DeleteAsync(int id);
    }
}