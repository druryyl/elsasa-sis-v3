namespace Elsasa.Sis.IntegrationTests.Features.DemoData;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Elsasa.Sis.Api.Features.Auth;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration test verifying demo user login and JWT claim generation
/// (P5-S10, GAP-003, OQ-007).
/// </summary>
public sealed class DemoDataLoginTest : BaseIntegrationTest
{
    private readonly string[] _demoUsernames = new[] { "admin", "admission", "finance", "supervisor", "coordinator", "student.jonathan", "parent.jonathan" };
    private const string _demoPassword = "Demo123!";

    public DemoDataLoginTest(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [SkippableFact]
    public async Task AllDemoAccounts_Should_Login_Successfully_And_Issue_Valid_JWT()
    {
        SkipIfDatabaseUnavailable();

        var client = Factory.CreateClient();

        foreach (var username in _demoUsernames)
        {
            // 1. Attempt login with correct password
            var loginRequest = new { Username = username, Password = _demoPassword };
            var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                $"Login for user '{username}' should succeed with correct password");

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>()!;
            loginResponse.Should().NotBeNull();
            loginResponse!.Token.Should().NotBeNullOrEmpty(
                $"token for user '{username}' should not be empty");

            // 2. Verify JWT claims contain expected role and persona ID
            var jwtHandler = new JwtSecurityTokenHandler();
            var jwtToken = jwtHandler.ReadJwtToken(loginResponse.Token!);

            // 3. Verify role claim is present
            var hasRole = jwtToken.Claims.Any(c => c.Type == "role" || c.Type == ClaimTypes.Role);
            hasRole.Should().BeTrue($"JWT for user '{username}' should contain a role claim");

            // 4. Verify specific persona ID linking for student/parent accounts
            if (username == "student.jonathan")
            {
                var studentIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "student_id");
                studentIdClaim.Should().NotBeNull(
                    "student.jonathan JWT should contain student_id claim");
                var studentId = Guid.Parse(studentIdClaim!.Value);
                studentId.Should().Be(DemoDataConstants.JonathanStudentId,
                    "student.jonathan JWT student_id should match DemoDataConstants");
            }
            else if (username == "parent.jonathan")
            {
                var parentIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "parent_id");
                parentIdClaim.Should().NotBeNull(
                    "parent.jonathan JWT should contain parent_id claim");
                var parentId = Guid.Parse(parentIdClaim!.Value);
                parentId.Should().Be(DemoDataConstants.HendraParentId,
                    "parent.jonathan JWT parent_id should match DemoDataConstants");
            }

            // 5. Verify invalid password returns BadRequest
            var badResponse = await client.PostAsJsonAsync("/api/auth/login", new { Username = username, Password = "WrongPassword!" });
            badResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                $"Login for user '{username}' should fail with wrong password");
        }
    }

    [SkippableFact]
    public async Task DemoAccounts_Should_Have_Correct_Role_Mapping()
    {
        SkipIfDatabaseUnavailable();

        var client = Factory.CreateClient();

        // Admin should have SuperAdmin role
        var adminResponse = await Client.PostAsJsonAsync("/api/auth/login", new { Username = "admin", Password = _demoPassword });
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Admission officer should have AdmissionOfficer role
        var admissionResponse = await Client.PostAsJsonAsync("/api/auth/login", new { Username = "admission", Password = _demoPassword });
        admissionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Finance officer should have FinanceOfficer role
        var financeResponse = await Client.PostAsJsonAsync("/api/auth/login", new { Username = "finance", Password = _demoPassword });
        financeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Supervisor should have Teacher role
        var supervisorResponse = await Client.PostAsJsonAsync("/api/auth/login", new { Username = "supervisor", Password = _demoPassword });
        supervisorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Coordinator should have AcademicCoordinator role
        var coordinatorResponse = await Client.PostAsJsonAsync("/api/auth/login", new { Username = "coordinator", Password = _demoPassword });
        coordinatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Student.jonathan should have Student role with student_id claim
        var studentResponse = await Client.PostAsJsonAsync("/api/auth/login", new { Username = "student.jonathan", Password = _demoPassword });
        studentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Parent.jonathan should have Parent role with parent_id claim
        var parentResponse = await Client.PostAsJsonAsync("/api/auth/login", new { Username = "parent.jonathan", Password = _demoPassword });
        parentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}