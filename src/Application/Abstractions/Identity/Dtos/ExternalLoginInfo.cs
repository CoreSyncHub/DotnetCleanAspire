namespace Application.Abstractions.Identity.Dtos;

public sealed record ExternalLoginInfo(
    string Provider,
    string ProviderKey,
    string? Email,
    string? Name,
    IReadOnlyList<string> Groups);
