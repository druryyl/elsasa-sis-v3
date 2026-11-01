namespace Elsasa.Sis.IntegrationTests.UnitTests.Host;

using Elsasa.Sis.Api.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class SwaggerConfigurationTests
{
    [Fact]
    public void AddSwaggerDocumentation_Should_ThrowArgumentNullException_When_ServicesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddSwaggerDocumentation());
    }

    [Fact]
    public void UseSwaggerDocumentation_Should_ThrowArgumentNullException_When_AppIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((WebApplication)null!).UseSwaggerDocumentation());
    }

    [Fact]
    public void AddSwaggerDocumentation_Should_RegisterSwaggerGen_WithBearerSecurityDefinition()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSwaggerDocumentation();
        using var provider = services.BuildServiceProvider();

        // Assert
        var swaggerOptions = provider.GetRequiredService<IOptions<SwaggerGenOptions>>().Value;
        Assert.NotNull(swaggerOptions);

        Assert.True(swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs.TryGetValue("v1", out var swaggerDoc));
        Assert.NotNull(swaggerDoc);
        Assert.Equal("Elsasa SIS API", swaggerDoc.Title);

        Assert.True(swaggerOptions.SwaggerGeneratorOptions.SecuritySchemes.TryGetValue("Bearer", out var securityScheme));
        Assert.NotNull(securityScheme);
        Assert.Equal(SecuritySchemeType.Http, securityScheme.Type);
        Assert.Equal("Bearer", securityScheme.Scheme);
        Assert.Equal("JWT", securityScheme.BearerFormat);
    }
}
