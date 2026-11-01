namespace Elsasa.Sis.IntegrationTests.Fixtures;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Mutable, test-controllable implementation of <see cref="ICurrentUser"/> (ADR-009).
/// Allows integration tests to easily simulate any actor, role, or permissions without token minting.
/// </summary>
public sealed class TestCurrentUser : ICurrentUser
{
    private static readonly Guid _defaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid? Id { get; set; } = _defaultUserId;

    public Guid? UserId => Id;

    public string? Username { get; set; } = "test-integration-user";

    public string? Email { get; set; } = "test-integration-user@elsasa.edu";

    public IReadOnlyList<string> Roles { get; set; } = ["Admin"];

    public IReadOnlyList<string> Permissions { get; set; } = ["*"];

    public Guid? CampusId { get; set; }

    public bool IsAuthenticated { get; set; } = true;

    /// <summary>
    /// Resets the test user to default authenticated admin state.
    /// </summary>
    public void Reset()
    {
        Id = _defaultUserId;
        Username = "test-integration-user";
        Email = "test-integration-user@elsasa.edu";
        Roles = ["Admin"];
        Permissions = ["*"];
        CampusId = null;
        IsAuthenticated = true;
    }

    /// <summary>
    /// Configures the test user as unauthenticated (null ID/username/email, empty roles/permissions).
    /// </summary>
    public void SetUnauthenticated()
    {
        Id = null;
        Username = null;
        Email = null;
        Roles = [];
        Permissions = [];
        CampusId = null;
        IsAuthenticated = false;
    }

    /// <summary>
    /// Configures specific identity claims for test execution.
    /// </summary>
    public void SetUser(
        Guid id,
        string username,
        string? email = null,
        IReadOnlyList<string>? roles = null,
        IReadOnlyList<string>? permissions = null,
        Guid? campusId = null)
    {
        Id = id;
        Username = username;
        Email = email ?? $"{username}@elsasa.edu";
        Roles = roles ?? [];
        Permissions = permissions ?? [];
        CampusId = campusId;
        IsAuthenticated = true;
    }
}
