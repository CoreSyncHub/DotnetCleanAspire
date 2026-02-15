using Application.Abstractions.Identity;
using Application.Abstractions.Identity.Dtos;
using Application.DependencyInjection.Options;
using Application.Features.Auth.Commands.ExchangeCode;
using Application.Features.Auth.Commands.ForgotPassword;
using Application.Features.Auth.Commands.Login;
using Application.Features.Auth.Commands.Logout;
using Application.Features.Auth.Commands.OidcCallback;
using Application.Features.Auth.Commands.RefreshToken;
using Application.Features.Auth.Commands.Register;
using Application.Features.Auth.Commands.ResetPassword;
using Application.Features.Auth.Errors;
using Application.Features.Auth.Queries.GetCurrentUser;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Presentation.Abstractions;
using Presentation.Extensions;
using System.Security.Claims;
using HttpResult = Microsoft.AspNetCore.Http.IResult;

namespace Presentation.Endpoints;

internal sealed class AuthEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app, ApiVersionSet versions)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/v{version:apiVersion}/auth")
            .WithApiVersionSet(versions)
            .WithTags("Authentication");

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Register a new user")
            .WithDescription("Creates a new user account with email and password.")
            .Produces<ApiResponse<AuthTokensDto>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRateLimiting("strict")
            .MapToApiVersion(1, 0);

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Login with email and password")
            .WithDescription("Authenticates a user and returns access and refresh tokens.")
            .Produces<ApiResponse<AuthTokensDto>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireRateLimiting("strict")
            .MapToApiVersion(1, 0);

        group.MapPost("/refresh", RefreshToken)
            .WithName("RefreshToken")
            .WithSummary("Refresh access token")
            .WithDescription("Generates a new access token using a valid refresh token.")
            .Produces<ApiResponse<AuthTokensDto>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .MapToApiVersion(1, 0);

        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithSummary("Logout and revoke refresh token")
            .WithDescription("Revokes the current refresh token or all user tokens.")
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization()
            .MapToApiVersion(1, 0);

        group.MapPost("/forgot-password", ForgotPassword)
            .WithName("ForgotPassword")
            .WithSummary("Request password reset")
            .WithDescription("Sends a password reset email if the account exists.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .RequireRateLimiting("strict")
            .MapToApiVersion(1, 0);

        group.MapPost("/reset-password", ResetPassword)
            .WithName("ResetPassword")
            .WithSummary("Reset password with token")
            .WithDescription("Resets the user's password using a valid reset token.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireRateLimiting("strict")
            .MapToApiVersion(1, 0);

        group.MapGet("/oidc/login", OidcLogin)
            .WithName("OidcLogin")
            .WithSummary("Initiate OIDC login")
            .WithDescription("Redirects to the configured OIDC provider for authentication.")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .MapToApiVersion(1, 0);

        group.MapGet("/oidc/callback", OidcCallback)
            .WithName("OidcCallback")
            .WithSummary("OIDC callback handler")
            .WithDescription("Handles the callback from the OIDC provider and issues tokens.")
            .Produces<ApiResponse<AuthTokensDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ExcludeFromDescription()
            .MapToApiVersion(1, 0);

        group.MapPost("/exchange", ExchangeCode)
            .WithName("ExchangeCode")
            .WithSummary("Exchange authorization code for tokens")
            .WithDescription("Exchanges a temporary authorization code for access and refresh tokens.")
            .Produces<ApiResponse<AuthTokensDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .MapToApiVersion(1, 0);

        group.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Get current user information")
            .WithDescription("Returns information about the currently authenticated user.")
            .Produces<ApiResponse<UserDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization()
            .MapToApiVersion(1, 0);
    }

    private static async Task<HttpResult> Register(
        RegisterCommand command,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<AuthTokensDto> result = await dispatcher.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<HttpResult> Login(
        LoginCommand command,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<AuthTokensDto> result = await dispatcher.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<HttpResult> RefreshToken(
        RefreshTokenCommand command,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<AuthTokensDto> result = await dispatcher.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<HttpResult> Logout(
        LogoutCommand command,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Unit> result = await dispatcher.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<HttpResult> ForgotPassword(
        ForgotPasswordCommand command,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        await dispatcher.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<HttpResult> ResetPassword(
        ResetPasswordCommand command,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Unit> result = await dispatcher.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static HttpResult OidcLogin(
        IOptions<OidcOptions> oidcOptions,
        ILogger<AuthEndpoints> logger,
        string? returnUrl = null)
    {
        if (!oidcOptions.Value.Enabled)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: AuthErrors.OidcNotEnabled.Message);
        }

        AuthenticationProperties properties = new()
        {
            RedirectUri = "/api/v1/auth/oidc/callback"
        };

        if (!string.IsNullOrEmpty(returnUrl) && IsValidReturnUrl(returnUrl, oidcOptions.Value.AllowedRedirectUris))
        {
            properties.Items["returnUrl"] = returnUrl;
        }
        else if (!string.IsNullOrEmpty(returnUrl))
        {
            logger.LogWarning("OIDC Login - returnUrl {ReturnUrl} not in allowed list", returnUrl);
        }

        return Results.Challenge(properties, ["oidc"]);
    }

    private static async Task<HttpResult> OidcCallback(
        HttpContext httpContext,
        IDispatcher dispatcher,
        IAuthCodeService authCodeService,
        ILogger<AuthEndpoints> logger,
        CancellationToken cancellationToken)
    {
        AuthenticateResult authenticateResult = await httpContext.AuthenticateAsync("oidc-cookie");

        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            logger.LogWarning("OIDC Callback - Authentication failed: {Failure}", authenticateResult.Failure?.Message);
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: AuthErrors.ExternalLoginFailed.Message);
        }

        ClaimsPrincipal principal = authenticateResult.Principal;

        Result<AuthTokensDto> result = await dispatcher.Send(new OidcCallbackCommand(principal), cancellationToken);

        if (result.IsFailure)
        {
            return result.ToHttpResult();
        }

        // Sign out from the temporary cookie
        await httpContext.SignOutAsync("oidc-cookie");

        string? returnUrl = authenticateResult.Properties?.Items.TryGetValue("returnUrl", out string? url) == true ? url : null;

        if (!string.IsNullOrEmpty(returnUrl))
        {
            string code = await authCodeService.CreateCodeAsync(result.Value, cancellationToken);
            return Results.Redirect($"{returnUrl}?code={code}");
        }

        return result.ToHttpResult();
    }

    private static async Task<HttpResult> GetCurrentUser(IDispatcher dispatcher, CancellationToken cancellationToken)
    {
        Result<UserDto> result = await dispatcher.Send(new GetCurrentUserQuery(), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<HttpResult> ExchangeCode(
        ExchangeCodeCommand command,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<AuthTokensDto> result = await dispatcher.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static bool IsValidReturnUrl(string returnUrl, IReadOnlyList<string> allowedUris)
    {
        if (Uri.TryCreate(returnUrl, UriKind.Relative, out _))
        {
            return !returnUrl.StartsWith("//", StringComparison.Ordinal);
        }

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out Uri? uri))
        {
            string origin = $"{uri.Scheme}://{uri.Host}";
            if (uri.Port is not 80 and not 443)
            {
                origin += $":{uri.Port}";
            }

            return allowedUris.Any(allowed => origin.Equals(allowed, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }
}
