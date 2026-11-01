namespace Elsasa.Sis.Api.Configuration;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

/// <summary>
/// Configures Swagger OpenAPI document generation with JWT Bearer authentication scheme (ADR-005, ADR-009).
/// </summary>
public static class SwaggerConfiguration
{
    public const string DocumentName = "v1";
    public const string DocumentTitle = "Elsasa SIS API";
    public const string DocumentVersion = "v1";
    public const string SecuritySchemeName = "Bearer";

    /// <summary>
    /// Registers Swagger/OpenAPI generators with JWT Bearer authentication scheme.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for fluent chaining.</returns>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(DocumentName, new OpenApiInfo
            {
                Title = DocumentTitle,
                Version = DocumentVersion,
                Description = "Elsasa Student Information System V3 Modular Monolith API",
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter JWT Bearer token format: Bearer {your_token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = SecuritySchemeName,
                },
            };

            options.AddSecurityDefinition(SecuritySchemeName, securityScheme);

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    securityScheme,
                    Array.Empty<string>()
                },
            };

            options.AddSecurityRequirement(securityRequirement);
            options.CustomSchemaIds(type => type.FullName?.Replace("+", ".", StringComparison.Ordinal));
        });

        return services;
    }

    /// <summary>
    /// Adds Swagger and Swagger UI middleware to the HTTP request pipeline.
    /// </summary>
    /// <param name="app">The web application host.</param>
    /// <returns>The web application for fluent chaining.</returns>
    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"/swagger/{DocumentName}/swagger.json", $"{DocumentTitle} {DocumentVersion}");
            c.RoutePrefix = "swagger";
        });

        return app;
    }
}
