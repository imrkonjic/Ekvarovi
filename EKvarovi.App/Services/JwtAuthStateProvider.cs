using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace EKvarovi.App.Services;

public sealed class JwtAuthStateProvider(ILocalStorageService storage) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await storage.GetItemAsStringAsync("authToken");
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous();

        var identity = new ClaimsIdentity(ParseClaims(token), "jwt", "sub", ClaimTypes.Role);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task NotifyUserLoginAsync(string token)
    {
        await storage.SetItemAsStringAsync("authToken", token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task NotifyUserLogoutAsync()
    {
        await storage.RemoveItemAsync("authToken");
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static AuthenticationState Anonymous()
        => new(new ClaimsPrincipal(new ClaimsIdentity()));

    private static IEnumerable<Claim> ParseClaims(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            Convert.FromBase64String(PadBase64(payload)))!;

        foreach (var (key, value) in json)
        {
            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                    yield return new Claim(NormalizeClaimType(key), item.GetString()!);
            }
            else
            {
                yield return new Claim(NormalizeClaimType(key), value.ToString());
            }
        }
    }

    private static string NormalizeClaimType(string key)
        => key == "role" ? ClaimTypes.Role : key;

    private static string PadBase64(string base64)
    {
        var padded = base64.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return padded;
    }
}
