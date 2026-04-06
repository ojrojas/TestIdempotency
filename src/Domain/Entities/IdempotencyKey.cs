namespace Domain.Entities
{
    public enum IdempotencyState
    {
        InProgress = 0,
        Completed = 1
    }

    // Simple persistence model for idempotency records.
    // Stored as a single table in the Infrastructure database.
    public class IdempotencyKey
    {
        // Primary key: the Idempotency-Key header value (e.g., UUID)
        public string Key { get; set; } = null!;

        public string Method { get; set; } = null!;
        public string Path { get; set; } = null!;
        public string? RequestHash { get; set; }

        // Response fields populated after the operation completes
        public int? ResponseStatus { get; set; }
        public string? ResponseHeaders { get; set; } // JSON
        public string? ResponseBody { get; set; } // JSON or raw body

        public IdempotencyState State { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
