using Application.Abstractions.Helpers;
using Application.DependencyInjection.Options;
using Domain.Users.Constants;
using Infrastructure.DependencyInjection.Options;
using Infrastructure.Identity;
using Infrastructure.Identity.Handlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.DependencyInjection;

internal static partial class InfrastructureDependencyInjection
{
    private static JwtOptions GetJwtOptions(IConfiguration configuration)
    {
        JwtOptions options = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration section is missing.");
        options.Validate();
        return options;
    }

    private static void ConfigureJwtBearer(JwtBearerOptions options, JwtOptions jwtOptions)
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }

                return Task.CompletedTask;
            }
        };
    }

    private static void ConfigureOidc(
        AuthenticationBuilder authBuilder,
        IServiceCollection services,
        OidcOptions oidcConfig)
    {
        if (!oidcConfig.Enabled || string.IsNullOrEmpty(oidcConfig.Provider.Authority))
        {
            return;
        }

        // Add cookie authentication for OIDC sign-in scheme
        authBuilder.AddCookie("oidc-cookie", options =>
        {
            options.Cookie.Name = "oidc-auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        });

        authBuilder.AddOpenIdConnect("oidc", options =>
        {
            options.SignInScheme = "oidc-cookie";
            options.Authority = oidcConfig.Provider.Authority;
            options.ClientId = oidcConfig.Provider.ClientId;
            options.ClientSecret = oidcConfig.Provider.ClientSecret;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.UsePkce = true;

            options.CallbackPath = oidcConfig.Provider.CallbackPath;
            options.SignedOutCallbackPath = oidcConfig.Provider.SignedOutCallbackPath;

            options.Scope.Clear();
            foreach (string scope in oidcConfig.Provider.Scopes)
            {
                options.Scope.Add(scope);
            }

            options.ClaimActions.MapJsonKey(
                oidcConfig.Provider.GroupClaimType,
                oidcConfig.Provider.GroupClaimType);

            // Preserve custom items (like returnUrl) from the original properties
            options.Events.OnTicketReceived = context =>
            {
                // The properties from the Challenge are in context.Properties
                // They will be stored in the cookie automatically
                return Task.CompletedTask;
            };
        });

        services.AddScoped<IClaimsTransformation, OidcClaimsTransformer>();
    }

    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddAuth()
        {
            IConfiguration configuration = builder.Configuration;
            JwtOptions jwtOptions = GetJwtOptions(configuration);

            builder.Services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
            builder.Services.Configure<OidcOptions>(configuration.GetSection(OidcOptions.SectionName));

            OidcOptions oidcConfig = configuration
                .GetSection(OidcOptions.SectionName)
                .Get<OidcOptions>() ?? new();

            AuthenticationBuilder authBuilder = builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options => ConfigureJwtBearer(options, jwtOptions));

            ConfigureOidc(authBuilder, builder.Services, oidcConfig);

            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("RequireAdmin", policy =>
                    policy.RequireRole(Roles.Admin))
                .AddPolicy("RequireUser", policy =>
                    policy.RequireRole(Roles.All));

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IUser, CurrentUser>();

            return builder;
        }
    }
}
