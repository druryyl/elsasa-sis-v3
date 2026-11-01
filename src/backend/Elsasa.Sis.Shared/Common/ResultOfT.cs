namespace Elsasa.Sis.Shared.Common;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents the outcome of an operation that produces a <typeparamref name="TValue"/> on success.
/// Business failures are values, never exceptions (ADR-007).
/// </summary>
/// <typeparam name="TValue">Type of the value produced by a successful operation.</typeparam>
public sealed class Result<TValue>
{
    private readonly TValue? _value;

    /// <summary>
    /// Initializes a new result. Instances are created through the static factories on
    /// the non-generic <see cref="Result"/> type or through the implicit conversions,
    /// which keeps static members off this generic type (CA1000).
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="value">The produced value, when successful.</param>
    /// <param name="error">The failure description, when unsuccessful.</param>
    internal Result(bool isSuccess, TValue? value, DomainError? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the value produced by the operation.</summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the result is a failure. Inspect <see cref="Error"/> (or check
    /// <see cref="IsSuccess"/>) before reading <see cref="Value"/>.
    /// </exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            "Result<TValue>.Value cannot be accessed when the result is a failure. Inspect Result<TValue>.Error or check IsSuccess first.");

    /// <summary>Gets the failure description, or <see langword="null"/> when the operation succeeded.</summary>
    public DomainError? Error { get; }

    /// <summary>Converts a value into a successful result.</summary>
    /// <param name="value">The value produced by the operation.</param>
    public static implicit operator Result<TValue>(TValue value) => Result.Success(value);

    /// <summary>Converts a <see cref="DomainError"/> into a failed result.</summary>
    /// <param name="error">The domain error describing the failure.</param>
    public static implicit operator Result<TValue>(DomainError error) => Result.Failure<TValue>(error);
}
