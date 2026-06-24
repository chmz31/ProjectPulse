using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectPulse.Api.DTOs;
using ProjectPulse.Api.Persistence;
using Xunit;

namespace ProjectPulse.Api.Tests;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class RefreshTokenTests
{
    private readonly CustomWebApplicationFactory _factory;

    public RefreshTokenTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RefreshTokenIsHashedRotatedAndCannotBeReused()
    {
        using var client = _factory.CreateClient();
        var tokens = await RegisterAndLoginAsync(client);
        var tokenHash = Hash(tokens.RefreshToken);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var storedHash = await db.RefreshTokens
                .Where(r => r.TokenHash == tokenHash)
                .Select(r => r.TokenHash)
                .SingleAsync();

            Assert.Equal(tokenHash, storedHash);
            Assert.NotEqual(tokens.RefreshToken, storedHash);
        }

        var refreshResponse = await client.PostAsJsonAsync(
            "/auth/refresh", new RefreshRequestDto(tokens.RefreshToken));
        refreshResponse.EnsureSuccessStatusCode();
        var rotatedTokens = await refreshResponse.Content.ReadFromJsonAsync<TokenResponseDto>()
                            ?? throw new InvalidOperationException("Refresh response did not contain tokens.");

        Assert.NotEqual(tokens.RefreshToken, rotatedTokens.RefreshToken);

        var reuseResponse = await client.PostAsJsonAsync(
            "/auth/refresh", new RefreshRequestDto(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        var rotatedTokenResponse = await client.PostAsJsonAsync(
            "/auth/refresh", new RefreshRequestDto(rotatedTokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, rotatedTokenResponse.StatusCode);
    }

    [Fact]
    public async Task ExpiredRefreshTokenIsRejected()
    {
        using var client = _factory.CreateClient();
        var tokens = await RegisterAndLoginAsync(client);
        var tokenHash = Hash(tokens.RefreshToken);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var token = await db.RefreshTokens.SingleAsync(r => r.TokenHash == tokenHash);
            token.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync(
            "/auth/refresh", new RefreshRequestDto(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<TokenResponseDto> RegisterAndLoginAsync(HttpClient client)
    {
        var id = Guid.NewGuid().ToString("N");
        var email = $"refresh-{id}@example.test";
        const string password = "TestPass!123";

        var registerResponse = await client.PostAsJsonAsync(
            "/auth/register", new RegisterDto(email, password, $"User {id}"));
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginDto(email, password));
        loginResponse.EnsureSuccessStatusCode();

        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>()
                     ?? throw new InvalidOperationException("Login response did not contain tokens.");
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        return tokens;
    }

    private static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
