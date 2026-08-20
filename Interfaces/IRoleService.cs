using SupportPanel.Models;

namespace SupportPanel.Services
{
    public interface IRoleService
    {
        Task<List<Role>> GetAllAsync();

        Task<Role?> GetByIdAsync(int id);

        Task AddAsync(Role role);

        Task UpdateAsync(Role role);

        Task DeleteAsync(int id);
    }
}