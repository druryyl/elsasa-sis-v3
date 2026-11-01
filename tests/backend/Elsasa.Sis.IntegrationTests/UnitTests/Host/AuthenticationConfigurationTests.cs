namespace Elsasa.Sis.IntegrationTests.UnitTests.Host;

using Elsasa.Sis.Api.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public sealed class AuthenticationConfigurationTests
{
    [Fact]
    public void AddJwtAuthentication_Should_ThrowArgumentNullException_When_ArgumentsAreNull()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddJwtAuthentication(configuration));
        Assert.Throws<ArgumentNullException>(() => services.AddJwtAuthentication(null!));
    }

    [Fact]
    public void AddJwtAuthentication_Should_RegisterAuthenticationAndSetDefaultSchemes()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        // Act
        services.AddJwtAuthentication(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        var authOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.DefaultAuthenticateScheme);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.DefaultChallengeScheme);
    }

    [Fact]
    public void AddJwtAuthentication_Should_ConfigureJwtBearerOptions_FromConfiguration()
    {
        // Arrange
        const string secretKey = "very_long_secret_key_used_only_for_unit_testing_12345";
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "https://auth.elsasa.sch.id",
            ["Jwt:Audience"] = "https://api.elsasa.sch.id",
            ["Jwt:SecretKey"] = secretKey,
            ["Jwt:RequireHttpsMetadata"] = "true",
            ["Jwt:ClockSkewSeconds"] = "60",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var services = new ServiceCollection();

        // Act
        services.AddJwtAuthentication(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.True(jwtOptions.RequireHttpsMetadata);
        Assert.True(jwtOptions.SaveToken);
        Assert.NotNull(jwtOptions.TokenValidationParameters);
        Assert.True(jwtOptions.TokenValidationParameters.ValidateIssuer);
        Assert.Equal("https://auth.elsasa.sch.id", jwtOptions.TokenValidationParameters.ValidIssuer);
        Assert.True(jwtOptions.TokenValidationParameters.ValidateAudience);
        Assert.Equal("https://api.elsasa.sch.id", jwtOptions.TokenValidationParameters.ValidAudience);
        Assert.True(jwtOptions.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.NotNull(jwtOptions.TokenValidationParameters.IssuerSigningKey);
        Assert.Equal(TimeSpan.FromSeconds(60), jwtOptions.TokenValidationParameters.ClockSkew);
    }

    [Fact]
    public void AddJwtAuthentication_Should_NotValidateKey_When_SecretKeyMissing()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "https://auth.elsasa.sch.id",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var services = new ServiceCollection();

        // Act
        services.AddJwtAuthentication(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.False(jwtOptions.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.Null(jwtOptions.TokenValidationParameters.IssuerSigningKey);
    }
}
