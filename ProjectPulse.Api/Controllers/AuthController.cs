using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ProjectPulse.Api.DTOs;
using ProjectPulse.Api.Domain;
using ProjectPulse.Api.Persistence;
using ProjectPulse.Api.Security;
using ProjectPulse.Api.Services;

namespace ProjectPulse.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;

    public AuthController(AppDbContext db, IPasswordHasher hasher, IJwtTokenService tokens)
    {
        _db = db; _hasher = hasher; _tokens = tokens;
    }

    // POST /auth/register
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("register")]
    public async Task<ActionResult<MeDto>> Register([FromBody] RegisterDto dto)
    {
        // 1) email único
        var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
        if (exists) return Conflict(new { message = "Email already in use." });

        // 2) crear usuario con contraseña hasheada
        var user = new User
        {
            Email = dto.Email.Trim().ToLowerInvariant(),
            DisplayName = dto.DisplayName,
            PasswordHash = _hasher.Hash(dto.Password),
            Role = GlobalRole.Member
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // 3) devolvemos datos públicos del usuario creado
        return CreatedAtAction(nameof(Register), new { id = user.Id },
            new MeDto(user.Id, user.Email, user.DisplayName, user.Role.ToString()));
    }

    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null) return Unauthorized();
        if (!_hasher.Verify(dto.Password, user.PasswordHash)) return Unauthorized();

        var access = _tokens.CreateAccessToken(user);
        var refresh = _tokens.CreateRefreshToken();

        // INSERT explícito (evita el estado Modified por navegación)
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokens.HashRefreshToken(refresh),
            ExpiresAt = _tokens.GetRefreshExpiry()
        });

        await _db.SaveChangesAsync();

        return new TokenResponseDto(access, refresh);
    }

    // renueva access y rota el refresh token
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] RefreshRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken)) return Unauthorized();

        var tokenHash = _tokens.HashRefreshToken(dto.RefreshToken);
        var token = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);

        if (token is null) return Unauthorized();

        var now = DateTime.UtcNow;
        if (token.RevokedAt is not null)
        {
            await RevokeActiveRefreshTokensAsync(token.UserId, now);
            return Unauthorized();
        }

        if (token.ExpiresAt <= now) return Unauthorized();

        var rotated = await _db.RefreshTokens
            .Where(r => r.Id == token.Id && r.RevokedAt == null && r.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.RevokedAt, now));

        if (rotated != 1)
        {
            await RevokeActiveRefreshTokensAsync(token.UserId, now);
            return Unauthorized();
        }

        var user = token.User;
        var access = _tokens.CreateAccessToken(user);
        var newRefresh = _tokens.CreateRefreshToken();

        // nuevo refresh: INSERT explícito
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokens.HashRefreshToken(newRefresh),
            ExpiresAt = _tokens.GetRefreshExpiry()
        });

        await _db.SaveChangesAsync();

        return new TokenResponseDto(access, newRefresh);
    }
    // logout: revoca un refresh token concreto
    [HttpPost("logout")]
public async Task<IActionResult> Logout([FromBody] RefreshRequestDto dto)
{
    if (string.IsNullOrWhiteSpace(dto.RefreshToken)) return NoContent();

    var tokenHash = _tokens.HashRefreshToken(dto.RefreshToken);
    var token = await _db.RefreshTokens
        .FirstOrDefaultAsync(r => r.TokenHash == tokenHash && r.RevokedAt == null);

    if (token is null) return NoContent();

    token.RevokedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();

    return NoContent();
}

private Task RevokeActiveRefreshTokensAsync(Guid userId, DateTime revokedAt)
{
    return _db.RefreshTokens
        .Where(r => r.UserId == userId && r.RevokedAt == null)
        .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.RevokedAt, revokedAt));
}
}
