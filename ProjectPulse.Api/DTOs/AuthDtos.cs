namespace ProjectPulse.Api.DTOs;

public record RegisterDto(string Email, string Password, string DisplayName);
public record MeDto(Guid Id, string Email, string DisplayName, string Role);
public record LoginDto(string Email, string Password);
public record TokenResponseDto(string AccessToken, string RefreshToken);
public record RefreshRequestDto(string RefreshToken);
