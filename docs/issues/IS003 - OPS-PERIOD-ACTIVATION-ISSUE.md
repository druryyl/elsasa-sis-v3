# ISSUE

## Metadata

ID: CR-OPS-PERIOD-ACTIVATION
Type: CHANGE-REQUEST
Status: OPEN
Title: Operational Period Activation and Academic Year Cascading in Schedule Management Workspace

## Source

Reported By: User
Reported Date: 2026-11-03

## Description

Enhance the Operations Calendar and Period lifecycle management so that operational periods (terms, semesters, admission windows, etc.) can be seamlessly transitioned to an active or approved state both automatically and explicitly:
1. **Operational Period Auto-Activation on Academic Year Activation:** When an Academic Year is activated, its configured operational periods should automatically transition to active or be synchronized with the academic year's active operational state.
2. **Direct Operational Period Activation / Approval in UI:** In the Schedule Management Workspace (`SCR-OPS-01`), administrators and operations authorities should be able to explicitly approve or activate individual operational periods directly from the periods hierarchy tree table via dedicated user interface actions.

## Desired Outcome

1. When an Academic Year transitions to active status, associated operational periods within that academic year are no longer left stuck in `draft` status without manual database or correction interventions.
2. Operations staff and authorities using the Schedule Management Workspace (`SCR-OPS-01` - Periods Tree Tab) have a first-class, explicit action to approve or activate draft operational periods directly in the UI.
3. Downstream domains consuming operational periods (specifically prospective student registration in the Admission Application Portal) can submit applications against configured admission windows without encountering 409 Conflict rejection errors stating that the admission period is in `draft` status.

## Current Situation

1. Operational periods created manually or generated via the 2-semester preset are persisted with `status = 'draft'`.
2. Activating an approved Academic Year (`POST /api/operations/academic-years/{id}/activate`) only updates `operations.academic_years.status` to `active` and demotes previous active years; it does not touch child records in `operations.operational_periods`.
3. In `SCR-OPS-01` (`PeriodsTreeTab.vue`), the only available row actions for operational periods are "Correct Record" (UC-OPS-13) and "Close Period" (`POST /api/operations/periods/{id}/close`). There is no UI action or standard endpoint to approve or activate an operational period.
4. When applicants submit applications via the public Admission Application Portal (`POST /api/students/admission/portal/applications`), `RegisterApplicationHandler` verifies that the target operational period has `status = 'active'` or `status = 'approved'`. Because the period remains in `draft`, submissions fail with the error: `Admission period '...' is not active or approved (current status: 'draft')`.

## Evidence

- User error message: `Application Submission Failed - Admission period 'Penerimaan Siswa Baru (SPMB)' is not active or approved (current status: 'draft').`
- Domain validation logic: `src/backend/Elsasa.Sis.Students/Features/Admission/RegisterApplication/RegisterApplicationHandler.cs` (lines 188-196).
- Academic year activation handler: `src/backend/Elsasa.Sis.Operations/Features/CalendarPeriods/AcademicYears/ActivateAcademicYear/ActivateAcademicYearHandler.cs` (lines 74-90).
- Period hierarchy UI component: `src/frontend/src/features/operations/calendar/components/tabs/PeriodsTreeTab.vue` (lines 539-575).
- Existing test workaround in integration tests: `tests/backend/Elsasa.Sis.IntegrationTests/Features/Students/Admission/ApplicationRegistrationIntegrationTests.cs` (line 51: `UPDATE operations.operational_periods SET status = 'active' WHERE id = @Id;`).

## Notes

- This artifact serves as the formal change intake capturing WHAT capability and usability improvements are requested, maintaining solution neutrality.
- Domain impact assessment, architectural specification, and implementation planning will be established in downstream SDLC stages.
