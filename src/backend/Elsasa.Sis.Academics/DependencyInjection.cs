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
using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectApprovedResult;
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
using Elsasa.Sis.Academics.Features.LearningUnits.UpdateLearningUnit;
using Elsasa.Sis.Academics.Services;
using Elsasa.Sis.Academics.Services.Reassessment;
using Elsasa.Sis.Contracts.Academics;
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

        // Academic Record Corrections feature handlers (P6-S14, UC-ACD-19, TD-06)
        services.AddScoped<CorrectApprovedResultHandler>();
        services.AddScoped<GetResultCorrectionHistoryHandler>();

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
        });

        return services;
    }
}
