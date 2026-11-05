namespace Elsasa.Sis.Shared.Common;

/// <summary>
/// Provides access to the currently authenticated user's identity and claims (ADR-009).
/// </summary>
public interface ICurrentUser
{
    /// <summary>Gets the unique identifier of the authenticated user, or <see langword="null"/> if unauthenticated.</summary>
    Guid? Id { get; }

    /// <summary>Gets the username of the authenticated user, or <see langword="null"/> if unauthenticated.</summary>
    string? Username { get; }

    /// <summary>Gets the email address of the authenticated user, or <see langword="null"/> if unauthenticated.</summary>
    string? Email { get; }

    /// <summary>Gets the list of roles assigned to the authenticated user.</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>Gets a value indicating whether the current user is authenticated.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Gets the user ID, aliasing <see cref="Id"/> for cross-cutting compatibility.</summary>
    Guid? UserId => Id;

    /// <summary>Gets the list of permissions assigned to the authenticated user.</summary>
    IReadOnlyList<string> Permissions => [];

    /// <summary>Gets the optional campus identifier associated with the authenticated user.</summary>
    Guid? CampusId => null;

    /// <summary>Gets the optional student identifier associated with the authenticated user.</summary>
    Guid? StudentId => null;
}
