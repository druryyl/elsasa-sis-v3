namespace Elsasa.Sis.Shared.Common;

/// <summary>
/// Categorises a <see cref="DomainError"/> so that the transport layer can map it
/// deterministically to an HTTP status code (ADR-007).
/// </summary>
public enum ErrorType
{
    /// <summary>The requested resource does not exist (HTTP 404).</summary>
    NotFound,

    /// <summary>The request conflicts with the current state of the resource (HTTP 409).</summary>
    Conflict,

    /// <summary>One or more input values are invalid (HTTP 422).</summary>
    Validation,

    /// <summary>Authentication is missing or invalid (HTTP 401).</summary>
    Unauthorized,

    /// <summary>The authenticated caller is not permitted to perform the operation (HTTP 403).</summary>
    Forbidden,

    /// <summary>The operation failed for a reason that maps to a generic client error (HTTP 400).</summary>
    Failure
}
