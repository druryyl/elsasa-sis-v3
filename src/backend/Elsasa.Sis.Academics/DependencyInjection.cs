namespace Elsasa.Sis.Academics;

using Elsasa.Sis.Academics.Features.AcademicParticipations.BatchRegisterCohortParticipation;
using Elsasa.Sis.Academics.Features.AcademicParticipations.ConcludeParticipation;
using Elsasa.Sis.Academics.Features.AcademicParticipations.GetParticipationsByUnit;
using Elsasa.Sis.Academics.Features.AcademicParticipations.GetStudentParticipations;
using Elsasa.Sis.Academics.Features.AcademicParticipations.RecognizePriorCredit;
using Elsasa.Sis.Academics.Features.AcademicParticipations.RegisterParticipation;
using Elsasa.Sis.Academics.Features.AcademicParticipations.UpdateParticipationStatus;
using Elsasa.Sis.Academics.Features.LearningActivities.CancelLearningActivity;
using Elsasa.Sis.Academics.Features.LearningActivities.ConductLearningActivity;
using Elsasa.Sis.Academics.Features.LearningActivities.GetActivitiesByUnit;
using Elsasa.Sis.Academics.Features.LearningActivities.GetActivityById;
using Elsasa.Sis.Academics.Features.LearningActivities.GetTeacherDailyActivities;
using Elsasa.Sis.Academics.Features.LearningActivities.PlanLearningActivity;
using Elsasa.Sis.Academics.Features.Attendance.RecordSessionAttendance;
using Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance;
using Elsasa.Sis.Academics.Features.Attendance.GetActivityAttendance;
using Elsasa.Sis.Academics.Features.Attendance.GetStudentAttendanceSummary;
using Elsasa.Sis.Academics.Features.AcademicRequirements.CreateAcademicRequirement;
using Elsasa.Sis.Academics.Features.Assignments.CloseAssignment;
using Elsasa.Sis.Academics.Features.Assignments.CreateAssignment;
using Elsasa.Sis.Academics.Features.Assignments.GetAssignmentsByActivity;
using Elsasa.Sis.Academics.Features.Assignments.GetAssignmentsByUnit;
using Elsasa.Sis.Academics.Features.Assignments.RecordSubmissionException;
using Elsasa.Sis.Academics.Features.Assignments.ReleaseAssignment;
using Elsasa.Sis.Academics.Features.Assignments.SubmitCoursework;
using Elsasa.Sis.Academics.Features.Assignments.UploadSubmissionFile;
using Elsasa.Sis.Academics.Features.Assignments.GetAssignmentSubmissions;
using Elsasa.Sis.Academics.Features.Assignments.GetStudentCourseworkWorkspace;
using Elsasa.Sis.Academics.Features.AcademicRequirements.DeleteAcademicRequirement;
using Elsasa.Sis.Academics.Features.AcademicRequirements.GetAcademicRequirementById;
using Elsasa.Sis.Academics.Features.AcademicRequirements.GetAcademicRequirements;
using Elsasa.Sis.Academics.Features.AcademicRequirements.UpdateAcademicRequirement;
using Elsasa.Sis.Academics.Features.LearningStructures.ApproveLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.ArchiveLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.CreateLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructureById;
using Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructures;
using Elsasa.Sis.Academics.Features.LearningStructures.SubmitLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.SupersedeLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.UpdateLearningStructure;
using Elsasa.Sis.Academics.Features.LearningUnits.CreateLearningUnit;
using Elsasa.Sis.Academics.Features.LearningUnits.DeleteLearningUnit;
using Elsasa.Sis.Academics.Features.LearningUnits.GetLearningUnitById;
using Elsasa.Sis.Academics.Features.LearningUnits.GetLearningUnits;
using Elsasa.Sis.Academics.Features.Assessments.CancelAssessment;
using Elsasa.Sis.Academics.Features.Assessments.ConductAssessment;
using Elsasa.Sis.Academics.Features.Assessments.CreateAssessment;
using Elsasa.Sis.Academics.Features.Assessments.GetAssessmentById;
using Elsasa.Sis.Academics.Features.Assessments.GetAssessmentsByUnit;
using Elsasa.Sis.Academics.Features.Assessments.UpdateAssessment;
using Elsasa.Sis.Academics.Features.AssessmentEvidence.GetEvidenceByAssessment;
using Elsasa.Sis.Academics.Features.AssessmentEvidence.RecordEvidence;
using Elsasa.Sis.Academics.Features.AcademicResults.ApproveSummativeResults;
using Elsasa.Sis.Academics.Features.AcademicResults.EvaluateStudentLearning;
using Elsasa.Sis.Academics.Features.AcademicResults.GetResultsByAssessment;
using Elsasa.Sis.Academics.Features.AcademicResults.GetStudentResultsWorkspace;
using Elsasa.Sis.Academics.Features.AcademicResults.PublishAcademicResults;
using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectAcademicStanding;
using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectApprovedResult;
using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectPromotionRecommendation;
using Elsasa.Sis.Academics.Features.RecordCorrections.GetResultCorrectionHistory;
using Elsasa.Sis.Academics.Features.ReportCards.ApproveReportCard;
using Elsasa.Sis.Academics.Features.ReportCards.BatchCompileCohortReportCards;
using Elsasa.Sis.Academics.Features.ReportCards.CompileReportCard;
using Elsasa.Sis.Academics.Features.ReportCards.GetReportCardById;
using Elsasa.Sis.Academics.Features.ReportCards.GetStudentReportCards;
using Elsasa.Sis.Academics.Features.ReportCards.PublishReportCard;
using Elsasa.Sis.Academics.Features.Transcripts.GenerateInterimTranscript;
using Elsasa.Sis.Academics.Features.Transcripts.GetStudentTranscript;
using Elsasa.Sis.Academics.Features.Transcripts.GetTranscriptById;
using Elsasa.Sis.Academics.Features.Transcripts.IssueTranscript;
using Elsasa.Sis.Academics.Features.AcademicAchievements.ConferAcademicAchievement;
using Elsasa.Sis.Academics.Features.AcademicAchievements.GetPeriodAchievements;
using Elsasa.Sis.Academics.Features.AcademicAchievements.GetStudentAchievements;
using Elsasa.Sis.Academics.Features.AcademicInterventions.GetActiveInterventions;
using Elsasa.Sis.Academics.Features.AcademicInterventions.GetAtRiskLearners;
using Elsasa.Sis.Academics.Features.AcademicInterventions.GetInterventionById;
using Elsasa.Sis.Academics.Features.AcademicInterventions.InitiateAcademicIntervention;
using Elsasa.Sis.Academics.Features.AcademicInterventions.UpdateInterventionStatus;
using Elsasa.Sis.Academics.Features.AcademicStandings.BatchDetermineCohortStanding;
using Elsasa.Sis.Academics.Features.AcademicStandings.DetermineAcademicStanding;
using Elsasa.Sis.Academics.Features.AcademicStandings.GetCohortStandings;
using Elsasa.Sis.Academics.Features.AcademicStandings.GetStudentStandings;
using Elsasa.Sis.Academics.Features.CohortReview;
using Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortProgression;
using Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortStanding;
using Elsasa.Sis.Academics.Features.PromotionRecommendations.GetCohortPromotionRecommendations;
using Elsasa.Sis.Academics.Features.PromotionRecommendations.GetStudentPromotionRecommendations;
using Elsasa.Sis.Academics.Features.LearningUnits.UpdateLearningUnit;
using Elsasa.Sis.Academics.Features.DemoData;
using Elsasa.Sis.Academics.Services;
using Elsasa.Sis.Academics.Services.Reassessment;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.DemoData;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection registrations for the Academics domain assembly (ADR-001, ADR-004, ADR-005, P3-S10, P5-S13).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all feature slice handlers, domain services, validators, storage services, and authorization policies for the Academics bounded context.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Optional configuration root for binding options.</param>
    /// <returns>The service collection for fluent chaining.</returns>
    public static IServiceCollection AddAcademicsDomain(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Coursework Submission storage options & helper service (GAP-006, OQ-004, TD-07, P5-S13)
        if (configuration is not null)
        {
            services.Configure<SubmissionStorageOptions>(configuration.GetSection(SubmissionStorageOptions.SectionName));
        }
        else
        {
            services.AddOptions<SubmissionStorageOptions>();
        }
        services.AddSingleton<ISubmissionStorageService, SubmissionStorageService>();

        // Learning Structures feature handlers (P3-S06)
        services.AddScoped<CreateLearningStructureHandler>();
        services.AddScoped<GetLearningStructuresHandler>();
        services.AddScoped<GetLearningStructureByIdHandler>();
        services.AddScoped<UpdateLearningStructureHandler>();

        // Learning Structures governance lifecycle handlers (P3-S07)
        services.AddScoped<SubmitLearningStructureHandler>();
        services.AddScoped<ApproveLearningStructureHandler>();
        services.AddScoped<SupersedeLearningStructureHandler>();
        services.AddScoped<ArchiveLearningStructureHandler>();

        // Learning Units feature handlers (P3-S08)
        services.AddScoped<CreateLearningUnitHandler>();
        services.AddScoped<GetLearningUnitsHandler>();
        services.AddScoped<GetLearningUnitByIdHandler>();
        services.AddScoped<UpdateLearningUnitHandler>();
        services.AddScoped<DeleteLearningUnitHandler>();

        // Academic Requirements feature handlers (P3-S09)
        services.AddScoped<CreateAcademicRequirementHandler>();
        services.AddScoped<GetAcademicRequirementsHandler>();
        services.AddScoped<GetAcademicRequirementByIdHandler>();
        services.AddScoped<UpdateAcademicRequirementHandler>();
        services.AddScoped<DeleteAcademicRequirementHandler>();

        // Academic Participations registration handlers (P2-S04, P2-S06)
        services.AddScoped<BatchRegisterCohortParticipationHandler>();
        services.AddScoped<RegisterParticipationHandler>();
        services.AddScoped<GetParticipationsByUnitHandler>();
        services.AddScoped<GetStudentParticipationsHandler>();

        // Academic Participations lifecycle & credit recognition handlers (P2-S05)
        services.AddScoped<UpdateParticipationStatusHandler>();
        services.AddScoped<RecognizePriorCreditHandler>();
        services.AddScoped<ConcludeParticipationHandler>();

        // Learning Activities feature handlers (P3-S07)
        services.AddScoped<PlanLearningActivityHandler>();
        services.AddScoped<ConductLearningActivityHandler>();
        services.AddScoped<CancelLearningActivityHandler>();
        services.AddScoped<GetActivitiesByUnitHandler>();
        services.AddScoped<GetActivityByIdHandler>();
        services.AddScoped<GetTeacherDailyActivitiesHandler>();

        // Attendance feature handlers (P4-S08, P4-S09)
        services.AddScoped<RecordSessionAttendanceHandler>();
        services.AddScoped<CorrectAttendanceHandler>();
        services.AddScoped<GetActivityAttendanceHandler>();
        services.AddScoped<GetStudentAttendanceSummaryHandler>();

        // Coursework Assignments feature handlers (P5-S10)
        services.AddScoped<CreateAssignmentHandler>();
        services.AddScoped<ReleaseAssignmentHandler>();
        services.AddScoped<CloseAssignmentHandler>();
        services.AddScoped<GetAssignmentsByActivityHandler>();
        services.AddScoped<GetAssignmentsByUnitHandler>();

        // Coursework Submissions, Upload & Exceptions feature handlers (P5-S11)
        services.AddScoped<SubmitCourseworkHandler>();
        services.AddScoped<UploadSubmissionFileHandler>();
        services.AddScoped<RecordSubmissionExceptionHandler>();

        // Missing Work Dynamic Projection & Student Workspace feature handlers (P5-S12)
        services.AddScoped<GetAssignmentSubmissionsHandler>();
        services.AddScoped<GetStudentCourseworkWorkspaceHandler>();

        // Assessment Planning & Management feature handlers (P2-S04)
        services.AddScoped<CreateAssessmentHandler>();
        services.AddScoped<UpdateAssessmentHandler>();
        services.AddScoped<ConductAssessmentHandler>();
        services.AddScoped<CancelAssessmentHandler>();

        // Assessment Inspection & Query feature handlers (P2-S05)
        services.AddScoped<GetAssessmentsByUnitHandler>();
        services.AddScoped<GetAssessmentByIdHandler>();

        // Assessment Evidence Collection feature handlers (P3-S06)
        services.AddScoped<RecordEvidenceHandler>();
        services.AddScoped<GetEvidenceByAssessmentHandler>();

        // Academic Results Evaluation, Approval, Publication & Query feature handlers (P4-S08, P4-S09, P4-S10)
        services.AddScoped<EvaluateStudentLearningHandler>();
        services.AddScoped<ApproveSummativeResultsHandler>();
        services.AddScoped<PublishAcademicResultsHandler>();
        services.AddScoped<GetResultsByAssessmentHandler>();
        services.AddScoped<GetStudentResultsWorkspaceHandler>();

        // Academic Record Corrections feature handlers (P6-S14, P5-S12, UC-ACD-15, UC-ACD-19, TD-05, TD-06)
        services.AddScoped<CorrectApprovedResultHandler>();
        services.AddScoped<GetResultCorrectionHistoryHandler>();
        services.AddScoped<CorrectAcademicStandingHandler>();
        services.AddScoped<CorrectPromotionRecommendationHandler>();

        // Report Cards Compilation, Snapshot Freezing, Publishing & Query handlers (P5-S11, P5-S12, UC-ACD-13, TD-05, TD-07)
        services.AddScoped<CompileReportCardHandler>();
        services.AddScoped<BatchCompileCohortReportCardsHandler>();
        services.AddScoped<ApproveReportCardHandler>();
        services.AddScoped<PublishReportCardHandler>();
        services.AddScoped<GetReportCardByIdHandler>();
        services.AddScoped<GetStudentReportCardsHandler>();

        // Cumulative Interim Transcripts feature handlers (P5-S13, UC-ACD-13, TD-05)
        services.AddScoped<GenerateInterimTranscriptHandler>();
        services.AddScoped<IssueTranscriptHandler>();
        services.AddScoped<GetStudentTranscriptHandler>();
        services.AddScoped<GetTranscriptByIdHandler>();

        // Learning Progress & Completion feature handlers (P2-S04, P2-S05, UC-ACD-12)
        services.AddScoped<Elsasa.Sis.Academics.Features.LearningProgress.RecordLearningProgress.RecordLearningProgressHandler>();
        services.AddScoped<Elsasa.Sis.Academics.Features.LearningProgress.ConcludeParticipation.ConcludeParticipationHandler>();
        services.AddScoped<Elsasa.Sis.Academics.Features.LearningProgress.GetStudentLearningProgress.GetStudentLearningProgressHandler>();
        services.AddScoped<Elsasa.Sis.Academics.Features.LearningProgress.GetUnitLearningProgress.GetUnitLearningProgressHandler>();

        // Academic Achievements feature handlers (P4-S11, UC-ACD-21)
        services.AddScoped<ConferAcademicAchievementHandler>();
        services.AddScoped<GetStudentAchievementsHandler>();
        services.AddScoped<GetPeriodAchievementsHandler>();

        // Cohort Academic Review & Threshold Evaluation feature handlers (P3-S06, TD-03, UC-ACD-17)
        services.AddSingleton<ThresholdRuleEvaluator>();
        services.AddScoped<EvaluateCohortStandingHandler>();
        services.AddScoped<EvaluateCohortProgressionHandler>();

        // Academic Standings feature handlers (P3-S07, P3-S09, UC-ACD-17, TD-03)
        services.AddScoped<DetermineAcademicStandingHandler>();
        services.AddScoped<BatchDetermineCohortStandingHandler>();
        services.AddScoped<GetStudentStandingsHandler>();
        services.AddScoped<GetCohortStandingsHandler>();

        // Promotion Recommendations feature handlers (P3-S08, P3-S09, UC-ACD-17, TD-03)
        services.AddScoped<Elsasa.Sis.Academics.Features.PromotionRecommendations.IssuePromotionRecommendation.IssuePromotionRecommendationHandler>();
        services.AddScoped<Elsasa.Sis.Academics.Features.PromotionRecommendations.BatchIssuePromotionRecommendations.BatchIssuePromotionRecommendationsHandler>();
        services.AddScoped<GetStudentPromotionRecommendationsHandler>();
        services.AddScoped<GetCohortPromotionRecommendationsHandler>();

        // Academic Interventions & At-Risk Query Engine feature handlers (P4-S10, UC-ACD-11, TD-04)
        services.AddScoped<GetAtRiskLearnersHandler>();
        services.AddScoped<InitiateAcademicInterventionHandler>();
        services.AddScoped<UpdateInterventionStatusHandler>();
        services.AddScoped<GetActiveInterventionsHandler>();
        services.AddScoped<GetInterventionByIdHandler>();

        // Demo Data Purger & Structure Seeder (P1-S03, ADR-002, ADR-004, TD-02, TD-05)
        // Register purger against the shared IDemoDataDomainPurger contract so the
        // host DemoDataOrchestrator can resolve it (ADR-001, ADR-002).
        services.AddScoped<AcademicsDemoDataPurger>();
        services.AddScoped<IAcademicsDemoDataPurger, AcademicsDemoDataPurger>();
        services.AddScoped<IDemoDataDomainPurger, AcademicsDemoDataPurger>();
        services.AddScoped<IAcademicsStructureDemoDataSeeder, AcademicsStructureDemoDataSeeder>();
        services.AddScoped<IAcademicsActivitiesDemoDataSeeder, AcademicsActivitiesDemoDataSeeder>();

        // Cross-domain query & cloning services (P2-S04, P2-S05)
        services.AddScoped<IAcademicsModule, AcademicsModule>();
        services.AddScoped<ICurriculumCloningService, CurriculumCloningService>();

        // Assessment Reassessment Policy Evaluator domain service (TD-03, P3-S07)
        services.AddSingleton<IReassessmentPolicyEvaluator, ReassessmentPolicyEvaluator>();

        // FluentValidation validators in Academics assembly
        services.AddValidatorsFromAssemblyContaining<AcademicsAssemblyMarker>();

        // Configure policy-based authorization requirements (ADR-009, TD-07)
        services.AddAuthorization(options =>
        {
            void AddPermissionPolicy(string permission, params string[] roles)
            {
                options.AddPolicy(permission, policy => policy.RequireAssertion(ctx =>
                    ctx.User.Identity?.IsAuthenticated == true && (
                        ctx.User.HasClaim(c => (c.Type == "permission" || c.Type == "permissions") && (c.Value == permission || c.Value == "*"))
                        || ctx.User.IsInRole("Admin")
                        || roles.Any(r => ctx.User.IsInRole(r)))));
            }

            // Curriculum permissions (ADR-009, IS008 Section 9.1)
            // Academic Coordinator: View, Manage, Submit
            // Academic Authority: View, Approve, Archive (and Manage/Supersede)
            // Admin: All
            AddPermissionPolicy(
                Permissions.Academics.Curriculum.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Curriculum.Manage,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Curriculum.Submit,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Curriculum.Approve,
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Curriculum.Archive,
                "AcademicAuthority", "Academic Authority");

            // Participation permissions (ADR-009, TD-09, IS009 Section 9)
            // Academic Coordinator & Academic Authority: Full access
            // Student: View own participations
            AddPermissionPolicy(
                Permissions.Academics.Participations.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Student");

            AddPermissionPolicy(
                Permissions.Academics.Participations.Manage,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Participations.RecognizePrior,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Participations.Conclude,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            // Activity permissions (ADR-009, TD-09)
            // Academic Coordinator & Academic Authority: Manage, Conduct, View
            // Teacher: Conduct, View
            // Student: View
            AddPermissionPolicy(
                Permissions.Academics.Activities.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher",
                "Student");

            AddPermissionPolicy(
                Permissions.Academics.Activities.Manage,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Activities.Conduct,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher");

            // Attendance permissions (ADR-009, TD-09)
            // Academic Coordinator & Academic Authority: View, Record, Correct
            // Teacher: Record, Correct, View
            // Student: View
            AddPermissionPolicy(
                Permissions.Academics.Attendance.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher",
                "Student");

            AddPermissionPolicy(
                Permissions.Academics.Attendance.Record,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher");

            AddPermissionPolicy(
                Permissions.Academics.Attendance.Correct,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher");

            // Coursework Assignment permissions (ADR-009, TD-09)
            // Academic Coordinator & Academic Authority: Manage, Release, Close, View
            // Teacher: Manage, Release, Close, View
            // Student: View
            AddPermissionPolicy(
                Permissions.Academics.Assignments.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher",
                "Student");

            AddPermissionPolicy(
                Permissions.Academics.Assignments.Manage,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher");

            AddPermissionPolicy(
                Permissions.Academics.Assignments.Release,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher");

            AddPermissionPolicy(
                Permissions.Academics.Assignments.Close,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher");

            // Coursework Submission permissions (ADR-009, TD-09)
            // Student: Submit, View
            // Teacher: Exception, View
            // Academic Coordinator & Academic Authority: Exception, View
            AddPermissionPolicy(
                Permissions.Academics.Submissions.Submit,
                "Student");

            AddPermissionPolicy(
                Permissions.Academics.Submissions.Exception,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher");

            AddPermissionPolicy(
                Permissions.Academics.Submissions.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher",
                "Student");

            // Assessment permissions (ADR-009, TD-09, IS010 Section 9.1)
            // Academic Coordinator & Academic Authority: View, Manage, Conduct
            // Teacher: View, Manage, Conduct
            // Student & Parent & Admission Officer: View
            AddPermissionPolicy(
                Permissions.Academics.Assessments.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher",
                "Student",
                "Parent",
                "AdmissionOfficer", "Admission Officer");

            AddPermissionPolicy(
                Permissions.Academics.Assessments.Manage,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher");

            AddPermissionPolicy(
                Permissions.Academics.Assessments.Conduct,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher");

            // Evidence collection permissions (ADR-009, TD-09, IS010 Section 9.1)
            // Teacher, Academic Coordinator & Academic Authority: Record, View
            AddPermissionPolicy(
                Permissions.Academics.Evidence.Record,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher");

            AddPermissionPolicy(
                Permissions.Academics.Evidence.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher");

            // Academic Results governance permissions (ADR-009, TD-09, IS010 Section 9.1)
            // Teacher: Evaluate
            // Academic Coordinator: Evaluate
            // Academic Authority: Evaluate, Approve, Publish
            // View: Academic Coordinator, Academic Authority, Teacher, Student, Parent, Admission Officer
            AddPermissionPolicy(
                Permissions.Academics.Results.Evaluate,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher");

            AddPermissionPolicy(
                Permissions.Academics.Results.Approve,
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Results.Publish,
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Results.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher",
                "Student",
                "Parent",
                "AdmissionOfficer", "Admission Officer");

            // Report Cards production permissions (ADR-009, TD-09, IS010 Section 9.1)
            // Academic Coordinator: Compile, View
            // Academic Authority: Compile, Approve, Publish, View
            // Teacher, Student, Parent: View
            AddPermissionPolicy(
                Permissions.Academics.ReportCards.Compile,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.ReportCards.Approve,
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.ReportCards.Publish,
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.ReportCards.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher",
                "Student",
                "Parent");

            // Cumulative Transcripts permissions (ADR-009, TD-09, IS010 Section 9.1)
            // Academic Coordinator: Generate, View
            // Academic Authority: Generate, Issue, View
            // Teacher, Student, Parent: View
            AddPermissionPolicy(
                Permissions.Academics.Transcripts.Generate,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Transcripts.Issue,
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Transcripts.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher",
                "Student",
                "Parent");

            // Academic Record Corrections permissions (ADR-009, TD-09, IS010 Section 9.1)
            // Academic Authority: Correct, ViewHistory
            // Academic Coordinator: ViewHistory
            AddPermissionPolicy(
                Permissions.Academics.Corrections.Correct,
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Corrections.ViewHistory,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            // Learning Progress permissions (ADR-009, TD-02, TD-07, IS012)
            // Academic Coordinator: View, Record, Conclude
            // Academic Authority: View, Record, Conclude
            // Teacher: View
            // Student: View
            AddPermissionPolicy(
                Permissions.Academics.Progress.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher",
                "Student");

            AddPermissionPolicy(
                Permissions.Academics.Progress.Record,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Progress.Conclude,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            // Academic Standings permissions (ADR-009, TD-03, IS012)
            // Academic Authority: View, Evaluate, Determine, BatchDetermine
            // Academic Coordinator: View, Evaluate
            // Teacher: View
            // StudentAdministrationOfficer: View
            AddPermissionPolicy(
                Permissions.Academics.Standings.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher",
                "StudentAdminOfficer", "Student Admin Officer",
                "StudentAdministrationOfficer", "Student Administration Officer");

            AddPermissionPolicy(
                Permissions.Academics.Standings.Evaluate,
                "AcademicAuthority", "Academic Authority",
                "AcademicCoordinator", "Academic Coordinator");

            AddPermissionPolicy(
                Permissions.Academics.Standings.Determine,
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Standings.BatchDetermine,
                "AcademicAuthority", "Academic Authority");

            // Promotion Recommendations permissions (ADR-009, TD-03, IS012)
            // Academic Authority: View, Evaluate, Issue, BatchIssue
            // Academic Coordinator: View, Evaluate
            // Teacher: View
            // StudentAdministrationOfficer: View
            AddPermissionPolicy(
                Permissions.Academics.Progression.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher",
                "StudentAdminOfficer", "Student Admin Officer",
                "StudentAdministrationOfficer", "Student Administration Officer");

            AddPermissionPolicy(
                Permissions.Academics.Progression.Evaluate,
                "AcademicAuthority", "Academic Authority",
                "AcademicCoordinator", "Academic Coordinator");

            AddPermissionPolicy(
                Permissions.Academics.Progression.Issue,
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Progression.BatchIssue,
                "AcademicAuthority", "Academic Authority");

            // Academic Interventions permissions (ADR-009, TD-04, IS012)
            // Academic Coordinator: View, Initiate, UpdateStatus
            // Academic Authority: View, Initiate, UpdateStatus
            // Teacher: View, Initiate (educator referral)
            AddPermissionPolicy(
                Permissions.Academics.Interventions.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher");

            AddPermissionPolicy(
                Permissions.Academics.Interventions.Initiate,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher");

            AddPermissionPolicy(
                Permissions.Academics.Interventions.UpdateStatus,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            // Academic Achievements permissions (ADR-009, IS012)
            // Academic Authority: View, Confer
            // Academic Coordinator: View
            // Teacher, Student, Parent, AdmissionOfficer: View
            AddPermissionPolicy(
                Permissions.Academics.Achievements.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority",
                "Teacher",
                "Student",
                "Parent",
                "AdmissionOfficer", "Admission Officer");

            AddPermissionPolicy(
                Permissions.Academics.Achievements.Confer,
                "AcademicAuthority", "Academic Authority");

            // Non-Grade Record Corrections permissions (ADR-009, TD-05, IS012)
            // Academic Authority: CorrectStanding, CorrectProgression
            AddPermissionPolicy(
                Permissions.Academics.Corrections.CorrectStanding,
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Corrections.CorrectProgression,
                "AcademicAuthority", "Academic Authority");
        });

        return services;
    }
}
