using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace PayPilot.Services;

public class SupabaseAuthService
{
    private readonly HttpClient _http;
    private readonly SupabaseAuthStateProvider _authState;
    private readonly string _url;
    private readonly string _anonKey;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public SupabaseAuthService(HttpClient http, IConfiguration config, SupabaseAuthStateProvider authState)
    {
        _http = http;
        _authState = authState;
        _url = config["Supabase:Url"] ?? throw new Exception("Missing Supabase:Url in wwwroot/appsettings.json");
        _anonKey = config["Supabase:AnonKey"] ?? throw new Exception("Missing Supabase:AnonKey in wwwroot/appsettings.json");
    }

    public async Task<(bool ok, string? error)> SignUpAsync(string email, string password)
    {
        var endpoint = $"{_url}/auth/v1/signup";
        var body = JsonSerializer.Serialize(new { email, password }, JsonOpts);

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Headers.Add("apikey", _anonKey);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req);
        if (!res.IsSuccessStatusCode)
            return (false, await res.Content.ReadAsStringAsync());

        return (true, null);
    }

    public async Task<(bool ok, string? error)> SignInAsync(string email, string password)
    {
        var endpoint = $"{_url}/auth/v1/token?grant_type=password";
        var body = JsonSerializer.Serialize(new { email, password }, JsonOpts);

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Headers.Add("apikey", _anonKey);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
            return (false, json);

        var session = JsonSerializer.Deserialize<SupabaseSession>(json, JsonOpts);
        if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
            return (false, "Invalid session response");

        session.IssuedAtUtc = DateTime.UtcNow;
        await _authState.SetSessionAsync(session);
        return (true, null);
    }

    public async Task SignOutAsync()
    {
        await _authState.ClearSessionAsync();
    }

    public async Task EnsureFreshTokenAsync()
    {
        var session = await _authState.GetSessionAsync();
        if (session is null || !session.IsExpired) return;

        var endpoint = $"{_url}/auth/v1/token?grant_type=refresh_token";
        var body = JsonSerializer.Serialize(new { refresh_token = session.RefreshToken }, JsonOpts);

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Headers.Add("apikey", _anonKey);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode) return;

        var newSession = JsonSerializer.Deserialize<SupabaseSession>(json, JsonOpts);
        if (newSession is null || string.IsNullOrWhiteSpace(newSession.AccessToken)) return;

        newSession.IssuedAtUtc = DateTime.UtcNow;
        await _authState.SetSessionAsync(newSession);
    }

    public void ApplyAuthHeaders(HttpRequestMessage req, string accessToken)
    {
        req.Headers.Add("apikey", _anonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
}
