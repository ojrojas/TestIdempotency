namespace Application.DTOs;

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public List<RoleClaimDto> Claims { get; set; } = new();
}
