namespace Elsasa.Sis.IntegrationTests.Features.DemoData;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration test verifying the complete demo data reset and seed cycle
/// (P5-S10, GAP-006, OQ-004, OQ-013, RISK-001).
/// Executes against SQL Server Testcontainers via WebApplicationFactory.
/// </summary>
public sealed class DemoDataResetTest : BaseIntegrationTest
{
    private const string _demoPassword = "Demo123!";

    public DemoDataResetTest(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [SkippableFact]
    public async Task FullResetAndSeed_Should_PurgeAllDomains_And_SeedWithCorrectCounts()
    {
        SkipIfDatabaseUnavailable();

        // Reset database to clean state
        await Factory.ContainerFixture.ResetDatabaseAsync();

        var client = Factory.CreateClient();

        // 1. Execute full reset and seed via API
        var resetRequest = new { cleanOnly = false };

        var response = await client.PostAsJsonAsync("/api/operations/demo-data/reset", resetRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<DemoResetSummary>()!;
        summary.Should().NotBeNull();

        // 2. Verify purge counts
        summary!.PurgedSchemas.Should().Be(4, "should purge all 4 domains: operations, students, finance, academics");

        // 3. Verify seeded domains
        summary.SeededDomains.Should().Be(6, "should seed all 6 domains");

        // 4. Verify student counts (approximately 48 enrolled + 6 staged applicants)
        summary.TotalStudentsSeeded.Should().BeGreaterThan(40, "should seed ~48 enrolled students");
        summary.TotalStudentsSeeded.Should().BeLessThan(60, "should not exceed ~60 enrolled students");

        // 5. Verify invoice counts
        summary.TotalInvoicesSeeded.Should().BeGreaterThan(0, "should seed invoices");
        summary.TotalInvoicesSeeded.Should().BeLessThan(200, "invoice count should be reasonable");

        // 6. Verify attendance records
        summary.TotalAttendanceSeeded.Should().BeGreaterThan(1000, "should seed ~1,200 attendance records");
        summary.TotalAttendanceSeeded.Should().BeLessThan(2000, "attendance count should be reasonable");

        // 7. Verify results
        summary.TotalResultsSeeded.Should().BeGreaterThan(0, "should seed results");
        summary.TotalResultsSeeded.Should().BeLessThan(200, "results count should be reasonable");

        // 8. Verify report cards
        summary.TotalReportCardsSeeded.Should().BeGreaterThan(0, "should seed report cards");
        summary.TotalReportCardsSeeded.Should().BeLessThan(100, "report card count should be reasonable");

        // 9. Verify idempotency: executing reset twice consecutively succeeds
        var secondResponse = await client.PostAsJsonAsync("/api/operations/demo-data/reset", resetRequest);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondSummary = await secondResponse.Content.ReadFromJsonAsync<DemoResetSummary>()!;
        secondSummary.Should().NotBeNull();
        secondSummary!.PurgedSchemas.Should().Be(4, "second reset should also purge 4 schemas");
        secondSummary.SeededDomains.Should().Be(6, "second reset should also seed 6 domains");

        // 10. Verify complete reset and seeding finishes in under 5 seconds (Criteria 1, bullet 6, MAJOR-002)
        summary.ElapsedMilliseconds.Should().BeLessThan(5000, "reset and seed should complete in under 5 seconds");

        // 11. Asserts zero orphaned foreign key records exist across schemas (Criteria 1, bullet 4, MAJOR-001)
        const string orphanCheckSql = """
            SELECT 
                (SELECT COUNT(*) FROM academics.academic_results ar WHERE ar.student_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM students.students s WHERE s.id = ar.student_id)) +
                (SELECT COUNT(*) FROM academics.report_cards rc WHERE NOT EXISTS (SELECT 1 FROM students.students s WHERE s.id = rc.student_id)) +
                (SELECT COUNT(*) FROM academics.transcripts tr WHERE NOT EXISTS (SELECT 1 FROM students.students s WHERE s.id = tr.student_id)) +
                (SELECT COUNT(*) FROM academics.attendance_records att WHERE NOT EXISTS (SELECT 1 FROM students.students s WHERE s.id = att.student_id)) +
                (SELECT COUNT(*) FROM academics.academic_participations ap WHERE NOT EXISTS (SELECT 1 FROM students.students s WHERE s.id = ap.student_id)) +
                (SELECT COUNT(*) FROM finance.financial_obligations fo WHERE NOT EXISTS (SELECT 1 FROM students.students s WHERE s.id = fo.student_id)) +
                (SELECT COUNT(*) FROM finance.billing_documents bd WHERE NOT EXISTS (SELECT 1 FROM students.students s WHERE s.id = bd.student_id)) +
                (SELECT COUNT(*) FROM finance.student_accounts sa WHERE NOT EXISTS (SELECT 1 FROM students.students s WHERE s.id = sa.student_id)) +
                (SELECT COUNT(*) FROM students.student_guardian_relationships sgr WHERE NOT EXISTS (SELECT 1 FROM students.students s WHERE s.id = sgr.student_id)) +
                (SELECT COUNT(*) FROM students.student_guardian_relationships sgr WHERE NOT EXISTS (SELECT 1 FROM students.guardians g WHERE g.id = sgr.guardian_id)) +
                (SELECT COUNT(*) FROM students.administrative_placements ap WHERE NOT EXISTS (SELECT 1 FROM students.students s WHERE s.id = ap.student_id))
            """;
        var totalOrphans = await QuerySingleAsync<int>(orphanCheckSql);
        totalOrphans.Should().Be(0, "zero orphaned foreign key records should exist across schemas after reset and seed cycle");
    }

    [SkippableFact]
    public async Task FullResetAndSeed_Should_PurgeThenSeedEachDomain()
    {
        SkipIfDatabaseUnavailable();

        await Factory.ContainerFixture.ResetDatabaseAsync();

        var client = Factory.CreateClient();

        // Execute clean-only first, then full reset
        var cleanRequest = new { cleanOnly = true };
        var cleanResponse = await client.PostAsJsonAsync("/api/operations/demo-data/reset", cleanRequest);
        cleanResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cleanSummary = await cleanResponse.Content.ReadFromJsonAsync<DemoResetSummary>()!;
        cleanSummary.Should().NotBeNull();
        cleanSummary!.PurgedSchemas.Should().Be(4, "clean-only should purge 4 schemas");
        cleanSummary.SeededDomains.Should().Be(0, "clean-only should seed 0 domains");

        // Now execute full reset
        var fullResetRequest = new { cleanOnly = false };
        var fullResponse = await client.PostAsJsonAsync("/api/operations/demo-data/reset", fullResetRequest);
        fullResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fullSummary = await fullResponse.Content.ReadFromJsonAsync<DemoResetSummary>()!;
        fullSummary.Should().NotBeNull();
        fullSummary!.PurgedSchemas.Should().Be(4, "full reset should purge 4 schemas");
        fullSummary.SeededDomains.Should().Be(6, "full reset should seed 6 domains");
        fullSummary.TotalStudentsSeeded.Should().BeGreaterThan(40, "should have enrolled students after full reset");
        fullSummary.ElapsedMilliseconds.Should().BeLessThan(5000, "reset and seed should complete in under 5 seconds");
    }
}