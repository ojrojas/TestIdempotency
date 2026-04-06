using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Infrastructure.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;

        public RoleService(RoleManager<ApplicationRole> roleManager) => _roleManager = roleManager;

        public async Task<RoleDto> CreateRoleAsync(string roleName)
        {
            var role = new ApplicationRole { Name = roleName, NormalizedName = roleName.ToUpperInvariant() };
            var res = await _roleManager.CreateAsync(role);
            if (!res.Succeeded) throw new ApplicationException(string.Join("; ", res.Errors.Select(e => e.Description)));
            return new RoleDto { Id = role.Id, Name = role.Name ?? "", Claims = new List<RoleClaimDto>() };
        }

        public async Task<RoleDto?> GetRoleByIdAsync(Guid id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return null;
            var claims = await _roleManager.GetClaimsAsync(role);
            return new RoleDto { Id = role.Id, Name = role.Name ?? "", Claims = claims.Select(c => new RoleClaimDto { ClaimType = c.Type, ClaimValue = c.Value }).ToList() };
        }

        public async Task<List<RoleDto>> GetRolesAsync()
        {
            var roles = _roleManager.Roles.ToList();
            var result = new List<RoleDto>();
            foreach (var r in roles)
            {
                var claims = await _roleManager.GetClaimsAsync(r);
                result.Add(new RoleDto { Id = r.Id, Name = r.Name ?? "", Claims = claims.Select(c => new RoleClaimDto { ClaimType = c.Type, ClaimValue = c.Value }).ToList() });
            }
            return result;
        }

        public async Task AddClaimToRoleAsync(Guid roleId, string claimType, string claimValue)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null) throw new KeyNotFoundException("Role not found");
            await _roleManager.AddClaimAsync(role, new Claim(claimType, claimValue));
        }

        public async Task RemoveClaimFromRoleAsync(Guid roleId, string claimType, string claimValue)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null) throw new KeyNotFoundException("Role not found");
            var claims = await _roleManager.GetClaimsAsync(role);
            var target = claims.FirstOrDefault(c => c.Type == claimType && c.Value == claimValue);
            if (target != null) await _roleManager.RemoveClaimAsync(role, target);
        }
    }
}
