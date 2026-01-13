using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace PayPilot.Services;

public class SupabaseAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _storage;
    private const string StorageKey = "pp_session";

    public SupabaseAuthStateProvider(ILocalStorageService storage)
    {
        _storage = storage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await _storage.GetItemAsync<SupabaseSession>(StorageKey);
        if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.User.Id),
            new(ClaimTypes.Email, session.User.Email ?? "")
        };

        var identity = new ClaimsIdentity(claims, "Supabase");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task SetSessionAsync(SupabaseSession session)
    {
        await _storage.SetItemAsync(StorageKey, session);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task ClearSessionAsync()
    {
        await _storage.RemoveItemAsync(StorageKey);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task<SupabaseSession?> GetSessionAsync()
        => await _storage.GetItemAsync<SupabaseSession>(StorageKey);
}
