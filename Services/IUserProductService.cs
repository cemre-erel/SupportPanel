using SupportPanel.Models;

namespace SupportPanel.Services
{
    public interface IUserProductService
    {
        Task<List<UserProduct>> GetAllAsync();
        Task<UserProduct?> GetByIdAsync(int id);
        Task AddAsync(UserProduct userProduct);
        Task UpdateAsync(UserProduct userProduct);
        Task DeleteAsync(int id);
    }
}