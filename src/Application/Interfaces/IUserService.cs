namespace Application.Interfaces;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(UserCreateDto dto);
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<List<UserDto>> GetUsersAsync();
    Task UpdateUserAsync(Guid id, UserUpdateDto dto);
    Task DeleteUserAsync(Guid id);
    Task AssignRoleAsync(Guid userId, string roleName);
    Task RemoveRoleAsync(Guid userId, string roleName);
}
