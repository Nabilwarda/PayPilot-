using System.Text.Json.Serialization;

namespace PayPilot.Services;

public class SupabaseUser
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("email")] public string? Email { get; set; }
}

public class SupabaseSession
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = "";
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    [JsonPropertyName("token_type")] public string TokenType { get; set; } = "bearer";
    [JsonPropertyName("user")] public SupabaseUser User { get; set; } = new();

    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public bool IsExpired => DateTime.UtcNow > IssuedAtUtc.AddSeconds(Math.Max(0, ExpiresIn - 30));
}

public class BillRow
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("user_id")] public Guid UserId { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("recurrence")] public string Recurrence { get; set; } = "monthly";
    [JsonPropertyName("due_day")] public int DueDay { get; set; }
    [JsonPropertyName("start_date")] public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    [JsonPropertyName("is_active")] public bool IsActive { get; set; } = true;
    [JsonPropertyName("category")] public string? Category { get; set; }
    [JsonPropertyName("notes")] public string? Notes { get; set; }
}
