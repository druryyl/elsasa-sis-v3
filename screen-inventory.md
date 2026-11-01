# Elsasa SIS V3 — Screen Inventory

## 1. Purpose and Design Principles

### 1.1 Purpose and Scope

This document translates the authoritative [`use-cases.md`](use-cases.md) inventory into an explicit, dependency-aware, and actionable Stage-Based Screen Inventory for the Elsasa School Information System (SIS) V3.

The inventory is derived from the use cases and the architectural implementation sequence established in [`sub-modul-and-development-phases.md`](sub-modul-and-development-phases.md). It organizes all **36 operational screens** across the **three planned development stages**, establishing a clear, verified implementation scope for each stage.

In accordance with architectural principles:
- **Scope Preservation:** Every screen defined in the authoritative screen inventory is accounted for. No screens have been removed, renamed, or redesigned.
- **Stage-Based Primary Structure:** The document is organized primarily by the three development stages defined in the Development Roadmap, providing engineering teams with an unambiguous operational baseline for each phase.
- **Incremental Evolution:** Screens whose capabilities span multiple development stages retain their unique Screen ID and identity. Each stage explicitly describes the operational capabilities delivered in that stage, what is available initially, and what remains gated until prerequisite modules are implemented.
- **Boundary and Authority Integrity:** Preserves domain ownership, actor responsibilities, zero-discretionary financial approvals, confidential counselling isolation, asymmetric operational autonomy, and disciplinary due-process boundaries.
- **Complete Traceability:** Covers all **95 enumerated use cases** from `use-cases.md` across the six domains (Students, Academics, Finance, Operations, Teaching, and Development).

---

### 1.2 Design Principles

The following principles govern every screen decision in this inventory.

#### 1.2.1 Operate, Do Not Merely Record

Screens are organized around the work an actor must complete: processing pending items, verifying evidence, making authorized decisions, recording operational activity, monitoring exceptions, or following up on outstanding matters. Entity maintenance screens are used only where routine lookup or correction is itself the operational need.

#### 1.2.2 One Screen, One Working Context

Each screen has a single primary operational purpose and a clearly identifiable primary actor. Related work items may share a screen when they belong to the same working context and use the same interaction model. Unrelated responsibilities are not combined merely to reduce screen count.

#### 1.2.3 Group by Work, Not by Entity or Use Case

Screens are grouped by the actor's actual working context, not by database entity and not one-for-one with use cases. A use case may appear on more than one screen when its work genuinely spans different contexts. A screen may support several use cases when they share the same work item and decision flow.

#### 1.2.4 Progressive Disclosure

The primary work item and its primary action are always visible. Secondary information, historical detail, exceptional actions, and less frequent corrections are disclosed only when the actor needs them. The workbench shows the context required to complete the selected item, not every attribute of the underlying entity.

#### 1.2.5 Consistent Interaction Model

Screens reuse a small set of patterns:

- Work items are processed through a **worklist + workbench**.
- Conditions and exceptions are reviewed through an **overview + drill-down**.
- Existing information is found through **search/list + detail**.
- Time-based work is organized through **calendar/schedule + workspace**.
- Structured plans are built in a **planning workspace**.

Worklists, status indicators, primary actions, and contextual information occupy consistent roles across screens so that learning one screen reduces the learning cost of the next.

#### 1.2.6 Domain Ownership Is Visible

Cross-domain screens coordinate work but do not transfer decision authority. A screen may show another domain's status or result as context, but the owning domain remains responsible for its determination. Clearance, approval, correction, and lifecycle execution remain with the actors and domains defined in the source artifacts.

#### 1.2.7 Minimum Necessary Information

Each screen shows only the information needed for the current task. Financial detail is not exposed in academic or pastoral contexts beyond the minimum result required. Counselling notes remain isolated from all other workspaces. Historical and audit information is available on demand, not displayed by default.

#### 1.2.8 No Screen Without Operational Value

A screen exists only if an actor needs a working environment to complete a defined use case. Correction, approval, and monitoring are normally actions or views within the owning operational workspace rather than separate screens. No screen exists solely because an entity exists.

---

### 1.3 Screen Pattern Catalog

The inventory uses five foundational interaction patterns:

| Pattern | Structure | Used When | Screens Using It |
|---|---|---|---|
| **Work Queue** | Worklist + Workbench | Actors must process a collection of pending, actionable, or exception items. | Intake, lifecycle, exit, teaching, assessment, finance, scheduling, behaviour, discipline, support, records, clearance |
| **Monitoring** | Overview + Drill-down | Actors must monitor capacity, progress, or exceptions and investigate causes. | Faculty Workload Monitoring (`SCR-TCH-03`) |
| **Information Lookup** | Search/List + Detail | Actors primarily need to find, inspect, and understand existing information. | Student Records Desk, Admission Application Portal, Extracurricular Registration, Support Referral |
| **Schedule Management** | Calendar/Schedule + Workspace | Work is organized around dates, periods, sessions, or resource allocation. | Calendar & Periods Workspace (`SCR-OPS-01`) |
| **Planning** | Planning Workspace | Actors create or modify structured plans with multiple related elements. | Curriculum & Learning Structure, Learning Plan, Billing Cycle, Financial Policy, My Teaching Availability |

#### 1.3.1 Work Queue Convention

Every Work Queue screen follows the same structural logic:
- **Worklist:** the actor's pending, actionable, or exception items, filtered to the actor's authority and current operational period.
- **Workbench:** the selected item's context, evidence, status, and primary action.
- **Context area:** the minimum related information needed to decide or act, including cross-domain status where relevant.
- **Secondary actions:** correction, history, escalation, or exceptional handling, disclosed only when authorized and relevant.

#### 1.3.2 Information Lookup Convention

Every Information Lookup screen follows the same structural logic:
- **Search/List:** the actor finds a record or a small set of candidate records.
- **Detail:** the selected record's current operational state and relevant history.
- **Authorized actions:** maintenance, correction, or related workflow actions available from the detail.

#### 1.3.3 Monitoring Convention

Every Monitoring screen follows the same structural logic:
- **Overview:** essential indicators, thresholds, and exceptions requiring attention.
- **Drill-down:** the underlying assignments, accounts, or records that explain the indicator.
- **Follow-up action:** the authorized response to an exception, without turning the overview into an unmanageable dashboard.

#### 1.3.4 Planning Convention

Every Planning screen follows the same structural logic:
- **Plan context:** the period, scope, or subject of the plan.
- **Plan structure:** the related elements being created or revised.
- **Validation and approval:** the constraints, approvals, and version state required before the plan becomes effective.
- **Publication action:** the authorized transition from draft to effective.

#### 1.3.5 Schedule Management Convention

Every Schedule Management screen follows the same structural logic:
- **Calendar/Schedule:** the authoritative date, period, or resource view.
- **Workspace:** the selected date, period, or entry and its operational implications.
- **Amendment action:** the authorized change, with impact visible before commitment.

---

### 1.4 Master Screen Inventory & Stage Allocation Summary

The inventory contains **36 screens**: 5 self-service screens and 31 staff screens across seven operational environments.

The table below outlines the master allocation across the three implementation stages:
- **Primary Stage:** The development stage in which the screen is first introduced and delivers initial operational capabilities.
- **Extension Stage(s):** Later stage(s) in which the screen receives additional capabilities, new supported use cases, or integrated cross-domain connections.

| Screen ID | Screen Name | Domain | Primary Actor | Pattern | Primary Stage | Extension Stage(s) | Supported Use Cases |
|---|---|---|---|---|:---:|:---:|---|
| `SCR-SELF-01` | Admission Application Portal | Students | Applicant / Parent / Guardian | Information Lookup | 1 | — | `UC-STD-01` |
| `SCR-SELF-02` | Family Tasks & Requests Portal | Students / Finance | Parent / Guardian / Payer | Work Queue | 1 | 2, 3 | `UC-FIN-01` (Stage 1); `UC-STD-02`, family inputs for `UC-STD-07`, `UC-STD-10` (Stage 2); family inputs for `UC-STD-11`, `UC-STD-12`, consent for `UC-DEV-02` (Stage 3) |
| `SCR-SELF-03` | Student Learning Workspace | Academics | Student | Work Queue | 2 | — | `UC-ACD-01` |
| `SCR-SELF-04` | Extracurricular Registration | Development | Student | Information Lookup | 3 | — | `UC-DEV-02` |
| `SCR-SELF-05` | Support Referral | Development | Student / Parent / Guardian | Information Lookup | 3 | — | `UC-DEV-01` |
| `SCR-STU-01` | Student Intake Workbench | Students | Admission Officer; Student Administration Officer | Work Queue | 1 | 2, 3 | `UC-STD-01`, `UC-STD-03`, `UC-STD-04` (Stage 1); `UC-ACD-07` integrated assessment (Stage 2); `UC-STD-18` (Stage 3) |
| `SCR-STU-02` | Student Records Desk | Students | Student Administration Officer | Information Lookup | 1 | — | `UC-STD-05`, `UC-STD-06` |
| `SCR-STU-03` | Student Lifecycle Workbench | Students | Student Administration Officer | Work Queue | 2 | — | `UC-STD-07`, `UC-STD-08`, `UC-STD-09`, `UC-STD-10`, `UC-STD-17` |
| `SCR-STU-04` | Student Exit & Graduation Workbench | Students | Student Administration Officer | Work Queue | 3 | — | `UC-STD-11`, `UC-STD-12`, `UC-STD-13`, `UC-STD-14`, `UC-STD-15`, `UC-STD-16` |
| `SCR-ACD-01` | Class Session Workbench | Academics | Teacher | Work Queue | 2 | — | `UC-ACD-02`, `UC-ACD-03`; attendance correction for `UC-ACD-15` |
| `SCR-ACD-02` | Coursework & Assessment Workbench | Academics | Teacher | Work Queue | 2 | — | `UC-ACD-04`, `UC-ACD-05`, `UC-ACD-06`; task metadata correction for `UC-ACD-15` |
| `SCR-ACD-03` | Curriculum & Learning Structure Workspace | Academics | Academic Coordinator | Planning | 2 | — | `UC-ACD-08` |
| `SCR-ACD-04` | Learning Plan Workspace | Academics | Academic Coordinator | Planning | 2 | — | `UC-ACD-09` |
| `SCR-ACD-05` | Participation & Roster Workbench | Academics | Academic Coordinator | Work Queue | 2 | — | `UC-ACD-10` |
| `SCR-ACD-06` | Academic Intervention Workbench | Academics | Academic Coordinator | Work Queue | 2 | — | `UC-ACD-11` |
| `SCR-ACD-07` | Academic Records Workbench | Academics | Academic Coordinator; Academic Authority | Work Queue | 2 | 3 | `UC-ACD-12`, `UC-ACD-13` (interim), `UC-ACD-16`, `UC-ACD-19`, non-grade correction for `UC-ACD-15` (Stage 2); official graduation transcripts & transfer credit for `UC-ACD-13`, `UC-STD-18` (Stage 3) |
| `SCR-ACD-08` | Academic Clearance Review | Academics | Academic Coordinator; Academic Authority | Work Queue | 3 | — | `UC-ACD-14`, `UC-ACD-20` |
| `SCR-ACD-09` | Cohort Academic Review | Academics | Academic Authority | Work Queue | 2 | 3 | `UC-ACD-17`, `UC-ACD-21` (Stage 2); `UC-ACD-18` (Stage 3) |
| `SCR-FIN-01` | Billing Cycle Workspace | Finance | Finance Officer | Planning | 1 | — | `UC-FIN-02`, `UC-FIN-03` |
| `SCR-FIN-02` | Payment Verification Queue | Finance | Finance Officer | Work Queue | 1 | — | `UC-FIN-04`, `UC-FIN-05`, `UC-FIN-09` |
| `SCR-FIN-03` | Student Accounts Workbench | Finance | Finance Officer; Finance Authority | Work Queue | 1 | 2, 3 | Account inquiry & balance view (Stage 1); `UC-FIN-06`, `UC-FIN-07`, `UC-FIN-15` (Stage 2); `UC-FIN-08` (Stage 3) |
| `SCR-FIN-04` | Financial Policy Workspace | Finance | Finance Authority | Planning | 1 | — | `UC-FIN-10` |
| `SCR-FIN-05` | Financial Decisions Queue | Finance | Finance Authority | Work Queue | 2 | — | `UC-FIN-11`, `UC-FIN-12`, `UC-FIN-13`, `UC-FIN-14` |
| `SCR-OPS-01` | Calendar & Periods Workspace | Operations | Operations Authority | Schedule Management | 1 | 2 | `UC-OPS-11`, `UC-OPS-12` (declaration), `UC-OPS-13` (calendar records) (Stage 1); closure schedule impact linkage to timetable (Stage 2) |
| `SCR-OPS-02` | Scheduling Workbench | Operations | Operations Coordinator; Operations Authority | Work Queue | 2 | 3 | `UC-OPS-03`, `UC-OPS-04`, `UC-OPS-05`, `UC-OPS-08`, `UC-OPS-09`, `UC-OPS-10`, `UC-OPS-13` (timetable records) (Stage 2); event & extracurricular scheduling for `UC-OPS-06`, `UC-DEV-09`, `UC-DEV-10` (Stage 3) |
| `SCR-OPS-03` | Operational Requests Portal | Operations | Teacher / Academic Coordinator / SDO | Work Queue | 2 | — | `UC-OPS-01`, `UC-OPS-02` |
| `SCR-OPS-04` | Events & Announcements Workspace | Operations | Operations Coordinator | Work Queue | 3 | — | `UC-OPS-06`, `UC-OPS-07` |
| `SCR-TCH-01` | Faculty Resource Workspace | Teaching | Teaching Coordinator; Teaching Authority | Work Queue | 2 | — | `UC-TCH-02`, `UC-TCH-03`, `UC-TCH-09`, `UC-TCH-12` |
| `SCR-TCH-02` | Teaching Assignment & Coverage Workbench | Teaching | Teaching Coordinator; Teaching Authority | Work Queue | 2 | — | `UC-TCH-04`, `UC-TCH-05`, `UC-TCH-06`, `UC-TCH-08`, `UC-TCH-10`, `UC-TCH-11` |
| `SCR-TCH-03` | Faculty Workload Monitoring | Teaching | Teaching Coordinator | Monitoring | 2 | — | `UC-TCH-07` |
| `SCR-TCH-04` | My Teaching Availability | Teaching | Teacher | Planning | 2 | — | `UC-TCH-01` |
| `SCR-DEV-01` | Behaviour & Incident Triage Workbench | Development | Student Development Officer; Teacher / Reporter | Work Queue | 3 | — | `UC-DEV-03`, `UC-DEV-04`, `UC-DEV-05`; behaviour correction for `UC-DEV-16` |
| `SCR-DEV-02` | Discipline Case Workbench | Development | Discipline Authority | Work Queue | 3 | — | `UC-DEV-14`, `UC-DEV-15`; discipline correction for `UC-DEV-16` |
| `SCR-DEV-03` | Pastoral Support Workbench | Development | Student Development Officer | Work Queue | 3 | — | `UC-DEV-06`, `UC-DEV-07`, `UC-DEV-08`, `UC-DEV-12`; pastoral correction for `UC-DEV-16` |
| `SCR-DEV-04` | Counselling Case Workspace | Development | Counselor | Work Queue | 3 | — | `UC-DEV-13`; clinical correction for `UC-DEV-16` |
| `SCR-DEV-05` | Extracurricular & Achievement Workspace | Development | Student Development Officer | Work Queue | 3 | — | `UC-DEV-09`, `UC-DEV-10`, `UC-DEV-11`; activity correction for `UC-DEV-16` |

### 1.5 Correction Use Cases

Three correction use cases span several operational records rather than residing in a single generic correction screen:

| Use Case | Supported By | Implementation Stages |
|---|---|:---:|
| `UC-ACD-15` Correct Non-Grade Academic Record | `SCR-ACD-01` (attendance history), `SCR-ACD-02` (assignment metadata), `SCR-ACD-07` (non-grade records), `SCR-ACD-09` (standing/progression records) | Stage 2 / Stage 3 |
| `UC-OPS-13` Correct Approved Operations Record | `SCR-OPS-01` (calendar records), `SCR-OPS-02` (room and timetable records) | Stage 1 / Stage 2 |
| `UC-DEV-16` Correct Approved Development Record | `SCR-DEV-01` to `SCR-DEV-05`, according to the record's confidentiality class and the correcting actor | Stage 3 |

This is deliberate: correction is an auditable action on an existing record, not a separate daily workspace. The correction action appears only in the owning screen and only for the actor authorized to perform it.

---

## 2. Stage 1 — Student Foundation, Operational Context, and Basic Finance

### 2.1 Stage Overview, Objectives, and Completion Criteria

**Stage Theme:** Student foundation + operational context + basic finance.

**Stage Objective:** Enable the school to admit applicants, enroll students, assign administrative placements, define the institutional calendar, and assess and collect basic financial obligations. At the conclusion of Stage 1, the school can independently operate its intake pipeline and establish who its students are, when the school year runs, and what each student owes.

**Prerequisites:** None. Stage 1 provides the universal foundation for all subsequent stages.

**Stage Completion Criteria (Roadmap Section D):**
1. An academic year with at least one term/semester and a school calendar (including holidays) is approved and published.
2. An applicant can register an admission application, undergo review, and receive an acceptance or rejection decision.
3. An accepted applicant can be enrolled, assigned a unique student number, and placed in an administrative location (campus, level, program, homeroom cohort).
4. Parent/guardian relationships are recorded for enrolled students.
5. Student status is set to Active and administrative history records the complete intake sequence.
6. At least one financial policy and charge rule (e.g., tuition, enrollment fee) is defined and assessed against an enrolled student.
7. A billing statement communicating obligations is produced for an authorized recipient.
8. A payment is received, confirmed, and allocated against an outstanding obligation.
9. Enrollment payment confirmation is available to Students to unblock the intake gate.
10. All domain events and administrative actions are logged and historically traceable.

---

### 2.2 Stage 1 Screen Inventory Table

The table below lists all 9 screens operational in Stage 1:

| Screen ID | Screen Name | Domain | Primary Actor | Implementation Scope | Dependencies |
|---|---|---|---|---|---|
| `SCR-SELF-01` | Admission Application Portal | Students | Applicant / Parent / Guardian | Full self-service admission application registration, credential submission, review tracking, and decision viewing. | `SCR-OPS-01` (open admission periods) |
| `SCR-SELF-02` | Family Tasks & Requests Portal | Students / Finance | Parent / Guardian / Payer | **Initial Scope:** View issued billing statements, submit electronic fee payments with documentary proof, and view payment receipts. | `SCR-FIN-01` (issued billing statements) |
| `SCR-STU-01` | Student Intake Workbench | Students | Admission Officer; Student Administration Officer | **Initial Scope:** Intake pipeline: assisted registration, document verification, manual entrance assessment evidence intake, admission review/decision, payment gate check, enrollment execution, and initial placement. | `SCR-OPS-01` (periods), `SCR-FIN-02` (payment confirmation) |
| `SCR-STU-02` | Student Records Desk | Students | Student Administration Officer | Full operational maintenance of official student identity, guardian relationships, independence flags, and administrative placement history. | `SCR-STU-01` (enrolled students) |
| `SCR-FIN-01` | Billing Cycle Workspace | Finance | Finance Officer | Full billing cycle preparation, charge obligation assessment (admission, enrollment, tuition), exception review, obligation posting, and statement generation. | `SCR-FIN-04` (policy), `SCR-STU-01`/`02` (students), `SCR-OPS-01` (periods) |
| `SCR-FIN-02` | Payment Verification Queue | Finance | Finance Officer | Full payment verification, receipt issuance, fund allocation against open obligations, and confirmation of enrollment payment gates. | `SCR-FIN-01` (obligations), `SCR-SELF-02` (submissions) |
| `SCR-FIN-03` | Student Accounts Workbench | Finance | Finance Officer | **Initial Scope:** Financial account inquiry, line-item charge/payment inspection, allocation tracking, and real-time student account balance viewing. | `SCR-FIN-01` (obligations), `SCR-FIN-02` (payments) |
| `SCR-FIN-04` | Financial Policy Workspace | Finance | Finance Authority | Full configuration, versioning, and approval of institutional fee schedules, charge rules, due dates, and billing policies. | `SCR-OPS-01` (academic periods) |
| `SCR-OPS-01` | Calendar & Periods Workspace | Operations | Operations Authority | **Initial Scope:** Full definition, versioning, and publication of academic years, terms, operational periods, official holidays, and closure declarations. | None (foundational) |

---

### 2.3 Stage 1 Detailed Screen Definitions

#### `SCR-SELF-01` — Admission Application Portal

| Attribute | Definition |
|---|---|
| **Domain** | Students |
| **Primary Actor** | Applicant / Parent / Guardian |
| **Pattern** | Information Lookup |
| **Operational Purpose** | Allow a prospective family to register and track an admission application without staff assistance. |
| **Supported Use Cases** | `UC-STD-01` Register Admission Application |

**Structure.** Search/List shows the applicant's applications and their current admission status. Detail shows the selected application, the requested admission period and entry preference, submitted credentials, missing items, and the official decision or request for correction.

**Primary Information.** Applicant identity, requested admission period and entry preference, documentary completeness, assessment evidence status where applicable, and the official admission decision.

**Available Actions.** Create an application, save an incomplete application, submit or update required credentials, view the decision, and respond to a returned-for-correction request.

**Navigation Context.** Accessed from the public/family entry point. A submitted application becomes visible to the Admission Officer in `SCR-STU-01`. The applicant does not see internal review notes or other applicants' information.

> [!NOTE]
> **Stage 1 Implementation Scope:** Full operational capability. Prospective families can register an application, submit required credentials, track documentary review, and view admission decisions autonomously during published admission periods.

---

#### `SCR-SELF-02` — Family Tasks & Requests Portal (Stage 1 Initial Implementation)

| Attribute | Definition |
|---|---|
| **Domain** | Students / Finance |
| **Primary Actor** | Parent / Guardian; Payer for payment tasks |
| **Pattern** | Work Queue |
| **Operational Purpose** | Provide families and fee payers an authoritative self-service interface to view assessed fee obligations and submit verified payment transactions. |
| **Stage 1 Supported Use Cases** | `UC-FIN-01` Submit Fee Payment |
| **Stage 1 Implementation Scope** | Basic fee settlement portal: viewing issued billing statements and outstanding obligations; submitting electronic payment evidence/proof across accepted channels; viewing payment receipt and confirmation status. |
| **Dependencies** | `SCR-FIN-01` (issued billing statements and charge obligations). |

**Worklist.** Active payment tasks for enrolled learners: outstanding tuition, admission, and enrollment fee statements. Completed payments remain visible as payment history.

**Workbench.** The selected obligation's statement breakdown, charge categories, due date, net amount owed, acceptable payment methods/banking details, and payment proof upload interface.

**Primary Information.** Statement ID, student identity, assessed charge items, due date, outstanding balance, accepted payment channels, and receipt/verification status.

**Available Actions.** Select outstanding obligation; input payment transaction details and upload documentary payment evidence (transfer receipt); submit payment for Finance Officer verification; view and download confirmed payment receipts.

**Navigation Context.** Accessed from the public/family portal entry point. Submissions flow directly to `SCR-FIN-02` (Payment Verification Queue).

> [!WARNING]
> **Stage 1 Implementation Boundaries & Gated Features:**
> - Continuing re-enrollment confirmation (`UC-STD-02`), leave requests (`UC-STD-07`), and internal transfer requests (`UC-STD-10`) are disabled until **Stage 2**.
> - Terminating lifecycle requests (`UC-STD-11` External Transfer Out, `UC-STD-12` Withdrawal), guardian dispute/conflicting directive recording, and extracurricular consent tasks (`UC-DEV-02`) are disabled until **Stage 3**.

---

#### `SCR-STU-01` — Student Intake Workbench (Stage 1 Initial Implementation)

| Attribute | Definition |
|---|---|
| **Domain** | Students (with external diagnostic assessment evidence and Finance payment confirmation) |
| **Primary Actor** | Admission Officer for application and review; Student Administration Officer for enrollment |
| **Pattern** | Work Queue |
| **Operational Purpose** | Process prospective learners from assisted registration through document verification, external diagnostic assessment intake, review, decision, and official enrollment in one continuous intake pipeline. |
| **Stage 1 Supported Use Cases** | `UC-STD-01` Register Admission Application (assisted); `UC-STD-03` Review Admission Application; `UC-STD-04` Enroll Accepted Applicant; `UC-ACD-07` Conduct Applicant Entrance Assessment (Stage 1 manual evidence intake) |
| **Stage 1 Implementation Scope** | Core intake lifecycle: assisted application registration; identity and document verification; manual entry of entrance diagnostic assessment scores (as external evidence); admission review and decision (Accept, Reject, Waitlist, Return for Correction); enrollment execution upon Finance payment clearance; assignment of unique Student Number, primary/secondary guardians, and initial administrative placement (campus, level, program, homeroom cohort). |
| **Dependencies** | `SCR-OPS-01` (admission periods), `SCR-FIN-02` (enrollment payment gate confirmation). |

**Worklist.** Intake cases grouped by operational stage: new or incomplete applications, applications awaiting external assessment evidence, applications ready for review, waitlisted or returned cases, and accepted applicants awaiting enrollment.

**Workbench.** The selected case's applicant identity, requested admission period, documentary completeness, attached entrance assessment evidence, admission decision, enrollment payment-gate confirmation status, and proposed initial placement.

**Primary Information.** Applicant identity, guardian contact information, uploaded credentials, external diagnostic scores, admission status, payment-gate clearance flag, unique student number, and initial cohort/program placement.

**Available Actions.** Register or complete an assisted application; attach and verify diagnostic assessment evidence; record an admission decision; return a case for correction; verify enrollment fee payment confirmation; execute official enrollment; assign student number; designate primary guardian and record administrative placement (campus, grade level, program, homeroom cohort).

**Navigation Context.** Receives self-service applications from `SCR-SELF-01`. Consumes payment gate confirmation from `SCR-FIN-02`. Hand-offs completed student records to `SCR-STU-02`.

> [!WARNING]
> **Stage 1 Implementation Boundaries & Gated Features:**
> - Entrance assessment scoring is supported via manual/external score entry; direct automated integration with Academics assessment workflows (`ACD-WF-20`) is added in **Stage 2**.
> - External Transfer In (`UC-STD-18`) processing, including prior-credit and origin-school curriculum evaluation, is disabled until **Stage 3**.

---

#### `SCR-STU-02` — Student Records Desk

| Attribute | Definition |
|---|---|
| **Domain** | Students |
| **Primary Actor** | Student Administration Officer |
| **Pattern** | Information Lookup |
| **Operational Purpose** | Find an official student record and maintain certified identity, guardian, and administrative placement information. |
| **Supported Use Cases** | `UC-STD-05` Maintain Student Identity; `UC-STD-06` Change Administrative Placement |

**Structure.** Search/List finds students by official identity or student number, with current status and placement. Detail shows the student's certified identity and guardian relationship information, current administrative placement, status, and administrative history. Historical values remain available but are not displayed by default.

**Primary Information.** Official identity, primary and secondary guardian relationships, independence status where officially verified, current campus/level/program/cohort placement, current administrative status, and effective-dated placement history.

**Available Actions.** Correct or update certified identity and guardian information with supporting evidence; designate the primary guardian; record or verify independence status; change administrative placement with an effective date and reason; view the preserved administrative history.

**Navigation Context.** Accessed from the student search. It is the common reference point linked from lifecycle, exit, academic, finance, and development workspaces. It does not execute lifecycle transitions; those belong to `SCR-STU-03` and `SCR-STU-04`.

> [!NOTE]
> **Stage 1 Implementation Scope:** Full operational capability. Student Administration Officers maintain official identity records, emergency contacts, primary guardian designations, independence flags (`is_independent`), and effective-dated administrative placements (campus, level, program, homeroom cohort) with preserved audit history.

---

#### `SCR-FIN-01` — Billing Cycle Workspace

| Attribute | Definition |
|---|---|
| **Domain** | Finance |
| **Primary Actor** | Finance Officer |
| **Pattern** | Planning |
| **Operational Purpose** | Prepare and issue a billing cycle by assessing obligations under the approved financial policy. |
| **Supported Use Cases** | `UC-FIN-02` Assess Financial Obligations; `UC-FIN-03` Issue Billing Statement |

**Plan Context.** The academic period, applicable financial policy version, student population or placement scope, and the charge categories being assessed.

**Plan Structure.** The obligations to be generated, grouped by charge rule and student account, with due dates and any policy-driven exclusions or proration. The workspace shows exceptions before posting rather than exposing every account attribute.

**Validation and Approval.** Policy applicability, duplicate or replacement charge checks, effective dates, currency and rounding rules, and the audit record of the assessment. Exceptions requiring Finance Authority approval are routed to `SCR-FIN-05`.

**Available Actions.** Preview and assess obligations for the selected scope; review and resolve assessment exceptions; post assessed obligations to student accounts; generate and issue billing statements to authorized recipients; view the resulting billing cycle history.

**Navigation Context.** Accessed from the Finance Officer's billing area. Policy comes from `SCR-FIN-04`; student and placement context comes from Students. Issued statements become visible to families in `SCR-SELF-02`. The workspace does not confirm payments or allocate them.

> [!NOTE]
> **Stage 1 Implementation Scope:** Full operational capability. Finance Officers configure billing scopes, assess recurring tuition and one-off admission/enrollment obligations under approved policy rules, review pre-posting exceptions, post obligations to student accounts, and issue billing statements to families.

---

#### `SCR-FIN-02` — Payment Verification Queue

| Attribute | Definition |
|---|---|
| **Domain** | Finance |
| **Primary Actor** | Finance Officer |
| **Pattern** | Work Queue |
| **Operational Purpose** | Verify incoming payments, confirm receipt, allocate value, and answer the enrollment payment gate. |
| **Supported Use Cases** | `UC-FIN-04` Confirm Payment Receipt; `UC-FIN-05` Allocate Payment to Obligations; `UC-FIN-09` Confirm Enrollment Payment Gate |

**Worklist.** Submitted payments awaiting verification, payments with unmatched or ambiguous evidence, confirmed but unallocated payments, and enrollment payment-gate requests from intake. Items show the payer, student account, amount, channel, and age.

**Workbench.** The selected payment's evidence and provider or bank reference, the payer and student account context, the obligations available for allocation, and — for an enrollment gate request — the specific mandatory admission and enrollment obligations that must be settled. The workbench shows only the accounts relevant to the selected item.

**Available Actions.** Verify or reject payment evidence; confirm receipt and issue the official receipt; allocate confirmed value to selected obligations according to priority rules; record an allocation correction where authorized; confirm enrollment payment status to unblock intake.

**Navigation Context.** Accessed from the Finance Officer's payment worklist. Payer submissions arrive from `SCR-SELF-02`; enrollment gate requests originate in `SCR-STU-01`. Confirmation and allocation update the account view in `SCR-FIN-03`. The screen does not approve refunds, adjustments, or benefits.

> [!NOTE]
> **Stage 1 Implementation Scope:** Full operational capability for core payment workflows. Finance Officers verify submitted payment evidence, issue official receipts, allocate confirmed funds to open obligations, and answer the enrollment payment gate (`UC-FIN-09`) to unblock student intake in `SCR-STU-01`.

---

#### `SCR-FIN-03` — Student Accounts Workbench (Stage 1 Initial Implementation)

| Attribute | Definition |
|---|---|
| **Domain** | Finance |
| **Primary Actor** | Finance Officer |
| **Pattern** | Work Queue / Information Lookup |
| **Operational Purpose** | Provide real-time financial account inquiry, obligation inspection, and payment allocation tracking for enrolled students. |
| **Stage 1 Supported Use Cases** | Student Financial Account inquiry & settlement monitoring (supporting core Obligations & Billing) |
| **Stage 1 Implementation Scope** | Read-only ledger and account inspection: search student accounts by student number or name; inspect assessed charges, posted payments, allocation breakdown, and net account balance. |
| **Dependencies** | `SCR-FIN-01` (assessed obligations), `SCR-FIN-02` (posted payments). |

**Worklist / Search.** Student account search by student ID, student name, or grade level, displaying current balance status (Current, Settled, Unpaid).

**Workbench.** Comprehensive student financial account statement showing effective-dated obligations (tuition, fees), payment receipts, allocation mapping, and current outstanding balance.

**Primary Information.** Student identity, account number, posted obligations, received payments, allocated amounts, outstanding balance, and transaction history.

**Available Actions.** Search student accounts; view line-item transaction history; generate account statement summary; drill down into payment receipts or billing statements.

**Navigation Context.** Accessed from the Finance Officer workspace. Serves as financial inquiry reference for staff.

> [!WARNING]
> **Stage 1 Implementation Boundaries & Gated Features:**
> - Overdue receivables monitoring, aging buckets, and reminder dispatch (`UC-FIN-06`) are deferred to **Stage 2**.
> - Account credit, overpayment management, and refund preparation (`UC-FIN-07`) are deferred to **Stage 2**.
> - Auditable finance record correction (`UC-FIN-15`) is deferred to **Stage 2**.
> - Formal Financial Clearance evaluation (`UC-FIN-08`) for student exits is deferred to **Stage 3**.

---

#### `SCR-FIN-04` — Financial Policy Workspace

| Attribute | Definition |
|---|---|
| **Domain** | Finance |
| **Primary Actor** | Finance Authority |
| **Pattern** | Planning |
| **Operational Purpose** | Define and approve the financial policies and charge rules that govern billing and clearance. |
| **Supported Use Cases** | `UC-FIN-10` Establish Financial Policy |

**Plan Context.** The academic period or effective range, the policy version, and the institutional scope to which the policy applies.

**Plan Structure.** Fee schedules, charge rules, payment due schedules, currency and rounding rules, and the policy's applicability conditions. Related charge rules are organized under the policy they belong to.

**Validation and Approval.** Effective dating, version comparison, required School Leadership participation where policy requires it, and the approval state that makes the policy effective.

**Available Actions.** Create or revise a financial policy; define or adjust charge rules and due schedules; compare versions; submit for required approval; approve and publish the policy; view historical versions.

**Navigation Context.** Accessed from the Finance Authority's governance area. Approved policy is consumed by `SCR-FIN-01` for assessment and by clearance evaluation in `SCR-FIN-03`. The workspace does not assess charges or issue bills.

> [!NOTE]
> **Stage 1 Implementation Scope:** Full operational capability. Finance Authorities define, version, and approve institutional fee structures, charge rules (admission, enrollment, tuition), payment schedules, and currency/rounding rules governing billing assessment.

---

#### `SCR-OPS-01` — Calendar & Periods Workspace (Stage 1 Initial Implementation)

| Attribute | Definition |
|---|---|
| **Domain** | Operations |
| **Primary Actor** | Operations Authority |
| **Pattern** | Schedule Management |
| **Operational Purpose** | Establish, version, and publish the institutional calendar, academic years, operational periods, terms, and holidays that govern all domains. |
| **Stage 1 Supported Use Cases** | `UC-OPS-11` Establish School Calendar and Periods; `UC-OPS-12` Declare Campus Closure (calendar declaration); `UC-OPS-13` Correct Approved Operations Record (calendar records) |
| **Stage 1 Implementation Scope** | Core institutional calendar management: define and publish academic years, terms, operational periods (admission, billing, registration), official holidays, and planned closures; execute auditable calendar record corrections. |
| **Dependencies** | None (foundational). |

**Calendar.** Visual calendar matrix showing the academic year, terms, operational periods, holidays, and closure days.

**Workspace.** Detail of selected operational periods, term start/end boundaries, affected dates, and institutional scope.

**Primary Information.** Academic year ID, term definitions, effective start and end dates, operational period types, official holidays, closure declarations, and publication version.

**Available Actions.** Create and revise academic years and terms; define operational periods; declare scheduled or emergency school closures on the calendar; publish approved calendar versions; execute auditable corrections to calendar records with mandatory justification.

**Navigation Context.** Consumed universally across all domains as shared date/period context.

> [!WARNING]
> **Stage 1 Implementation Boundaries & Gated Features:**
> - In Stage 1, campus closures update calendar dates only. Automated schedule-impact analysis and mass rescheduling/cancellation across timetabled instructional sessions require the Scheduling Workbench (`SCR-OPS-02`), introduced in **Stage 2**.

---

### 2.4 Stage 1 Operational Summary

| Metric | Count | Details |
|---|:---:|---|
| **Screens Introduced** | **9** | `SCR-SELF-01`, `SCR-SELF-02`, `SCR-STU-01`, `SCR-STU-02`, `SCR-FIN-01`, `SCR-FIN-02`, `SCR-FIN-03`, `SCR-FIN-04`, `SCR-OPS-01` |
| **Screens Extended** | **0** | Foundational stage; no prior screens exist. |
| **Total Active Screens** | **9** | Complete baseline student intake, operational calendar, and financial assessment/collection. |

**Major Operational Capabilities Supported:**
- Autonomous applicant registration and document submission via self-service portal (`SCR-SELF-01`).
- End-to-end staff intake pipeline from application triage and document verification to acceptance decisions (`SCR-STU-01`).
- Direct intake of diagnostic entrance assessment scores as external documentary evidence (`SCR-STU-01`).
- Enrollment execution with automated student number assignment and parent/guardian link establishment (`SCR-STU-01`).
- Maintenance of certified student identity, guardian relationships, and effective-dated placement records (`SCR-STU-02`).
- Financial governance, fee schedule versioning, and charge rule authorization (`SCR-FIN-04`).
- Automated billing cycle assessment for tuition and enrollment fees, with statement generation (`SCR-FIN-01`).
- Online fee payment submission with payment proof upload (`SCR-SELF-02`).
- Staff payment verification, fund allocation, and enrollment payment gate clearance (`SCR-FIN-02`).
- Student financial ledger inquiry and account balance monitoring (`SCR-FIN-03`).
- Authoritative definition and publication of academic years, terms, operational periods, and school calendar (`SCR-OPS-01`).

---

## 3. Stage 2 — Academic Learning, Teaching, and Expanded Finance

### 3.1 Stage Overview, Objectives, and Completion Criteria

**Stage Theme:** Academic learning + teaching assignments + full finance.

**Stage Objective:** Enable the school to define curriculum, assign teachers, conduct learning activities, record attendance and assignments, produce assessment results and report cards, determine academic progression, and operate full financial capabilities including scholarships, receivables, refunds, and payment arrangements.

**Prerequisites:** Stage 1 baseline (Student identity, administrative placement, operational calendar, and basic obligations/payments).

**Stage Completion Criteria (Roadmap Section D):**
1. A learning structure with at least one learning unit and academic requirements is approved and effective.
2. An active student has academic participation in at least one learning unit.
3. A learning activity has been conducted with attendance recorded.
4. An assignment has been issued, a submission received, and an evaluation recorded.
5. An academic result has been approved and published. A report card or progress report is produced.
6. Academic standing has been determined for at least one student. A promotion or retention recommendation has been issued.
7. A teacher has a verified profile, granted authorization, and an approved teaching assignment linking them to a learning unit.
8. Teaching workload is visible and substitution for an absent teacher is recorded.
9. A student has been promoted or retained based on an academic recommendation.
10. A scholarship or discount is approved and applied to an obligation; an overdue receivable triggers a reminder; a payment arrangement is approved; a refund is processed.
11. An operational timetable with at least one scheduled occurrence and room allocation is published.
12. A student learning plan is created, approved, and governs an exceptional student's learning.
13. Leave of absence is placed and returned successfully. Internal transfer is executed.
14. A student re-enrollment is confirmed for the next academic period; re-enrollment history is preserved and a financial trigger (if policy requires) is available to Finance.
15. An entrance assessment is conducted through an Academics workflow and its result is consumed by the Admission module as an authorized admission input.

---

### 3.2 Stage 2 Screen Inventory Table

The table below lists all 21 screens operational in Stage 2 (17 newly introduced + 4 extended from Stage 1):

| Screen ID | Screen Name | Domain | Primary Actor | Implementation Scope | Dependencies |
|---|---|---|---|---|---|
| `SCR-SELF-03` | Student Learning Workspace | Academics | Student | View coursework tasks, rubrics, and deadlines; submit coursework files; view released formative evaluations. | `SCR-ACD-02`, `SCR-ACD-05` |
| `SCR-STU-03` | Student Lifecycle Workbench | Students | Student Administration Officer | Continuing-student lifecycle: leave of absence, return from leave, internal transfers, annual re-enrollment, and promotion/retention execution. | `SCR-STU-02`, `SCR-ACD-09`, `SCR-SELF-02` |
| `SCR-ACD-01` | Class Session Workbench | Academics | Teacher | Daily instructional sessions: curriculum delivery logging, session attendance capture, and justified attendance corrections. | `SCR-OPS-02`, `SCR-TCH-02`, `SCR-ACD-05` |
| `SCR-ACD-02` | Coursework & Assessment Workbench | Academics | Teacher | Coursework planning, assessment administration, rubric-based student evaluation, formative feedback publication, and provisional summative scoring. | `SCR-ACD-03`, `SCR-ACD-05`, `SCR-OPS-01` |
| `SCR-ACD-03` | Curriculum & Learning Structure Workspace | Academics | Academic Coordinator | Curriculum frameworks, learning units, syllabi, academic prerequisites, and versioned academic requirements management. | `SCR-OPS-01` (calendar periods) |
| `SCR-ACD-04` | Learning Plan Workspace | Academics | Academic Coordinator | Individualized Student Learning Plans (SLP): accommodations, course substitutions, and exemptions formulation and approval. | `SCR-ACD-03`, `SCR-STU-02` |
| `SCR-ACD-05` | Participation & Roster Workbench | Academics | Academic Coordinator | Academic participation registration, prerequisite validation, SLP alignment, capacity resolution, and class roster finalization. | `SCR-STU-02`, `SCR-ACD-03`, `SCR-ACD-04` |
| `SCR-ACD-06` | Academic Intervention Workbench | Academics | Academic Coordinator | At-risk learner identification, remediation plan formulation, target tracking, and support review checkpoints. | `SCR-ACD-01`, `SCR-ACD-02` |
| `SCR-ACD-07` | Academic Records Workbench | Academics | Academic Coordinator; Academic Authority | **Stage 2 Scope:** Participation completion, summative grade moderation and approval, official result publication, period report cards, interim transcripts, and result corrections. | `SCR-ACD-01`, `SCR-ACD-02`, `SCR-ACD-05` |
| `SCR-ACD-09` | Cohort Academic Review | Academics | Academic Authority | **Stage 2 Scope:** Cohort academic performance review, academic standing determination, progression recommendations (promote/retain), and academic achievement conferral. | `SCR-ACD-07` (approved results) |
| `SCR-FIN-05` | Financial Decisions Queue | Finance | Finance Authority | Zero-discretionary approval for scholarships/discounts, installment payment arrangements, billing dispute adjustments, and student refunds. | `SCR-FIN-01`, `SCR-FIN-03` |
| `SCR-OPS-02` | Scheduling Workbench | Operations | Operations Coordinator; Operations Authority | **Stage 2 Scope:** Operational feasibility review, classroom and homeroom allocations, timetable conflict resolution, room restrictions, rescheduling, and arrangement closure. | `SCR-OPS-01`, `SCR-OPS-03`, `SCR-TCH-02`, `SCR-TCH-04` |
| `SCR-OPS-03` | Operational Requests Portal | Operations | Teacher / Academic Coordinator / SDO | Activity request submissions for facilities/rooms and autonomous cancellation of approved reservations. | `SCR-OPS-01` (calendar) |
| `SCR-TCH-01` | Faculty Resource Workspace | Teaching | Teaching Coordinator; Teaching Authority | Teacher profile management, qualification verification, teaching authorization determinations, supervision conditions, and faculty record corrections. | None (within SIS) |
| `SCR-TCH-02` | Teaching Assignment & Coverage Workbench | Teaching | Teaching Coordinator; Teaching Authority | Teaching demand intake, candidate evaluation, binding assignment approvals (incl. homerooms), emergency substitution, and assignment conclusion. | `SCR-TCH-01`, `SCR-TCH-04`, `SCR-ACD-03`/`05` |
| `SCR-TCH-03` | Faculty Workload Monitoring | Teaching | Teaching Coordinator | Aggregate teaching workload monitoring against institutional thresholds, overloading alerts, and capacity rebalancing. | `SCR-TCH-01`, `SCR-TCH-02` |
| `SCR-TCH-04` | My Teaching Availability | Teaching | Teacher | Teacher availability declarations: weekly preferred blocks, unavailable windows, and dated personal constraints. | `SCR-OPS-01` (calendar periods) |
| `SCR-SELF-02` | Family Tasks & Requests Portal | Students / Finance | Parent / Guardian | **Stage 2 Extension:** Submit re-enrollment confirmation; submit guardian requests for leave of absence or internal program transfer. | `SCR-STU-03` |
| `SCR-STU-01` | Student Intake Workbench | Students | Admission Officer; Assessment Officer | **Stage 2 Extension:** Direct automated integration of Academics entrance assessment results into admission review dossiers. | Academics Assessment module |
| `SCR-FIN-03` | Student Accounts Workbench | Finance | Finance Officer; Finance Authority | **Stage 2 Extension:** Overdue receivables monitoring, aging tracking, payment reminder dispatch, credit balance management, refund dossiers, and finance record corrections. | `SCR-FIN-05` |
| `SCR-OPS-01` | Calendar & Periods Workspace | Operations | Operations Authority | **Stage 2 Extension:** Campus closure schedule-impact coordination linked directly to timetable rescheduling in `SCR-OPS-02`. | `SCR-OPS-02` |

---

### 3.3 Stage 2 Detailed Screen Definitions

#### 3.3.1 Newly Introduced Screens in Stage 2

#### `SCR-SELF-03` — Student Learning Workspace

| Attribute | Definition |
|---|---|
| **Domain** | Academics |
| **Primary Actor** | Student |
| **Pattern** | Work Queue |
| **Operational Purpose** | Allow a student to see assigned coursework and submit the required evidence on time. |
| **Supported Use Cases** | `UC-ACD-01` Submit Coursework Assignment |

**Worklist.** Coursework and assessment tasks assigned to the student that are upcoming, due, returned, or missing a submission. Items are grouped by learning unit and deadline.

**Workbench.** The selected task's instructions, evaluation criteria, due date, submission state, and the feedback already released to the student. The workbench does not expose other students' submissions or unreleased evaluations.

**Available Actions.** Submit or replace coursework evidence, record a late or excused submission response where policy permits, and view released feedback and submission status.

**Navigation Context.** Accessed from the student entry point. Submissions become visible to the Teacher in `SCR-ACD-02`. Released results may also be referenced in the student's academic record, but the workspace does not publish official results.

> [!NOTE]
> **Stage 2 Implementation Scope:** Full operational capability. Students access assigned coursework, view rubrics and deadlines, upload assignment submissions, and review formative feedback and evaluations released by teachers.

---

#### `SCR-STU-03` — Student Lifecycle Workbench

| Attribute | Definition |
|---|---|
| **Domain** | Students |
| **Primary Actor** | Student Administration Officer |
| **Pattern** | Work Queue |
| **Operational Purpose** | Process administrative transitions for students who are continuing their relationship with the school. |
| **Supported Use Cases** | `UC-STD-07` Place Student on Leave; `UC-STD-08` Return Student from Leave; `UC-STD-09` Execute Promotion or Retention; `UC-STD-10` Process Internal Transfer; `UC-STD-17` Process Student Re-enrollment |

**Worklist.** Pending or upcoming transitions: leave requests and approved leaves approaching expiry, students in `On Leave` or `Leave Expired` awaiting return, approved progression recommendations awaiting execution, internal transfer requests, and re-enrollment confirmations awaiting administrative processing. Items are grouped by transition type and target period.

**Workbench.** The selected student's current status and placement, the requesting or recommending authority, approval evidence, effective date, any guardian dispute or leadership hold, and the intended outcome. For promotion and retention, the workbench supports cohort-level review while preserving each student's individual outcome.

**Available Actions.** Place a student on leave with effective and expected-return dates; record a guardian dispute hold; return a student to active status; execute promotion, retention, or conditional promotion from an approved recommendation; process an internal transfer; confirm or reject a re-enrollment submission.

**Navigation Context.** Accessed from the lifecycle worklist. It links to `SCR-STU-02` for identity and placement context. Parent re-enrollment submissions arrive from `SCR-SELF-02`. Approved progression recommendations arrive from `SCR-ACD-09`. The workbench consumes cross-domain decisions; it does not recalculate them.

> [!NOTE]
> **Stage 2 Implementation Scope:** Full operational capability. Student Administration Officers process continuing-student administrative transitions: placing students on leave of absence with expected return dates, processing returns from leave, confirming annual re-enrollment submissions received from families, executing internal cohort/program transfers, and committing progression recommendations (promotion, retention, conditional promotion) issued by Academics.

---

#### `SCR-ACD-01` — Class Session Workbench

| Attribute | Definition |
|---|---|
| **Domain** | Academics |
| **Primary Actor** | Teacher |
| **Pattern** | Work Queue |
| **Operational Purpose** | Support the teacher's daily instructional session by recording what was delivered and who attended. |
| **Supported Use Cases** | `UC-ACD-02` Record Learning Activity Delivery; `UC-ACD-03` Record and Correct Student Attendance |

**Worklist.** The teacher's scheduled or recently held sessions that require a delivery record or attendance capture, including statutory homeroom roll-call where applicable. Sessions are grouped by date and learning unit, with exceptions such as missing attendance or submitted corrections.

**Workbench.** The selected session's learning unit, scheduled context, expected roster, curriculum content or delivery notes, attendance state, and any correction history. The roster is limited to students participating in that session.

**Available Actions.** Record session delivery and curriculum milestones; record attendance as Present, Absent, Late, or Excused; submit an attendance correction with a reason; review the resulting attendance summary for the session.

**Navigation Context.** Accessed from the teacher's daily worklist. Session context comes from `SCR-OPS-02`; teaching authorization comes from `SCR-TCH-02`. Attendance summaries feed `SCR-ACD-07` and may be referenced in `SCR-ACD-09`. Corrections are auditable actions within this screen.

> [!NOTE]
> **Stage 2 Implementation Scope:** Full operational capability. Teachers manage daily instructional sessions: logging curriculum milestones and pedagogical delivery against approved syllabi, recording student session attendance (Present, Absent, Late, Excused), and submitting auditable attendance corrections with mandatory justification.

---

#### `SCR-ACD-02` — Coursework & Assessment Workbench

| Attribute | Definition |
|---|---|
| **Domain** | Academics |
| **Primary Actor** | Teacher |
| **Pattern** | Work Queue |
| **Operational Purpose** | Plan, issue, and evaluate coursework and assessments for a learning unit. |
| **Supported Use Cases** | `UC-ACD-04` Issue Coursework Assignment; `UC-ACD-05` Plan and Administer Assessment; `UC-ACD-06` Evaluate Student Learning |

**Worklist.** Work items by learning unit and state: assignments or assessments to be planned or issued, issued work with submissions awaiting evaluation, evaluations in progress, and work ready for release of formative feedback. Summative result publication is not performed here.

**Workbench.** The selected assignment or assessment's instructions, criteria or rubric, due date, submission evidence, and evaluation state. For evaluation, the workbench shows the student's submitted evidence and the rubric; it does not expose other students' work beyond the selected evaluation task.

**Primary Information.** Learning unit and cohort, task purpose and type, instructions and criteria, due date, submission status, and evaluation or feedback state.

**Available Actions.** Create and issue a coursework assignment; schedule and administer a formative or summative assessment; score submitted evidence against the rubric; record provisional scores and qualitative feedback; publish routine formative feedback directly to students and parents; submit provisional summative results for moderation.

**Navigation Context.** Accessed from the teacher's learning-unit worklist. Assignment and assessment schedules may reference the operational calendar and exam timetable in `SCR-OPS-01`/`SCR-OPS-02`. Provisional summative results flow to `SCR-ACD-07` for approval and publication. Formative feedback is released directly under the teacher's authority.

> [!NOTE]
> **Stage 2 Implementation Scope:** Full operational capability. Teachers configure and issue coursework assignments and assessments, evaluate student evidence against rubrics, publish formative feedback directly to students and parents, and submit provisional summative marks for authority moderation.

---

#### `SCR-ACD-03` — Curriculum & Learning Structure Workspace

| Attribute | Definition |
|---|---|
| **Domain** | Academics |
| **Primary Actor** | Academic Coordinator |
| **Pattern** | Planning |
| **Operational Purpose** | Define and version the learning structure that teaching and participation depend on. |
| **Supported Use Cases** | `UC-ACD-08` Define Learning Structure |

**Plan Context.** The academic year or period, school level, program or stream, and the curriculum framework being adopted or revised.

**Plan Structure.** Learning units, syllabi, academic prerequisites, academic requirements, and the effective version of each structure. Related learning units are organized together rather than as isolated records.

**Validation and Approval.** Completeness and prerequisite checks, effective dating, version comparison, and the Academic Authority approval state. A structure becomes effective only after approval.

**Available Actions.** Create or revise a learning structure and its learning units; define prerequisites and requirements; prepare a new version; submit for Academic Authority approval; view the approved and historical versions.

**Navigation Context.** Accessed from the Academic Coordinator's planning area. Approved structures become available to `SCR-ACD-04`, `SCR-ACD-05`, and `SCR-TCH-02` for demand and assignment. The workspace does not register students or assign teachers.

> [!NOTE]
> **Stage 2 Implementation Scope:** Full operational capability. Academic Coordinators establish curriculum frameworks, learning units, syllabi, academic prerequisites, and versioned academic requirements, submitting them for Academic Authority approval to govern teaching and learning.

---

#### `SCR-ACD-04` — Learning Plan Workspace

| Attribute | Definition |
|---|---|
| **Domain** | Academics |
| **Primary Actor** | Academic Coordinator |
| **Pattern** | Planning |
| **Operational Purpose** | Formulate and maintain individualized learning plans for exceptional learners. |
| **Supported Use Cases** | `UC-ACD-09` Establish Student Learning Plan |

**Plan Context.** The selected student, academic period, current administrative placement, and the reason the individualized plan is required. A student has at most one active plan per period.

**Plan Structure.** Academic accommodations, course substitutions, exemptions, and other approved academic plan adjustments, each with its rationale and effective period. The plan references the student's learning structure and participation but does not change administrative enrollment or status.

**Validation and Approval.** Alignment with the approved learning structure, required supporting evidence, Academic Authority approval or re-approval, and the plan's active or historical state.

**Available Actions.** Create or revise a learning plan; add or remove approved adjustments; submit for approval; record approval, rejection, or revision; view the active and historical plans.

**Navigation Context.** Accessed from the Academic Coordinator's student-support planning area. The plan is consumed as context in `SCR-ACD-05` (registration) and `SCR-ACD-07` (evaluation and records). Pastoral recommendations from `SCR-DEV-03` may inform the plan but do not approve it.

> [!NOTE]
> **Stage 2 Implementation Scope:** Full operational capability. Academic Coordinators formulate and maintain individualized Student Learning Plans (SLPs) for exceptional learners, configuring accommodations, course substitutions, and exemptions for Academic Authority approval.

---

#### `SCR-ACD-05` — Participation & Roster Workbench

| Attribute | Definition |
|---|---|
| **Domain** | Academics |
| **Primary Actor** | Academic Coordinator |
| **Pattern** | Work Queue |
| **Operational Purpose** | Register eligible students into the correct learning units and resolve roster exceptions. |
| **Supported Use Cases** | `UC-ACD-10` Register Academic Participation |

**Worklist.** Students awaiting registration, registration requests requiring prerequisite review, students with learning-plan alignment issues, capacity exceptions, and elective-selection cases awaiting confirmation. Items are grouped by learning unit and academic period.

**Workbench.** The selected student's active administrative status and placement, the target learning unit and its prerequisites, learning-plan alignment, current roster capacity, and any Academic Authority waiver. The workbench shows only the student and learning unit in scope.

**Available Actions.** Verify eligibility and prerequisites; register the student into the learning unit; record a prerequisite waiver where authorized; resolve or return a capacity or plan-alignment exception; confirm the resulting roster.

**Navigation Context.** Accessed from the Academic Coordinator's roster worklist. It consumes student status from `SCR-STU-02`, learning structure from `SCR-ACD-03`, and learning plans from `SCR-ACD-04`. Registered participation enables `SCR-ACD-01` and `SCR-ACD-02`. It does not change administrative enrollment.

> [!NOTE]
> **Stage 2 Implementation Scope:** Full operational capability. Academic Coordinators register active students into learning units, verify prerequisites and SLP alignments, resolve elective selections and class capacity exceptions, and finalize official class rosters.

---

#### `SCR-ACD-06` — Academic Intervention Workbench

| Attribute | Definition |
|---|---|
| **Domain** | Academics |
| **Primary Actor** | Academic Coordinator |
| **Pattern** | Work Queue |
| **Operational Purpose** | Identify students at academic risk and establish, monitor, and conclude structured academic support. |
| **Supported Use Cases** | `UC-ACD-11` Initiate Academic Intervention |

**Worklist.** Students identified through progress review as at risk, intervention plans due for review, and plans approaching conclusion or requiring escalation. Items show the learning unit, risk basis, and next checkpoint.

**Workbench.** The selected student's progress evidence, attendance and submission patterns, current intervention plan, measurable targets, assigned support strategies, review checkpoints, and any pastoral referral. The workbench does not expose confidential counselling information.

**Available Actions.** Confirm or reject the risk identification; establish an intervention plan with targets and strategies; assign support responsibilities; schedule review checkpoints; record a review outcome; conclude or escalate the intervention; refer a non-academic concern to `SCR-DEV-03`.

**Navigation Context.** Accessed from the Academic Coordinator's progress worklist. Evidence comes from `SCR-ACD-01`, `SCR-ACD-02`, and `SCR-ACD-07`. The intervention plan is distinct from a Student Learning Plan and from a Development intervention plan; links preserve ownership.

> [!NOTE]
> **Stage 2 Implementation Scope:** Full operational capability. Academic Coordinators identify academically at-risk learners based on attendance and coursework performance, formulate remediation plans with measurable targets, and schedule review checkpoints.

---

#### `SCR-ACD-07` — Academic Records Workbench (Stage 2 Initial Implementation)

| Attribute | Definition |
|---|---|
| **Domain** | Academics |
| **Primary Actor** | Academic Coordinator for completion, report cards, and interim transcripts; Academic Authority for result approval, publication, and correction |
| **Pattern** | Work Queue |
| **Operational Purpose** | Conclude term learning participation, moderate provisional summative grades, publish official academic results, and produce certified period report cards and interim transcripts. |
| **Stage 2 Supported Use Cases** | `UC-ACD-12` Conclude Learning Unit Participation; `UC-ACD-13` Produce Report Card and Transcript (periodic/interim); `UC-ACD-16` Approve and Publish Academic Results; `UC-ACD-19` Correct Approved Academic Result; non-grade corrections for `UC-ACD-15` |
| **Stage 2 Implementation Scope** | End-of-period academic records operations: conclude learning participation (`Completed`/`Incomplete`) and tally credit; moderate and grant Academic Authority approval for summative results; publish official marks; produce and certify term report cards and interim academic transcripts; execute auditable result corrections with mandatory rationale. |
| **Dependencies** | `SCR-ACD-01` (attendance summaries), `SCR-ACD-02` (provisional scores), `SCR-ACD-05` (rosters). |

| Attribute | Definition |
|---|---|
| **Domain** | Academics |
| **Primary Actor** | Academic Coordinator for completion, report cards, and transcripts; Academic Authority for result approval, publication, and correction |
| **Pattern** | Work Queue |
| **Operational Purpose** | Conclude learning participation and process approved academic results into official records and reports. |
| **Supported Use Cases** | `UC-ACD-12` Conclude Learning Unit Participation; `UC-ACD-13` Produce Report Card and Transcript; `UC-ACD-16` Approve and Publish Academic Results; `UC-ACD-19` Correct Approved Academic Result |

**Worklist.** End-of-period work items by learning unit, cohort, or student: participation due for completion review, result sets awaiting moderation or approval, report cards or transcripts awaiting production, and correction requests for approved results. The worklist is filtered by the actor's authority.

**Workbench.** For completion, the selected participation's attendance, submission, and passing-score evidence and the proposed `Completed` or `Incomplete` outcome. For result approval, the provisional results, moderation evidence, and publication scope. For report cards and transcripts, the approved results, attendance summary, and certification state. For correction, the approved result, the requested change, the rationale, and the downstream impact.

**Available Actions.** Mark learning participation `Completed` or `Incomplete` and tally credit; moderate and approve formal summative results; publish approved results; produce and certify report cards or transcripts; correct an approved result with mandatory audit rationale and preserved previous value.

**Navigation Context.** Accessed from the academic records worklist. Inputs come from `SCR-ACD-01`, `SCR-ACD-02`, `SCR-ACD-05`, and `SCR-ACD-06`. Approved results feed `SCR-ACD-09`; report cards and transcripts are issued to students and families outside this workspace. Correction of non-grade records is handled as an authorized action in the owning screen, not here.

> [!WARNING]
> **Stage 2 Implementation Boundaries & Gated Features:**
> - Official final graduation transcript certification and production, conferral honors notation, and accredited external transfer-in credit integration (`UC-STD-18`) are deferred to **Stage 3**.

---

#### `SCR-ACD-09` — Cohort Academic Review (Stage 2 Initial Implementation)

| Attribute | Definition |
|---|---|
| **Domain** | Academics |
| **Primary Actor** | Academic Authority |
| **Pattern** | Work Queue |
| **Operational Purpose** | Make formal cohort and student academic determinations: determine academic standing, issue progression recommendations, and confer academic honors. |
| **Stage 2 Supported Use Cases** | `UC-ACD-17` Determine Academic Standing and Progression; `UC-ACD-21` Confer Academic Achievement; progression record corrections for `UC-ACD-15` |
| **Stage 2 Implementation Scope** | Review cohort performance against approved academic standards; assign academic standing (`Good Standing`, `Warning`, `Probation`); approve progression recommendations (`Promote`, `Retain`, `Conditional Promotion`) and transmit them to `SCR-STU-03`; formally approve and confer academic honors and distinctions. |
| **Dependencies** | `SCR-ACD-07` (approved academic results). |

| Attribute | Definition |
|---|---|
| **Domain** | Academics |
| **Primary Actor** | Academic Authority |
| **Pattern** | Work Queue |
| **Operational Purpose** | Make formal academic determinations for cohorts and students based on approved results. |
| **Supported Use Cases** | `UC-ACD-17` Determine Academic Standing and Progression; `UC-ACD-18` Certify Graduation Academic Eligibility; `UC-ACD-21` Confer Academic Achievement |

**Worklist.** Cohort and student review items by determination type: academic standing due, progression recommendation due, graduation eligibility due, achievement nomination or conferral due, and correction requests affecting a prior determination. Items are grouped by academic period and cohort.

**Workbench.** The selected student's or cohort's approved results, cumulative performance, attendance and completion context, the applicable standing, progression, graduation, or achievement criteria, and the proposed determination. For achievement, the supporting approved results or competition evidence are shown.

**Available Actions.** Determine academic standing; approve a progression recommendation (`Promote`, `Retain`, `Conditional Promotion`) and transmit it to Students; certify graduation eligibility (`Eligible`, `Ineligible`, or conditional recommendation) and transmit it; approve or confer an academic achievement; correct a prior determination where authorized, preserving the audit trail.

**Navigation Context.** Accessed from the Academic Authority's review worklist. Approved results come from `SCR-ACD-07`. Progression recommendations are consumed by `SCR-STU-03`; graduation eligibility is consumed by `SCR-STU-04`; achievements are published to the student's academic record and, where applicable, notified to Finance. The screen does not execute student lifecycle transitions or financial benefits.

---

### 4.4 Finance Domain Screens

> [!WARNING]
> **Stage 2 Implementation Boundaries & Gated Features:**
> - Certification of Graduation Academic Eligibility (`UC-ACD-18`) is deferred to **Stage 3**.

---

#### `SCR-FIN-05` — Financial Decisions Queue

| Attribute | Definition |
|---|---|
| **Domain** | Finance |
| **Primary Actor** | Finance Authority |
| **Pattern** | Work Queue |
| **Operational Purpose** | Review and authorize the financial decisions that require explicit Finance Authority approval under zero-discretionary-spending rules. |
| **Supported Use Cases** | `UC-FIN-11` Approve Financial Benefit; `UC-FIN-12` Establish Installment Payment Arrangement; `UC-FIN-13` Adjudicate Financial Dispute or Adjustment; `UC-FIN-14` Approve and Execute Refund |

**Worklist.** Prepared decision cases by type: benefit applications or nominations, installment proposals, billing disputes or adjustment recommendations, and refund dossiers. Each item shows the requester, student or payer, amount or impact, preparation status, and age.

**Workbench.** The selected case's evidence, policy basis, financial impact, prior account state, and the Finance Officer's validation. For a refund, the verified refundable balance and payer identity; for an adjustment or dispute, the obligation history and the proposed resolution; for an installment arrangement, the proposed schedule and hardship basis; for a benefit, the eligibility evidence and stacking implications.

**Available Actions.** Approve, deny, or return a case for further preparation; authorize a benefit or adjustment; establish an installment arrangement; approve a refund and authorize its execution; record mandatory rationale for every decision.

**Navigation Context.** Accessed from the Finance Authority's decision queue. Dossiers are prepared in `SCR-FIN-03` or `SCR-FIN-01`. Approved outcomes update the account in `SCR-FIN-03`. The queue groups these decisions because they share one operational context — binding financial authorization — not because they are the same transaction.

---

### 4.5 Operations Domain Screens

> [!NOTE]
> **Stage 2 Implementation Scope:** Full operational capability. Finance Authorities exercise zero-discretionary approval over prepared financial dossiers: approving scholarship/discount benefits and handling stacking rules, establishing binding installment arrangements, adjudicating billing disputes and balance adjustments, and approving student fee refunds.

---

#### `SCR-OPS-02` — Scheduling Workbench (Stage 2 Initial Implementation)

| Attribute | Definition |
|---|---|
| **Domain** | Operations |
| **Primary Actor** | Operations Coordinator; Operations Authority for overrides |
| **Pattern** | Work Queue |
| **Operational Purpose** | Evaluate operational feasibility, allocate rooms, resolve timetable conflicts, and maintain the institutional timetable for all instructional activities. |
| **Stage 2 Supported Use Cases** | `UC-OPS-03` Review Operational Feasibility; `UC-OPS-04` Schedule Activity and Allocate Room; `UC-OPS-05` Resolve Operational Resource Conflict; `UC-OPS-08` Record Room Restriction; `UC-OPS-09` Reschedule or Relocate Scheduled Occurrence; `UC-OPS-10` Close Operational Arrangement; `UC-OPS-13` Correct Approved Operations Record (room and timetable records) |
| **Stage 2 Implementation Scope** | Academic instructional scheduling and room management: review activity requests from teaching staff; allocate rooms and timetable slots for learning units and homeroom cohorts; resolve resource double-bookings; log physical room maintenance/restrictions; execute centralized rescheduling or relocation; close completed arrangements; correct timetable records. |
| **Dependencies** | `SCR-OPS-01` (calendar), `SCR-OPS-03` (requests), `SCR-TCH-02` (teaching assignments), `SCR-TCH-04` (teacher availability). |

| Attribute | Definition |
|---|---|
| **Domain** | Operations |
| **Primary Actor** | Operations Coordinator; Operations Authority for overrides |
| **Pattern** | Work Queue |
| **Operational Purpose** | Turn approved operational activity requests into feasible scheduled occurrences and keep the timetable and room allocations operationally valid. |
| **Supported Use Cases** | `UC-OPS-03` Review Operational Feasibility; `UC-OPS-04` Schedule Activity and Allocate Room; `UC-OPS-05` Resolve Operational Resource Conflict; `UC-OPS-08` Record Room Restriction; `UC-OPS-09` Reschedule or Relocate Scheduled Occurrence; `UC-OPS-10` Close Operational Arrangement; `UC-OPS-13` Correct Approved Operations Record (room and timetable records) |

**Worklist.** Operational work items by type: actionable activity requests awaiting feasibility review, approved activities awaiting room/slot allocation, schedule or room conflicts, room restrictions or unavailability windows, reschedule or relocation requests, and completed arrangements awaiting closure. Items show priority, source owner, required time or period, and status.

**Workbench.** The selected request or occurrence's source purpose, responsible actor, participants, required room characteristics and capacity, requested time, teacher availability context, affected occurrences, and proposed resolution. For a room restriction, the workbench shows the blocked window and the impacted schedule. For a conflict, it shows the competing demands and the applicable priority rule.

**Available Actions.** Return an incomplete request for clarification; assess feasibility and record constraints; allocate a room and commit a scheduled occurrence; resolve a conflict by alternative time, room, or escalation; record a room restriction and flag affected occurrences; reschedule or relocate an occurrence; close a completed arrangement and release the facility; correct an approved room or timetable record with justification.

**Navigation Context.** Accessed from the Operations Coordinator's scheduling worklist. Requests originate in `SCR-OPS-03`; teacher availability comes from `SCR-TCH-04`; the calendar comes from `SCR-OPS-01`; approved teaching assignments come from `SCR-TCH-02`. Published occurrences appear in `SCR-ACD-01` and the relevant participant views. The workbench coordinates operational resources but does not change the source activity or teaching assignment.

> [!WARNING]
> **Stage 2 Implementation Boundaries & Gated Features:**
> - Room allocations and scheduling for school-wide events (`UC-OPS-06`) and extracurricular program sessions (`UC-DEV-09`, `UC-DEV-10`) are disabled until **Stage 3**.

---

#### `SCR-OPS-03` — Operational Requests Portal

| Attribute | Definition |
|---|---|
| **Domain** | Operations |
| **Primary Actor** | Teacher / Academic Coordinator / Student Development Officer as requesting actor |
| **Pattern** | Work Queue |
| **Operational Purpose** | Allow an authorized requester to submit operational activity requests and cancel their own approved reservations. |
| **Supported Use Cases** | `UC-OPS-01` Submit Operational Activity Request; `UC-OPS-02` Cancel Approved Activity Reservation |

**Worklist.** The requester's own requests and reservations, grouped by state: draft or incomplete, submitted for feasibility review, approved and scheduled, change requested, cancelled, or closed. Items show the purpose, requested time or period, room, and current status.

**Workbench.** The selected request's source purpose, participants, facility or equipment requirements, scheduling preferences, feasibility or allocation status, and cancellation implications. For a cancellation, the workbench confirms the reservation to be released and the participants to be notified.

**Available Actions.** Create or complete an operational activity request; submit it for feasibility review; view its status and any clarification request; cancel an approved reservation that this actor initiated, immediately releasing the resource; view the resulting notification state.

**Navigation Context.** Accessed from the requesting actor's operational area. Submitted requests appear in `SCR-OPS-02`. Rescheduling or relocation is not available here; those require `SCR-OPS-02` under centralized rescheduling authority. The portal enforces the asymmetric autonomy rule: cancellation is autonomous for the initiator, rescheduling is not.

> [!NOTE]
> **Stage 2 Implementation Scope:** Full operational capability. Teaching and academic staff submit room and equipment reservations for instructional sessions, review feasibility progress, and exercise autonomous cancellation authority to release reservations back to the pool.

---

#### `SCR-TCH-01` — Faculty Resource Workspace

| Attribute | Definition |
|---|---|
| **Domain** | Teaching |
| **Primary Actor** | Teaching Coordinator; Teaching Authority for authorization decisions and corrections |
| **Pattern** | Work Queue |
| **Operational Purpose** | Maintain teacher readiness by managing professional profiles, qualification evidence, teaching authorizations, and approved-record corrections. |
| **Supported Use Cases** | `UC-TCH-02` Maintain Teaching Profile; `UC-TCH-03` Verify Teacher Qualifications; `UC-TCH-09` Determine Teaching Authorization; `UC-TCH-12` Correct Approved Teaching Record |

**Worklist.** Teacher-readiness work items: profile updates awaiting completion, qualification evidence awaiting verification or renewal, authorization decisions awaiting determination, and correction requests for approved teaching records. Items show the teacher, the missing or expiring element, and the responsible actor.

**Workbench.** The selected teacher's professional profile and institutional affiliation, qualification evidence and validity, verified capabilities, current authorization state and any supervision condition, and the history relevant to the current work item. Confidential qualification evidence is shown only to authorized actors.

**Available Actions.** Create or update a teaching profile; verify or reject qualification evidence and record validity; grant, restrict, suspend, or revoke a teaching authorization; record supervision requirements; correct an approved teaching record with mandatory justification.

**Navigation Context.** Accessed from the Teaching Coordinator's faculty-readiness worklist. Verified authorization and profile state feed `SCR-TCH-02` for assignment eligibility and `SCR-TCH-03` for workload. The workspace does not assign teachers to learning units.

> [!NOTE]
> **Stage 2 Implementation Scope:** Full operational capability. Teaching Coordinators and Teaching Authorities manage faculty profiles, verify qualification credentials and validities, formally determine teaching authorizations, record supervision conditions, and execute auditable record corrections.

---

#### `SCR-TCH-02` — Teaching Assignment & Coverage Workbench

| Attribute | Definition |
|---|---|
| **Domain** | Teaching |
| **Primary Actor** | Teaching Coordinator; Teaching Authority for binding approval and exceptions |
| **Pattern** | Work Queue |
| **Operational Purpose** | Match authorized teachers to teaching demand, commit assignments, handle changes, arrange emergency coverage, and conclude assignments. |
| **Supported Use Cases** | `UC-TCH-04` Process Teaching Demand; `UC-TCH-05` Evaluate Teacher Eligibility and Capacity; `UC-TCH-06` Arrange Substitute Teacher; `UC-TCH-08` Conclude Teaching Assignment; `UC-TCH-10` Approve Teaching Assignment; `UC-TCH-11` Modify or Cancel Teaching Assignment |

**Worklist.** Work items by type: teaching demand awaiting validation, demand awaiting candidate evaluation, assignment proposals awaiting approval, active assignments with change or cancellation requests, urgent coverage requests for absent teachers, and assignments due for end-of-period conclusion. Items show the learning unit or homeroom, required capability, period, and urgency.

**Workbench.** The selected demand or assignment's learning unit or homeroom context, required subject and grade capability, candidate teachers with verified authorization and available capacity, workload impact, and — for coverage — the affected session and substitute eligibility. For changes, the workbench shows the current commitment and the proposed replacement or cancellation. For conclusion, it shows grade-submission and handover status.

**Available Actions.** Validate and clarify teaching demand; evaluate eligible and available candidates; propose a teaching assignment; grant binding approval; arrange a substitute from the pre-authorized pool or escalate an exception; modify, suspend, or cancel an assignment; conclude an assignment and release capacity.

**Navigation Context.** Accessed from the Teaching Coordinator's assignment worklist. Demand originates from Academics (`SCR-ACD-03`/`SCR-ACD-05`); authorization and qualification come from `SCR-TCH-01`; availability comes from `SCR-TCH-04`; workload context comes from `SCR-TCH-03`. Approved assignments authorize teaching in `SCR-ACD-01` and scheduling in `SCR-OPS-02`. Assignments do not alter academic participation or room allocation.

> [!NOTE]
> **Stage 2 Implementation Scope:** Full operational capability. Teaching Coordinators and Teaching Authorities intake teaching demand, evaluate faculty competencies and workload, approve binding teaching assignments (including homeroom advisory roles), arrange emergency substitute teachers from authorized pools, and conclude completed assignments.

---

#### `SCR-TCH-03` — Faculty Workload Monitoring

| Attribute | Definition |
|---|---|
| **Domain** | Teaching |
| **Primary Actor** | Teaching Coordinator |
| **Pattern** | Monitoring |
| **Operational Purpose** | Monitor faculty teaching load against policy thresholds and identify capacity conflicts requiring rebalancing or exception. |
| **Supported Use Cases** | `UC-TCH-07` Monitor Faculty Workload |

**Overview.** Aggregate teaching load by teacher, department, subject, or period, with threshold indicators for approaching limit, at limit, and capacity exceeded. The overview highlights exceptions rather than presenting an exhaustive metric dashboard.

**Drill-down.** The selected teacher's approved assignments, workload contribution, available capacity, and the demand or assignment that caused the exception. The drill-down links to the relevant assignment in `SCR-TCH-02`.

**Available Actions.** Investigate an overload or under-allocation; initiate rebalancing through `SCR-TCH-02`; prepare a workload exception recommendation; record the Teaching Authority's exception decision; review the resulting workload state.

**Navigation Context.** Accessed from the Teaching Coordinator's monitoring area. It consumes approved assignments from `SCR-TCH-02` and authorization context from `SCR-TCH-01`. It does not create or approve assignments; those actions remain in `SCR-TCH-02`.

> [!NOTE]
> **Stage 2 Implementation Scope:** Full operational capability. Teaching Coordinators monitor faculty teaching hours against institutional workload thresholds, identify overloaded teachers, and initiate workload rebalancing or overload exceptions.

---

#### `SCR-TCH-04` — My Teaching Availability

| Attribute | Definition |
|---|---|
| **Domain** | Teaching |
| **Primary Actor** | Teacher |
| **Pattern** | Planning |
| **Operational Purpose** | Allow a teacher to declare weekly availability, preferred teaching blocks, and unavailable periods for scheduling. |
| **Supported Use Cases** | `UC-TCH-01` Declare Teaching Availability |

**Plan Context.** The relevant academic period or planning horizon and the teacher's current assignment and scheduling context.

**Plan Structure.** Weekly working availability, preferred teaching blocks, recurring unavailable windows, and dated exceptions such as approved leave or personal constraints. The plan is organized by the period it applies to.

**Validation and Approval.** Effective dates, overlap or contradiction checks, and the recorded state that makes the availability visible to Teaching and Operations. Availability informs feasibility; it does not itself create a teaching assignment or a scheduled occurrence.

**Available Actions.** Declare or revise availability; add or remove unavailable windows; submit the availability for the applicable period; view the current and historical availability profile.

**Navigation Context.** Accessed from the teacher's self-service area. Availability is consumed by `SCR-TCH-02` for capacity evaluation and `SCR-OPS-02` for scheduling feasibility. The teacher does not schedule rooms or commit timetable occurrences.

---

### 4.7 Development Domain Screens

> [!NOTE]
> **Stage 2 Implementation Scope:** Full operational capability. Teachers declare weekly working availability, preferred teaching blocks, and recurring or dated unavailable periods (leaves, personal constraints) to inform timetable scheduling.

---

#### 3.3.2 Incremental Capability Extensions to Stage 1 Screens

#### `SCR-SELF-02` — Family Tasks & Requests Portal (Stage 2 Incremental Capabilities)

| Attribute | Capability Extension |
|---|---|
| **Screen ID** | `SCR-SELF-02` (retained identity) |
| **Domain** | Students / Finance |
| **Primary Actor** | Parent / Guardian |
| **Pattern** | Work Queue |
| **New Stage 2 Use Cases** | `UC-STD-02` Submit Re-enrollment Confirmation; family request inputs for `UC-STD-07` Place Student on Leave and `UC-STD-10` Process Internal Transfer |
| **Stage 2 Incremental Scope** | Expanded family administrative requests: families can confirm continuation intent during annual re-enrollment windows, provide updated contact details, and submit formal guardian requests for leave of absence or internal cohort/program transfers. |
| **Dependencies** | `SCR-STU-03` (receives re-enrollment and leave/transfer requests). |

**Incremental Worklist Additions.** Pending annual re-enrollment confirmation tasks for continuing students; active guardian requests for student leave or internal program transfer.

**Incremental Workbench Additions.** Re-enrollment task workspace displaying continuation options, target academic period, and mandatory contact/emergency update forms; Leave request form capturing start date, expected return date, and reason; Internal transfer request form capturing requested grade level, stream, or campus.

**Incremental Available Actions.** Formally submit re-enrollment confirmation or intent to withdraw; update and certify family emergency contacts; submit leave of absence requests; submit internal transfer requests; track submission status.

> [!WARNING]
> **Remaining Gated Features (Deferred to Stage 3):**
> - External Transfer Out (`UC-STD-11`) and Withdrawal (`UC-STD-12`) requests remain disabled until Stage 3.
> - Secondary-guardian dispute and conflicting-directive recording are disabled until Stage 3.
> - Parental consent tasks for extracurricular programs (`UC-DEV-02`) are disabled until Stage 3.

---

#### `SCR-STU-01` — Student Intake Workbench (Stage 2 Incremental Capabilities)

| Attribute | Capability Extension |
|---|---|
| **Screen ID** | `SCR-STU-01` (retained identity) |
| **Domain** | Students (with Academics assessment integration) |
| **Primary Actor** | Admission Officer; Assessment Officer |
| **Pattern** | Work Queue |
| **New Stage 2 Use Cases** | `UC-ACD-07` Conduct Applicant Entrance Assessment (automated Academics integration) |
| **Stage 2 Incremental Scope** | Integration of Academic Entrance Assessment: replaces Stage 1 manual external score entry with automated diagnostic test request dispatch to Academics, diagnostic scoring via role-specific assessment panels, and automated consumption of approved assessment results into admission review cases. |
| **Dependencies** | Academics Assessment & Results module (`ACD-WF-20`). |

**Incremental Worklist Additions.** Applications queued for entrance assessment; diagnostic tests administered awaiting scoring; approved scores ready for admission committee review.

**Incremental Workbench Additions.** Dedicated Assessment Officer view: test scheduling details, standardized scoring rubrics, evaluator notes, diagnostic score reports, and automatic score ingestion into the applicant's master review dossier.

**Incremental Available Actions.** Schedule diagnostic entrance assessment session; record diagnostic scores against standardized criteria; approve and transmit score reports directly into the admission review workflow; view Academics assessment status without leaving the intake case.

> [!WARNING]
> **Remaining Gated Features (Deferred to Stage 3):**
> - External Transfer In (`UC-STD-18`) processing, including prior-credit evaluation, origin-school transcript evaluation, and multi-year credit mapping, remains disabled until Stage 3.

---

#### `SCR-FIN-03` — Student Accounts Workbench (Stage 2 Incremental Capabilities)

| Attribute | Capability Extension |
|---|---|
| **Screen ID** | `SCR-FIN-03` (retained identity) |
| **Domain** | Finance |
| **Primary Actor** | Finance Officer; Finance Authority for corrections |
| **Pattern** | Work Queue |
| **New Stage 2 Use Cases** | `UC-FIN-06` Monitor Overdue Receivables; `UC-FIN-07` Manage Overpayment and Account Credit; `UC-FIN-15` Correct Approved Finance Record |
| **Stage 2 Incremental Scope** | Receivables stewardship and credit management: overdue account monitoring across aging buckets (30/60/90+ days), payment reminder dispatch and log tracking, overpayment handling, credit balance reallocation, refund dossier preparation for `SCR-FIN-05`, and auditable corrections to approved finance records. |
| **Dependencies** | `SCR-FIN-05` (Finance Authority decision queue for refunds/arrangements). |

**Incremental Worklist Additions.** Overdue accounts categorized by aging bucket; accounts with unallocated or excess credit balances; submitted correction requests for finalized finance records.

**Incremental Workbench Additions.** Detailed debt aging analysis; history of dispatched reminders and guardian communications; credit allocation tools; refund dossier preparation workspace; auditable record correction panel with previous-value preservation and mandatory justification.

**Incremental Available Actions.** Dispatch payment reminders to primary payers/guardians; record follow-up payment notes; apply account credit balances against eligible open obligations; compile and forward refund dossiers or installment proposals to `SCR-FIN-05`; execute auditable finance record corrections under Finance Authority authorization.

> [!WARNING]
> **Remaining Gated Features (Deferred to Stage 3):**
> - Financial Clearance evaluation (`UC-FIN-08`) for outgoing transfer and graduation remains disabled until Stage 3.

---

#### `SCR-OPS-01` — Calendar & Periods Workspace (Stage 2 Incremental Capabilities)

| Attribute | Capability Extension |
|---|---|
| **Screen ID** | `SCR-OPS-01` (retained identity) |
| **Domain** | Operations |
| **Primary Actor** | Operations Authority |
| **Pattern** | Schedule Management |
| **New Stage 2 Capabilities** | Automated campus closure schedule-impact coordination (`UC-OPS-12` Stage 2 scope) |
| **Stage 2 Incremental Scope** | Timetable disruption handoff: declaring scheduled or emergency campus closures automatically identifies affected timetable occurrences, logs facility impacts, and dispatches disruption dossiers to `SCR-OPS-02` for mass cancellation or rescheduling. |
| **Dependencies** | `SCR-OPS-02` (Scheduling Workbench). |

**Incremental Workspace Additions.** Disruption impact preview showing total timetabled occurrences, affected teachers, learning units, and booked facilities across the declared closure date range.

**Incremental Available Actions.** Trigger schedule-impact calculation upon declaring closure; transmit closure event to `SCR-OPS-02` with automated instructions to release booked rooms and notify affected participants.

---

### 3.4 Stage 2 Operational Summary

| Metric | Count | Details |
|---|:---:|---|
| **Screens Introduced** | **17** | `SCR-SELF-03`, `SCR-STU-03`, `SCR-ACD-01`, `SCR-ACD-02`, `SCR-ACD-03`, `SCR-ACD-04`, `SCR-ACD-05`, `SCR-ACD-06`, `SCR-ACD-07`, `SCR-ACD-09`, `SCR-FIN-05`, `SCR-OPS-02`, `SCR-OPS-03`, `SCR-TCH-01`, `SCR-TCH-02`, `SCR-TCH-03`, `SCR-TCH-04` |
| **Screens Extended** | **4** | `SCR-SELF-02`, `SCR-STU-01`, `SCR-FIN-03`, `SCR-OPS-01` |
| **Total Active Screens** | **21** | Complete teaching and learning lifecycle, faculty assignments, timetabling, and expanded finance. |

**Major Operational Capabilities Supported:**
- Curriculum framework and learning unit configuration with versioned academic policies (`SCR-ACD-03`).
- Teacher professional profile maintenance, credential verification, and official teaching authorizations (`SCR-TCH-01`).
- Teacher availability declaration and timetable scheduling constraints capture (`SCR-TCH-04`).
- Intake of teaching demand, teacher assignment matching, binding authorization, and emergency coverage (`SCR-TCH-02`).
- Real-time faculty teaching workload monitoring against institutional caps (`SCR-TCH-03`).
- Coursework and assessment planning, assignment publishing, and student coursework submission (`SCR-ACD-02`, `SCR-SELF-03`).
- Instructional session delivery tracking, curriculum logging, and session roll-call attendance capture (`SCR-ACD-01`).
- Formative evaluation publishing directly to students and provisional summative scoring (`SCR-ACD-02`).
- Summative score moderation, official result publishing, period report cards, and interim transcripts (`SCR-ACD-07`).
- Academic standing determinations, promotion/retention recommendations, and academic honors conferral (`SCR-ACD-09`).
- Execution of continuing student administrative transitions (leave, return, internal transfer, promotion, retention) (`SCR-STU-03`).
- Annual student re-enrollment confirmation by families and administrative processing (`SCR-SELF-02`, `SCR-STU-03`).
- Direct integration of entrance assessment results from Academics into admission reviews (`SCR-STU-01`).
- Full receivables stewardship: aging analysis, reminder dispatch, credit balance handling, and record corrections (`SCR-FIN-03`).
- Zero-discretionary Finance Authority approval for scholarships, installment plans, fee disputes, and refunds (`SCR-FIN-05`).
- Operational timetable creation, room/homeroom allocation, conflict resolution, and closure rescheduling (`SCR-OPS-02`, `SCR-OPS-01`).

---

## 4. Stage 3 — Student Development, Advanced Lifecycle, and Operational Maturity

### 4.1 Stage Overview, Objectives, and Completion Criteria

**Stage Theme:** Student development + advanced lifecycle + operational maturity.

**Stage Objective:** Enable the school to manage behaviour, discipline, counselling, wellbeing, extracurricular programs, non-academic achievements, and the advanced lifecycle transitions of transfer-out, withdrawal, and graduation (including cross-domain academic and financial clearances). Complete operational coordination with school events and announcements.

**Prerequisites:** Stage 1 Foundation and Stage 2 Academic/Teaching/Finance capabilities.

**Stage Completion Criteria (Roadmap Section D):**
1. A behaviour observation (positive and concerning) is recorded. An incident is triaged and a discipline case is investigated and determined.
2. A development referral is accepted, routed, and results in a counselling or wellbeing case with appropriate confidentiality controls.
3. A non-academic intervention plan is established, reviewed, and closed.
4. An extracurricular program is established with student participation recorded. A non-academic achievement is recognized.
5. Graduation eligibility is determined by Academics and provided to Students.
6. Academic clearance for purpose Transfer Out is requested, determined, and consumed.
7. Financial clearance for purposes Transfer Out and Graduation is requested, determined, and consumed.
8. An external transfer-out is executed with both academic and financial clearance Granted.
9. A withdrawal is formally processed with proper authorization and history.
10. Graduation is executed with academic eligibility and financial clearance (when required). Alumni status is activated automatically.
11. A school event is proposed, approved, published, and completed through its operational lifecycle.
12. An operational announcement is published to a defined audience, corrected, and withdrawn.
13. A cross-domain Development recommendation (e.g., discipline outcome recommending withdrawal) is issued and consumed by Students.

---

### 4.2 Stage 3 Screen Inventory Table

The table below lists all 16 screens operational in Stage 3 (10 newly introduced + 6 extended from earlier stages):

| Screen ID | Screen Name | Domain | Primary Actor | Implementation Scope | Dependencies |
|---|---|---|---|---|---|
| `SCR-SELF-04` | Extracurricular Registration | Development | Student | Browse open programs, review charters/schedules/capacity, submit registrations, and track parental consent status. | `SCR-DEV-05`, `SCR-STU-02` |
| `SCR-SELF-05` | Support Referral | Development | Student / Parent / Guardian | Submit confidential pastoral, wellbeing, or counselling support requests with urgency flags and privacy preferences. | `SCR-STU-02` |
| `SCR-STU-04` | Student Exit & Graduation Workbench | Students | Student Administration Officer | Terminating transitions: request/track academic & financial clearance, execute transfer-out, withdrawal, graduation, and alumni activation. | `SCR-ACD-08`, `SCR-FIN-03`, `SCR-ACD-09` |
| `SCR-ACD-08` | Academic Clearance Review | Academics | Academic Coordinator; Academic Authority | Evaluate academic obligations/blockers for outgoing transfer students; issue formal clearance determinations and corrections. | `SCR-STU-04`, `SCR-ACD-07` |
| `SCR-OPS-04` | Events & Announcements Workspace | Operations | Operations Coordinator | End-to-end logistics coordination for school events; compose, schedule, publish, correct, or withdraw operational announcements. | `SCR-OPS-01`, `SCR-OPS-02` |
| `SCR-DEV-01` | Behaviour & Incident Triage Workbench | Development | Student Development Officer; Teacher / Reporter | Record factual behaviour observations and incident reports; triage incidents into pastoral support or formal discipline routes; record corrections. | `SCR-STU-02` |
| `SCR-DEV-02` | Discipline Case Workbench | Development | Discipline Authority | Conduct formal investigations preserving due process: hearings, evidence, findings, sanctions, severe administrative recommendations, and appeals. | `SCR-DEV-01` |
| `SCR-DEV-03` | Pastoral Support Workbench | Development | Student Development Officer | Route non-clinical referrals; manage wellbeing support plans and non-academic interventions; issue sanitized pastoral recommendations; record corrections. | `SCR-SELF-05`, `SCR-DEV-01` |
| `SCR-DEV-04` | Counselling Case Workspace | Development | Counselor | Manage confidential intake, care goals, clinical session notes, and consent in complete isolation; safety escalations; record corrections. | `SCR-DEV-03` / `SCR-SELF-05` |
| `SCR-DEV-05` | Extracurricular & Achievement Workspace | Development | Student Development Officer | Establish extracurricular program charters; approve rosters after consent verification; track attendance; confer non-academic achievements; record corrections. | `SCR-SELF-04`, `SCR-OPS-02` |
| `SCR-SELF-02` | Family Tasks & Requests Portal | Students / Finance / Dev | Parent / Guardian | **Stage 3 Extension:** Submit exit requests (transfer-out, withdrawal), lodge guardian dispute/conflicting directives, and complete extracurricular consent tasks. | `SCR-STU-04`, `SCR-DEV-05` |
| `SCR-STU-01` | Student Intake Workbench | Students | Student Administration Officer | **Stage 3 Extension:** Process External Transfer In (`UC-STD-18`), capture origin-school history, coordinate prior-credit evaluation with Academics, and execute enrollment. | Academics prior-credit evaluation |
| `SCR-FIN-03` | Student Accounts Workbench | Finance | Finance Officer; Finance Authority | **Stage 3 Extension:** Evaluate purpose-specific Financial Clearance (`UC-FIN-08`) for transfer-out and graduation; record blockers and clearance exceptions. | `SCR-STU-04` |
| `SCR-OPS-02` | Scheduling Workbench | Operations | Operations Coordinator | **Stage 3 Extension:** Operational scheduling and facility/venue allocation for school events (`UC-OPS-06`) and extracurricular recurring sessions (`UC-DEV-09`/`10`). | `SCR-OPS-04`, `SCR-DEV-05` |
| `SCR-ACD-07` | Academic Records Workbench | Academics | Academic Coordinator; Academic Authority | **Stage 3 Extension:** Compile, certify, and produce official final graduation transcripts with honors notations, and integrate accredited external transfer credits. | `SCR-ACD-09`, `SCR-STU-04` |
| `SCR-ACD-09` | Cohort Academic Review | Academics | Academic Authority | **Stage 3 Extension:** Cohort and student-level audit of graduation requirements; formally certify Graduation Academic Eligibility (`UC-ACD-18`) for `SCR-STU-04`. | `SCR-ACD-07` |

---

### 4.3 Stage 3 Detailed Screen Definitions

#### 4.3.1 Newly Introduced Screens in Stage 3

#### `SCR-SELF-04` — Extracurricular Registration

| Attribute | Definition |
|---|---|
| **Domain** | Development |
| **Primary Actor** | Student |
| **Pattern** | Information Lookup |
| **Operational Purpose** | Allow a student to register for an approved extracurricular program during its registration window. |
| **Supported Use Cases** | `UC-DEV-02` Register for Extracurricular Program |

**Structure.** Search/List shows approved programs open for registration, with eligibility, schedule context, and capacity status. Detail shows the selected program's charter, participation expectations, consent requirement, and the student's registration status.

**Primary Information.** Program identity and purpose, registration window, eligibility, schedule context, capacity, parental-consent requirement, and the student's registration or waiting status.

**Available Actions.** Submit a registration, withdraw a pending registration where policy permits, and view the consent or confirmation status.

**Navigation Context.** Accessed from the student entry point. Registrations appear to the Student Development Officer in `SCR-DEV-05` for consent verification and roster approval. The student does not see other applicants' details.

> [!NOTE]
> **Stage 3 Implementation Scope:** Full operational capability. Students browse approved extracurricular programs, review charters, schedules, capacity, and parental consent requirements, submit activity registrations, and monitor roster/waitlist status.

---

#### `SCR-SELF-05` — Support Referral

| Attribute | Definition |
|---|---|
| **Domain** | Development |
| **Primary Actor** | Student / Parent / Guardian |
| **Pattern** | Information Lookup |
| **Operational Purpose** | Allow a student or guardian to request non-academic pastoral, wellbeing, or counselling support while preserving privacy at intake. |
| **Supported Use Cases** | `UC-DEV-01` Submit Development Referral |

**Structure.** Search/List shows the requester's own referrals and their status. Detail shows the selected referral's stated concern, urgency indication, preferred contact or privacy request, and the intake status. Clinical notes and internal triage reasoning are never shown.

**Primary Information.** Referral subject, stated concern, urgency indication, privacy preference, and intake or routing status.

**Available Actions.** Submit a self-referral or guardian referral, add information requested during intake, and view whether the referral has been received and routed. Initial intake does not require prior parental consent.

**Navigation Context.** Accessed from the student or family entry point. Referrals appear to the Student Development Officer in `SCR-DEV-03` for routing and, when clinically appropriate, to the Counselor in `SCR-DEV-04`. The referral screen never exposes counselling notes.

---

### 4.2 Students Domain Screens

> [!NOTE]
> **Stage 3 Implementation Scope:** Full operational capability. Students and parents submit confidential non-academic pastoral or counselling support requests with urgency flags and privacy requests, tracking intake status without exposing clinical notes or internal triage reasoning.

---

#### `SCR-STU-04` — Student Exit & Graduation Workbench

| Attribute | Definition |
|---|---|
| **Domain** | Students (with Academics and Finance clearance results) |
| **Primary Actor** | Student Administration Officer |
| **Pattern** | Work Queue |
| **Operational Purpose** | Process terminating student transitions — external transfer-out, withdrawal, and graduation — including the required clearance coordination. |
| **Supported Use Cases** | `UC-STD-11` Process External Transfer Out; `UC-STD-12` Process Withdrawal; `UC-STD-13` Execute Graduation; `UC-STD-14` Activate Alumni Status; `UC-STD-15` Request Academic Clearance for Transfer Out; `UC-STD-16` Request Purpose-Specific Financial Clearance |

**Worklist.** Open exit cases by type: transfer-out, withdrawal, and graduation. Each case shows its stage — request received, clearances requested, clearance result pending, ready to execute, executed, or on hold. Alumni activation is shown as the automatic outcome of a completed graduation, not as a separate task.

**Workbench.** The selected student's identity and current placement, exit type and reason, requesting guardian or independent-student authorization, leadership approval where required, any guardian dispute hold, academic and financial clearance status for the stated purpose, effective date, and the resulting administrative outcome.

**Available Actions.** Request academic and financial clearance for the correct purpose; record or update the exit reason and authorization; place a case on dispute hold; execute transfer-out when both required clearances are `Granted`; execute withdrawal with its authorizing decision; execute graduation when academic eligibility and any required financial clearance are satisfied; confirm the automatic alumni outcome.

**Navigation Context.** Accessed from the exit worklist. Academic clearance requests appear in `SCR-ACD-08`; financial clearance requests appear in `SCR-FIN-03`. The workbench displays only the clearance result and blockers needed for the exit decision, not unrelated academic or financial internals. Completed exits feed the student's administrative history in `SCR-STU-02`.

---

### 4.3 Academics Domain Screens

> [!NOTE]
> **Stage 3 Implementation Scope:** Full operational capability. Student Administration Officers manage terminating student transitions: coordinating purpose-specific academic clearance from `SCR-ACD-08` and financial clearance from `SCR-FIN-03`, handling guardian disputes/conflicts, executing external transfer-out upon dual clearance, executing formalized withdrawals, executing graduation upon satisfaction of academic eligibility and financial obligations, and confirming automatic activation of alumni status.

---

#### `SCR-ACD-08` — Academic Clearance Review

| Attribute | Definition |
|---|---|
| **Domain** | Academics |
| **Primary Actor** | Academic Coordinator for routine determination; Academic Authority for exception or correction |
| **Pattern** | Work Queue |
| **Operational Purpose** | Evaluate an outgoing student's academic obligations and issue the Academic Clearance result required for transfer-out. |
| **Supported Use Cases** | `UC-ACD-14` Determine Academic Clearance; `UC-ACD-20` Adjudicate Academic Clearance Correction |

**Worklist.** Clearance requests from Students, grouped by purpose, needed-by date, and status: awaiting review, determined, denied with blockers, or correction requested. Only Transfer Out is in the current purpose scope.

**Workbench.** The selected student and purpose, the academic obligations under review, identified blockers, evidence of resolution, the determination and reason, and the correction history where applicable. It does not expose unrelated financial or pastoral information.

**Available Actions.** Record a routine `Granted` or `Denied` determination with reasons and blockers; escalate an exceptional case to the Academic Authority; authorize a correction to a prior determination with mandatory rationale; notify Students of the corrected result.

**Navigation Context.** Accessed from the academic clearance worklist. Requests originate in `SCR-STU-04`. The result is consumed there; this screen does not execute transfer-out or change student status. Correction is a controlled action within this screen, not a separate workspace.

> [!NOTE]
> **Stage 3 Implementation Scope:** Full operational capability. Academic Coordinators and Academic Authorities evaluate academic obligations, outstanding coursework, and library/equipment returns for outgoing learners, issuing formal `Granted` or `Denied` clearance determinations for transfer-out and adjudicating auditable clearance corrections.

---

#### `SCR-OPS-04` — Events & Announcements Workspace

| Attribute | Definition |
|---|---|
| **Domain** | Operations |
| **Primary Actor** | Operations Coordinator |
| **Pattern** | Work Queue |
| **Operational Purpose** | Coordinate approved school events and publish, correct, or withdraw official operational announcements. |
| **Supported Use Cases** | `UC-OPS-06` Coordinate School Event Logistics; `UC-OPS-07` Publish Operational Announcement |

**Worklist.** Event items by stage — proposed, awaiting approval, logistics in preparation, scheduled, completed — and announcement items by state — draft, scheduled, published, correction pending, withdrawn. Items show the event or audience, required approval, and publication timing.

**Workbench.** For an event, the event purpose and organizer, required facilities and security, operational approvals, linked room and schedule arrangements, and publication state. For an announcement, the content, target audience, channel, timing, required authorization, and correction or withdrawal history. The two work item types share the workspace because both are operational publication and coordination tasks, but their workbenches remain distinct.

**Available Actions.** Coordinate event logistics and facility reservations; record required approvals; publish an approved event to the calendar; draft, schedule, publish, correct, or withdraw an announcement within the actor's authority; record the resulting audience and publication state.

**Navigation Context.** Accessed from the Operations Coordinator's events and communications worklist. Event facility needs are fulfilled through `SCR-OPS-02`; calendar context comes from `SCR-OPS-01`. Critical announcements requiring Operations Authority authorization are escalated within the workspace. The screen does not own academic, development, or financial event content.

---

### 4.6 Teaching Domain Screens

> [!NOTE]
> **Stage 3 Implementation Scope:** Full operational capability. Operations Coordinators manage the complete operational lifecycle of approved school events (logistics, security, facilities) and compose, schedule, publish, correct, or withdraw official school announcements to target audiences across approved channels.

---

#### `SCR-DEV-01` — Behaviour & Incident Triage Workbench

| Attribute | Definition |
|---|---|
| **Domain** | Development |
| **Primary Actor** | Student Development Officer for triage; Teacher / Authorized Reporter for recording |
| **Pattern** | Work Queue |
| **Operational Purpose** | Record factual behaviour observations and incident reports, then triage incidents into pastoral support or formal discipline. |
| **Supported Use Cases** | `UC-DEV-03` Record Behaviour Observation; `UC-DEV-04` Report Behaviour Incident; `UC-DEV-05` Triage Behaviour Incident |

**Worklist.** New or returned observations and incident reports awaiting review, incidents awaiting triage, and triaged incidents awaiting routing confirmation. Items show the student, event date, reporter, severity indication, and status.

**Workbench.** The selected report's factual account, date and location, involved individuals, evidence, prior relevant observations, and current triage state. The workbench distinguishes observation from allegation and from formal finding. Access to prior development history is limited to what triage requires.

**Available Actions.** Record a positive recognition or factual behaviour observation; submit an incident report; review an incident for severity and risk; assign the handling route — pastoral support or formal disciplinary investigation; record the triage rationale; route the case to `SCR-DEV-03` or `SCR-DEV-02`.

**Navigation Context.** Accessed from the Development reporting and triage worklist. Reporters can create observations and incidents; the Student Development Officer performs triage. Routed pastoral cases appear in `SCR-DEV-03`; escalated discipline cases appear in `SCR-DEV-02`. The workbench does not determine discipline findings.

> [!NOTE]
> **Stage 3 Implementation Scope:** Full operational capability. Teachers and authorized reporters record factual behaviour observations and incident reports; Student Development Officers triage incidents against severity thresholds, routing cases to pastoral support (`SCR-DEV-03`) or formal disciplinary investigation (`SCR-DEV-02`), and execute auditable behaviour record corrections.

---

#### `SCR-DEV-02` — Discipline Case Workbench

| Attribute | Definition |
|---|---|
| **Domain** | Development |
| **Primary Actor** | Discipline Authority |
| **Pattern** | Work Queue |
| **Operational Purpose** | Conduct a fair formal investigation, determine a discipline case, and recommend severe administrative sanctions where warranted. |
| **Supported Use Cases** | `UC-DEV-14` Investigate and Determine Discipline Case; `UC-DEV-15` Recommend Disciplinary Administrative Sanction |

**Worklist.** Formal discipline cases by stage: investigation opened, evidence collection, hearing or response awaited, determination due, sanction recorded, appeal escalated, and administrative-sanction recommendation pending. Items show the student, alleged infraction category, stage, and age.

**Workbench.** The selected case's incident report, evidence, hearing or student response, factual findings, applicable policy and sanction limits, prior relevant discipline history, and appeal state. Confidential counselling information is not exposed. The workbench separates allegation, evidence, finding, and sanction.

**Available Actions.** Open or continue an investigation; record evidence and hearing outcomes; determine factual findings; impose sanctions within policy limits; recommend suspension or disciplinary expulsion to School Leadership and Students; escalate a formal appeal to School Leadership; record the final case outcome.

**Navigation Context.** Accessed from the Discipline Authority's case worklist. Cases arrive from `SCR-DEV-01` triage. Severe administrative recommendations are consumed by `SCR-STU-04` for the eventual withdrawal transition; the discipline workspace does not change student status. Appeals are adjudicated by School Leadership outside the original determination, preserving structural separation.

> [!NOTE]
> **Stage 3 Implementation Scope:** Full operational capability. Discipline Authorities conduct formal investigations preserving due process: recording hearings and evidence, making factual findings, imposing policy sanctions, issuing recommendations for severe administrative sanctions (suspension, expulsion) to School Leadership and Students (`SCR-STU-04`), and managing appeal records.

---

#### `SCR-DEV-03` — Pastoral Support Workbench

| Attribute | Definition |
|---|---|
| **Domain** | Development |
| **Primary Actor** | Student Development Officer |
| **Pattern** | Work Queue |
| **Operational Purpose** | Route non-clinical support referrals and manage wellbeing support plans, non-academic interventions, and pastoral cross-domain recommendations. |
| **Supported Use Cases** | `UC-DEV-06` Route Development Referral; `UC-DEV-07` Manage Wellbeing Support Plan; `UC-DEV-08` Establish Non-Academic Intervention; `UC-DEV-12` Issue Pastoral Cross-Domain Recommendation |

**Worklist.** Support work items by type: new referrals awaiting routing, active wellbeing plans due for review, non-academic intervention plans due for review or conclusion, and cross-domain recommendations awaiting transmission or follow-up. Items show the student, support type, urgency, and next checkpoint.

**Workbench.** The selected student's non-clinical support context, the referral or plan purpose, agreed goals and accommodations, review checkpoints, participating staff, and the recommendation being issued. Clinical counselling notes are never shown; only sanitized, non-confidential recommendations may be referenced.

**Available Actions.** Classify referral urgency and route it to counselling or pastoral support; establish or revise a wellbeing support plan; establish, review, adjust, or conclude a non-academic intervention; issue a pastoral cross-domain recommendation to Academics or Students; record the receiving domain's response.

**Navigation Context.** Accessed from the Student Development Officer's support worklist. Referrals arrive from `SCR-SELF-05`, `SCR-DEV-01`, teachers, or families. Counselling cases remain in `SCR-DEV-04`; only sanitized recommendations cross into this workbench. Recommendations to Academics inform `SCR-ACD-04`; recommendations to Students inform `SCR-STU-03` or `SCR-STU-04`, but do not execute their transitions.

> [!NOTE]
> **Stage 3 Implementation Scope:** Full operational capability. Student Development Officers route non-clinical support referrals, formulate and review wellbeing support plans and non-academic intervention plans, issue sanitized pastoral recommendations to Academics (`SCR-ACD-04`) and Students (`SCR-STU-03`/`SCR-STU-04`), and execute auditable support record corrections.

---

#### `SCR-DEV-04` — Counselling Case Workspace

| Attribute | Definition |
|---|---|
| **Domain** | Development |
| **Primary Actor** | Counselor |
| **Pattern** | Work Queue |
| **Operational Purpose** | Manage confidential counselling intake, ongoing care, consent status, and safety escalation in an isolated clinical workspace. |
| **Supported Use Cases** | `UC-DEV-13` Manage Confidential Counselling Case |

**Worklist.** The Counselor's own counselling cases by state: intake pending, active care, consent awaited, review due, safety escalation, and closed. The worklist is visible only to the Counselor and authorized safeguarding leads as defined by policy.

**Workbench.** The selected case's intake information, clinical urgency, consent status, care goals, session record, and safety-escalation state. Clinical notes and session records are isolated to the Counselor and are never exposed to student, parent, academic, or administrative portals.

**Available Actions.** Conduct confidential intake without prior parental consent; assess urgency; record consent status according to policy; establish and revise the care plan; record session notes; issue sanitized non-confidential recommendations to `SCR-DEV-03`; escalate imminent risk or child-protection concerns to School Leadership under mandatory reporting.

**Navigation Context.** Accessed only from the Counselor's confidential workspace. Referrals arrive from `SCR-DEV-03` or directly from `SCR-SELF-05` where policy permits. Sanitized recommendations may leave the workspace; clinical content does not. Safeguarding escalation is a controlled action within this screen.

> [!NOTE]
> **Stage 3 Implementation Scope:** Full operational capability. School Counselors manage confidential intake, care goals, clinical session notes, and consent status in complete clinical isolation from administrative and academic portals, issuing sanitized non-clinical recommendations and executing immediate safety escalations to School Leadership.

---

#### `SCR-DEV-05` — Extracurricular & Achievement Workspace

| Attribute | Definition |
|---|---|
| **Domain** | Development |
| **Primary Actor** | Student Development Officer |
| **Pattern** | Work Queue |
| **Operational Purpose** | Establish extracurricular programs, manage participation and attendance, and certify non-academic achievements. |
| **Supported Use Cases** | `UC-DEV-09` Establish Extracurricular Program; `UC-DEV-10` Manage Extracurricular Participation and Attendance; `UC-DEV-11` Recognize Non-Academic Achievement |

**Worklist.** Work items by type: program proposals awaiting approval, registration requests awaiting consent or roster decision, active programs with attendance or completion review due, and achievement nominations awaiting verification or conferral. Items show the program or achievement, student or cohort, and status.

**Workbench.** For a program, its charter, eligibility, capacity, schedule context, roster, and consent state. For participation, the selected student's registration, consent, attendance, and completion evidence. For an achievement, the category, supporting evidence, and publication state. The workbench shows only the program or achievement in scope.

**Available Actions.** Establish or revise an extracurricular program charter and submit it for approval; approve or decline registrations after consent verification; record participation and attendance; certify program completion; verify and confer a non-academic achievement; publish the achievement to the student's holistic profile.

**Navigation Context.** Accessed from the Student Development Officer's extracurricular worklist. Student registrations arrive from `SCR-SELF-04`; facility and schedule needs are coordinated through `SCR-OPS-02` and `SCR-OPS-04`. Achievement records are published to the student's development profile; any financial merit consequence remains a Finance decision in `SCR-FIN-05`. The workspace does not manage academic achievements, which belong to `SCR-ACD-09`.

> [!NOTE]
> **Stage 3 Implementation Scope:** Full operational capability. Student Development Officers establish extracurricular program charters, approve participant rosters after parental consent verification, track activity attendance and program completion, verify and confer non-academic achievements to holistic student profiles, and execute auditable record corrections.

---

#### 4.3.2 Incremental Capability Extensions to Earlier Screens

#### `SCR-SELF-02` — Family Tasks & Requests Portal (Stage 3 Incremental Capabilities)

| Attribute | Capability Extension |
|---|---|
| **Screen ID** | `SCR-SELF-02` (retained identity) |
| **Domain** | Students / Finance / Development |
| **Primary Actor** | Parent / Guardian |
| **Pattern** | Work Queue |
| **New Stage 3 Use Cases** | Family request and authorization inputs for `UC-STD-11` Process External Transfer Out and `UC-STD-12` Process Withdrawal; parental consent tasks for `UC-DEV-02` Register for Extracurricular Program |
| **Stage 3 Incremental Scope** | Terminating exit requests and extracurricular consent: parents/guardians can submit formal exit requests for student transfer-out or voluntary withdrawal, attach required documentation, record formal secondary-guardian dispute notices or conflicting directives to trigger administrative holds, and complete parental consent tasks for student extracurricular participation. |
| **Dependencies** | `SCR-STU-04` (exit processing), `SCR-DEV-05` (extracurricular consent verification). |

**Incremental Worklist Additions.** Pending parental consent tasks for student extracurricular registrations; active exit requests (transfer-out or withdrawal); confirmation of exit authorization.

**Incremental Workbench Additions.** Exit request submission interface capturing reason, effective departure date, destination school details (for transfer-out), and mandatory guardian authorization; Conflicting-directive recording interface to register parental disputes; Extracurricular consent authorization form.

**Incremental Available Actions.** Formally initiate external transfer-out or withdrawal request; provide digitally signed guardian exit authorization; lodge a formal guardian dispute/conflicting directive placing exit processing on hold; grant or decline parental consent for extracurricular programs.

---

#### `SCR-STU-01` — Student Intake Workbench (Stage 3 Incremental Capabilities)

| Attribute | Capability Extension |
|---|---|
| **Screen ID** | `SCR-STU-01` (retained identity) |
| **Domain** | Students (with Academics prior-credit evaluation) |
| **Primary Actor** | Student Administration Officer |
| **Pattern** | Work Queue |
| **New Stage 3 Use Cases** | `UC-STD-18` Process External Transfer In |
| **Stage 3 Incremental Scope** | External Transfer-In lifecycle: processing incoming transfer learners, capturing origin-school academic records and accreditation history, coordinating prior-learning and credit evaluations with Academics, establishing advanced grade-level placement, and executing enrollment. |
| **Dependencies** | Academics (evaluates transferred credits and verifies syllabus equivalencies). |

**Incremental Worklist Additions.** External transfer-in applications awaiting origin-school verification; transfer cases queued for Academics prior-credit evaluation; evaluated transfer cases ready for enrollment.

**Incremental Workbench Additions.** Origin-school profile, official transfer certificates, prior transcript evaluation panel, accredited credit mapping to school curriculum units, and adjusted initial placement recommendations.

**Incremental Available Actions.** Record external transfer-in dossier and origin-school credentials; request prior-credit evaluation from Academics; record accredited transfer credits; assign advanced administrative placement; execute transfer-in enrollment.

---

#### `SCR-FIN-03` — Student Accounts Workbench (Stage 3 Incremental Capabilities)

| Attribute | Capability Extension |
|---|---|
| **Screen ID** | `SCR-FIN-03` (retained identity) |
| **Domain** | Finance |
| **Primary Actor** | Finance Officer; Finance Authority for exceptions |
| **Pattern** | Work Queue |
| **New Stage 3 Use Cases** | `UC-FIN-08` Evaluate Financial Clearance |
| **Stage 3 Incremental Scope** | Formal Financial Clearance determination: processing clearance requests originating from `SCR-STU-04` for student transfer-out and graduation; evaluating outstanding fee obligations, unbilled charges, or property liabilities; issuing formal `Granted` or `Denied` clearance determinations with specific financial blockers; processing Finance Authority clearance exceptions. |
| **Dependencies** | `SCR-STU-04` (requests clearance for Transfer Out or Graduation). |

**Incremental Worklist Additions.** Clearance requests queued from `SCR-STU-04` grouped by purpose (`Transfer Out`, `Graduation`), required-by date, and clearance status.

**Incremental Workbench Additions.** Purpose-specific clearance evaluation workspace displaying mandatory obligations, unsettled balances, refundable deposits, active disputes, and blocker resolution tracking.

**Incremental Available Actions.** Evaluate account obligations against clearance criteria; record formal `Granted` determination; record `Denied` determination with detailed blocker descriptions; escalate clearance exceptions to Finance Authority for override.

---

#### `SCR-OPS-02` — Scheduling Workbench (Stage 3 Incremental Capabilities)

| Attribute | Capability Extension |
|---|---|
| **Screen ID** | `SCR-OPS-02` (retained identity) |
| **Domain** | Operations |
| **Primary Actor** | Operations Coordinator |
| **Pattern** | Work Queue |
| **New Stage 3 Capabilities** | Operational scheduling and venue allocation for school events (`UC-OPS-06`) and extracurricular programs (`UC-DEV-09`, `UC-DEV-10`) |
| **Stage 3 Incremental Scope** | Institutional event and activity scheduling: allocates physical rooms, auditoriums, sports facilities, and specialized equipment for approved school events coordinated in `SCR-OPS-04` and recurring extracurricular sessions established in `SCR-DEV-05`; resolves facility conflicts between academic timetable and co-curricular programs. |
| **Dependencies** | `SCR-OPS-04` (event logistics), `SCR-DEV-05` (extracurricular session schedules). |

**Incremental Worklist Additions.** School event facility requests; extracurricular recurring session bookings; venue conflict alerts between events and regular classes.

**Incremental Workbench Additions.** Multi-facility reservation view; special equipment and auditorium allocation controls; event setup/takedown buffer windows.

**Incremental Available Actions.** Allocate event venues and specialized facilities; schedule recurring extracurricular timetable slots; arbitrate venue priority conflicts between academic and co-curricular requests; commit operational event bookings.

---

#### `SCR-ACD-07` — Academic Records Workbench (Stage 3 Incremental Capabilities)

| Attribute | Capability Extension |
|---|---|
| **Screen ID** | `SCR-ACD-07` (retained identity) |
| **Domain** | Academics |
| **Primary Actor** | Academic Coordinator; Academic Authority |
| **Pattern** | Work Queue |
| **New Stage 3 Capabilities** | Official graduation transcript certification and production (`UC-ACD-13` Stage 3 scope); accredited external transfer-in credit integration (`UC-STD-18`) |
| **Stage 3 Incremental Scope** | Advanced graduation records: compiling, certifying, and generating official final graduation academic transcripts with degree/diploma conferral notations, cumulative GPA calculations, honors notations, and official integration of accredited external transfer credits. |
| **Dependencies** | `SCR-ACD-09` (graduation academic eligibility certification), `SCR-STU-04` (graduation execution). |

**Incremental Worklist Additions.** Graduating cohort transcript compilation queue; certified transcript requests for transfer-out students; external credit integration dossiers.

**Incremental Workbench Additions.** Multi-year cumulative transcript builder; graduation requirement completion audit; transfer-in credit equivalency ledger; official seal certification controls.

**Incremental Available Actions.** Compile cumulative multi-year academic transcripts; embed certified graduation honors and completion notations; certify official final transcripts for graduating students and outgoing transfers; record accredited transfer-in credit lines.

---

#### `SCR-ACD-09` — Cohort Academic Review (Stage 3 Incremental Capabilities)

| Attribute | Capability Extension |
|---|---|
| **Screen ID** | `SCR-ACD-09` (retained identity) |
| **Domain** | Academics |
| **Primary Actor** | Academic Authority |
| **Pattern** | Work Queue |
| **New Stage 3 Use Cases** | `UC-ACD-18` Certify Graduation Academic Eligibility |
| **Stage 3 Incremental Scope** | Graduation academic eligibility determination: cohort-level and student-level audit of cumulative academic credits, mandatory core requirements, minimum GPA thresholds, and curriculum completion standards; issuing formal `Eligible`, `Ineligible`, or conditional graduation recommendations transmitted to `SCR-STU-04`. |
| **Dependencies** | `SCR-ACD-07` (approved cumulative records). |

**Incremental Worklist Additions.** Graduating cohort eligibility review lists; individual graduation appeals or conditional eligibility cases.

**Incremental Workbench Additions.** Graduation requirement audit matrix (credit tallies, core requirements, prerequisite checks, GPA verification) and eligibility determination panel.

**Incremental Available Actions.** Certify student graduation eligibility (`Eligible`, `Ineligible`, `Conditional`); document specific academic blockers for ineligible students; transmit certified graduation eligibility to `SCR-STU-04` for graduation execution.

---

### 4.4 Stage 3 Operational Summary

| Metric | Count | Details |
|---|:---:|---|
| **Screens Introduced** | **10** | `SCR-SELF-04`, `SCR-SELF-05`, `SCR-STU-04`, `SCR-ACD-08`, `SCR-OPS-04`, `SCR-DEV-01`, `SCR-DEV-02`, `SCR-DEV-03`, `SCR-DEV-04`, `SCR-DEV-05` |
| **Screens Extended** | **6** | `SCR-SELF-02`, `SCR-STU-01`, `SCR-FIN-03`, `SCR-OPS-02`, `SCR-ACD-07`, `SCR-ACD-09` |
| **Total Active Screens** | **16** | Full maturity: student pastoral support, counselling, discipline, extracurriculars, exits, and clearances. |

**Major Operational Capabilities Supported:**
- Student self-service extracurricular registration and status tracking (`SCR-SELF-04`).
- Confidential self-service pastoral and counselling support requests (`SCR-SELF-05`).
- Factual behaviour observations, incident logging, and incident triage (`SCR-DEV-01`).
- Formal disciplinary investigation workbench preserving due process and sanctions (`SCR-DEV-02`).
- Non-clinical pastoral referral routing, wellbeing support plans, and non-academic interventions (`SCR-DEV-03`).
- Isolated, confidential clinical counselling workspace with total note protection (`SCR-DEV-04`).
- Extracurricular program charter establishment, roster approvals, and non-academic achievements (`SCR-DEV-05`).
- Terminating lifecycle transitions: external transfer-out, withdrawal, graduation, and automatic alumni activation (`SCR-STU-04`).
- Purpose-specific academic clearance evaluation for transfer-out (`SCR-ACD-08`).
- Purpose-specific financial clearance evaluation for transfer-out and graduation (`SCR-FIN-03`).
- Graduation academic eligibility certification and audit against degree requirements (`SCR-ACD-09`).
- Production and official certification of final graduation academic transcripts (`SCR-ACD-07`).
- External transfer-in intake processing with origin-school records and prior-credit integration (`SCR-STU-01`).
- Family-facing exit requests, guardian dispute notices, and extracurricular consent tasks (`SCR-SELF-02`).
- School-wide event logistical coordination and official operational announcements (`SCR-OPS-04`).
- Scheduling and room/facility allocations for school events and extracurricular programs (`SCR-OPS-02`).

---

## 5. Cross-Stage Screen References & Multi-Stage Implementation Notes

### 5.1 Multi-Stage Screens Index

Seven screens evolve across multiple development stages. In accordance with architectural principles, each multi-stage screen retains its unique **Screen ID** and primary operational identity. It is introduced in the earliest stage where its foundational capabilities become operationally valuable, and gains additional submodules, actions, and cross-domain integrations in subsequent stages as prerequisite modules mature.

| Screen ID | Screen Name | Domain | Primary Actor | Introduced | Extended In | Strategy |
|---|---|---|---|:---:|:---:|---|
| `SCR-SELF-02` | Family Tasks & Requests Portal | Students / Finance / Dev | Parent / Guardian / Payer | **Stage 1** | **Stage 2**, **Stage 3** | Incremental Evolution |
| `SCR-STU-01` | Student Intake Workbench | Students | Admission / Admin Officer | **Stage 1** | **Stage 2**, **Stage 3** | Incremental Evolution |
| `SCR-FIN-03` | Student Accounts Workbench | Finance | Finance Officer / Authority | **Stage 1** | **Stage 2**, **Stage 3** | Incremental Evolution |
| `SCR-OPS-01` | Calendar & Periods Workspace | Operations | Operations Authority | **Stage 1** | **Stage 2** | Incremental Evolution |
| `SCR-OPS-02` | Scheduling Workbench | Operations | Operations Coordinator | **Stage 2** | **Stage 3** | Incremental Evolution |
| `SCR-ACD-07` | Academic Records Workbench | Academics | Academic Coordinator / Authority | **Stage 2** | **Stage 3** | Incremental Evolution |
| `SCR-ACD-09` | Cohort Academic Review | Academics | Academic Authority | **Stage 2** | **Stage 3** | Incremental Evolution |

---

### 5.2 Multi-Stage Evolution and Implementation Boundaries

#### 5.2.1 `SCR-SELF-02` — Family Tasks & Requests Portal

- **Why it spans multiple stages:** Families require a unified self-service portal for administrative obligations and requests throughout the student lifecycle. However, the underlying capabilities that families request (payments, re-enrollment, leaves, transfers, exits, and extracurricular consent) mature across all three development stages.
- **Stage 1 Initial Implementation:** Introduced to support basic fee payments (`UC-FIN-01`). Families and payers view assessed fee statements and submit electronic payment evidence with proof uploads.
- **Stage 2 Additions:** Adds annual continuing student re-enrollment confirmation (`UC-STD-02`) during published re-enrollment periods, including emergency contact certifications. Adds formal guardian request submissions for leave of absence (`UC-STD-07`) and internal cohort/program transfers (`UC-STD-10`).
- **Stage 3 Additions:** Adds formal guardian exit requests for external transfer-out (`UC-STD-11`) and voluntary withdrawal (`UC-STD-12`). Adds digital recording of conflicting guardian directives and dispute notices. Adds parental consent task handling for extracurricular registrations (`UC-DEV-02`).
- **Gating Constraints:**
  - In Stage 1, all lifecycle request forms and re-enrollment links must be hidden/disabled.
  - In Stage 2, exit request workflows must remain disabled because terminating exit workbenches (`SCR-STU-04`) and clearances do not exist.
  - In Stage 3, exit requests are enabled and routed directly to `SCR-STU-04`.

#### 5.2.2 `SCR-STU-01` — Student Intake Workbench

- **Why it spans multiple stages:** Prospective learner intake is a cohesive pipeline from application to enrollment. However, entrance assessment evidence intake begins as manual entry in Stage 1 and becomes an automated Academics integration in Stage 2, while external transfer-in requires complex prior-credit evaluation available only in Stage 3.
- **Stage 1 Initial Implementation:** Full core intake lifecycle for standard applicants: assisted registration (`UC-STD-01`), identity/document verification, admission review and decisions (`UC-STD-03`), verification of Finance payment gate (`UC-FIN-09`), official enrollment execution (`UC-STD-04`), automated Student Number generation, and initial placement. Diagnostic assessment scores (`UC-ACD-07`) are entered manually as external documentary evidence.
- **Stage 2 Additions:** Automates entrance assessment integration. Academics-administered diagnostic assessment workflows (`ACD-WF-20`) push verified diagnostic scores directly into the intake review dossier, eliminating manual score entry.
- **Stage 3 Additions:** Adds External Transfer In (`UC-STD-18`). Captures origin-school history, official transfer credentials, and coordinates prior-learning and credit evaluations with Academics before executing advanced placement and enrollment.
- **Gating Constraints:**
  - In Stage 1, automated Academics test scheduling is disabled; staff use manual score fields.
  - In Stages 1 and 2, external transfer-in intake cases (`UC-STD-18`) must not be accepted or must be handled as standard new admissions without prior-credit transfer recognition.

#### 5.2.3 `SCR-FIN-03` — Student Accounts Workbench

- **Why it spans multiple stages:** The student financial account ledger exists from Stage 1 (tracking obligations assessed and payments received), but formal debt collection, credit management, adjustments, and cross-domain clearance workflows belong to later stages.
- **Stage 1 Initial Implementation:** Read-only account inspection. Finance staff search accounts and inspect assessed obligations, payment receipts, allocation breakdowns, and current balances.
- **Stage 2 Additions:** Full receivables stewardship and credit management: overdue receivables monitoring (`UC-FIN-06`) across aging buckets (30/60/90+ days), reminder dispatching, overpayment and credit balance management (`UC-FIN-07`), refund dossier compilation for `SCR-FIN-05`, and auditable corrections to approved finance records (`UC-FIN-15`).
- **Stage 3 Additions:** Adds purpose-specific Financial Clearance evaluation (`UC-FIN-08`) in response to exit requests from `SCR-STU-04`. Staff verify outstanding obligations, property liabilities, and record formal `Granted` or `Denied` clearance results with blockers.
- **Gating Constraints:**
  - In Stage 1, debt collection, reminder generation, credit transfers, and ledger corrections are disabled.
  - In Stages 1 and 2, the Financial Clearance evaluation queue is disabled as no exit workflows exist.

#### 5.2.4 `SCR-OPS-01` — Calendar & Periods Workspace

- **Why it spans multiple stages:** Calendar definition is foundational in Stage 1, but operational closure execution interacts with timetabled occurrences introduced in Stage 2.
- **Stage 1 Initial Implementation:** Full governance and publication of the institutional calendar, academic years, operational terms, admission periods, holidays, and closure declarations (`UC-OPS-11`, `UC-OPS-12`, `UC-OPS-13`).
- **Stage 2 Additions:** Integrates campus closure declarations with the operational timetable (`SCR-OPS-02`). Declaring an emergency or scheduled closure triggers automated schedule-impact analysis and dispatches mass cancellation/rescheduling instructions to `SCR-OPS-02`.
- **Gating Constraints:**
  - In Stage 1, closure declarations only mark calendar days as non-instructional; timetable impact analysis is inactive because the scheduling engine is not yet deployed.

#### 5.2.5 `SCR-OPS-02` — Scheduling Workbench

- **Why it spans multiple stages:** Deployed in Stage 2 to schedule academic curriculum sessions, then expanded in Stage 3 to coordinate institutional events and extracurricular program facilities.
- **Stage 2 Initial Implementation:** Full operational scheduling for instructional activities: reviewing requests (`UC-OPS-03`), allocating rooms and timetable slots (`UC-OPS-04`), resolving timetable conflicts (`UC-OPS-05`), managing room restrictions (`UC-OPS-08`), centralized rescheduling (`UC-OPS-09`), closing arrangements (`UC-OPS-10`), and timetable record corrections (`UC-OPS-13`).
- **Stage 3 Additions:** Facility and room allocation for approved school events (`UC-OPS-06` via `SCR-OPS-04`) and recurring extracurricular program sessions (`UC-DEV-09`, `UC-DEV-10` via `SCR-DEV-05`). Resolves venue priority collisions between academic, event, and extracurricular demands.
- **Gating Constraints:**
  - In Stage 2, event and extracurricular request queues are inactive.

#### 5.2.6 `SCR-ACD-07` — Academic Records Workbench

- **Why it spans multiple stages:** Academic result moderation, publishing, report cards, and interim transcripts belong to Stage 2. Formal graduation transcripts and external credit integration belong to Stage 3.
- **Stage 2 Initial Implementation:** Concluding learning unit participation (`UC-ACD-12`), summative result moderation, official grade publication (`UC-ACD-16`), term report cards, periodic/interim transcripts (`UC-ACD-13`), and auditable grade corrections (`UC-ACD-19`, `UC-ACD-15`).
- **Stage 3 Additions:** Production and official certification of final graduation academic transcripts with degree/diploma conferral notations, cumulative GPA tallies, honors recognitions, and official integration of accredited external transfer-in credits (`UC-STD-18`).
- **Gating Constraints:**
  - In Stage 2, final graduation transcript certification is disabled; only periodic term report cards and interim transcripts can be generated.

#### 5.2.7 `SCR-ACD-09` — Cohort Academic Review

- **Why it spans multiple stages:** Determining periodic academic standing and progression recommendations occurs at the close of Stage 2. Graduation academic eligibility certification occurs in Stage 3.
- **Stage 2 Initial Implementation:** Cohort academic performance review, academic standing determination (`UC-ACD-17`), progression recommendations (`Promote`, `Retain`, `Conditional Promotion`), and academic achievement conferral (`UC-ACD-21`).
- **Stage 3 Additions:** Adds Graduation Academic Eligibility Certification (`UC-ACD-18`). Audits cumulative credits, mandatory core requirements, and curriculum completion standards, transmitting formal `Eligible` or `Ineligible` determinations to `SCR-STU-04`.
- **Gating Constraints:**
  - In Stage 2, graduation eligibility certification queues are disabled.

---

## 6. Complete Use Case Coverage Matrix

Every enumerated use case in `use-cases.md` (95 use cases across six domains) is explicitly mapped to its supporting screen(s) and development stage(s).

### 6.1 Students Domain (18 Use Cases)

| Use Case ID | Name | Primary Screen(s) | Primary Stage | Extension Stage(s) | Coverage Note |
|---|---|---|:---:|:---:|---|
| `UC-STD-01` | Register Admission Application | `SCR-SELF-01`; `SCR-STU-01` | 1 | — | Family self-service portal (`SCR-SELF-01`) is the primary path; assisted registration is in `SCR-STU-01`. |
| `UC-STD-02` | Submit Re-enrollment Confirmation | `SCR-SELF-02` | 2 | — | Family task queue presents continuation intent; administrative processing occurs in `SCR-STU-03`. |
| `UC-STD-03` | Review Admission Application | `SCR-STU-01` | 1 | 2 | Core admission review in Stage 1; integrated Academics diagnostic assessment in Stage 2. |
| `UC-STD-04` | Enroll Accepted Applicant | `SCR-STU-01` | 1 | — | Official enrollment execution upon Finance payment confirmation; student number assignment and initial placement. |
| `UC-STD-05` | Maintain Student Identity | `SCR-STU-02` | 1 | — | Maintenance of certified identity, emergency contacts, primary guardians, and independence status. |
| `UC-STD-06` | Change Administrative Placement | `SCR-STU-02` | 1 | — | Maintenance of effective-dated campus, grade level, program, and homeroom cohort placement. |
| `UC-STD-07` | Place Student on Leave | `SCR-STU-03`; `SCR-SELF-02` | 2 | — | Continuing student lifecycle transition; guardian request enters via `SCR-SELF-02`. |
| `UC-STD-08` | Return Student from Leave | `SCR-STU-03` | 2 | — | Re-activation from `On Leave` or `Leave Expired` status to active status. |
| `UC-STD-09` | Execute Promotion or Retention | `SCR-STU-03` | 2 | — | Cohort review and execution based on approved Academic Authority recommendations (`SCR-ACD-09`). |
| `UC-STD-10` | Process Internal Transfer | `SCR-STU-03`; `SCR-SELF-02` | 2 | — | Internal program or cohort transfer; guardian request enters via `SCR-SELF-02`. |
| `UC-STD-11` | Process External Transfer Out | `SCR-STU-04`; `SCR-SELF-02` | 3 | — | Terminating transition requiring academic clearance (`SCR-ACD-08`) and financial clearance (`SCR-FIN-03`). |
| `UC-STD-12` | Process Withdrawal | `SCR-STU-04`; `SCR-SELF-02` | 3 | — | Voluntary, relocation, or disciplinary exit execution with authorization history. |
| `UC-STD-13` | Execute Graduation | `SCR-STU-04` | 3 | — | Graduation execution requiring academic eligibility (`SCR-ACD-09`) and financial clearance (`SCR-FIN-03`). |
| `UC-STD-14` | Activate Alumni Status | `SCR-STU-04` | 3 | — | Automatic administrative outcome of successful graduation confirmed in `SCR-STU-04`. |
| `UC-STD-15` | Request Academic Clearance for Transfer Out | `SCR-STU-04` | 3 | — | Tracks request from exit workbench; determined by Academics in `SCR-ACD-08`. |
| `UC-STD-16` | Request Purpose-Specific Financial Clearance | `SCR-STU-04` | 3 | — | Tracks request from exit workbench; determined by Finance in `SCR-FIN-03`. |
| `UC-STD-17` | Process Student Re-enrollment | `SCR-STU-03` | 2 | — | Administrative confirmation of family re-enrollment submissions and cohort roll-forward. |
| `UC-STD-18` | Process External Transfer In | `SCR-STU-01` | 3 | — | Intake case variant capturing origin-school history and coordinating Academics prior-credit evaluation. |

---

### 6.2 Academics Domain (21 Use Cases)

| Use Case ID | Name | Primary Screen(s) | Primary Stage | Extension Stage(s) | Coverage Note |
|---|---|---|:---:|:---:|---|
| `UC-ACD-01` | Submit Coursework Assignment | `SCR-SELF-03` | 2 | — | Student-facing coursework file and project submission portal. |
| `UC-ACD-02` | Record Learning Activity Delivery | `SCR-ACD-01` | 2 | — | Teacher daily instructional session conduct and curriculum milestone tracking. |
| `UC-ACD-03` | Record and Correct Student Attendance | `SCR-ACD-01` | 2 | — | Session roll-call attendance capture and auditable correction. |
| `UC-ACD-04` | Issue Coursework Assignment | `SCR-ACD-02` | 2 | — | Teacher assignment task creation, rubrics, and submission deadline publishing. |
| `UC-ACD-05` | Plan and Administer Assessment | `SCR-ACD-02` | 2 | — | Formative and summative assessment planning and conduct. |
| `UC-ACD-06` | Evaluate Student Learning | `SCR-ACD-02` | 2 | — | Formative grading published directly to students; provisional summative scoring for moderation. |
| `UC-ACD-07` | Conduct Applicant Entrance Assessment | `SCR-STU-01` | 1 | 2 | Manual documentary score entry in Stage 1; integrated Academics diagnostic workflow in Stage 2. |
| `UC-ACD-08` | Define Learning Structure | `SCR-ACD-03` | 2 | — | Curriculum framework, learning unit, syllabus, and academic policy versioning workspace. |
| `UC-ACD-09` | Establish Student Learning Plan | `SCR-ACD-04` | 2 | — | Individualized Student Learning Plans (SLP) for exceptional learners. |
| `UC-ACD-10` | Register Academic Participation | `SCR-ACD-05` | 2 | — | Term registration into learning units, prerequisite checks, and roster finalization. |
| `UC-ACD-11` | Initiate Academic Intervention | `SCR-ACD-06` | 2 | — | At-risk learner support plan formulation and progress review checkpoints. |
| `UC-ACD-12` | Conclude Learning Unit Participation | `SCR-ACD-07` | 2 | — | Period participation completion audit (`Completed`/`Incomplete`) and credit tallies. |
| `UC-ACD-13` | Produce Report Card and Transcript | `SCR-ACD-07` | 2 | 3 | Period report cards and interim transcripts in Stage 2; certified graduation transcripts in Stage 3. |
| `UC-ACD-14` | Determine Academic Clearance | `SCR-ACD-08` | 3 | — | Academic clearance determination for outgoing transfer students. |
| `UC-ACD-15` | Correct Non-Grade Academic Record | `SCR-ACD-01`, `02`, `07`, `09` | 2 | 3 | Auditable in-place correction across owning screens according to record type. |
| `UC-ACD-16` | Approve and Publish Academic Results | `SCR-ACD-07` | 2 | — | Formal summative moderation and official grade publication by Academic Authority. |
| `UC-ACD-17` | Determine Academic Standing and Progression | `SCR-ACD-09` | 2 | — | Cohort review for academic standing and promotion/retention recommendations. |
| `UC-ACD-18` | Certify Graduation Academic Eligibility | `SCR-ACD-09` | 3 | — | Audit of cumulative graduation requirements and formal eligibility certification. |
| `UC-ACD-19` | Correct Approved Academic Result | `SCR-ACD-07` | 2 | — | Controlled grade correction on approved result sets with mandatory justification. |
| `UC-ACD-20` | Adjudicate Academic Clearance Correction | `SCR-ACD-08` | 3 | — | Formal clearance determination correction with audit log. |
| `UC-ACD-21` | Confer Academic Achievement | `SCR-ACD-09` | 2 | — | Formal conferral of academic honors and subject distinctions. |

---

### 6.3 Finance Domain (15 Use Cases)

| Use Case ID | Name | Primary Screen(s) | Primary Stage | Extension Stage(s) | Coverage Note |
|---|---|---|:---:|:---:|---|
| `UC-FIN-01` | Submit Fee Payment | `SCR-SELF-02` | 1 | — | Family online payment submission with transfer evidence upload. |
| `UC-FIN-02` | Assess Financial Obligations | `SCR-FIN-01` | 1 | — | Billing cycle assessment of tuition, admission, and enrollment obligations. |
| `UC-FIN-03` | Issue Billing Statement | `SCR-FIN-01` | 1 | — | Generation and issuance of official billing statements to families. |
| `UC-FIN-04` | Confirm Payment Receipt | `SCR-FIN-02` | 1 | — | Verification of payment evidence and official receipt issuance. |
| `UC-FIN-05` | Allocate Payment to Obligations | `SCR-FIN-02` | 1 | — | Allocation of confirmed payments against open charge obligations. |
| `UC-FIN-06` | Monitor Overdue Receivables | `SCR-FIN-03` | 2 | — | Aging analysis and payment reminder dispatching for overdue accounts. |
| `UC-FIN-07` | Manage Overpayment and Account Credit | `SCR-FIN-03` | 2 | — | Account credit management, obligation offsetting, and refund dossier preparation. |
| `UC-FIN-08` | Evaluate Financial Clearance | `SCR-FIN-03` | 3 | — | Clearance evaluation for transfer-out and graduation; blocker management. |
| `UC-FIN-09` | Confirm Enrollment Payment Gate | `SCR-FIN-02` | 1 | — | Enrollment payment clearance confirmation to unblock student intake in `SCR-STU-01`. |
| `UC-FIN-10` | Establish Financial Policy | `SCR-FIN-04` | 1 | — | Institutional fee schedule, charge rule, and billing policy governance. |
| `UC-FIN-11` | Approve Financial Benefit | `SCR-FIN-05` | 2 | — | Finance Authority approval for scholarships, discounts, and waivers. |
| `UC-FIN-12` | Establish Installment Payment Arrangement | `SCR-FIN-05` | 2 | — | Binding authorization of hardship installment payment arrangements. |
| `UC-FIN-13` | Adjudicate Financial Dispute or Adjustment | `SCR-FIN-05` | 2 | — | Adjudication of billing disputes and authorization of balance adjustments. |
| `UC-FIN-14` | Approve and Execute Refund | `SCR-FIN-05` | 2 | — | Zero-discretionary approval and execution of student fee refunds. |
| `UC-FIN-15` | Correct Approved Finance Record | `SCR-FIN-03` | 2 | — | Auditable corrections to finalized financial ledgers and accounting records. |

---

### 6.4 Operations Domain (13 Use Cases)

| Use Case ID | Name | Primary Screen(s) | Primary Stage | Extension Stage(s) | Coverage Note |
|---|---|---|:---:|:---:|---|
| `UC-OPS-01` | Submit Operational Activity Request | `SCR-OPS-03` | 2 | — | Staff submissions for room and equipment bookings. |
| `UC-OPS-02` | Cancel Approved Activity Reservation | `SCR-OPS-03` | 2 | — | Autonomous cancellation by original reservation initiator. |
| `UC-OPS-03` | Review Operational Feasibility | `SCR-OPS-02` | 2 | — | Coordinator evaluation of room capacity, facilities, and teacher availability. |
| `UC-OPS-04` | Schedule Activity and Allocate Room | `SCR-OPS-02` | 2 | 3 | Timetable commit for classes in Stage 2; events and extracurriculars in Stage 3. |
| `UC-OPS-05` | Resolve Operational Resource Conflict | `SCR-OPS-02` | 2 | — | Conflict arbitration for room double-bookings and timetable collisions. |
| `UC-OPS-06` | Coordinate School Event Logistics | `SCR-OPS-04` | 3 | — | End-to-end logistics coordination for approved institutional school events. |
| `UC-OPS-07` | Publish Operational Announcement | `SCR-OPS-04` | 3 | — | Composition, scheduling, publication, and withdrawal of announcements. |
| `UC-OPS-08` | Record Room Restriction | `SCR-OPS-02` | 2 | — | Logging physical room maintenance windows and capacity restrictions. |
| `UC-OPS-09` | Reschedule or Relocate Scheduled Occurrence | `SCR-OPS-02` | 2 | — | Centralized timetable rescheduling under Coordinator authority. |
| `UC-OPS-10` | Close Operational Arrangement | `SCR-OPS-02` | 2 | — | End-of-period closure of room allocations and resource releases. |
| `UC-OPS-11` | Establish School Calendar and Periods | `SCR-OPS-01` | 1 | — | Definition and publication of academic years, terms, and holidays. |
| `UC-OPS-12` | Declare Campus Closure | `SCR-OPS-01`; `SCR-OPS-02` | 1 | 2 | Calendar declaration in Stage 1; automated timetable rescheduling handoff in Stage 2. |
| `UC-OPS-13` | Correct Approved Operations Record | `SCR-OPS-01`, `SCR-OPS-02` | 1 | 2 | Calendar record corrections in `SCR-OPS-01`; timetable/room corrections in `SCR-OPS-02`. |

---

### 6.5 Teaching Domain (12 Use Cases)

| Use Case ID | Name | Primary Screen(s) | Primary Stage | Extension Stage(s) | Coverage Note |
|---|---|---|:---:|:---:|---|
| `UC-TCH-01` | Declare Teaching Availability | `SCR-TCH-04` | 2 | — | Teacher self-service availability and constraint declaration. |
| `UC-TCH-02` | Maintain Teaching Profile | `SCR-TCH-01` | 2 | — | Faculty professional profile and institutional competency management. |
| `UC-TCH-03` | Verify Teacher Qualifications | `SCR-TCH-01` | 2 | — | Verification of degree credentials, teaching certificates, and validities. |
| `UC-TCH-04` | Process Teaching Demand | `SCR-TCH-02` | 2 | — | Validation and intake of curriculum teaching demand from Academics. |
| `UC-TCH-05` | Evaluate Teacher Eligibility and Capacity | `SCR-TCH-02` | 2 | — | Candidate evaluation against competencies, authorizations, and workloads. |
| `UC-TCH-06` | Arrange Substitute Teacher | `SCR-TCH-02` | 2 | — | Immediate assignment of emergency substitute teachers from authorized pools. |
| `UC-TCH-07` | Monitor Faculty Workload | `SCR-TCH-03` | 2 | — | Real-time monitoring of teaching hours against institutional workload limits. |
| `UC-TCH-08` | Conclude Teaching Assignment | `SCR-TCH-02` | 2 | — | Period conclusion of teaching assignments upon grade submission verification. |
| `UC-TCH-09` | Determine Teaching Authorization | `SCR-TCH-01` | 2 | — | Binding determination, granting, suspension, or revocation of authorizations. |
| `UC-TCH-10` | Approve Teaching Assignment | `SCR-TCH-02` | 2 | — | Binding approval of faculty assignments to learning units and homeroom advisories. |
| `UC-TCH-11` | Modify or Cancel Teaching Assignment | `SCR-TCH-02` | 2 | — | Authorized changes, suspensions, or cancellations of active assignments. |
| `UC-TCH-12` | Correct Approved Teaching Record | `SCR-TCH-01` | 2 | — | Auditable corrections to finalized faculty profiles and authorization records. |

---

### 6.6 Development Domain (16 Use Cases)

| Use Case ID | Name | Primary Screen(s) | Primary Stage | Extension Stage(s) | Coverage Note |
|---|---|---|:---:|:---:|---|
| `UC-DEV-01` | Submit Development Referral | `SCR-SELF-05`; `SCR-DEV-03` | 3 | — | Self-service confidential submission; routing in `SCR-DEV-03`. |
| `UC-DEV-02` | Register for Extracurricular Program | `SCR-SELF-04`; `SCR-DEV-05` | 3 | — | Student registration in `SCR-SELF-04`; parental consent in `SCR-SELF-02`; roster approval in `SCR-DEV-05`. |
| `UC-DEV-03` | Record Behaviour Observation | `SCR-DEV-01` | 3 | — | Positive recognition and factual behaviour observations logging. |
| `UC-DEV-04` | Report Behaviour Incident | `SCR-DEV-01` | 3 | — | Factual incident reporting by teachers and staff. |
| `UC-DEV-05` | Triage Behaviour Incident | `SCR-DEV-01` | 3 | — | Incident risk triage routing cases to pastoral support or formal discipline. |
| `UC-DEV-06` | Route Development Referral | `SCR-DEV-03` | 3 | — | Classification and routing of support referrals. |
| `UC-DEV-07` | Manage Wellbeing Support Plan | `SCR-DEV-03` | 3 | — | Formulation, review, and adjustment of non-clinical wellbeing plans. |
| `UC-DEV-08` | Establish Non-Academic Intervention | `SCR-DEV-03` | 3 | — | Structured non-academic support plan formulation and target monitoring. |
| `UC-DEV-09` | Establish Extracurricular Program | `SCR-DEV-05` | 3 | — | Chartering and configuring extracurricular programs and schedules. |
| `UC-DEV-10` | Manage Extracurricular Participation and Attendance | `SCR-DEV-05` | 3 | — | Roster management, attendance tracking, and completion certification. |
| `UC-DEV-11` | Recognize Non-Academic Achievement | `SCR-DEV-05` | 3 | — | Verification and conferral of non-academic student achievements. |
| `UC-DEV-12` | Issue Pastoral Cross-Domain Recommendation | `SCR-DEV-03` | 3 | — | Sanitized recommendations to Academics (`SCR-ACD-04`) or Students (`SCR-STU-03`/`04`). |
| `UC-DEV-13` | Manage Confidential Counselling Case | `SCR-DEV-04` | 3 | — | Completely isolated clinical counselling intake, care plans, and session records. |
| `UC-DEV-14` | Investigate and Determine Discipline Case | `SCR-DEV-02` | 3 | — | Formal investigation, hearings, findings, policy sanctions, and appeals. |
| `UC-DEV-15` | Recommend Disciplinary Administrative Sanction | `SCR-DEV-02` | 3 | — | Recommendations for suspension/expulsion transmitted to Students (`SCR-STU-04`). |
| `UC-DEV-16` | Correct Approved Development Record | `SCR-DEV-01` to `05` | 3 | — | Auditable in-place corrections across owning workspaces based on confidentiality. |

---

### 6.7 Reconciled Correction Use Cases Across Stages

| Correction Use Case | Record Type | Owning Screen | Stage | Authority Required |
|---|---|---|:---:|---|
| `UC-ACD-15` | Attendance History | `SCR-ACD-01` | 2 | Teacher (with auditable justification) |
| `UC-ACD-15` | Assignment Metadata | `SCR-ACD-02` | 2 | Teacher / Academic Coordinator |
| `UC-ACD-15` | Participation Record | `SCR-ACD-07` | 2 | Academic Coordinator |
| `UC-ACD-15` | Standing / Progression Record | `SCR-ACD-09` | 2 | Academic Authority |
| `UC-ACD-15` | External Transfer Credit Record | `SCR-ACD-07` | 3 | Academic Authority |
| `UC-OPS-13` | Calendar & Period Records | `SCR-OPS-01` | 1 | Operations Authority |
| `UC-OPS-13` | Room Allocation & Timetable Records | `SCR-OPS-02` | 2 | Operations Coordinator / Operations Authority |
| `UC-DEV-16` | Behaviour & Triage Records | `SCR-DEV-01` | 3 | Student Development Officer |
| `UC-DEV-16` | Formal Discipline Records | `SCR-DEV-02` | 3 | Discipline Authority |
| `UC-DEV-16` | Pastoral & Wellbeing Records | `SCR-DEV-03` | 3 | Student Development Officer |
| `UC-DEV-16` | Confidential Clinical Counselling Records | `SCR-DEV-04` | 3 | Counselor (clinical isolation strictly enforced) |
| `UC-DEV-16` | Extracurricular & Achievement Records | `SCR-DEV-05` | 3 | Student Development Officer |

---

### 6.8 Domain and Stage Coverage Summary

| Domain | Total Use Cases | Stage 1 Active | Stage 2 Active | Stage 3 Active | Total Covered |
|---|:---:|:---:|:---:|:---:|:---:|
| **Students** | 18 | 5 | 6 | 7 | **18 / 18 (100%)** |
| **Academics** | 21 | 1 | 16 | 4 | **21 / 21 (100%)** |
| **Finance** | 15 | 7 | 7 | 1 | **15 / 15 (100%)** |
| **Operations** | 13 | 3 | 7 | 3 | **13 / 13 (100%)** |
| **Teaching** | 12 | 0 | 12 | 0 | **12 / 12 (100%)** |
| **Development** | 16 | 0 | 0 | 16 | **16 / 16 (100%)** |
| **Total** | **95** | **16** | **48** | **31** | **95 / 95 (100%)** |

> [!NOTE]
> Active use case count per stage reflects when capabilities become operational. Use cases that evolve across stages (e.g., `UC-ACD-07`, `UC-OPS-12`, `UC-OPS-13`, `UC-ACD-13`, `UC-ACD-15`) are counted in their primary introduction stage in the table above, with evolutionary scope detailed in §6.1–§6.7.

---

## 7. Screen Grouping Rationale and Boundary Enforcements

### 7.1 One Intake Pipeline Instead of Separate Admission and Enrollment Screens

`SCR-STU-01` combines application registration, admission review, entrance assessment, enrollment, and external transfer-in. These are stages of the same operational case: a prospective learner entering the school. Splitting them would force the same applicant identity, documents, assessment evidence, decision, and payment-gate status to be carried across separate screens and would create an artificial handoff between the Admission Officer and the Student Administration Officer.

The workbench is stage-sensitive. An Admission Officer sees the review and decision context; an Assessment Officer sees the assessment context; a Student Administration Officer sees the enrollment context. The screen's primary actor therefore changes by stage, which is explicit in the definition. This is preferable to three screens that each expose a fragment of the same intake case.

The alternative — a separate entrance-assessment screen — was rejected because entrance assessment exists only as an authorized input to admission review. In Stage 1 it functions as manual external evidence intake, transitioning in Stage 2 to an automated Academics integration. A separate screen would add navigation without adding a distinct working context.

### 7.2 Continuing Transitions Separated from Terminating Transitions

`SCR-STU-03` handles transitions for students who remain with the school: leave, return, promotion or retention, internal transfer, and re-enrollment. `SCR-STU-04` handles transitions that end the relationship: external transfer-out, withdrawal, graduation, alumni activation, and the clearance requests those exits require.

This split aligns directly with the development stages:
- **Stage 2:** Introduces continuing student transitions (`SCR-STU-03`), which focus on cohort placement, academic progression, and roll-forward to the next term.
- **Stage 3:** Introduces terminating student transitions (`SCR-STU-04`), which require mature cross-domain clearance capabilities from Academics (`SCR-ACD-08`) and Finance (`SCR-FIN-03`).

Combining them would produce an oversized lifecycle screen and would obscure the clearance gates that only apply to exits.

### 7.3 Student Records Desk Is Not a Lifecycle Screen

`SCR-STU-02` exists because staff frequently need to find and verify a student's certified identity, guardians, placement, status, and history without processing a transition. This is genuine information lookup, not a CRUD screen: it supports two maintenance use cases (`UC-STD-05` and `UC-STD-06`) and serves as the shared student reference for every other workspace.

Lifecycle execution is deliberately kept out of the records desk. A user who needs to place a student on leave, execute a promotion, or process an exit is directed to the relevant workbench (`SCR-STU-03` or `SCR-STU-04`). This preserves the distinction between maintaining the record and changing the student's official relationship with the school.

### 7.4 Teacher Daily Session Work Separated from Coursework and Assessment

`SCR-ACD-01` is the teacher's daily instructional workspace: what was delivered and who attended. `SCR-ACD-02` is the teacher's student-work workspace: what was assigned, what was assessed, and how evidence was evaluated.

These are different operational rhythms:
- Attendance and delivery recording happen in or immediately after a session and are time-critical.
- Assignment and assessment planning, submission review, and evaluation happen across a longer cycle.

Combining them would force the teacher to navigate assessment tools while taking a roll-call, or attendance records while evaluating a rubric. Formative feedback and provisional summative scores are recorded in `SCR-ACD-02`; formal approval and publication are moved to `SCR-ACD-07` under Academic Authority, preserving the documented formative-versus-summative authority boundary.

### 7.5 Academic Records, Cohort Determinations, and Clearance Are Separate Decisions

`SCR-ACD-07` produces and publishes academic records. `SCR-ACD-09` makes formal determinations from those records: standing, progression, graduation eligibility, and achievement conferral. `SCR-ACD-08` evaluates academic clearance for transfer-out.

These screens share approved results as an input but answer different questions:
- **Records (`SCR-ACD-07`):** What is the official result, report card, or transcript? (Stage 2 periodic, Stage 3 graduation).
- **Cohort Review (`SCR-ACD-09`):** What standing, progression, eligibility, or achievement follows from the approved record? (Stage 2 standing/progression, Stage 3 graduation eligibility).
- **Clearance (`SCR-ACD-08`):** Has this outgoing student satisfied academic obligations? (Stage 3).

Merging them would blur the distinction between producing a record and making a determination from it, and would make it harder to enforce the different authorities involved.

### 7.6 Finance Separated by Operational Rhythm and Authority

The Finance screens are separated by the work being done, not by financial entity:
- `SCR-FIN-01` is the periodic billing cycle: assess obligations, review exceptions, issue statements (Stage 1).
- `SCR-FIN-02` is the incoming-payment queue: verify, confirm, allocate, and answer the enrollment gate (Stage 1).
- `SCR-FIN-03` is account stewardship: overview (Stage 1), overdue receivables, credit, corrections (Stage 2), and clearance (Stage 3).
- `SCR-FIN-04` is financial governance: policy and charge rules (Stage 1).
- `SCR-FIN-05` is the Finance Authority's binding decision queue (Stage 2).

Billing and payment verification are distinct operational rhythms. Billing is a planned cycle; payment verification is a continuous queue.

The Finance Authority queue (`SCR-FIN-05`) groups benefits, installment arrangements, disputes/adjustments, and refunds because they share one operational context: a prepared case requiring binding authorization under zero-discretionary-spending rules.

Financial clearance is placed in `SCR-FIN-03` rather than a separate queue because it is an account determination based on the same mandatory-obligation view used for receivables and credits.

### 7.7 Operations Separated by Coordination Role

`SCR-OPS-01` is the authoritative calendar and period workspace, owned by Operations Authority. `SCR-OPS-02` is the Operations Coordinator's scheduling and room workbench. `SCR-OPS-03` is the requesting actor's own request and cancellation portal. `SCR-OPS-04` is the event and announcement workspace.

The separation follows the asymmetric autonomy rule:
- Requesting actors submit and autonomously cancel their own reservations in `SCR-OPS-03`.
- The Operations Coordinator performs feasibility, allocation, conflict resolution, rescheduling, and closure in `SCR-OPS-02`.
- The Operations Authority owns calendar amendments and closure declarations in `SCR-OPS-01`.

Placing cancellation and rescheduling in the same screen would obscure the different authorities. Events and announcements share `SCR-OPS-04` in Stage 3 because both are operational publication and coordination tasks performed by the same actor.

### 7.8 Teaching Separated by Readiness, Allocation, Monitoring, and Availability

`SCR-TCH-01` establishes teacher readiness: profile, qualifications, authorization, and record correction. `SCR-TCH-02` allocates teaching resources: demand, eligibility, assignment, substitution, change, and conclusion. `SCR-TCH-03` monitors workload against policy thresholds. `SCR-TCH-04` is the teacher's own availability declaration.

These represent distinct working contexts deployed together in Stage 2:
- Readiness (`SCR-TCH-01`) is a prerequisite to allocation.
- Allocation (`SCR-TCH-02`) is a prerequisite to workload monitoring.
- Availability (`SCR-TCH-04`) informs both allocation and scheduling.

Emergency substitution is included in `SCR-TCH-02` because it uses the same candidate pool, authorization data, and workload capacity as normal assignment. It is a work item type with an urgent flag, not a separate planning process.

### 7.9 Development Separated by Confidentiality and Due Process

`SCR-DEV-01` handles behaviour observations and incident triage. `SCR-DEV-02` handles formal discipline. `SCR-DEV-03` handles non-clinical pastoral support. `SCR-DEV-04` handles confidential counselling. `SCR-DEV-05` handles extracurricular programs and non-academic achievements.

All five screens are deployed in Stage 3:
- **Clinical Isolation:** `SCR-DEV-04` isolates clinical counselling notes from all administrative and academic portals.
- **Due Process:** `SCR-DEV-02` separates formal disciplinary investigations and findings from initial incident triage (`SCR-DEV-01`), and separates investigation from appeal (adjudicated by School Leadership).
- **Pastoral Support vs. Sanction:** `SCR-DEV-03` manages supportive interventions and wellbeing plans, distinct from disciplinary penalties.
- **Extracurricular Management:** `SCR-DEV-05` manages program charters and rosters, separated from case management.

### 7.10 Corrections Are Actions, Not Screens

`UC-ACD-15`, `UC-OPS-13`, and `UC-DEV-16` are correction workflows that apply to several record types. Rather than creating generic correction screens, correction is designed as an authorized action in the screen that owns the record. This preserves the record's context, maintains the audit trail in place, and avoids creating redundant, disjointed CRUD interfaces.

### 7.11 Self-Service Separated from Staff Operations

The five self-service screens exist because applicants, families, and students perform work that staff cannot perform on their behalf without violating self-service boundaries:
- `SCR-SELF-01` (Stage 1): Applicant admission registration.
- `SCR-SELF-02` (Stages 1–3): Consolidated family administrative and financial portal.
- `SCR-SELF-03` (Stage 2): Student coursework workspace.
- `SCR-SELF-04` (Stage 3): Student extracurricular registration.
- `SCR-SELF-05` (Stage 3): Confidential support referral.

Combining student self-service into an undifferentiated portal would obscure distinct operational purposes and privacy levels.

### 7.12 Why the Larger Workbenches Are Not Overloaded

Ten screens support four or more use cases: `SCR-STU-01`, `SCR-STU-03`, `SCR-STU-04`, `SCR-ACD-07`, `SCR-FIN-03`, `SCR-FIN-05`, `SCR-TCH-01`, `SCR-TCH-02`, `SCR-OPS-02`, and `SCR-DEV-03`.

Each avoids cognitive overload by organizing work around a single active work item:
- Worklists are filtered by item type and actor authority.
- The workbench displays only the selected item's context.
- Secondary information is disclosed progressively on demand.
- Cross-domain status is surfaced as a result, gate, or blocker rather than a secondary workspace.

### 7.13 Why Single-Use Screens Are Justified

Twelve screens support one primary use case: `SCR-SELF-01`, `SCR-SELF-03`, `SCR-SELF-04`, `SCR-SELF-05`, `SCR-ACD-03`, `SCR-ACD-04`, `SCR-ACD-05`, `SCR-ACD-06`, `SCR-FIN-04`, `SCR-TCH-03`, `SCR-TCH-04`, and `SCR-DEV-04`.

Each single-use screen possesses an unambiguous primary actor, a distinct operational rhythm, or a mandatory confidentiality boundary that prevents it from being subsumed into another screen without sacrificing operational clarity.

### 7.14 Consistency Review and Cross-Stage Integrity

- **Fragmentation Prevention:** The stage-based allocation groups screens into clear, cohesive operational sets for each release. Cross-domain dependencies are explicitly satisfied prior to screen activation.
- **Progressive Information Disclosure:** Complex workbenches (e.g., `SCR-OPS-02`, `SCR-STU-04`) present primary actions and critical blockers first, keeping audit trails and historical context behind secondary tabs.
- **Stage Gating Enforcement:** UI navigation shells must enforce stage boundaries, ensuring screens and incremental features scheduled for later stages remain inaccessible until prerequisite modules are active.

---

## 8. Allocation Issues, Decisions, and Open Questions

### 8.1 Confirmed Allocation Decisions

1. **Intake vs. Continuing vs. Terminating Transitions:**
   - **Stage 1:** Focuses exclusively on intake of new applicants through official enrollment (`SCR-STU-01`).
   - **Stage 2:** Introduces continuing student lifecycle transitions (`SCR-STU-03`), including leave of absence, return from leave, internal cohort/program transfers, annual re-enrollment, and promotion/retention execution.
   - **Stage 3:** Introduces terminating lifecycle transitions (`SCR-STU-04`), including external transfer-out, withdrawal, graduation execution, and alumni status activation, which strictly depend on cross-domain clearances.

2. **Two-Stage Entrance Assessment Architecture:**
   - In accordance with Roadmap Resolution Model B, diagnostic entrance assessment evidence intake in Stage 1 is implemented as manual external evidence capture in `SCR-STU-01`.
   - In Stage 2, it upgrades to direct, automated integration with Academics diagnostic assessment conduct (`ACD-WF-20`), eliminating manual score input.

3. **Homeroom Responsibility Decomposition:**
   - The three-way split from the development roadmap is strictly preserved:
     - Student cohort placement belongs to Students in Stage 1 (`SCR-STU-01`, `SCR-STU-02`).
     - Teacher homeroom advisory assignment belongs to Teaching in Stage 2 (`SCR-TCH-02`).
     - Physical homeroom room allocation belongs to Operations in Stage 2 (`SCR-OPS-02`).

4. **Annual Re-enrollment Allocated to Stage 2:**
   - Administrative re-enrollment confirmation is an operational capability required for roll-forward into the subsequent academic year. It belongs to Stage 2 (`SCR-SELF-02` family confirmation, `SCR-STU-03` administrative execution).

5. **In-Place Corrections Architecture:**
   - Correction workflows (`UC-ACD-15`, `UC-OPS-13`, `UC-DEV-16`) are confirmed as authorized actions embedded directly within owning screens, preserving audit history and context without introducing generic CRUD correction screens.

6. **Reconciled Multi-Stage Scope of `SCR-SELF-02`:**
   - The original screen inventory summary tagged `SCR-SELF-02` as `1–2`. However, the detailed screen definition explicitly covers family-facing requests and authorizations for `UC-STD-11` (External Transfer Out) and `UC-STD-12` (Withdrawal), as well as parental consent tasks for `UC-DEV-02` (Extracurricular Registration). Therefore, its allocation is formally confirmed as spanning **Stages 1, 2, and 3**.

---

### 8.2 Decisions Based on Reasonable Implementation Assumptions

1. **`SCR-FIN-03` (Student Accounts Workbench) Baseline in Stage 1:**
   - *Assumption:* Finance staff require a working view of student financial accounts from Stage 1 to inspect assessed charges, payments, allocations, and balances.
   - *Allocation:* Introduced in Stage 1 with read-only ledger inspection capabilities. Receivables aging (`UC-FIN-06`), credit management (`UC-FIN-07`), and corrections (`UC-FIN-15`) are introduced in Stage 2. Financial Clearance (`UC-FIN-08`) is added in Stage 3.

2. **`SCR-OPS-02` (Scheduling Workbench) Facility Allocation for Events and Extracurriculars in Stage 3:**
   - *Assumption:* Physical room and facility scheduling is deployed in Stage 2 for instructional curriculum classes. In Stage 3, it is incrementally extended to allocate specialized venues (auditoriums, sports grounds) for school events (`UC-OPS-06`) and recurring extracurricular sessions (`UC-DEV-09`, `UC-DEV-10`).
   - *Allocation:* Introduced in Stage 2 for academic timetabling; extended in Stage 3 for institutional events and co-curricular programs.

3. **`SCR-ACD-07` (Academic Records Workbench) Official Graduation Transcripts in Stage 3:**
   - *Assumption:* Interim transcripts and periodic term report cards are generated in Stage 2 (`UC-ACD-13`). Official final cumulative graduation transcripts (bearing graduation conferral notations, cumulative GPA, and accredited transfer credits) are certified in Stage 3.
   - *Allocation:* Introduced in Stage 2; extended in Stage 3.

4. **`SCR-STU-01` External Transfer In (`UC-STD-18`) Sequenced in Stage 3:**
   - *Assumption:* Incoming transfer learners require prior-learning evaluation, origin-school transcript mapping, and accredited credit equivalency reviews against the completed curriculum.
   - *Allocation:* External Transfer In (`UC-STD-18`) is integrated into `SCR-STU-01` in Stage 3.

---

### 8.3 Open Questions Requiring Stakeholder or Architectural Decisions

The following 15 open questions remain unresolved and should be reviewed with institutional stakeholders prior to detailed UI design or sprint implementation:

| # | Question | Why It Matters | Current Treatment | Recommended Resolution |
|---|---|---|---|---|
| 1 | `use-cases.md` refers to 86 source workflows but enumerates 95 use cases. Which count is authoritative? | Affects roadmap sizing, traceability reporting, and sprint planning. | All 95 enumerated use cases are fully covered. | Treat 86 as source business workflows and 95 as atomic technical use cases. |
| 2 | Is `UC-STD-14` Activate Alumni Status a distinct manual action or an automatic graduation outcome? | Determines whether `SCR-STU-04` requires an explicit alumni trigger button. | Treated as an automatic system transition confirmed upon graduation. | Confirm automatic event-driven status transition. |
| 3 | Should School Leadership have a consolidated cross-domain approval inbox? | Leaders acting across admissions, finance, closures, and discipline face fragmented navigation. | Approvals remain embedded in originating domain workbenches. | Maintain embedded approvals; evaluate a unified executive summary feed for Phase 2. |
| 4 | Is the stage-based primary actor of `SCR-STU-01` acceptable, or must intake screens split for audit separation? | Combined intake assumes role-filtered worklists. Policy may mandate separate admission vs. enrollment screens. | Combined intake workbench with role-filtered work items and actions. | Retain unified workbench; enforce strict RBAC permissions per stage. |
| 5 | Should diagnostic entrance assessment be embedded in `SCR-STU-01` or have a separate assessment screen? | Assessment evaluators may require an isolated testing workspace. | Role-specific assessment panel within `SCR-STU-01`. | Retain panel unless diagnostic testing volume exceeds 500 applicants/session. |
| 6 | Does the school require a dedicated bulk promotion/retention execution screen? | Processing hundreds of student progressions individually in `SCR-STU-03` may be inefficient. | `SCR-STU-03` supports cohort review with individual outcome overrides. | Retain in `SCR-STU-03`; provide batch-apply actions. |
| 7 | Should academic record completion, grade publishing, and transcript certification remain combined in `SCR-ACD-07`? | Combines Coordinator and Authority roles. Separation of duties may demand distinct screens. | Single workbench with role-based action authorization. | Retain unified records workbench; enforce authority checks for publishing/correction. |
| 8 | Should financial clearance be a separate queue rather than a work item in `SCR-FIN-03`? | Clearance is a formal cross-domain determination. Embedding in account operations may reduce visibility. | Clearance is a dedicated work item type within `SCR-FIN-03`. | Retain in `SCR-FIN-03` to leverage existing obligation and balance views. |
| 9 | Is it acceptable to combine teaching assignment planning and emergency substitution in `SCR-TCH-02`? | Substitution is urgent and time-critical. A combined workbench could introduce latency. | Combined workbench with an `Urgent Coverage` priority filter. | Retain in `SCR-TCH-02`; provide dedicated quick-coverage modal. |
| 10 | Should school events and operational announcements remain combined in `SCR-OPS-04`? | They share an actor but have different approval chains and publication channels. | Combined workspace with distinct work item types. | Retain in `SCR-OPS-04` given shared Operations Coordinator ownership. |
| 11 | What family-facing interaction should support parental consent for counselling and extracurricular programs? | Consent is a supporting actor responsibility in `UC-DEV-13` and `UC-DEV-02`. | Extracurricular consent surfaces in `SCR-SELF-02`; clinical consent is recorded in `SCR-DEV-04`. | Confirm `SCR-SELF-02` for co-curricular consent; keep clinical consent clinician-recorded. |
| 12 | Are dedicated self-service screens required for viewing published results, report cards, and billing statements? | Inventory supports operational issuance, not dedicated long-term student portals. | Information is viewed contextually within existing self-service tasks. | Address long-term historical transcript/receipt viewer in future portal enhancements. |
| 13 | How should in-place correction actions be discovered and audited across screens? | Corrections are distributed across screens. Auditors need unified audit trails. | In-place authorized actions on records with mandatory audit justification. | Implement a centralized audit event log accessible to institutional compliance officers. |
| 14 | What technical mechanism should enforce stage gating in the UI navigation shell? | Users should not see premature screens or un-implemented submodules. | Documented stage boundaries and gated features. | Implement feature flags tied to active institutional deployment stages. |
| 15 | Does the school require a separate payer experience for non-guardian sponsors? | Third-party payers (embassies, corporate sponsors) do not require family portal features. | `SCR-SELF-02` supports payment-only payer tasks. | Maintain payer view in `SCR-SELF-02`; evaluate standalone sponsor portal if B2B billing grows. |

---

## 9. Final Validation and Verification

This restructured Stage-Based Screen Inventory has been verified against the ten mandatory validation criteria:

- [x] **1. Every existing Screen ID is accounted for:** All 36 screens defined in the authoritative screen inventory are present and explicitly allocated.
- [x] **2. No Screen ID is duplicated unintentionally:** Screen IDs remain unique. Screens that span multiple stages (`SCR-SELF-02`, `SCR-STU-01`, `SCR-FIN-03`, `SCR-OPS-01`, `SCR-OPS-02`, `SCR-ACD-07`, `SCR-ACD-09`) retain their original identifiers and describe incremental capabilities without duplicating screen identities.
- [x] **3. No existing screen has been silently removed or renamed:** All 36 screen titles, primary domains, actors, and interaction patterns match the source specifications exactly.
- [x] **4. Every screen allocation is traceable to supported use cases and roadmap modules:** Every screen's primary stage and extension stage(s) map directly to the module implementation sequence in `sub-modul-and-development-phases.md`.
- [x] **5. All stage prerequisites are respected:**
  - Stage 1 requires no prerequisites and delivers foundation identity, calendar, and basic finance.
  - Stage 2 teaching and learning workflows build upon Stage 1 student identity and calendar context.
  - Stage 3 behaviour, counselling, extracurriculars, and exit transitions build upon mature student, academic, and financial capabilities.
- [x] **6. Cross-domain dependencies are explicitly considered:**
  - Intake payment gate in `SCR-STU-01` consumes `SCR-FIN-02` confirmation in Stage 1.
  - Learning activities in `SCR-ACD-01` consume timetable occurrences from `SCR-OPS-02` and teaching assignments from `SCR-TCH-02` in Stage 2.
  - Promotion execution in `SCR-STU-03` consumes academic recommendations from `SCR-ACD-09` in Stage 2.
  - Terminating transitions in `SCR-STU-04` consume academic clearance from `SCR-ACD-08` and financial clearance from `SCR-FIN-03` in Stage 3.
- [x] **7. Multi-stage screens have clear implementation boundaries:** Each of the 7 multi-stage screens explicitly documents its initial scope, incremental additions, and gated features.
- [x] **8. Stage completion criteria remain achievable:** All 10 Stage 1 criteria, 15 Stage 2 criteria, and 13 Stage 3 criteria defined in the Development Roadmap are supported by the screens allocated to each stage.
- [x] **9. No screen is allocated merely by domain scheduling:** Allocation is strictly driven by underlying operational capabilities and use case dependencies (e.g., Development domain screens are placed in Stage 3 not arbitrarily, but because behaviour cases, pastoral interventions, and disciplinary exit recommendations depend on operational student and academic records).
- [x] **10. Minimalist and non-redundant scope:** No unnecessary screens, CRUD wrappers, or speculative features were introduced. The inventory remains at exactly 36 screens.

---
