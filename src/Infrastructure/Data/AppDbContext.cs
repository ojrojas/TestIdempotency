namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Use the idempotency key (the header value) as the PK.
        builder.Entity<IdempotencyKey>().HasKey(k => k.Key);

        // Keep timestamps explicit for portability between providers.
        builder.Entity<IdempotencyKey>().Property(k => k.CreatedAt).IsRequired();
        builder.Entity<IdempotencyKey>().Property(k => k.ExpiresAt).IsRequired();
    }
}