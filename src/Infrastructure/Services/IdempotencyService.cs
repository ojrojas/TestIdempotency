namespace Infrastructure.Services;
// Infrastructure implementation of the idempotency store using EF Core.
// This keeps storage concerns out of the API pipeline and makes the middleware testable.
public class IdempotencyService : IIdempotencyService
{
    private readonly AppDbContext _db;
    public IdempotencyService(AppDbContext db) => _db = db;

    public async Task<IdempotencyKey?> GetAsync(string key)
    {
        var entry = await _db.IdempotencyKeys.FirstOrDefaultAsync(e => e.Key == key);
        if (entry == null) return null;
        if (entry.ExpiresAt < DateTime.UtcNow) return null;
        return entry;
    }

    public async Task<bool> TryCreateInProgressAsync(string key, string method, string path, string requestHash, TimeSpan ttl)
    {
        var now = DateTime.UtcNow;
        var entity = new IdempotencyKey
        {
            Key = key,
            Method = method,
            Path = path,
            RequestHash = requestHash,
            State = IdempotencyState.InProgress,
            CreatedAt = now,
            ExpiresAt = now.Add(ttl)
        };
        _db.IdempotencyKeys.Add(entity);
        try
        {
            await _db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // Likely a unique-constraint violation created by a concurrent request.
            return false;
        }
    }

    public async Task SaveResponseAsync(string key, int status, string headersJson, string responseBody, DateTime expiresAt)
    {
        var entry = await _db.IdempotencyKeys.FirstOrDefaultAsync(e => e.Key == key);
        if (entry == null) return;
        entry.ResponseStatus = status;
        entry.ResponseHeaders = headersJson;
        entry.ResponseBody = responseBody;
        entry.State = IdempotencyState.Completed;
        entry.ExpiresAt = expiresAt;
        await _db.SaveChangesAsync();
    }
}