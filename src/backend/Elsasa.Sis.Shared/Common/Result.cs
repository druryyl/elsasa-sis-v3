namespace Elsasa.Sis.Shared.Common;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents the outcome of an operation that does not produce a value.
/// Success and failure are explicit; business failures are values, never exceptions (ADR-007).
/// </summary>
public sealed class Result
{
    private static readonly Result _successInstance = new(error: null);

    private Result(DomainError? error) => Error = error;

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the failure description, or <see langword="null"/> when the operation succeeded.</summary>
    public DomainError? Error { get; }

    /// <summary>Creates a successful result.</summary>
    /// <returns>A shared successful result instance.</returns>
    public static Result Success() => _successInstance;

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The domain error describing the failure.</param>
    /// <returns>A failed result carrying <paramref name="error"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result Failure(DomainError error) =>
        new(error ?? throw new ArgumentNullException(nameof(error)));

    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    /// <typeparam name="TValue">Type of the produced value.</typeparam>
    /// <param name="value">The value produced by the operation.</param>
    /// <returns>A successful <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Success<TValue>(TValue value) => new(isSuccess: true, value, error: null);

    /// <summary>Creates a failed result for an operation that would have returned <typeparamref name="TValue"/>.</summary>
    /// <typeparam name="TValue">Type of the value the operation would have produced.</typeparam>
    /// <param name="error">The domain error describing the failure.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result<TValue> Failure<TValue>(DomainError error) =>
        new(isSuccess: false, default, error ?? throw new ArgumentNullException(nameof(error)));

    /// <summary>Converts a <see cref="DomainError"/> into a failed result.</summary>
    /// <param name="error">The domain error describing the failure.</param>
    public static implicit operator Result(DomainError error) => Failure(error);
}
