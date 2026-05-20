namespace Api.Middleware;
// Middleware that implements idempotency semantics for mutating HTTP methods.
// Uses IIdempotencyService (persisted via EF Core) to store/replay responses.
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IIdempotencyService _store;
    private readonly TimeSpan _ttl;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(
        RequestDelegate next, 
        ILogger<IdempotencyMiddleware> logger, 
        IIdempotencyService store,
        IConfiguration configuration )
    {
        _next = next;
        _store = store;
        _logger = logger;

        var timeSpanHours = configuration.GetValue("Idempotency:TTLHours", 24);

        _ttl = TimeSpan.FromHours(timeSpanHours);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsMutatingMethod(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var keyValues))
        {
            // No idempotency header provided; proceed normally.
            await _next(context);
            return;
        }

        var key = keyValues.FirstOrDefault();
        // read and hash request body to detect conflicting bodies for same key.
        context.Request.EnableBuffering();
        string body;
        using (var sr = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            body = await sr.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }
        var hash = ComputeHash(body);

        // If a completed response exists, replay it.
        var existing = await _store.GetAsync(key);
        if (existing != null && existing.State == IdempotencyState.Completed)
        {
            // Optional: validate request hash matches; if not, return 409.
            if (existing.RequestHash != null && existing.RequestHash != hash)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsync("Idempotency-Key reused with different payload");
                return;
            }

            // Replay stored response (status, headers, body)
            if (existing.ResponseHeaders != null)
            {
                var headers = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string[]>>(existing.ResponseHeaders)!;
                foreach (var h in headers)
                {
                    context.Response.Headers[h.Key] = h.Value;
                }
            }
            context.Response.StatusCode = existing.ResponseStatus ?? StatusCodes.Status200OK;
            if (!string.IsNullOrEmpty(existing.ResponseBody))
            {
                await context.Response.WriteAsync(existing.ResponseBody);
            }
            return;
        }

        if (existing != null && existing.State == IdempotencyState.InProgress)
        {
            // Another request is processing with this key.
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsync("Request with this Idempotency-Key is already in progress");
            return;
        }

        // Try to create a new in-progress entry; if fails, someone else beat us - fetch again and behave accordingly.
        var created = await _store.TryCreateInProgressAsync(key, context.Request.Method, context.Request.Path, hash, _ttl);
        if (!created)
        {
            var again = await _store.GetAsync(key);
            if (again != null && again.State == IdempotencyState.Completed)
            {
                if (again.ResponseHeaders != null)
                {
                    var headers = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string[]>>(again.ResponseHeaders)!;
                    foreach (var h in headers) context.Response.Headers[h.Key] = h.Value;
                }
                context.Response.StatusCode = again.ResponseStatus ?? StatusCodes.Status200OK;
                if (!string.IsNullOrEmpty(again.ResponseBody)) await context.Response.WriteAsync(again.ResponseBody);
                return;
            }
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsync("Request with this Idempotency-Key is already in progress");
            return;
        }

        // Capture the response
        var originalBody = context.Response.Body;
        await using var ms = new MemoryStream();
        context.Response.Body = ms;

        await _next(context);

        // Read response
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        // Copy back to original stream
        await ms.CopyToAsync(originalBody);
        context.Response.Body = originalBody;

        // Serialize headers
        var headersDict = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray());
        var headersJson = JsonSerializer.Serialize(headersDict);

        // Save completed response for replay
        var expiresAt = DateTime.UtcNow.Add(_ttl);
        await _store.SaveResponseAsync(key, context.Response.StatusCode, headersJson, responseText, expiresAt);
    }

    private static bool IsMutatingMethod(string method) =>
        string.Equals(method, HttpMethods.Post, StringComparison.OrdinalIgnoreCase)
        || string.Equals(method, HttpMethods.Put, StringComparison.OrdinalIgnoreCase)
        || string.Equals(method, HttpMethods.Delete, StringComparison.OrdinalIgnoreCase);

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input ?? ""));
        return Convert.ToHexString(bytes);
    }
}