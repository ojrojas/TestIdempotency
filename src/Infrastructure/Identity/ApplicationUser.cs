using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    // Derived IdentityUser with GUID PK and a CreatedAt timestamp.
    // Kept in Infrastructure to avoid coupling Domain to ASP.NET Identity.
    public class ApplicationUser : IdentityUser<Guid>
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
