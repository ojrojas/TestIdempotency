namespace Infrastructure.Services;
    // User operations implemented with ASP.NET Core Identity.
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UserService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<UserDto> CreateUserAsync(UserCreateDto dto)
        {
            var user = new ApplicationUser { UserName = dto.UserName, Email = dto.Email };
            var res = await _userManager.CreateAsync(user, dto.Password);
            if (!res.Succeeded) throw new ApplicationException(string.Join("; ", res.Errors.Select(e => e.Description)));
            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto { Id = user.Id, UserName = user.UserName ?? "", Email = user.Email ?? "", Roles = roles };
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return null;
            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto { Id = user.Id, UserName = user.UserName ?? "", Email = user.Email ?? "", Roles = roles };
        }

        public async Task<List<UserDto>> GetUsersAsync()
        {
            var users = _userManager.Users.ToList();
            var result = new List<UserDto>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                result.Add(new UserDto { Id = u.Id, UserName = u.UserName ?? "", Email = u.Email ?? "", Roles = roles });
            }
            return result;
        }

        public async Task UpdateUserAsync(Guid id, UserUpdateDto dto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) throw new KeyNotFoundException("User not found");
            if (!string.IsNullOrWhiteSpace(dto.UserName)) user.UserName = dto.UserName;
            if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;
            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded) throw new ApplicationException(string.Join("; ", res.Errors.Select(e => e.Description)));
        }

        public async Task DeleteUserAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return;
            await _userManager.DeleteAsync(user);
        }

        public async Task AssignRoleAsync(Guid userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) throw new KeyNotFoundException("User not found");
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole { Name = roleName, NormalizedName = roleName.ToUpperInvariant() };
                await _roleManager.CreateAsync(role);
            }
            await _userManager.AddToRoleAsync(user, roleName);
        }

        public async Task RemoveRoleAsync(Guid userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) throw new KeyNotFoundException("User not found");
            await _userManager.RemoveFromRoleAsync(user, roleName);
        }
    }