namespace Elsasa.Sis.Shared.Common;

using System.Collections.ObjectModel;

/// <summary>
/// Describes a business rule outcome that prevents an operation from succeeding.
/// A domain error is an expected result, not an exception (ADR-007).
/// </summary>
/// <remarks>
/// The record is immutable: all properties are get-only and the supplied
/// <c>validationErrors</c> dictionary (including its message arrays) is copied
/// into a read-only snapshot at construction time.
/// </remarks>
public sealed record DomainError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainError"/> record.
    /// </summary>
    /// <param name="code">Stable machine-readable error code (for example <c>Admission.DuplicateActiveApplication</c>).</param>
    /// <param name="message">Human-readable description of the failure.</param>
    /// <param name="type">Category used to map the error to an HTTP status code.</param>
    /// <param name="validationErrors">Optional field-keyed validation messages.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> or <paramref name="message"/> is null or whitespace.</exception>
    public DomainError(
        string code,
        string message,
        ErrorType type,
        IDictionary<string, string[]>? validationErrors = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
        Type = type;
        ValidationErrors = validationErrors is null ? null : Snapshot(validationErrors);
    }

    /// <summary>Gets the stable machine-readable error code.</summary>
    public string Code { get; }

    /// <summary>Gets the human-readable description of the failure.</summary>
    public string Message { get; }

    /// <summary>Gets the category used to map the error to an HTTP status code.</summary>
    public ErrorType Type { get; }

    /// <summary>Gets the optional read-only field-keyed validation messages.</summary>
    public IDictionary<string, string[]>? ValidationErrors { get; }

    /// <summary>Creates a <see cref="ErrorType.NotFound"/> error.</summary>
    /// <param name="code">Stable machine-readable error code.</param>
    /// <param name="message">Human-readable description of the failure.</param>
    /// <returns>The domain error.</returns>
    public static DomainError NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    /// <summary>Creates a <see cref="ErrorType.Conflict"/> error.</summary>
    /// <param name="code">Stable machine-readable error code.</param>
    /// <param name="message">Human-readable description of the failure.</param>
    /// <returns>The domain error.</returns>
    public static DomainError Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    /// <summary>Creates a <see cref="ErrorType.Validation"/> error.</summary>
    /// <param name="code">Stable machine-readable error code.</param>
    /// <param name="message">Human-readable description of the failure.</param>
    /// <param name="validationErrors">Optional field-keyed validation messages.</param>
    /// <returns>The domain error.</returns>
    public static DomainError Validation(
        string code,
        string message,
        IDictionary<string, string[]>? validationErrors = null) =>
        new(code, message, ErrorType.Validation, validationErrors);

    /// <summary>Creates an <see cref="ErrorType.Unauthorized"/> error.</summary>
    /// <param name="code">Stable machine-readable error code.</param>
    /// <param name="message">Human-readable description of the failure.</param>
    /// <returns>The domain error.</returns>
    public static DomainError Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    /// <summary>Creates a <see cref="ErrorType.Forbidden"/> error.</summary>
    /// <param name="code">Stable machine-readable error code.</param>
    /// <param name="message">Human-readable description of the failure.</param>
    /// <returns>The domain error.</returns>
    public static DomainError Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);

    /// <summary>Creates a generic <see cref="ErrorType.Failure"/> error.</summary>
    /// <param name="code">Stable machine-readable error code.</param>
    /// <param name="message">Human-readable description of the failure.</param>
    /// <returns>The domain error.</returns>
    public static DomainError Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);

    private static ReadOnlyDictionary<string, string[]> Snapshot(IDictionary<string, string[]> validationErrors)
    {
        var copy = new Dictionary<string, string[]>(validationErrors.Count, StringComparer.Ordinal);
        foreach (var (field, messages) in validationErrors)
        {
            copy[field] = messages is null ? [] : (string[])messages.Clone();
        }

        return new ReadOnlyDictionary<string, string[]>(copy);
    }
}
