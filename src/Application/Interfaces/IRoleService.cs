using Application.DTOs;

namespace Application.Interfaces
{
    public interface IRoleService
    {
        Task<RoleDto> CreateRoleAsync(string roleName);
        Task<RoleDto?> GetRoleByIdAsync(Guid id);
        Task<List<RoleDto>> GetRolesAsync();
        Task AddClaimToRoleAsync(Guid roleId, string claimType, string claimValue);
        Task RemoveClaimFromRoleAsync(Guid roleId, string claimType, string claimValue);
    }
}
