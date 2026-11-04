# ISSUE

## Metadata

ID: CR-ACD-LEARNING-STRUCTURE-REQUIREMENTS
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage-2 Learning Structure & Requirements Full Scope

## Source

Reported By: Drury
Reported Date: 2026-11-04

## Description

Implement the full scope of Stage 2 "Learning Structure & Requirements" under the Academic domain as planned in the SIS implementation roadmap (`docs/sub-modul-and-development-phases.md`).

The scope encompasses three cohesive submodules:
1. **Learning Structure Management (`ACD-WF-01`, `UC-ACD-08`):**
   - Establish and maintain top-level curriculum framework containers (`learning_structures`) that group learning units into coherent educational programs for academic periods (e.g., "Kurikulum Merdeka SD 2024", "National Primary Curriculum", "IB Primary Years").
   - Model curriculum versioning, effective dates (`effective_date`, `end_date`), approval governance (`approved_by`, `approved_date`), and status lifecycle (`draft`, `active`, `superseded`, `archived`) with explicit historical lineage (`supersedes_id`) so historical curriculum interpretations are preserved.
2. **Curriculum Configuration & Learning Unit Abstraction (`ACD-WF-01`, `UC-ACD-08`):**
   - Implement the single curriculum-neutral abstraction `Learning Unit` (`learning_units`) that unifies subjects, courses, modules, PACEs, projects, units, and competencies into one flexible structure without introducing separate top-level domains or relational tables for curriculum variants.
   - Support hierarchical parent-child composition via `parent_unit_id` (e.g., a course comprising multiple modules, or a subject containing fine-grained competency units).
   - Manage unit codes (unique within a learning structure), human-readable names, curriculum expressions (`unit_type`: `subject`, `course`, `module`, `pace`, `project`, `competency`, `unit`, `other`), level context (e.g., Grade 4), credit or weight allocations, and status lifecycles (`draft`, `active`, `archived`).
3. **Academic Requirements & Policy Versioning (`ACD-WF-01`, `UC-ACD-08`):**
   - Establish, version, and approve academic requirement rules (`academic_requirements`) defining expectations, prerequisites, completion criteria, credit quotas, grading/assessment policies, and progression/graduation requirements across defined scopes: `program`, `level`, or specific `learning_unit`.
   - Enforce versioning with effective dating (`version`, `effective_date`, `end_date`, `supersedes_id`) and status transitions (`draft`, `active`, `superseded`, `archived`) to prevent silent alteration of historical evaluation criteria.

Scope Boundaries & Deferrals:
The following Academic, Teaching, and Operational capabilities are explicitly out of scope for this issue and belong to companion or subsequent Stage 2 modules:
- Student participation and roster registration (`ACD-WF-03`, `SCR-ACD-05`) belong to `Participation & Learning Activities`.
- Learning activity execution, session delivery, and attendance tracking (`ACD-WF-04`, `ACD-WF-05`, `SCR-ACD-01`) belong to `Participation & Learning Activities`.
- Individualized Student Learning Plans (SLP) and accommodations/exemptions (`ACD-WF-02`, `SCR-ACD-04`) belong to `Student Learning Plan`.
- Coursework assignments, submissions, evaluations, grading, and assessments (`ACD-WF-06`, `ACD-WF-07`, `ACD-WF-08`, `ACD-WF-09`, `SCR-ACD-02`, `SCR-ACD-07`) belong to `Assessment & Results`.
- Academic standing determinations and progression recommendations (`ACD-WF-13`, `SCR-ACD-09`) belong to `Standing & Progression`.
- Teacher qualification, profiling, and teaching assignments (`TCH-WF-01` to `TCH-WF-12`, `SCR-TCH-01`, `SCR-TCH-02`) belong to the Teaching domain.
- Operational room allocation and timetable occurrence scheduling (`OPS-WF-02` to `OPS-WF-05`, `SCR-OPS-02`) belong to the Operational domain.

## Desired Outcome

1. Academic Coordinators can configure, validate, and organize curriculum frameworks, learning units (with hierarchical parent-child composition), and academic requirements in `SCR-ACD-03` (`Curriculum & Learning Structure Workspace`, `UC-ACD-08`, `ACD-WF-01`).
2. Academic Authorities can review, formally approve, and activate learning structures and requirement policies (`approved_by`, `approved_date`, `status: active`), or supersede older versions while preserving historical lineage (`supersedes_id`).
3. Operations Calendar & Periods (`academic_period_id`, `academic_period_label`) is referenced as authoritative external time context for learning structures.
4. Downstream Stage 2 modules can consume the established curriculum definitions as their authoritative prerequisite baseline:
   - `Participation & Learning Activities` can register students to approved learning units.
   - `Student Learning Plan` can formulate individualized adjustments against approved learning units.
   - `Teaching Assignment & Workload` can receive teaching demand derived from learning units.
   - `Assessment & Results` can evaluate student learning against published academic requirements.
   - `Scheduling & Rooms` can schedule instructional activities for active learning units.
5. Meets Stage 2 Completion Criteria:
   - **Criterion 1:** "A learning structure with at least one learning unit and academic requirements is approved and effective."

## Current Situation

1. Schema status: The database currently provisions tables for Operations, Students, and Finance domains (from Stage 1). The tables for the Academic domain (`learning_structures`, `learning_units`, `academic_requirements`) specified in `docs/entities/academic-entities-tables.md` (Submodule 1, lines 22–148) are not yet provisioned.
2. Backend status: In `src/backend`, there is no `Elsasa.Sis.Academics` project, and the solution `Elsasa.Sis.sln` does not contain any Academic domain models, contracts, entity configurations, repositories, or MediatR commands/queries for learning structures, learning units, or academic requirements.
3. Frontend status: In `src/frontend`, `src/features/` contains `auth`, `dashboard`, `finance`, `operations`, `portal`, and `students`, but does not contain an `academics` feature module. `SCR-ACD-03` (`Curriculum & Learning Structure Workspace`) is not yet implemented, and application navigation does not yet expose Academic planning tools.
4. Upstream prerequisites: Operational Calendar & Periods (`IS001`, `IS003`) is active and provides the necessary academic years and operational periods required to associate learning structures with temporal context.

## Evidence

- Roadmap Specification: `docs/sub-modul-and-development-phases.md` (Stage 2, Module: Learning Structure & Requirements — full scope; Section D Stage 2 Criterion 1; Section E Cross-Domain Dependencies).
- Academic Domain Blueprint: `docs/domains/academic-domain.md` (Section: Domain Responsibility; Section: Learning Structure; Section: Academic Requirements; Preferred hierarchy: `Learning Structure → Learning Unit → Learning Activity`).
- Academic Entities Specification: `docs/entities/academic-entities.md` (Submodule 1 — Learning Structure & Requirements: `Learning Structure`, `Learning Unit`, `Academic Requirement`, `Curriculum Version`, `Academic Period`; Entity Relationship Summary).
- Academic Database Tables Specification: `docs/entities/academic-entities-tables.md` (Submodule 1: `learning_structures`, `learning_units`, `academic_requirements`, lines 22–148).
- Operational Workflow Discovery: `docs/operational-workflow-discovery.md` (`ACD-WF-01: Define or Adopt Learning Structure`; Section E.2 Stage 2 Roadmap logic).
- Use Cases Specification: `docs/use-cases.md` (`UC-ACD-08: Define Learning Structure`).
- Screen Inventory: `docs/screen-inventory.md` (`SCR-ACD-03: Curriculum & Learning Structure Workspace`).
- Existing Baseline Code: `src/backend/Elsasa.Sis.sln`, `src/frontend/src/features/`.

## Notes

- This artifact serves as the formal change intake. It defines WHAT capability is requested and the operational outcomes expected, maintaining solution neutrality without prescribing technical architecture, database migration designs, API schemas, or UI component implementations.
- Scope boundary discipline: Curriculum framework definition, learning unit organization, and versioned academic requirements belong to this issue. Student enrollment/participation, classroom delivery, individualized learning plans (SLP), grading, teacher assignments, and timetable scheduling belong to subsequent modules and must not be conflated with this issue.
- Technical feasibility assessment, domain modeling, feature decomposition, and architecture updates will be performed in downstream SDLC stages.
