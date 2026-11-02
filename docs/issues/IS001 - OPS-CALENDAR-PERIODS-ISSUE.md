# ISSUE

## Metadata

ID: CR-OPS-CALENDAR-PERIODS
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage-1 Calendar & Periods Full Scope

## Source

Reported By: Drury
Reported Date: 2026-11-02

## Description

Implement the full scope of Stage 1 "Calendar & Periods" under the Operational domain as planned in the SIS implementation roadmap (`docs/sub-modul-and-development-phases.md`).

The scope encompasses four cohesive submodules:
1. Academic Year Management: creation, configuration, and approval/publication of academic years.
2. Operational Period Management: definition and lifecycle management of terms, semesters, quarters, and specialized operational windows (admission windows, assessment windows).
3. School Calendar Management: definition of instructional days, operational calendar milestones, and official holidays.
4. Closure Coordination: foundational calendar-level emergency and scheduled school/campus closures and date adjustments.

## Desired Outcome

1. The Operations Authority and Coordinator can configure, approve, and publish official academic years, operational periods (terms, semesters), and official school calendars with statutory holidays.
2. Published academic years and operational periods become the authoritative shared date and period context for all school domains (satisfying prerequisites for Student admission and enrollment, Academic learning structures, and Finance billing).
3. Calendar-level closure coordination is available to amend operational dates and designate non-instructional days when unscheduled closures occur.
4. Meets Stage 1 Completion Criterion 1: "An academic year with at least one term/semester and a school calendar (including holidays) is approved and published."

## Current Situation

1. Only an initial vertical slice proof-of-concept (`CreateAcademicYear`) exists in the backend (`src/backend/Elsasa.Sis.Operations/Features/CalendarPeriods/CreateAcademicYear`).
2. Comprehensive operational period management (terms, semesters, operational windows), school calendar day classification (school days, non-instructional days, holidays), and calendar closure coordination are not yet implemented.
3. Downstream Stage 1 domain capabilities (e.g., Student Admission windows, Finance charge effective dates) currently lack the authoritative operational calendar and period services/data required for their operations.

## Evidence

- Roadmap Specification: `docs/sub-modul-and-development-phases.md` (Stage 1, Module: Calendar & Periods — full scope; Section D Criterion 1).
- Operational Domain Blueprint: `docs/domains/operational-domain.md` (Section: Academic Year and Operational Period; School Calendar).
- Operational Workflow Discovery: `docs/operational-workflow-discovery.md` (`OPS-WF-01: Establish or Amend Academic Year and School Calendar`, `OPS-WF-10: Coordinate School or Campus Closure`).
- Use Cases: `docs/use-cases.md` (`UC-OPS-11: Establish School Calendar and Periods`, `UC-OPS-12: Declare Campus Closure`).
- Existing Backend POC: `src/backend/Elsasa.Sis.Operations/Features/CalendarPeriods/CreateAcademicYear` and `tests/backend/Elsasa.Sis.IntegrationTests/Operations/CreateAcademicYearTests.cs`.

## Notes

- This artifact serves as the formal change intake. It defines WHAT capability is requested and the operational outcomes expected, without prescribing technical architecture, API schemas, or UI implementations.
- Technical feasibility, domain models, feature decomposition, and architecture updates will be performed in downstream SDLC stages.
