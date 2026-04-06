using Domain.Entities;

namespace Application.Interfaces
{
    public interface IIdempotencyService
    {
        Task<IdempotencyKey?> GetAsync(string key);
        Task<bool> TryCreateInProgressAsync(string key, string method, string path, string requestHash, TimeSpan ttl);
        Task SaveResponseAsync(string key, int status, string headersJson, string responseBody, DateTime expiresAt);
    }
}
