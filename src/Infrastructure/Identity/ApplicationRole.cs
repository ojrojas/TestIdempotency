using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    // Simple role type using GUID primary key.
    public class ApplicationRole : IdentityRole<Guid>
    {
    }
}
