namespace Application.Abstractions.Identity.Dtos;

public sealed record AuthTokensDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);
