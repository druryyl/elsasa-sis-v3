namespace Elsasa.Sis.Api.Configuration;

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Strongly-typed configuration options for JWT bearer authentication (ADR-009).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string? Issuer { get; set; }

    public string? Audience { get; set; }

    public string? SecretKey { get; set; }

    public bool RequireHttpsMetadata { get; set; }

    public int ClockSkewSeconds { get; set; } = 30;
}

/// <summary>
/// Provides extension methods for wiring ASP.NET Core JWT bearer authentication (ADR-009).
/// </summary>
public static class AuthenticationConfiguration
{
    /// <summary>
    /// Configures JWT Bearer authentication and policy-based authorization using settings
    /// from the <c>Jwt</c> configuration section.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configureOptions">Optional delegate to customize <see cref="JwtBearerOptions"/>.</param>
    /// <returns>The service collection for fluent chaining.</returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtBearerOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
            options.SaveToken = true;

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrWhiteSpace(jwtOptions.Issuer),
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = !string.IsNullOrWhiteSpace(jwtOptions.Audience),
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(Math.Max(0, jwtOptions.ClockSkewSeconds)),
                NameClaimType = "name",
                RoleClaimType = "role",
            };

            if (!string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
            {
                tokenValidationParameters.ValidateIssuerSigningKey = true;
                tokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.SecretKey));
            }
            else
            {
                tokenValidationParameters.ValidateIssuerSigningKey = false;
            }

            options.TokenValidationParameters = tokenValidationParameters;

            configureOptions?.Invoke(options);
        });

        services.AddAuthorization();

        return services;
    }
}
