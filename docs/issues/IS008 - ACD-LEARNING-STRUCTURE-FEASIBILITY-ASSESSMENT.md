---
Title: Feasibility Assessment — Stage 2 Learning Structure & Requirements Full Scope
Code: CR-ACD-LEARNING-STRUCTURE-REQUIREMENTS
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-04
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Implementation of the full scope of Stage 2 "Learning Structure & Requirements" under the Academic domain as specified in [IS008 - ACD-LEARNING-STRUCTURE-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS008%20-%20ACD-LEARNING-STRUCTURE-ISSUE.md) and the SIS implementation roadmap ([sub-modul-and-development-phases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md)).

Referenced artifacts:
- **DOMAIN:** [academic-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/academic-domain.md)
- **FEATURE:** Learning Structure & Requirements Core (`ACD-WF-01`, `UC-ACD-08`, `SCR-ACD-03`)

## Objective

Evaluate the technical feasibility, architectural consistency, codebase readiness, and structural impacts of implementing Stage 2 Learning Structure & Requirements:
1. **Learning Structure Management (`ACD-WF-01`, `UC-ACD-08`):** Provision and maintain top-level curriculum framework containers (`learning_structures`) that group learning units for operational academic periods, enforcing effective dating (`effective_date`, `end_date`), governance approval (`approved_by`, `approved_date`), lifecycle states (`draft`, `active`, `superseded`, `archived`), and version lineage (`supersedes_id`).
2. **Curriculum Configuration & Learning Unit Abstraction (`ACD-WF-01`, `UC-ACD-08`):** Implement the single curriculum-neutral abstraction `Learning Unit` (`learning_units`) unifying subjects, courses, modules, PACEs, projects, and competencies without variant domain tables, supporting 2-level hierarchical parent-child composition via `parent_unit_id`, scoped unique codes, credit weights, and status lifecycles.
3. **Academic Requirements & Policy Versioning (`ACD-WF-01`, `UC-ACD-08`):** Model, version, and approve academic requirement rules (`academic_requirements`) defining expectations, prerequisites, completion criteria, credit quotas, grading/assessment policies, and progression/graduation requirements across scopes: `program`, `level`, or `learning_unit`.
4. **Curriculum Planning Workspace (`SCR-ACD-03`):** Deliver the Vue 3 planning workbench allowing Academic Coordinators to draft and organize curriculum frameworks, and Academic Authorities to review and formally approve/activate them.

Deliver the assessment strictly adhering to the **Pragmatic**, **Minimalist**, and **Explicit** architectural principles, preserving domain boundaries, compile-time assembly isolation, and the established vertical slice conventions of Elsasa SIS V3.

---

# 2. Current State

## Existing Behavior

1. **Database Schema:**
   - The database contains provisioned tables for Operations (`operations.academic_years`, `operations.operational_periods`, `operations.school_calendars`, `operations.school_closures`, `operations.calendar_amendments`), Students (`students.applicants`, `students.admission_applications`, `students.students`, `students.student_placements`, `students.student_statuses`), and Finance (`finance.financial_policies`, `finance.charge_rules`, `finance.student_financial_accounts`, `finance.financial_obligations`, `finance.payers`, `finance.payments`, `finance.payment_allocations`).
   - The `academics` schema is **not yet created** in SQL Server.
   - The tables specified for Submodule 1 in [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md) (`academics.learning_structures`, `academics.learning_units`, `academics.academic_requirements`) are **not provisioned**.
2. **Backend Domain & Services:**
   - `src/backend/` contains assemblies for `Elsasa.Sis.Operations`, `Elsasa.Sis.Students`, and `Elsasa.Sis.Finance`.
   - There is **no `Elsasa.Sis.Academics` project** in `src/backend/`, and the solution file `Elsasa.Sis.sln` contains no Academic domain references.
   - `Elsasa.Sis.Contracts` does not have an `Academics` folder or an `IAcademicsModule` interface.
   - Architecture test suite `Elsasa.Sis.ArchitectureTests` verifies compile-time isolation for Operations, Students, and Finance, but has no tests for Academics.
   - Upstream service `IOperationsCalendarQueryService` is fully functional and exposed in `Elsasa.Sis.Contracts/Operations`, enabling lookup of active academic years and operational periods.
3. **Frontend Implementation:**
   - `src/frontend/src/features/` contains `auth`, `dashboard`, `finance`, `operations`, `portal`, and `students`.
   - There is **no `academics` feature module** in the frontend.
   - `SCR-ACD-03` (`Curriculum & Learning Structure Workspace`) is not implemented.
   - Main navigation in `AppLayout.vue` and route definitions in `routes.ts` do not contain any Academic planning navigation links or route records.

## Existing Constraints

1. **Domain Isolation & ADR Compliance (ADR-001, ADR-002, ADR-010):**
   - The new Academic domain assembly must reference only `Elsasa.Sis.Shared` and `Elsasa.Sis.Contracts`. It must never reference `Students`, `Finance`, or `Operations` class libraries directly.
   - SQL Server schema isolation: `academics` schema owns its tables. Physical cross-schema foreign keys are strictly prohibited; external references to `academic_period_id` (from Operations) are stored as logical `UUID` (`uniqueidentifier`) values.
2. **Curriculum-Neutral Abstraction (academic-domain.md, academic-entities.md):**
   - Subjects, courses, modules, PACEs, units, and competencies must not be modeled as separate database tables. They must be stored in the single `learning_units` table with discriminator `unit_type`.
3. **Historical Immutability & Traceability:**
   - Approved curriculum frameworks governing past or active academic periods must never be silently modified or overwritten.
   - Any structural curriculum evolution requires formal versioning (`version`, `effective_date`, `supersedes_id`) and status progression (`draft` -> `active` -> `superseded` / `archived`).
4. **Segregation of Duties & Actor Authorities (actors.md):**
   - `Academic Coordinator`: Creates and edits learning structures, learning units, and requirements while in `draft` status; submits structures for approval.
   - `Academic Authority`: Possesses sole authority to approve and activate (`draft` -> `active`), supersede, or archive learning structures and academic requirements.
5. **Stage 2 Completion Gate:**
   - Meets Stage 2 Criterion 1: *"A learning structure with at least one learning unit and academic requirements is approved and effective."*

---

# 3. Gap Analysis

| ID | Severity | Gap | Status | Resolution |
|------|------|------|:---:|------|
| `GAP-001` | CRITICAL | **Missing Academics SQL Schema & Tables:** The database has no `academics` schema and lacks tables `academics.learning_structures`, `academics.learning_units`, and `academics.academic_requirements`. | **CLOSED** | Author DbUp migration script `20261104_0401_Academics_LearningStructure_Tables.sql` with idempotent schema creation and table DDL matching `academic-entities-tables.md`. |
| `GAP-002` | CRITICAL | **Missing Backend Domain Project (`Elsasa.Sis.Academics`):** Solution lacks an Academics domain class library, dependency injection registration, and API endpoint discovery. | **CLOSED** | Scaffold `Elsasa.Sis.Academics.csproj` referencing `Shared` and `Contracts`, wire into `Elsasa.Sis.sln` and `Elsasa.Sis.Api`, and add NetArchTest compile-time isolation rules. |
| `GAP-003` | CRITICAL | **Curriculum Framework Versioning & Lineage Mechanics:** Lack of an explicit mechanism for creating successor curriculum versions without forcing coordinators to re-type existing units/requirements. | **CLOSED** | Adopted **Deep Clone-on-Supersede into Draft**: creating a new version via `supersedes_id` automatically clones predecessor units and requirements into draft for easy modification. |
| `GAP-004` | MAJOR | **Learning Unit Hierarchy & Cycle Prevention:** Need an explicit, lightweight constraint mechanism for `parent_unit_id` composition (e.g. course-to-module) that avoids recursive graph cycles. | **CLOSED** | Adopted **2-Level Hierarchy Cap (Parent → Child only)**: root units (`parent_unit_id = null`) may have child units, but child units cannot have nested sub-children, eliminating recursive CTEs and cycle risks. |
| `GAP-005` | MAJOR | **Academic Requirements Rule Evaluation Scope:** Determining whether requirements are evaluated via a complex dynamic JSON rule engine or categorized descriptive policies. | **CLOSED** | Adopted **Categorized Metadata with Explicit Description (Human-Governed Evaluation)**: follow documented schema (`requirement_scope`, `requirement_type`, `description`) for Stage 2 coordinator-supervised reviews. |
| `GAP-006` | MAJOR | **Active Structure Mutability Governance:** Defining what changes can occur to an `active` structure versus what requires creating a new version. | **CLOSED** | Adopted **Controlled In-Place Edit with Historical Immutability Guard**: non-breaking metadata (description, display name, notes) is editable with audit logging; structural changes (code, type, parentage, deletion) are locked once active. |
| `GAP-007` | MAJOR | **Missing Frontend Workspace (`SCR-ACD-03`):** No curriculum planning screen exists in `src/frontend/src/features/`. | **CLOSED** | Build `src/frontend/src/features/academics/curriculum` as a Unified Planning Workspace with Top Context Bar & 3 Cohesive Tabs (Overview, Learning Units, Academic Requirements). |
| `GAP-008` | MINOR | **Cross-Domain Contract & Event Definition:** Downstream Stage 2 modules require synchronous query access and event notifications for active curriculum. | **CLOSED** | Define `IAcademicsModule` and `LearningStructureActivatedEvent` in `Elsasa.Sis.Contracts/Academics`. |

---

# 4. Open Questions

| ID | Question | Impact | Status | Decision Summary |
|------|------|------|:---:|------|
| `OQ-001` | How should Curriculum Versioning and Lifecycle Governance handle learning units and requirements when a Learning Structure is superseded? | Directly dictates table schema, cloning handlers, operator workload, and lineage tracking. | **CLOSED** | **Deep Clone-on-Supersede into Draft:** System clones existing units and requirements into the new draft structure. Upon activating the new version, the predecessor is set to `superseded` with `end_date` set. |
| `OQ-002` | How should Learning Unit Hierarchical Composition (`parent_unit_id`) and nesting depth be governed? | Dictates query complexity, data validation, UI tree rendering, and cycle detection. | **CLOSED** | **2-Level Hierarchy Cap (Parent → Child):** Root units can have child units; children cannot have further sub-children. Enforced on creation/update (`parent.parent_unit_id == null`). |
| `OQ-003` | How should Academic Requirements rules and evaluation mechanics be modeled for Stage 2? | Dictates requirement table complexity, JSON parsing overhead, and evaluation logic. | **CLOSED** | **Categorized Metadata with Explicit Description:** Follow documented schema (`requirement_scope`, `requirement_type`, `description`). Governs human review in Stage 2 without a premature JSON DSL parser. |
| `OQ-004` | How should `SCR-ACD-03` (Curriculum & Learning Structure Workspace) be structured in the frontend? | Determines user navigation, component modularity, and operational ergonomics. | **CLOSED** | **Unified Planning Workspace with Top Context Bar & 3 Cohesive Tabs:** Academic Period and Structure selector header, lifecycle status badges, and 3 tabs: 1) Overview & Lineage, 2) Learning Units tree/table, 3) Academic Requirements. |
| `OQ-005` | How should mutability of Learning Structures, Learning Units, and Requirements be governed once a structure is in 'active' status? | Balances operational flexibility (fixing typos) with academic historical integrity. | **CLOSED** | **Controlled In-Place Edit with Historical Immutability Guard:** Non-breaking metadata (description, display name, notes) is editable with audit logging. Structural changes (codes, unit types, parentage, deletions) are locked once active. |

---

# 5. Assumptions

| ID | Assumption |
|------|------|
| `ASM-001` | The database engine is SQL Server 2022+ supporting schema creation, raw T-SQL migrations via DbUp, and UUID logical foreign keys. |
| `ASM-002` | Operational periods (`academic_period_id`) are configured and queryable via `IOperationsCalendarQueryService` before a learning structure is created. |
| `ASM-003` | Unit codes (`code`) are unique within a given `learning_structure_id`, enforced by a database unique constraint `UQ_learning_units_structure_code`. |
| `ASM-004` | A Learning Structure cannot be approved and activated unless it contains at least one Learning Unit and at least one Academic Requirement (Stage 2 Criterion 1). |
| `ASM-005` | All user identity tracking (`created_by`, `approved_by`) utilizes username/email strings from the authenticated JWT token via `ICurrentUser`. |
| `ASM-006` | Child units (`parent_unit_id IS NOT NULL`) must belong to the exact same `learning_structure_id` as their parent unit. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|------|------|------|------|
| `RISK-001` | Accidental circular references in learning unit hierarchy causing infinite recursion in UI or queries. | High | Enforce **2-Level Hierarchy Cap** (`parent.parent_unit_id == null`) and validate `parent_unit_id != id` in both command validators and Dapper handlers. |
| `RISK-002` | Silent modification of an active curriculum breaking downstream enrollments, assessments, or historical transcripts. | High | Enforce strict immutability locks on structural fields (`code`, `unit_type`, `parent_unit_id`, `learning_structure_id`, deletions) when status is `active`. |
| `RISK-003` | Coordinator administrative fatigue when creating annual curriculum updates. | Medium | Implement **Deep Clone-on-Supersede** so a new version inherits all units and requirements in draft, requiring only incremental edits. |
| `RISK-004` | Inconsistent temporal dating between learning structure effective dates and Operations academic period dates. | Medium | Query `IOperationsCalendarQueryService` to default `effective_date` to the operational period `StartDate` and validate boundaries. |
| `RISK-005` | Codebase architecture regression from unauthorized references across domain boundaries. | High | Add `Elsasa.Sis.Academics` to `Elsasa.Sis.ArchitectureTests` verifying compile-time assembly isolation against sibling domains. |

---

# 7. Recommendations

## Curriculum Versioning Architecture (GAP-003 / OQ-001)

### Option A: Deep Clone-on-Supersede into Draft — Recommended
- When creating a new version of an active structure via `POST /api/v1/academics/learning-structures/{id}/supersede`:
  1. A new `learning_structures` record is created with `status = 'draft'`, `supersedes_id = {predecessor_id}`, and incremented `version` (e.g. "2025.1").
  2. All `learning_units` of the predecessor are cloned into the new structure (generating new `id`s, remapping `parent_unit_id` to cloned parent IDs, and resetting unit status to `draft`).
  3. All `academic_requirements` of the predecessor are cloned into the new structure in `draft` status with `supersedes_id` referencing predecessor requirements.
  4. The coordinator edits the cloned draft units/requirements without altering the active predecessor.
  5. When the new structure is approved (`POST /api/v1/academics/learning-structures/{id}/approve`), the predecessor is automatically updated to `status = 'superseded'` and `end_date = @NewStructureEffectiveDate`.

#### Advantages
- **Pragmatic & Highly Productive:** Reduces annual curriculum rollout time from hours of re-entry to minutes of review.
- **Explicit & Immutable:** Historical predecessor remains 100% intact, untouched, and fully queryable for past student transcripts.
- **No Complex Junction Tables:** Preserves simple, clean table schema where `learning_units.learning_structure_id` is a direct FK.

#### Disadvantages
- Deep clone requires transactional re-mapping of parent-child `learning_unit` IDs during cloning. (Handled cleanly with a dictionary lookup in a single command handler transaction).

---

### Option B: Manual Blank Slate with Explicit Lineage
- Creating a new version creates an empty draft structure with `supersedes_id` set, requiring manual entry of all units and requirements.

#### Advantages
- Minimalist backend cloning logic.

#### Disadvantages
- Severe operator friction: schools have hundreds of subject units; manual re-entry invites human error.

---

## Learning Unit Hierarchy Architecture (GAP-004 / OQ-002)

### Option A: 2-Level Hierarchy Cap (Parent → Child) — Recommended
- Constrain hierarchy to at most 2 levels:
  - Root Units (`parent_unit_id IS NULL`): E.g., `subject`, `course`, `project`.
  - Child Units (`parent_unit_id IS NOT NULL`): E.g., `module`, `competency`, `pace`, `unit`.
  - Rule: A child unit **cannot** have `parent_unit_id` pointing to a unit that itself has a `parent_unit_id`.
  - Rule: `parent_unit_id` must belong to the same `learning_structure_id`.

#### Advantages
- **Pragmatic & Explicit:** Covers all primary educational curriculum models (Subjects with Modules, Courses with Units, Subjects with Competencies).
- **Zero Cycle Risk:** Graph cycles are mathematically impossible under a 2-level cap.
- **Clean UI & Queries:** Simple Dapper query without recursive CTEs; clean tree/table rendering with single indent.

#### Disadvantages
- Does not permit arbitrary 5-level deep taxonomy trees (which are unnecessary and actively discouraged for Phase 2).

---

## Academic Requirements Representation (GAP-005 / OQ-003)

### Option A: Categorized Metadata with Explicit Description — Recommended
- Align directly with the table design in `academic-entities-tables.md`:
  - `requirement_scope`: `program`, `level`, `learning_unit`.
  - `requirement_type`: `completion`, `prerequisite`, `credit`, `progression`, `graduation`, `assessment_policy`.
  - `description`: Text clearly detailing the requirement policy.
  - Linked entities: `learning_structure_id`, `learning_unit_id` (when scope is `learning_unit`), `level` (when scope is `level`).

#### Advantages
- **Minimalist & Robust:** No complex DSL, syntax parsers, or runtime AST evaluators.
- **Explicit Human Governance:** Aligns with Stage 2 workflows where coordinators and authorities review student progress holistically.

#### Disadvantages
- Automated rule checking requires human interpretation or custom queries per requirement type in future stages.

---

## Screen Inventory Structure (`SCR-ACD-03`) (GAP-007 / OQ-004)

### Option A: Unified Planning Workspace with Top Context Bar & 3 Cohesive Tabs — Recommended
- A single cohesive planning workspace view:
  - **Context Bar:** Structure dropdown, Academic Period selector, Version badge, Status pill, and Action buttons (New Structure, Supersede/New Version, Submit, Approve).
  - **Tab 1: Overview & Lineage:** Name, period, version, effective dates, approval record, and historical predecessor link.
  - **Tab 2: Learning Units:** Expandable 2-level table/tree with code, name, type, parent unit, credit weight, status, and unit creation modal.
  - **Tab 3: Academic Requirements:** Grouped card/table view by scope (Program-wide, Level-specific, Unit-specific) with requirement types and version status.

#### Advantages
- **High Cohesion:** The coordinator maintains full context of the active framework while managing units and requirements.
- **Pattern Alignment:** Perfectly mirrors `SCR-OPS-01` (Calendar Workspace) and `SCR-FIN-01` (Billing Cycle Workspace).

---

# 8. Gap Closure

## GAP-001 / OQ-001: Academics Database Schema & Deep Clone-on-Supersede

### Decision
Provision SQL Server schema `academics` and tables `learning_structures`, `learning_units`, and `academic_requirements` using DbUp migration script `20261104_0401_Academics_LearningStructure_Tables.sql`. Implement **Deep Clone-on-Supersede into Draft** for curriculum versioning.

### Rationale
Preserves historical curriculum records without mutating past data, while providing seamless annual curriculum renewal without tedious manual data re-entry.

### Impact
- Migration script provisions 3 tables with audit columns and unique constraints.
- `SupersedeLearningStructureCommand` creates draft structure and clones child units/requirements transactionally.
- `ApproveLearningStructureCommand` activates new structure and sets predecessor to `superseded` with `end_date = effective_date`.

### Architecture Impact
Standardizes curriculum versioning across all academic entities.

### Resolved By
Drury & Assistant Architecture Team

### Resolved Date
2026-11-04

---

## GAP-002 / GAP-008: Backend Domain Scaffolding & Compile-Time Isolation

### Decision
Scaffold `Elsasa.Sis.Academics` as a .NET 8 class library referencing only `Shared` and `Contracts`. Add `Elsasa.Sis.Contracts/Academics` with `IAcademicsModule` and domain events. Register in `Elsasa.Sis.Api` and enforce isolation in `Elsasa.Sis.ArchitectureTests`.

### Rationale
Strictly enforces ADR-001, ADR-002, and ADR-010 modular monolith boundaries.

### Impact
- New project file `src/backend/Elsasa.Sis.Academics/Elsasa.Sis.Academics.csproj`.
- Sibling domain independence verified by NetArchTest rules.
- Fast compile times and clear service contracts.

### Architecture Impact
Establishes the foundation for all Stage 2 Academic modules.

### Resolved By
Drury & Assistant Architecture Team

### Resolved Date
2026-11-04

---

## GAP-004 / OQ-002: Learning Unit Hierarchy & Depth Constraints

### Decision
Enforce a **2-Level Hierarchy Cap (Parent → Child only)** for `learning_units`. A root unit (`parent_unit_id IS NULL`) can have child units; child units cannot be parents.

### Rationale
Satisfies the full business need for course/module and subject/competency groupings, eliminates graph cycles, and simplifies Dapper queries and UI rendering.

### Impact
- Enforced in `CreateLearningUnitCommandValidator` and `UpdateLearningUnitCommandValidator`.
- Database self-referencing foreign key on `academics.learning_units.parent_unit_id`.
- Simple SQL queries without recursive CTE overhead.

### Architecture Impact
Prevents unbounded hierarchy depth and ensures fast, deterministic query performance.

### Resolved By
Drury & Assistant Architecture Team

### Resolved Date
2026-11-04

---

## GAP-005 / OQ-003: Academic Requirements Modeling

### Decision
Model academic requirements using **Categorized Metadata with Explicit Description** matching `academic-entities-tables.md`, governed by human review in Stage 2 without a dynamic JSON rule engine.

### Rationale
Pragmatic, minimalist, and directly aligns with the phase guidelines prohibiting premature advanced planning constructs.

### Impact
- `academics.academic_requirements` table with `requirement_scope`, `requirement_type`, `description`, `version`, and status lifecycle.
- Simple CRUD and versioning vertical slices.

### Architecture Impact
Clear, auditable requirement rules that can be extended with automated criteria in later stages if needed.

### Resolved By
Drury & Assistant Architecture Team

### Resolved Date
2026-11-04

---

## GAP-006 / OQ-005: Mutability Governance for Active Structures

### Decision
Adopt **Controlled In-Place Edit with Historical Immutability Guard**. When in `active` status, non-breaking metadata (description, display name, notes) is editable with audit logging. Structural changes (codes, unit types, parentage, deletions) are locked once active and require a new version via `supersedes_id`.

### Rationale
Protects historical and active enrollments, grades, and transcripts from silent corruption while permitting routine typo corrections.

### Impact
- Update handlers verify structure status: if `active`, changes to structural fields return `DomainError.Conflict`.
- Full audit fields (`updated_at`, `updated_by`) updated on metadata edits.

### Architecture Impact
Enforces academic integrity and audit compliance across all curriculum entities.

### Resolved By
Drury & Assistant Architecture Team

### Resolved Date
2026-11-04

---

## GAP-007 / OQ-004: Frontend Architecture for `SCR-ACD-03`

### Decision
Implement `SCR-ACD-03` as a **Unified Planning Workspace with Top Context Bar & 3 Cohesive Tabs** in `src/frontend/src/features/academics/curriculum`.

### Rationale
Provides a consolidated planning experience for Academic Coordinators and Authorities, maintaining structure context across units and requirements.

### Impact
- Views: `CurriculumWorkspaceView.vue`.
- Tabs: `StructureOverviewTab.vue`, `LearningUnitsTab.vue`, `AcademicRequirementsTab.vue`.
- Modals: `CreateStructureModal.vue`, `CreateLearningUnitModal.vue`, `CreateRequirementModal.vue`.
- Routes: `/academics/curriculum` protected by permission `Permissions.Academics.Curriculum.View`.

### Architecture Impact
Establishes the Vue 3 feature structure for the Academic domain.

### Resolved By
Drury & Assistant Architecture Team

### Resolved Date
2026-11-04

---

# 9. Architecture Applicability

The Architect owns the Architecture Applicability decision, determining if a formal ARCHITECTURE artifact is required.

Allowed states:
- ARCHITECTURE-REQUIRED
- ARCHITECTURE-NOT-REQUIRED

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Implementation of Stage 2 Learning Structure & Requirements introduces:
1. A **new backend domain assembly** (`Elsasa.Sis.Academics`) and solution modifications to `Elsasa.Sis.sln`.
2. A **new SQL Server database schema** (`academics`) with 3 core tables, indexes, and unique constraints.
3. **Cross-domain contract definitions** (`IAcademicsModule`, `LearningStructureActivatedEvent`) in `Elsasa.Sis.Contracts`.
4. **New frontend feature architecture** (`src/frontend/src/features/academics/curriculum`) with route registrations, navigation items, and permission policies.
5. **Significant domain lifecycle algorithms** (deep clone-on-supersede, 2-level hierarchy validation, and active mutability locks).

A formal ARCHITECTURE artifact (`IS008 - ACD-LEARNING-STRUCTURE-ARCHITECTURE.md`) is required to define the technical realization, vertical slice catalog, DTO contracts, endpoint specifications, and integration test strategy before implementation planning.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture can be finalized or updated

## Status

**READY-FOR-PLANNING**

## Notes

All architectural decisions and open questions have been systematically aligned and resolved with the stakeholder under the **Pragmatic**, **Minimalist**, and **Explicit** philosophy. Feasibility is fully established. The Architecture skill can immediately proceed to draft `IS008 - ACD-LEARNING-STRUCTURE-ARCHITECTURE.md`.

---

# 11. References

Referenced artifacts:
- **ISSUE:** [IS008 - ACD-LEARNING-STRUCTURE-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS008%20-%20ACD-LEARNING-STRUCTURE-ISSUE.md)
- **DOMAIN:** [academic-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/academic-domain.md)
- **ENTITIES:** [academic-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities.md)
- **TABLES:** [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md)
- **ROADMAP:** [sub-modul-and-development-phases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md)
- **WORKFLOWS:** [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md)
- **USE CASES:** [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md)
- **ACTORS:** [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md)
- **SCREENS:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md)

Referenced codebase locations:
- Backend Solution: `src/backend/Elsasa.Sis.sln`
- Contracts: `src/backend/Elsasa.Sis.Contracts/`
- Operations Query Service: `src/backend/Elsasa.Sis.Contracts/Operations/IOperationsCalendarQueryService.cs`
- Database Migrations: `src/backend/Elsasa.Sis.Database/Scripts/`
- Frontend Routes: `src/frontend/src/app/router/routes.ts`
- Architecture Tests: `tests/backend/Elsasa.Sis.ArchitectureTests/`
