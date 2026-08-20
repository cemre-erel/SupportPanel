using SupportPanel.Interfaces;
using SupportPanel.Models;

namespace SupportPanel.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _repository;

        public RoleService(IRoleRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Role>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Role?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task AddAsync(Role role)
        {
            await _repository.AddAsync(role);
        }

        public async Task UpdateAsync(Role role)
        {
            await _repository.UpdateAsync(role);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}