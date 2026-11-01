namespace Elsasa.Sis.IntegrationTests.UnitTests.Observability;

using System.Text.Json;
using Elsasa.Sis.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public sealed class GlobalExceptionHandlerTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(null!));
    }

    [Fact]
    public async Task TryHandleAsync_Should_ThrowArgumentNullException_When_HttpContextIsNull()
    {
        // Arrange
        var logger = new TestLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.TryHandleAsync(null!, new InvalidOperationException("Test"), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task TryHandleAsync_Should_ThrowArgumentNullException_When_ExceptionIsNull()
    {
        // Arrange
        var logger = new TestLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);
        var context = new DefaultHttpContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.TryHandleAsync(context, null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task TryHandleAsync_Should_ReturnTrueAndWriteRfc7807ProblemDetails_When_UnhandledExceptionOccurs()
    {
        // Arrange
        var logger = new TestLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test/resource";
        const string correlationId = "corr-error-trace-123";
        context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeader] = correlationId;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var exception = new InvalidOperationException("CRITICAL: Database connection pool exhausted!");

        // Act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.NotNull(context.Response.ContentType);
        Assert.Contains("application/problem+json", context.Response.ContentType);

        responseBody.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(responseBody, _jsonOptions);

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.Equal("Internal Server Error", problem.Title);
        Assert.Equal("An unexpected error occurred while processing your request.", problem.Detail);
        Assert.Equal("/api/test/resource", problem.Instance);

        // Verify correlationId is present in problem details extensions
        Assert.True(problem.Extensions.TryGetValue("correlationId", out var extVal));
        Assert.Equal(correlationId, extVal?.ToString());

        // Verify that sensitive internal exception details are NEVER leaked into the problem details payload
        Assert.DoesNotContain("Database connection pool exhausted", problem.Detail);
        Assert.DoesNotContain("InvalidOperationException", problem.Detail);

        // Verify error was logged with correlation ID and path
        var errorEntry = Assert.Single(logger.Entries, e => e.Level == LogLevel.Error);
        Assert.Same(exception, errorEntry.Exception);
        Assert.Contains("/api/test/resource", errorEntry.Message);
        Assert.Contains(correlationId, errorEntry.Message);
    }

    [Fact]
    public async Task TryHandleAsync_Should_FallbackToTraceIdentifier_When_NoCorrelationHeaderPresent()
    {
        // Arrange
        var logger = new TestLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);

        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-fallback-999";
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Act
        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("Some error"), CancellationToken.None);

        // Assert
        Assert.True(handled);
        responseBody.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(responseBody, _jsonOptions);

        Assert.NotNull(problem);
        Assert.True(problem.Extensions.TryGetValue("correlationId", out var extVal));
        Assert.Equal("trace-fallback-999", extVal?.ToString());
    }

    [Fact]
    public async Task TryHandleAsync_Should_ReturnFalse_When_ResponseHasAlreadyStarted()
    {
        // Arrange
        var logger = new TestLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);

        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedHttpResponseFeature());

        // Act
        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("Too late error"), CancellationToken.None);

        // Assert
        Assert.False(handled);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("Response has already started"));
    }

    [Fact]
    public void AddGlobalExceptionHandler_Should_ThrowArgumentNullException_When_ServicesIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => GlobalExceptionHandlerExtensions.AddGlobalExceptionHandler(null!));
    }

    [Fact]
    public void AddGlobalExceptionHandler_Should_RegisterRequiredServices_When_Invoked()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGlobalExceptionHandler();
        using var provider = services.BuildServiceProvider();

        // Assert
        var handlers = provider.GetServices<IExceptionHandler>();
        Assert.Contains(handlers, h => h is GlobalExceptionHandler);
    }

    private sealed class StartedHttpResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => true;
        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public sealed record LogEntry(LogLevel Level, EventId EventId, Exception? Exception, string Message);

        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, eventId, exception, formatter(state, exception)));
        }
    }
}
