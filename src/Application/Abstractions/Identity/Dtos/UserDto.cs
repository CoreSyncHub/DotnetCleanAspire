namespace Application.Abstractions.Identity.Dtos;

public sealed record UserDto(
    Id Id,
    string Email,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    IReadOnlyList<string> Roles);
