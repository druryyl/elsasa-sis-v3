# ISSUE

## Metadata

ID: CR-SYS-SAMPLE-DATA-GENERATOR
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Business-Compliant Sample Data Generator for Presentation and Demonstration

## Source

Reported By: Drury
Reported Date: 2026-11-05

## Description

Implement a comprehensive sample data generation capability for the Elsasa School Information System (SIS) to support product demonstrations, stakeholder presentations, user training, and realistic functional verification.

The sample data generator must produce coherent, realistic, and domain-rich datasets that strictly conform to all established business rules, domain invariants, and state-lifecycle transitions across all implemented SIS domains:

1. **Operations Domain Context:**
   - Active, historical, and future academic years and operational terms/semesters.
   - Operational calendar milestones, statutory holidays, instructional days, and scheduled/emergency closure records.
   - Active operational windows (e.g., student admission windows, enrollment periods, assessment periods).

2. **Students Domain Context:**
   - Realistic applicant profiles and admission applications across various lifecycle states (`draft`, `submitted`, `under_review`, `accepted`, `rejected`, `withdrawn`).
   - Enrolled students with complete student identity profiles, national student identifiers (NISN/NIS), demographic data, and linked parent/guardian contact information.
   - Class section cohort placements, student status histories (`active`, `on_leave`, `transferred`, `graduated`), and placement tracking.

3. **Finance Domain Context:**
   - Fee policies, fee structures, component definitions (tuition, registration, facility), and payment schedule plans.
   - Student billing obligations, generated periodic invoices, and fee adjustments/waivers.
   - Payment transactions across various settlement statuses (`unpaid`, `partially_paid`, `paid`, `overdue`), payment receipts, and balance allocations against specific invoice items.

4. **Academics Domain Context:**
   - Curriculum frameworks, subject courses, learning units, and instructional syllabus structures.
   - Academic class cohorts, subject enrollments/participations, and teacher instructional assignments.
   - Daily instructional sessions, attendance records with varied statuses (`present`, `absent`, `late`, `excused`), and coursework assignments with student submissions.
   - Formative and summative assessment events with scoring specifications, evaluation criteria/rubrics, provisional teacher scorings, moderated and approved academic results, and compiled periodic report cards (*Buku Rapor*) and interim transcripts.

5. **Operational Consistency & Scenario Realism:**
   - The generated data must reflect plausible school scenarios (e.g., top-performing students, average students, struggling students requiring remediation, diverse family financial payment behaviors, realistic teacher grading distributions).
   - Referential integrity and temporal ordering must be preserved (e.g., payment dates after invoice dates, submission dates within coursework window, assessment dates aligned with term calendar).

## Desired Outcome

1. Stakeholders and presenters can execute end-to-end demonstrations of Elsasa SIS across administrative workbenches and self-service portals with rich, realistic, and interconnected data out-of-the-box.
2. All generated entities strictly comply with domain rules, validation constraints, and lifecycle workflows enforced by the application core (zero orphaned records, zero invalid state transitions, zero inconsistent financial or grading balances).
3. The data generator provides configurable or repeatable generation capability, allowing environments to be cleanly initialized, populated, or refreshed on demand for demonstration and testing purposes.
4. Product evaluators can inspect meaningful dashboards, reports, student profiles, financial ledgers, and academic report cards without requiring manual, error-prone data entry.

## Current Situation

1. The Elsasa SIS application currently possesses rich database schemas and business logic across Operations, Students, Finance, and Academics (covering Stage 1 and Stage 2 scopes), but lacks an automated, standardized demo data generation facility.
2. Presenting system capabilities currently requires either manual data entry via UI forms or isolated unit/integration test data fixtures that do not persist a complete, multi-domain cohesive school dataset.
3. Manual test data creation is labor-intensive, error-prone, and frequently leads to inconsistent states (e.g., missing parent links, mismatched calendar dates, or unbalanced billing records) that undermine product demonstrations.

## Evidence

- User Request: Formal request for a data generator to produce sample data for Elsasa SIS presentation and demonstration conforming to existing business rules.
- Operational Domain Blueprint: `docs/domains/operational-domain.md`
- Student Domain Blueprint: `docs/domains/student-domain.md`
- Finance Domain Blueprint: `docs/domains/finance-domain.md`
- Academic Domain Blueprint: `docs/domains/academic-domain.md`
- Database Schemas & Migrations: `src/backend/Elsasa.Sis.Database/Scripts/` (covering Operations, Students, Finance, and Academics schema definitions).
- System Use Cases & Workflows: `docs/use-cases.md`, `docs/operational-workflow-discovery.md`, `docs/screen-inventory.md`.
- Implemented Issues Baseline: `IS001` through `IS010` in `docs/issues/`.

## Notes

- This artifact functions strictly as an intake specification (WHAT capability is needed) and remains solution-neutral.
- It intentionally does not prescribe technical implementation details (e.g., CLI command, MediatR command handler, API endpoint, Bogus-based generator, database seed script, or external utility).
- Technical feasibility, architectural placement, scenario definitions, and implementation planning will be conducted in subsequent SDLC stages.
