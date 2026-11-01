namespace Elsasa.Sis.IntegrationTests.UnitTests.Observability;

using Elsasa.Sis.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Core;
using Serilog.Events;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_NextIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CorrelationIdMiddleware(null!));
    }

    [Fact]
    public async Task InvokeAsync_Should_ThrowArgumentNullException_When_ContextIsNull()
    {
        // Arrange
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => middleware.InvokeAsync(null!));
    }

    [Fact]
    public async Task InvokeAsync_Should_PreserveExistingCorrelationId_When_XCorrelationIdHeaderIsProvided()
    {
        // Arrange
        const string expectedCorrelationId = "custom-test-correlation-id-9876";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeader] = expectedCorrelationId;

        var nextInvoked = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextInvoked);
        Assert.Equal(expectedCorrelationId, context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeader].ToString());
        Assert.Equal(expectedCorrelationId, context.TraceIdentifier);
        Assert.Equal(expectedCorrelationId, context.Items[CorrelationIdMiddleware.CorrelationIdItemKey]);
    }

    [Fact]
    public async Task InvokeAsync_Should_GenerateNewGuidCorrelationId_When_XCorrelationIdHeaderIsMissing()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextInvoked = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextInvoked);
        var responseHeader = context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeader].ToString();
        Assert.True(Guid.TryParse(responseHeader, out var parsedGuid));
        Assert.NotEqual(Guid.Empty, parsedGuid);
        Assert.Equal(responseHeader, context.TraceIdentifier);
        Assert.Equal(responseHeader, context.Items[CorrelationIdMiddleware.CorrelationIdItemKey]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvokeAsync_Should_GenerateNewGuidCorrelationId_When_XCorrelationIdHeaderIsWhitespace(string headerValue)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeader] = headerValue;

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseHeader = context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeader].ToString();
        Assert.True(Guid.TryParse(responseHeader, out var parsedGuid));
        Assert.NotEqual(Guid.Empty, parsedGuid);
    }

    [Fact]
    public async Task InvokeAsync_Should_PushCorrelationIdToSerilogLogContext_When_Invoked()
    {
        // Arrange
        const string testCorrelationId = "serilog-correlation-test-abc";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeader] = testCorrelationId;

        string? insideProperty = null;
        string? outsideProperty = null;

        var capturingSink = new CapturingLogEventSink();
        using var testLogger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(capturingSink)
            .CreateLogger();

        var middleware = new CorrelationIdMiddleware(_ =>
        {
            testLogger.Information("Event inside middleware execution");
            insideProperty = capturingSink.LastCorrelationId;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        testLogger.Information("Event outside middleware execution");
        outsideProperty = capturingSink.LastCorrelationId;

        // Assert
        Assert.Equal(testCorrelationId, insideProperty);
        Assert.Null(outsideProperty);
    }

    [Fact]
    public void UseCorrelationId_Should_ThrowArgumentNullException_When_AppIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CorrelationIdMiddlewareExtensions.UseCorrelationId(null!));
    }

    private sealed class CapturingLogEventSink : ILogEventSink
    {
        public string? LastCorrelationId { get; private set; }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Properties.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var propertyValue) &&
                propertyValue is ScalarValue scalar)
            {
                LastCorrelationId = scalar.Value?.ToString();
            }
            else
            {
                LastCorrelationId = null;
            }
        }
    }
}
