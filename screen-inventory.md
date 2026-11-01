# Elsasa SIS V3 — Screen Inventory

## Purpose and Scope

This document translates the authoritative [`use-cases.md`](use-cases.md) inventory into a pragmatic, minimalist, and explicit Screen Inventory for the School Information System.

The inventory is derived from the use cases and the existing domain, actor, entity, workflow, and roadmap artifacts. It does **not** modify domain ownership, introduce new actors, invent business rules, or define detailed UI layouts, fields, or visual styling.

The inventory covers all **95 enumerated use cases** in `use-cases.md`. The source document's executive summary refers to 86 source workflows; the use-case catalog itself contains 95 use-case definitions across the six domains. This discrepancy is recorded in [Open Questions](#8-open-questions).

---

## 1. Design Principles

The following principles govern every screen decision in this inventory.

### 1.1 Operate, Do Not Merely Record

Screens are organized around the work an actor must complete: processing pending items, verifying evidence, making authorized decisions, recording operational activity, monitoring exceptions, or following up on outstanding matters. Entity maintenance screens are used only where routine lookup or correction is itself the operational need.

### 1.2 One Screen, One Working Context

Each screen has a single primary operational purpose and a clearly identifiable primary actor. Related work items may share a screen when they belong to the same working context and use the same interaction model. Unrelated responsibilities are not combined merely to reduce screen count.

### 1.3 Group by Work, Not by Entity or Use Case

Screens are grouped by the actor's actual working context, not by database entity and not one-for-one with use cases. A use case may appear on more than one screen when its work genuinely spans different contexts. A screen may support several use cases when they share the same work item and decision flow.

### 1.4 Progressive Disclosure

The primary work item and its primary action are always visible. Secondary information, historical detail, exceptional actions, and less frequent corrections are disclosed only when the actor needs them. The workbench shows the context required to complete the selected item, not every attribute of the underlying entity.

### 1.5 Consistent Interaction Model

Screens reuse a small set of patterns:

- Work items are processed through a **worklist + workbench**.
- Conditions and exceptions are reviewed through an **overview + drill-down**.
- Existing information is found through **search/list + detail**.
- Time-based work is organized through **calendar/schedule + workspace**.
- Structured plans are built in a **planning workspace**.

Worklists, status indicators, primary actions, and contextual information occupy consistent roles across screens so that learning one screen reduces the learning cost of the next.

### 1.6 Domain Ownership Is Visible

Cross-domain screens coordinate work but do not transfer decision authority. A screen may show another domain's status or result as context, but the owning domain remains responsible for its determination. Clearance, approval, correction, and lifecycle execution remain with the actors and domains defined in the source artifacts.

### 1.7 Minimum Necessary Information

Each screen shows only the information needed for the current task. Financial detail is not exposed in academic or pastoral contexts beyond the minimum result required. Counselling notes remain isolated from all other workspaces. Historical and audit information is available on demand, not displayed by default.

### 1.8 No Screen Without Operational Value

A screen exists only if an actor needs a working environment to complete a defined use case. Correction, approval, and monitoring are normally actions or views within the owning operational workspace rather than separate screens. No screen exists solely because an entity exists.

---

## 2. Screen Pattern Catalog

The inventory uses the five patterns defined for this analysis. No additional pattern is required.

| Pattern | Structure | Used When | Screens Using It |
|---|---|---|---|
| **Work Queue** | Worklist + Workbench | Actors must process a collection of pending, actionable, or exception items. | Intake, lifecycle, exit, teaching, assessment, finance, scheduling, behaviour, discipline, support, records, clearance |
| **Monitoring** | Overview + Drill-down | Actors must monitor capacity, progress, or exceptions and investigate causes. | Faculty Workload Monitoring |
| **Information Lookup** | Search/List + Detail | Actors primarily need to find, inspect, and understand existing information. | Student Records Desk, Admission Application Portal, Extracurricular Registration, Support Referral |
| **Schedule Management** | Calendar/Schedule + Workspace | Work is organized around dates, periods, sessions, or resource allocation. | Calendar & Periods Workspace |
| **Planning** | Planning Workspace | Actors create or modify structured plans with multiple related elements. | Curriculum & Learning Structure, Learning Plan, Billing Cycle, Financial Policy, My Teaching Availability |

### 2.1 Work Queue Convention

Every Work Queue screen follows the same structural logic:

- **Worklist:** the actor's pending, actionable, or exception items, filtered to the actor's authority and current operational period.
- **Workbench:** the selected item's context, evidence, status, and primary action.
- **Context area:** the minimum related information needed to decide or act, including cross-domain status where relevant.
- **Secondary actions:** correction, history, escalation, or exceptional handling, disclosed only when authorized and relevant.

### 2.2 Information Lookup Convention

Every Information Lookup screen follows the same structural logic:

- **Search/List:** the actor finds a record or a small set of candidate records.
- **Detail:** the selected record's current operational state and relevant history.
- **Authorized actions:** maintenance, correction, or related workflow actions available from the detail.

### 2.3 Monitoring Convention

Every Monitoring screen follows the same structural logic:

- **Overview:** essential indicators, thresholds, and exceptions requiring attention.
- **Drill-down:** the underlying assignments, accounts, or records that explain the indicator.
- **Follow-up action:** the authorized response to an exception, without turning the overview into a chart collection.

### 2.4 Planning Convention

Every Planning screen follows the same structural logic:

- **Plan context:** the period, scope, or subject of the plan.
- **Plan structure:** the related elements being created or revised.
- **Validation and approval:** the constraints, approvals, and version state required before the plan becomes effective.
- **Publication action:** the authorized transition from draft to effective.

### 2.5 Schedule Management Convention

Every Schedule Management screen follows the same structural logic:

- **Calendar/Schedule:** the authoritative date, period, or resource view.
- **Workspace:** the selected date, period, or entry and its operational implications.
- **Amendment action:** the authorized change, with impact visible before commitment.

---

## 3. Screen Inventory Summary

The inventory contains **36 screens**: 5 self-service screens and 31 staff screens. Screens are grouped by the actor's working environment, not by domain module.

The **Supported Use Cases** column lists each screen's primary operational homes. Where a screen contributes only an input to a use case, that is stated in the row. Correction use cases that intentionally span several screens are listed separately in [§3.1](#31-correction-use-cases).

| Screen ID | Screen Name | Domain | Primary Actor | Pattern | Stage | Supported Use Cases |
|---|---|---|---|---|---|---|
| `SCR-SELF-01` | Admission Application Portal | Students | Applicant / Parent / Guardian | Information Lookup | 1 | UC-STD-01 |
| `SCR-SELF-02` | Family Tasks & Requests Portal | Students / Finance | Parent / Guardian / Payer | Work Queue | 1–2 | UC-STD-02, UC-FIN-01; family request and authorization inputs for UC-STD-07, UC-STD-10, UC-STD-11, UC-STD-12 |
| `SCR-SELF-03` | Student Learning Workspace | Academics | Student | Work Queue | 2 | UC-ACD-01 |
| `SCR-SELF-04` | Extracurricular Registration | Development | Student | Information Lookup | 3 | UC-DEV-02 |
| `SCR-SELF-05` | Support Referral | Development | Student / Parent / Guardian | Information Lookup | 3 | UC-DEV-01 |
| `SCR-STU-01` | Student Intake Workbench | Students | Admission Officer; Student Administration Officer (enrollment stage) | Work Queue | 1–3 | UC-STD-01, UC-STD-03, UC-STD-04, UC-STD-18, UC-ACD-07 |
| `SCR-STU-02` | Student Records Desk | Students | Student Administration Officer | Information Lookup | 1 | UC-STD-05, UC-STD-06 |
| `SCR-STU-03` | Student Lifecycle Workbench | Students | Student Administration Officer | Work Queue | 2 | UC-STD-07, UC-STD-08, UC-STD-09, UC-STD-10, UC-STD-17 |
| `SCR-STU-04` | Student Exit & Graduation Workbench | Students | Student Administration Officer | Work Queue | 3 | UC-STD-11, UC-STD-12, UC-STD-13, UC-STD-14, UC-STD-15, UC-STD-16 |
| `SCR-ACD-01` | Class Session Workbench | Academics | Teacher | Work Queue | 2 | UC-ACD-02, UC-ACD-03 |
| `SCR-ACD-02` | Coursework & Assessment Workbench | Academics | Teacher | Work Queue | 2 | UC-ACD-04, UC-ACD-05, UC-ACD-06 |
| `SCR-ACD-03` | Curriculum & Learning Structure Workspace | Academics | Academic Coordinator | Planning | 2 | UC-ACD-08 |
| `SCR-ACD-04` | Learning Plan Workspace | Academics | Academic Coordinator | Planning | 2 | UC-ACD-09 |
| `SCR-ACD-05` | Participation & Roster Workbench | Academics | Academic Coordinator | Work Queue | 2 | UC-ACD-10 |
| `SCR-ACD-06` | Academic Intervention Workbench | Academics | Academic Coordinator | Work Queue | 2 | UC-ACD-11 |
| `SCR-ACD-07` | Academic Records Workbench | Academics | Academic Coordinator; Academic Authority | Work Queue | 2–3 | UC-ACD-12, UC-ACD-13, UC-ACD-16, UC-ACD-19 |
| `SCR-ACD-08` | Academic Clearance Review | Academics | Academic Coordinator; Academic Authority | Work Queue | 3 | UC-ACD-14, UC-ACD-20 |
| `SCR-ACD-09` | Cohort Academic Review | Academics | Academic Authority | Work Queue | 2–3 | UC-ACD-17, UC-ACD-18, UC-ACD-21 |
| `SCR-FIN-01` | Billing Cycle Workspace | Finance | Finance Officer | Planning | 1 | UC-FIN-02, UC-FIN-03 |
| `SCR-FIN-02` | Payment Verification Queue | Finance | Finance Officer | Work Queue | 1 | UC-FIN-04, UC-FIN-05, UC-FIN-09 |
| `SCR-FIN-03` | Student Accounts Workbench | Finance | Finance Officer; Finance Authority (correction) | Work Queue | 1–3 | UC-FIN-06, UC-FIN-07, UC-FIN-08, UC-FIN-15 |
| `SCR-FIN-04` | Financial Policy Workspace | Finance | Finance Authority | Planning | 1 | UC-FIN-10 |
| `SCR-FIN-05` | Financial Decisions Queue | Finance | Finance Authority | Work Queue | 2 | UC-FIN-11, UC-FIN-12, UC-FIN-13, UC-FIN-14 |
| `SCR-OPS-01` | Calendar & Periods Workspace | Operations | Operations Authority | Schedule Management | 1–2 | UC-OPS-11, UC-OPS-12, UC-OPS-13 |
| `SCR-OPS-02` | Scheduling Workbench | Operations | Operations Coordinator | Work Queue | 2–3 | UC-OPS-03, UC-OPS-04, UC-OPS-05, UC-OPS-08, UC-OPS-09, UC-OPS-10, UC-OPS-13 |
| `SCR-OPS-03` | Operational Requests Portal | Operations | Teacher / Academic Coordinator / Student Development Officer | Work Queue | 2 | UC-OPS-01, UC-OPS-02 |
| `SCR-OPS-04` | Events & Announcements Workspace | Operations | Operations Coordinator | Work Queue | 3 | UC-OPS-06, UC-OPS-07 |
| `SCR-TCH-01` | Faculty Resource Workspace | Teaching | Teaching Coordinator; Teaching Authority | Work Queue | 2 | UC-TCH-02, UC-TCH-03, UC-TCH-09, UC-TCH-12 |
| `SCR-TCH-02` | Teaching Assignment & Coverage Workbench | Teaching | Teaching Coordinator; Teaching Authority | Work Queue | 2 | UC-TCH-04, UC-TCH-05, UC-TCH-06, UC-TCH-08, UC-TCH-10, UC-TCH-11 |
| `SCR-TCH-03` | Faculty Workload Monitoring | Teaching | Teaching Coordinator | Monitoring | 2 | UC-TCH-07 |
| `SCR-TCH-04` | My Teaching Availability | Teaching | Teacher | Planning | 2 | UC-TCH-01 |
| `SCR-DEV-01` | Behaviour & Incident Triage Workbench | Development | Student Development Officer; Teacher / Authorized Reporter | Work Queue | 3 | UC-DEV-03, UC-DEV-04, UC-DEV-05 |
| `SCR-DEV-02` | Discipline Case Workbench | Development | Discipline Authority | Work Queue | 3 | UC-DEV-14, UC-DEV-15 |
| `SCR-DEV-03` | Pastoral Support Workbench | Development | Student Development Officer | Work Queue | 3 | UC-DEV-06, UC-DEV-07, UC-DEV-08, UC-DEV-12 |
| `SCR-DEV-04` | Counselling Case Workspace | Development | Counselor | Work Queue | 3 | UC-DEV-13 |
| `SCR-DEV-05` | Extracurricular & Achievement Workspace | Development | Student Development Officer | Work Queue | 3 | UC-DEV-09, UC-DEV-10, UC-DEV-11 |

### 3.1 Correction Use Cases

Three correction use cases span several operational records rather than one screen:

| Use Case | Supported By |
|---|---|
| `UC-ACD-15` Correct Non-Grade Academic Record | `SCR-ACD-01` (attendance history), `SCR-ACD-02` (assignment metadata), `SCR-ACD-07` (non-grade record corrections), `SCR-ACD-09` (standing and progression records) |
| `UC-OPS-13` Correct Approved Operations Record | `SCR-OPS-01` (calendar records), `SCR-OPS-02` (room and timetable records) |
| `UC-DEV-16` Correct Approved Development Record | `SCR-DEV-01` to `SCR-DEV-05`, according to the record's confidentiality class and the correcting actor |

This is deliberate: correction is an auditable action on an existing record, not a separate daily workspace. The correction action appears only in the owning screen and only for the actor authorized to perform it.

---

## 4. Detailed Screen Definitions

Definitions are grouped by working environment. Each definition states the operational purpose, structural areas, principal actions, navigation, and supported use cases. Information is described by category, not by individual field.

### 4.1 Self-Service Screens

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

---

#### `SCR-SELF-02` — Family Tasks & Requests Portal

| Attribute | Definition |
|---|---|
| **Domain** | Students / Finance |
| **Primary Actor** | Parent / Guardian; Payer for payment-only tasks |
| **Pattern** | Work Queue |
| **Operational Purpose** | Give families one place to complete the administrative and financial tasks the school requires of them, and to submit the guardian requests or authorizations that initiate student lifecycle actions. |
| **Supported Use Cases** | `UC-STD-02` Submit Re-enrollment Confirmation; `UC-FIN-01` Submit Fee Payment. The portal also provides the family-facing request and authorization input used by `UC-STD-07` Place Student on Leave, `UC-STD-10` Process Internal Transfer, `UC-STD-11` Process External Transfer Out, and `UC-STD-12` Process Withdrawal. |

**Worklist.** Pending family tasks for the current student or family: re-enrollment confirmation for the upcoming period, outstanding billing statements or payment obligations, requests for a leave, internal transfer, external transfer-out, or withdrawal, authorizations awaiting guardian action, and submitted items awaiting school confirmation. Completed tasks remain available as history.

**Workbench.** For a re-enrollment task, the student and target period, the continuation confirmation, and the updated guardian or contact information requested by policy. For a payment task, the billing statement, outstanding obligation, accepted payment channels, and payment evidence submission. For a lifecycle request, the requested change, reason, supporting information, effective date, and the guardian's authorization. For a secondary-guardian dispute, the workbench records the conflicting directive so that the Student Administration Officer can place the action on hold. The workbench shows only the selected task.

**Available Actions.** Confirm or decline re-enrollment intent for an eligible student; update requested contact or emergency information; submit a fee payment with evidence; submit a guardian request for leave, internal transfer, external transfer-out, or withdrawal; provide or withdraw guardian authorization; record a formal dispute or conflicting directive; view the resulting confirmation, receipt, request, or hold status.

**Navigation Context.** Accessed from the family entry point. Re-enrollment submissions and lifecycle requests appear to the Student Administration Officer in `SCR-STU-03` or `SCR-STU-04`. Payment submissions appear to the Finance Officer in `SCR-FIN-02`. The portal does not show internal clearance, adjustment, collection, discipline, or counselling detail. The staff workbench remains responsible for verifying authorization and executing the transition.

---

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

---

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

#### `SCR-STU-01` — Student Intake Workbench

| Attribute | Definition |
|---|---|
| **Domain** | Students (with Academics assessment input and Finance payment-gate status) |
| **Primary Actor** | Admission Officer for application and assessment stages; Student Administration Officer for enrollment and transfer-in stages |
| **Pattern** | Work Queue |
| **Operational Purpose** | Process prospective learners from application through assessment, decision, acceptance, and official enrollment in one continuous intake pipeline. |
| **Supported Use Cases** | `UC-STD-01` Register Admission Application (assisted); `UC-STD-03` Review Admission Application; `UC-STD-04` Enroll Accepted Applicant; `UC-STD-18` Process External Transfer In; `UC-ACD-07` Conduct Applicant Entrance Assessment |

**Worklist.** Intake cases grouped by stage: new or incomplete applications, applications awaiting entrance assessment, applications ready for review, waitlisted or returned cases, accepted applicants awaiting enrollment, and external transfer-in cases awaiting prior-learning evaluation or enrollment.

**Workbench.** The selected case's applicant identity, requested admission period and entry preference, documentary completeness, entrance assessment evidence and approval status, prior-school and prior-credit evaluation for transfer-in, admission decision, enrollment payment-gate status, and proposed initial placement. Actions depend on the case stage and the actor's authority.

**Available Actions.** Register or complete an assisted application; request, conduct, score, or submit an entrance assessment; record an admission decision; return a case for correction; enroll an accepted applicant; assign the official student number and initial placement; record external transfer-in details and origin-school history.

**Navigation Context.** Accessed from the intake worklist. The self-service application (`SCR-SELF-01`) feeds the same case. The Finance Officer confirms the enrollment payment gate in `SCR-FIN-02`; this workbench displays that status but does not determine it. Completed enrollment hands the student to `SCR-STU-02` and `SCR-STU-03`. External transfer-in prior-credit evaluation is an Academics determination displayed here, not made here.

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

---

#### `SCR-ACD-07` — Academic Records Workbench

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

---

#### `SCR-ACD-09` — Cohort Academic Review

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

---

#### `SCR-FIN-03` — Student Accounts Workbench

| Attribute | Definition |
|---|---|
| **Domain** | Finance |
| **Primary Actor** | Finance Officer; Finance Authority for record correction |
| **Pattern** | Work Queue |
| **Operational Purpose** | Monitor and resolve student account conditions: overdue receivables, credits, clearance requests, and account corrections. |
| **Supported Use Cases** | `UC-FIN-06` Monitor Overdue Receivables; `UC-FIN-07` Manage Overpayment and Account Credit; `UC-FIN-08` Evaluate Financial Clearance; `UC-FIN-15` Correct Approved Finance Record |

**Worklist.** Account work items by condition: overdue accounts by aging stage, accounts with unallocated or excess credit, financial clearance requests from Students, and correction requests for finalized financial records. A search entry point allows an authorized actor to inspect any student account without creating a work item.

**Workbench.** The selected account's obligations, payments, allocations, credit balance, installment-arrangement status, and — for a clearance request — the mandatory obligations and blockers relevant to the stated purpose. Correction work shows the original record, the requested change, the rationale, and the downstream account impact. Unrelated student or academic information is not shown.

**Available Actions.** Dispatch a payment reminder and record follow-up; apply account credit to an eligible obligation; prepare a credit transfer or refund dossier for Finance Authority decision; evaluate financial clearance and record `Granted` or `Denied` with blockers; correct a finalized financial record with mandatory audit rationale.

**Navigation Context.** Accessed from the Finance Officer's account worklist or search. Confirmed payments arrive from `SCR-FIN-02`; clearance requests originate in `SCR-STU-04`; refund and adjustment dossiers are sent to `SCR-FIN-05`. The workbench consumes Finance Authority decisions but does not make them. Correction is an authority-only action within the owning account view.

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

#### `SCR-OPS-01` — Calendar & Periods Workspace

| Attribute | Definition |
|---|---|
| **Domain** | Operations |
| **Primary Actor** | Operations Authority |
| **Pattern** | Schedule Management |
| **Operational Purpose** | Establish, amend, and publish the institutional calendar and operational periods that all domains depend on. |
| **Supported Use Cases** | `UC-OPS-11` Establish School Calendar and Periods; `UC-OPS-12` Declare Campus Closure; `UC-OPS-13` Correct Approved Operations Record (calendar records) |

**Calendar.** The academic year, terms, operational periods, holidays, scheduled and emergency closures, and the effective version of the official calendar.

**Workspace.** The selected period or date range, its dependencies across admission, billing, learning, and scheduling, and the impact of a proposed amendment. For a closure, the workspace shows the affected dates and the scope of operational disruption.

**Available Actions.** Define or amend academic year dates, terms, operational periods, and holidays; publish the official calendar; declare a scheduled or emergency campus closure and amend affected dates; correct an approved calendar record with mandatory justification. A closure declaration hands the schedule-impact response to `SCR-OPS-02`.

**Navigation Context.** Accessed from the Operations Authority's calendar area. The published calendar is consumed by every domain and by `SCR-OPS-02`, `SCR-ACD-03`, `SCR-ACD-05`, and `SCR-TCH-04`. Closure coordination links to `SCR-OPS-02` for affected occurrences but does not execute timetable changes here.

---

#### `SCR-OPS-02` — Scheduling Workbench

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

---

## 5. Use Case Coverage

Every enumerated use case is assigned to at least one screen. Where a use case is supported by more than one screen, the primary operational home is listed first and the reason for the additional coverage is stated.

### 5.1 Students Domain

| Use Case | Name | Primary Screen(s) | Coverage Note |
|---|---|---|---|
| `UC-STD-01` | Register Admission Application | `SCR-SELF-01`; `SCR-STU-01` | Family self-service is the primary path; the intake workbench supports assisted or in-person registration. |
| `UC-STD-02` | Submit Re-enrollment Confirmation | `SCR-SELF-02` | The family task queue presents the confirmation; administrative processing is in `SCR-STU-03`. |
| `UC-STD-03` | Review Admission Application | `SCR-STU-01` | Admission review is a stage of the intake pipeline. |
| `UC-STD-04` | Enroll Accepted Applicant | `SCR-STU-01` | Enrollment is the final stage of the same intake case. |
| `UC-STD-05` | Maintain Student Identity | `SCR-STU-02` | Official identity and guardian maintenance is a record-desk action. |
| `UC-STD-06` | Change Administrative Placement | `SCR-STU-02` | Placement change is an effective-dated maintenance action; it is not a lifecycle transition. |
| `UC-STD-07` | Place Student on Leave | `SCR-STU-03`; `SCR-SELF-02` | Leave is a continuing-student lifecycle transition; the guardian request or independent-student submission enters through the family portal. |
| `UC-STD-08` | Return Student from Leave | `SCR-STU-03` | Return, including from `Leave Expired`, is the same lifecycle workbench. |
| `UC-STD-09` | Execute Promotion or Retention | `SCR-STU-03` | The workbench supports cohort review while preserving individual outcomes. |
| `UC-STD-10` | Process Internal Transfer | `SCR-STU-03`; `SCR-SELF-02` | Internal transfer is a continuing-student placement transition; the guardian request enters through the family portal. |
| `UC-STD-11` | Process External Transfer Out | `SCR-STU-04`; `SCR-SELF-02` | Exit workbench; consumes academic and financial clearance results. The guardian request and any conflicting directive enter through the family portal. |
| `UC-STD-12` | Process Withdrawal | `SCR-STU-04`; `SCR-SELF-02` | Exit workbench; includes voluntary, relocation, non-return, and disciplinary categories. The guardian request enters through the family portal; a disciplinary exit is initiated from the discipline outcome. |
| `UC-STD-13` | Execute Graduation | `SCR-STU-04` | Exit workbench; consumes academic eligibility and any required financial clearance. |
| `UC-STD-14` | Activate Alumni Status | `SCR-STU-04` | Alumni activation is an automatic outcome of successful graduation; the workbench confirms it rather than offering a separate task. |
| `UC-STD-15` | Request Academic Clearance for Transfer Out | `SCR-STU-04` | The exit workbench records and tracks the request; determination is in `SCR-ACD-08`. |
| `UC-STD-16` | Request Purpose-Specific Financial Clearance | `SCR-STU-04` | The exit workbench records and tracks the request; determination is in `SCR-FIN-03`. |
| `UC-STD-17` | Process Student Re-enrollment | `SCR-STU-03` | Administrative processing of the family's confirmation in the lifecycle workbench. |
| `UC-STD-18` | Process External Transfer In | `SCR-STU-01` | Transfer-in is an intake case variant requiring prior-credit evaluation before enrollment. |

### 5.2 Academics Domain

| Use Case | Name | Primary Screen(s) | Coverage Note |
|---|---|---|---|
| `UC-ACD-01` | Submit Coursework Assignment | `SCR-SELF-03` | Student-facing coursework submission. |
| `UC-ACD-02` | Record Learning Activity Delivery | `SCR-ACD-01` | Daily session workbench. |
| `UC-ACD-03` | Record and Correct Student Attendance | `SCR-ACD-01` | Attendance capture and auditable correction occur in the session workbench. |
| `UC-ACD-04` | Issue Coursework Assignment | `SCR-ACD-02` | Coursework lifecycle workbench. |
| `UC-ACD-05` | Plan and Administer Assessment | `SCR-ACD-02` | Assessment planning and administration share the coursework workbench. |
| `UC-ACD-06` | Evaluate Student Learning | `SCR-ACD-02` | Formative evaluation and provisional summative scoring occur here; formal publication is in `SCR-ACD-07`. |
| `UC-ACD-07` | Conduct Applicant Entrance Assessment | `SCR-STU-01` | The assessment is an intake input; an Assessment Officer uses the role-specific assessment panel in the intake workbench. |
| `UC-ACD-08` | Define Learning Structure | `SCR-ACD-03` | Curriculum planning workspace. |
| `UC-ACD-09` | Establish Student Learning Plan | `SCR-ACD-04` | Individualized learning-plan workspace. |
| `UC-ACD-10` | Register Academic Participation | `SCR-ACD-05` | Roster and participation workbench. |
| `UC-ACD-11` | Initiate Academic Intervention | `SCR-ACD-06` | Academic intervention workbench. |
| `UC-ACD-12` | Conclude Learning Unit Participation | `SCR-ACD-07` | Completion review is an academic-records work item. |
| `UC-ACD-13` | Produce Report Card and Transcript | `SCR-ACD-07` | Report card and transcript production use the approved records workbench. |
| `UC-ACD-14` | Determine Academic Clearance | `SCR-ACD-08` | Dedicated academic clearance review. |
| `UC-ACD-15` | Correct Non-Grade Academic Record | `SCR-ACD-01`, `SCR-ACD-02`, `SCR-ACD-07`, `SCR-ACD-09` | Correction is an authorized action in the owning screen for attendance, assignment metadata, non-grade records, or standing/progression records. |
| `UC-ACD-16` | Approve and Publish Academic Results | `SCR-ACD-07` | Formal moderation, approval, and publication of summative results. |
| `UC-ACD-17` | Determine Academic Standing and Progression | `SCR-ACD-09` | Cohort review workbench. |
| `UC-ACD-18` | Certify Graduation Academic Eligibility | `SCR-ACD-09` | Graduation eligibility is a cohort-review determination. |
| `UC-ACD-19` | Correct Approved Academic Result | `SCR-ACD-07` | Correction is a controlled action on the approved result set. |
| `UC-ACD-20` | Adjudicate Academic Clearance Correction | `SCR-ACD-08` | Correction is a controlled action within the clearance review. |
| `UC-ACD-21` | Confer Academic Achievement | `SCR-ACD-09` | Achievement conferral is an academic determination based on approved results. |

### 5.3 Finance Domain

| Use Case | Name | Primary Screen(s) | Coverage Note |
|---|---|---|---|
| `UC-FIN-01` | Submit Fee Payment | `SCR-SELF-02` | Family payment submission; verification is in `SCR-FIN-02`. |
| `UC-FIN-02` | Assess Financial Obligations | `SCR-FIN-01` | Billing cycle planning workspace. |
| `UC-FIN-03` | Issue Billing Statement | `SCR-FIN-01` | Statement generation and issuance follow assessment in the same cycle. |
| `UC-FIN-04` | Confirm Payment Receipt | `SCR-FIN-02` | Payment verification queue. |
| `UC-FIN-05` | Allocate Payment to Obligations | `SCR-FIN-02` | Allocation is performed on the confirmed payment in the verification workbench. |
| `UC-FIN-06` | Monitor Overdue Receivables | `SCR-FIN-03` | Student accounts workbench, overdue worklist. |
| `UC-FIN-07` | Manage Overpayment and Account Credit | `SCR-FIN-03` | Credit management is an account work item; refund preparation flows to `SCR-FIN-05`. |
| `UC-FIN-08` | Evaluate Financial Clearance | `SCR-FIN-03` | Clearance requests are an account work item; Finance Authority exceptions are handled in the same workbench. |
| `UC-FIN-09` | Confirm Enrollment Payment Gate | `SCR-FIN-02` | Enrollment gate requests are a payment-verification work item. |
| `UC-FIN-10` | Establish Financial Policy | `SCR-FIN-04` | Financial policy planning workspace. |
| `UC-FIN-11` | Approve Financial Benefit | `SCR-FIN-05` | Finance Authority decision queue. |
| `UC-FIN-12` | Establish Installment Payment Arrangement | `SCR-FIN-05` | Finance Authority decision queue; preparation begins in `SCR-FIN-03`. |
| `UC-FIN-13` | Adjudicate Financial Dispute or Adjustment | `SCR-FIN-05` | Finance Authority decision queue. |
| `UC-FIN-14` | Approve and Execute Refund | `SCR-FIN-05` | Finance Authority decision queue; execution outcome updates `SCR-FIN-03`. |
| `UC-FIN-15` | Correct Approved Finance Record | `SCR-FIN-03` | Authority-only correction action within the owning account view. |

### 5.4 Operations Domain

| Use Case | Name | Primary Screen(s) | Coverage Note |
|---|---|---|---|
| `UC-OPS-01` | Submit Operational Activity Request | `SCR-OPS-03` | Requesting-actor portal. |
| `UC-OPS-02` | Cancel Approved Activity Reservation | `SCR-OPS-03` | Autonomous cancellation by the original initiator. |
| `UC-OPS-03` | Review Operational Feasibility | `SCR-OPS-02` | Scheduling workbench. |
| `UC-OPS-04` | Schedule Activity and Allocate Room | `SCR-OPS-02` | Scheduling workbench. |
| `UC-OPS-05` | Resolve Operational Resource Conflict | `SCR-OPS-02` | Conflict resolution is a scheduling work item. |
| `UC-OPS-06` | Coordinate School Event Logistics | `SCR-OPS-04` | Events and announcements workspace. |
| `UC-OPS-07` | Publish Operational Announcement | `SCR-OPS-04` | Events and announcements workspace. |
| `UC-OPS-08` | Record Room Restriction | `SCR-OPS-02` | Room restriction is a scheduling-availability work item. |
| `UC-OPS-09` | Reschedule or Relocate Scheduled Occurrence | `SCR-OPS-02` | Centralized rescheduling is a scheduling work item. |
| `UC-OPS-10` | Close Operational Arrangement | `SCR-OPS-02` | Closure is an end-of-arrangement scheduling work item. |
| `UC-OPS-11` | Establish School Calendar and Periods | `SCR-OPS-01` | Calendar and periods workspace. |
| `UC-OPS-12` | Declare Campus Closure | `SCR-OPS-01`; `SCR-OPS-02` | Declaration and calendar amendment occur in the calendar workspace; mass schedule impact is executed in the scheduling workbench. |
| `UC-OPS-13` | Correct Approved Operations Record | `SCR-OPS-01`, `SCR-OPS-02` | Calendar corrections belong to the calendar workspace; room and timetable corrections belong to the scheduling workbench. |

### 5.5 Teaching Domain

| Use Case | Name | Primary Screen(s) | Coverage Note |
|---|---|---|---|
| `UC-TCH-01` | Declare Teaching Availability | `SCR-TCH-04` | Teacher self-service availability planning. |
| `UC-TCH-02` | Maintain Teaching Profile | `SCR-TCH-01` | Faculty resource workspace. |
| `UC-TCH-03` | Verify Teacher Qualifications | `SCR-TCH-01` | Faculty resource workspace. |
| `UC-TCH-04` | Process Teaching Demand | `SCR-TCH-02` | Demand validation is an assignment-pipeline work item. |
| `UC-TCH-05` | Evaluate Teacher Eligibility and Capacity | `SCR-TCH-02` | Candidate evaluation occurs in the assignment workbench. |
| `UC-TCH-06` | Arrange Substitute Teacher | `SCR-TCH-02` | Urgent coverage is a work item type in the assignment and coverage workbench. |
| `UC-TCH-07` | Monitor Faculty Workload | `SCR-TCH-03` | Dedicated workload monitoring. |
| `UC-TCH-08` | Conclude Teaching Assignment | `SCR-TCH-02` | End-of-period conclusion is an assignment work item. |
| `UC-TCH-09` | Determine Teaching Authorization | `SCR-TCH-01` | Faculty resource workspace. |
| `UC-TCH-10` | Approve Teaching Assignment | `SCR-TCH-02` | Assignment approval occurs in the assignment workbench. |
| `UC-TCH-11` | Modify or Cancel Teaching Assignment | `SCR-TCH-02` | Change and cancellation requests are assignment work items. |
| `UC-TCH-12` | Correct Approved Teaching Record | `SCR-TCH-01` | Authority-only correction action within the faculty resource workspace. |

### 5.6 Development Domain

| Use Case | Name | Primary Screen(s) | Coverage Note |
|---|---|---|---|
| `UC-DEV-01` | Submit Development Referral | `SCR-SELF-05`; `SCR-DEV-03` | Self-service submission; routing and intake are in the pastoral support workbench. |
| `UC-DEV-02` | Register for Extracurricular Program | `SCR-SELF-04`; `SCR-DEV-05` | Student registration; consent and roster approval are in the extracurricular workspace. Guardian consent may be surfaced as a family task in `SCR-SELF-02` where policy requires it. |
| `UC-DEV-03` | Record Behaviour Observation | `SCR-DEV-01` | Behaviour and triage workbench. |
| `UC-DEV-04` | Report Behaviour Incident | `SCR-DEV-01` | Behaviour and triage workbench. |
| `UC-DEV-05` | Triage Behaviour Incident | `SCR-DEV-01` | Behaviour and triage workbench. |
| `UC-DEV-06` | Route Development Referral | `SCR-DEV-03` | Pastoral support workbench. |
| `UC-DEV-07` | Manage Wellbeing Support Plan | `SCR-DEV-03` | Pastoral support workbench. |
| `UC-DEV-08` | Establish Non-Academic Intervention | `SCR-DEV-03` | Pastoral support workbench. |
| `UC-DEV-09` | Establish Extracurricular Program | `SCR-DEV-05` | Extracurricular and achievement workspace. |
| `UC-DEV-10` | Manage Extracurricular Participation and Attendance | `SCR-DEV-05` | Extracurricular and achievement workspace. |
| `UC-DEV-11` | Recognize Non-Academic Achievement | `SCR-DEV-05` | Extracurricular and achievement workspace. |
| `UC-DEV-12` | Issue Pastoral Cross-Domain Recommendation | `SCR-DEV-03` | Pastoral support workbench. |
| `UC-DEV-13` | Manage Confidential Counselling Case | `SCR-DEV-04` | Isolated counselling workspace. Parental consent status is recorded in the clinical workspace; the family-facing consent interaction is an open question. |
| `UC-DEV-14` | Investigate and Determine Discipline Case | `SCR-DEV-02` | Discipline case workbench. |
| `UC-DEV-15` | Recommend Disciplinary Administrative Sanction | `SCR-DEV-02` | Discipline case workbench; the recommendation is consumed by `SCR-STU-04`. |
| `UC-DEV-16` | Correct Approved Development Record | `SCR-DEV-01` to `SCR-DEV-05` | Correction is an authorized action in the owning workspace, according to record type and confidentiality class. |

### 5.7 Coverage Summary

| Domain | Use Cases | Covered | Screens with Primary Coverage |
|---|---:|---:|---|
| Students | 18 | 18 | `SCR-SELF-01`, `SCR-SELF-02`, `SCR-STU-01` to `SCR-STU-04` |
| Academics | 21 | 21 | `SCR-SELF-03`, `SCR-STU-01` (assessment), `SCR-ACD-01` to `SCR-ACD-09` |
| Finance | 15 | 15 | `SCR-SELF-02`, `SCR-FIN-01` to `SCR-FIN-05` |
| Operations | 13 | 13 | `SCR-OPS-01` to `SCR-OPS-04` |
| Teaching | 12 | 12 | `SCR-TCH-01` to `SCR-TCH-04` |
| Development | 16 | 16 | `SCR-SELF-04`, `SCR-SELF-05`, `SCR-DEV-01` to `SCR-DEV-05` |
| **Total** | **95** | **95** | **36 screens** |

---

## 6. Screen Grouping Rationale

### 6.1 One Intake Pipeline Instead of Separate Admission and Enrollment Screens

`SCR-STU-01` combines application registration, admission review, entrance assessment, enrollment, and external transfer-in. These are stages of the same operational case: a prospective learner entering the school. Splitting them would force the same applicant identity, documents, assessment evidence, decision, and payment-gate status to be carried across separate screens and would create an artificial handoff between the Admission Officer and the Student Administration Officer.

The workbench is stage-sensitive. An Admission Officer sees the review and decision context; an Assessment Officer sees the assessment context; a Student Administration Officer sees the enrollment context. The screen's primary actor therefore changes by stage, which is explicit in the definition. This is preferable to three screens that each expose a fragment of the same intake case.

The alternative — a separate entrance-assessment screen — was rejected because entrance assessment exists only as an authorized input to admission review. It has no independent operational life outside the intake case. A separate screen would add navigation without adding a distinct working context.

### 6.2 Continuing Transitions Separated from Terminating Transitions

`SCR-STU-03` handles transitions for students who remain with the school: leave, return, promotion or retention, internal transfer, and re-enrollment. `SCR-STU-04` handles transitions that end the relationship: external transfer-out, withdrawal, graduation, alumni activation, and the clearance requests those exits require.

This split follows the operational difference between preserving continuity and concluding it. Continuing transitions are mostly about placement, status, and the next academic period. Terminating transitions are dominated by authorization, dual clearance, and final release. Combining them would produce a single oversized lifecycle screen and would obscure the clearance gates that only apply to exits.

Promotion and retention share the continuing workbench with leave and transfer because all are administrative transitions executed by the same actor using the same student context. The workbench supports cohort review for promotion while preserving each student's individual outcome, as required by the domain rules.

### 6.3 Student Records Desk Is Not a Lifecycle Screen

`SCR-STU-02` exists because staff frequently need to find and verify a student's certified identity, guardians, placement, status, and history without processing a transition. This is genuine information lookup, not a disguised CRUD screen: it supports the two maintenance use cases that have no pending work queue (`UC-STD-05` and `UC-STD-06`) and serves as the shared student reference for every other workspace.

Lifecycle execution is deliberately kept out of the records desk. A user who needs to place a student on leave, execute a promotion, or process an exit is sent to the relevant workbench. This preserves the distinction between maintaining the record and changing the student's official relationship with the school.

### 6.4 Teacher Daily Session Work Separated from Coursework and Assessment

`SCR-ACD-01` is the teacher's daily instructional workspace: what was delivered and who attended. `SCR-ACD-02` is the teacher's student-work workspace: what was assigned, what was assessed, and how evidence was evaluated.

These are different rhythms. Attendance and delivery recording happen in or immediately after a session and are time-critical. Assignment and assessment planning, submission review, and evaluation happen across a longer cycle. Combining them would force the teacher to navigate assessment tools while taking a roll-call, or attendance records while evaluating a rubric. The split keeps each screen focused on one primary purpose while using the same Work Queue interaction model.

Formative feedback and provisional summative scores are recorded in `SCR-ACD-02`; formal approval and publication are deliberately moved to `SCR-ACD-07` under the Academic Authority. This preserves the documented formative-versus-summative authority boundary.

### 6.5 Academic Records, Cohort Determinations, and Clearance Are Separate Decisions

`SCR-ACD-07` produces and publishes academic records. `SCR-ACD-09` makes formal determinations from those records: standing, progression, graduation eligibility, and achievement conferral. `SCR-ACD-08` evaluates academic clearance for transfer-out.

These screens share approved results as an input but answer different questions:

- Records: What is the official result, report card, or transcript?
- Cohort review: What standing, progression, eligibility, or achievement follows from the approved record?
- Clearance: Has this outgoing student satisfied academic obligations?

Merging them would blur the distinction between producing a record and making a determination from it, and would make it harder to enforce the different authorities involved. The separation also keeps the graduation-eligibility and transfer-out-clearance gates visible rather than burying them in a general records screen.

### 6.6 Finance Separated by Operational Rhythm and Authority

The Finance screens are separated by the work being done, not by financial entity:

- `SCR-FIN-01` is the periodic billing cycle: assess obligations, review exceptions, issue statements.
- `SCR-FIN-02` is the incoming-payment queue: verify, confirm, allocate, and answer the enrollment gate.
- `SCR-FIN-03` is account stewardship: overdue follow-up, credit, clearance, and correction.
- `SCR-FIN-04` is financial governance: policy and charge rules.
- `SCR-FIN-05` is the Finance Authority's binding decision queue.

Billing and payment verification are distinct operational rhythms. Billing is a planned cycle; payment verification is a continuous queue. Combining them would create a screen that is neither a good planning workspace nor a good work queue.

The Finance Authority queue groups benefits, installment arrangements, disputes and adjustments, and refunds because they share one operational context: a prepared case requiring binding authorization under zero-discretionary-spending rules. The workbench is case-specific, so the grouping does not merge the underlying business meanings. The Finance Officer's preparation work remains in the account or billing workspace.

Financial clearance is placed in `SCR-FIN-03` rather than a separate queue because it is an account determination based on the same mandatory-obligation view used for receivables and credits. This keeps the account context in one place while preserving the formal clearance result and blockers.

### 6.7 Operations Separated by Coordination Role

`SCR-OPS-01` is the authoritative calendar and period workspace, owned by the Operations Authority. `SCR-OPS-02` is the Operations Coordinator's scheduling and room workbench. `SCR-OPS-03` is the requesting actor's own request and cancellation portal. `SCR-OPS-04` is the event and announcement workspace.

The separation follows the asymmetric autonomy rule:

- Requesting actors submit and may cancel their own reservations in `SCR-OPS-03`.
- The Operations Coordinator performs feasibility, allocation, conflict resolution, rescheduling, and closure in `SCR-OPS-02`.
- The Operations Authority owns calendar amendments and closure declarations in `SCR-OPS-01`.

Placing cancellation and rescheduling in the same screen would obscure the different authorities. Placing calendar governance inside the scheduling queue would give the Coordinator workbench an authority it does not own.

Events and announcements share `SCR-OPS-04` because both are operational publication and coordination tasks performed by the same actor. Their workbenches are distinct, so the grouping does not force event logistics and announcement composition into one undifferentiated form.

### 6.8 Teaching Separated by Readiness, Allocation, Monitoring, and Availability

`SCR-TCH-01` establishes teacher readiness: profile, qualifications, authorization, and record correction. `SCR-TCH-02` allocates teaching resources: demand, eligibility, assignment, substitution, change, and conclusion. `SCR-TCH-03` monitors workload against policy thresholds. `SCR-TCH-04` is the teacher's own availability declaration.

These are different working contexts and different primary actors. Readiness is a prerequisite to allocation; allocation is a prerequisite to workload; availability informs both allocation and scheduling. Combining readiness and allocation would mix a credential-verification workspace with an urgent coverage workspace. Combining allocation and workload monitoring would make it harder to see capacity exceptions across the faculty. The four screens preserve clear authority and clear operational purpose.

Emergency substitution is included in `SCR-TCH-02` because it uses the same candidate pool, authorization data, and workload capacity as normal assignment. It is a work item type with an urgent flag and an exception path, not a separate planning process.

### 6.9 Development Separated by Confidentiality and Due Process

`SCR-DEV-01` handles behaviour observations and incident triage. `SCR-DEV-02` handles formal discipline. `SCR-DEV-03` handles non-clinical pastoral support. `SCR-DEV-04` handles confidential counselling. `SCR-DEV-05` handles extracurricular programs and non-academic achievements.

The strongest separation is counselling. Clinical notes are isolated by policy and must never appear in the behaviour, discipline, pastoral, academic, or family workspaces. A separate counselling workspace is therefore mandatory, not a matter of screen-count preference.

Discipline is separated from triage because the Discipline Authority conducts a formal investigation and determination, while the Student Development Officer triages whether a case should enter that process. Combining them would risk exposing formal findings or sanctions to the triage context and would blur the structural separation between investigation and appeal.

Pastoral support is separated from discipline because support and sanction are different purposes. A behaviour incident may route to either, but once routed, the work must not be mixed. Extracurricular programs and achievements are separated because they are program-management and recognition work, not case management.

### 6.10 Corrections Are Actions, Not Screens

`UC-ACD-15`, `UC-OPS-13`, and `UC-DEV-16` are correction workflows that apply to several record types. Creating a separate correction screen for each domain would produce three screens that all do the same thing at a high level — find a record, review its history, submit a justified correction — while requiring users to know which record type belongs to which correction screen.

Instead, correction is an authorized action in the screen that owns the record. This keeps the record's context visible, preserves the audit trail in place, and avoids a generic correction workspace that would have to reproduce every domain's record view. The trade-off is discoverability, which is addressed by a consistent correction action and a clear authority check in each owning screen.

### 6.11 Self-Service Separated from Staff Operations

The five self-service screens exist because applicants, families, and students perform work that staff screens cannot perform on their behalf without losing the self-service intent. They are separated from staff workbenches because the actors, privacy boundaries, and available actions are different.

`SCR-SELF-02` deliberately combines family administrative and financial tasks. From the family's perspective, re-enrollment confirmation, fee payment, and a lifecycle request are all "things the school needs from me." The worklist keeps them together; the workbench keeps each task distinct. This is a family task queue, not a staff account-management screen.

`SCR-SELF-03`, `SCR-SELF-04`, and `SCR-SELF-05` are separated because coursework submission, extracurricular registration, and support referral are different student contexts. Combining them into one "student portal" would create a navigation shell with no single operational purpose.

### 6.12 Why the Larger Workbenches Are Not Overloaded

Ten screens support four or more primary use cases: `SCR-STU-01`, `SCR-STU-03`, `SCR-STU-04`, `SCR-ACD-07`, `SCR-FIN-03`, `SCR-FIN-05`, `SCR-TCH-01`, `SCR-TCH-02`, `SCR-OPS-02`, and `SCR-DEV-03`. Each is organized around one work item type at a time:

- The worklist is filtered by work item type and actor authority.
- The workbench shows only the selected item's context.
- Secondary information and exceptional actions are disclosed only when relevant.
- Cross-domain status is shown as a result or gate, not as a second full workspace.

The test applied was: would splitting the screen make the actor's work simpler? For intake, continuing transitions, exits, faculty readiness, assignment allocation, scheduling, account stewardship, financial authorization, and pastoral support, the answer was no. The work items share a case, a queue, or an authority context. Splitting would add navigation and duplicate context rather than reduce cognitive load.

### 6.13 Why Single-Use Screens Are Justified

Twelve screens support one primary use case: `SCR-SELF-01`, `SCR-SELF-03`, `SCR-SELF-04`, `SCR-SELF-05`, `SCR-ACD-03`, `SCR-ACD-04`, `SCR-ACD-05`, `SCR-ACD-06`, `SCR-FIN-04`, `SCR-TCH-03`, `SCR-TCH-04`, and `SCR-DEV-04`. Each has a distinct actor, a distinct operational rhythm, or a mandatory confidentiality boundary:

- Admission application is the applicant's own submission context.
- Student coursework submission is the student's own assignment workspace.
- Extracurricular registration and support referral are separate student self-service contexts.
- Curriculum planning is a structured planning workspace, not a queue.
- Individualized learning plans are composed and approved separately from the curriculum version that defines prerequisites.
- Participation and roster registration is an eligibility gate that belongs neither in curriculum planning nor in session recording.
- Academic intervention is a case-management workspace for at-risk students.
- Financial policy is governance, not billing operations.
- Workload monitoring is a monitoring view, not assignment processing.
- Teacher availability is a teacher-owned planning input.
- Counselling is an isolated clinical workspace.

A single-use screen is not automatically a bad screen. It is bad only when the use case could be handled naturally inside an existing working context without losing clarity or authority. These twelve cannot.

---

## 7. Consistency Review

### 7.1 Screen Fragmentation

**Finding:** The main fragmentation risk is the number of Work Queue screens in the academic and student domains. A user could perceive `SCR-STU-03` and `SCR-STU-04` as two versions of "student lifecycle," and `SCR-ACD-07`, `SCR-ACD-08`, and `SCR-ACD-09` as three versions of "academic records."

**Assessment:** The separations are operationally justified. Continuing versus terminating transitions have different authorization and clearance gates. Records production, cohort determination, and clearance answer different questions and involve different authorities. The shared Work Queue pattern makes them feel familiar even when the purpose differs.

**Decision:** Keep the separation. Add cross-links so a user can move from a student's record to the relevant lifecycle, exit, academic, or finance context without searching again.

**Residual risk:** If users frequently move between continuing and terminating transitions for the same student, a shared student context header and a consistent "related work" entry point will be needed. This is a design detail for the next phase, not a reason to merge the screens now.

### 7.2 Information Overload

**Finding:** The largest workbenches — intake, exit, assignment and coverage, scheduling, academic records, and pastoral support — could overload users if they display all related information at once.

**Assessment:** The definitions deliberately limit the workbench to the selected work item. The worklist is filtered by work item type and actor authority. Cross-domain information is shown as a status, result, or blocker, not as a full secondary workspace. The main residual overload risk is `SCR-OPS-02`, where a scheduling conflict may require simultaneous visibility of room capacity, teacher availability, calendar constraints, and competing requests. That is inherent to scheduling, but the workbench should present the conflict and the resolution options before exposing all underlying data.

**Decision:** Apply progressive disclosure consistently: selected item first, decision-critical evidence second, history and secondary actions on demand. Do not turn any workbench into a dashboard of all related entities.

### 7.3 Missing Operational Coverage

**Finding:** All 95 enumerated use cases are mapped. Two coverage gaps were identified during review and addressed:

1. **Family-originated lifecycle requests.** Several student lifecycle use cases depend on a guardian request or authorization, but the initial inventory only showed staff processing. `SCR-SELF-02` was expanded to include family requests and authorizations for leave, internal transfer, external transfer-out, and withdrawal, including the ability to record a conflicting guardian directive.
2. **Parental consent for extracurricular participation.** The consent requirement is recorded in `SCR-DEV-05`; a family-facing consent task may be surfaced through `SCR-SELF-02` where policy requires it. The exact consent interaction remains an open question.

**Remaining gap — School Leadership approvals.** School Leadership is a supporting actor in many use cases across Students, Finance, Operations, Teaching, and Development, but no use case has School Leadership as its primary actor. The current inventory embeds leadership approvals in the originating domain workbench (for example, an exceptional admission in `SCR-STU-01`, a payment override in `SCR-STU-01`/`SCR-FIN-02`, a closure in `SCR-OPS-01`, or a disciplinary appeal in `SCR-DEV-02`). This preserves domain ownership but may fragment the work of a leader who needs to act across domains. A consolidated leadership approval inbox is a candidate addition, but it is recorded as an open question rather than assumed.

**Remaining gap — Published-information viewing.** The inventory supports the operational production of results, report cards, transcripts, statements, and announcements, but it does not define dedicated screens for viewing published information. The self-service screens show the information needed for their tasks. If the school requires a separate "view my results" or "view my statement" experience, that is a scope question for the next phase.

### 7.4 Interaction Consistency

**Finding:** The inventory uses the five patterns consistently, but some screens combine work item types that could drift into different interaction models.

**Assessment and decision:**

| Consistency Concern | Decision |
|---|---|
| Work Queue screens must all use worklist + workbench, even when the work item is a plan or a case. | Apply the same structure. A planning-heavy work item (for example, an SLP case or a financial decision) still uses the worklist to select the item and the workbench to act on it. |
| Primary action placement must be predictable. | Each workbench has one primary action appropriate to the selected item and the actor's authority. Secondary actions are visually and structurally subordinate. |
| Status indicators must mean the same thing across screens. | Use a shared status vocabulary for pending, in review, blocked, awaiting approval, approved, executed, denied, cancelled, and closed. Domain-specific states are shown in addition to, not instead of, the shared vocabulary. |
| Cross-domain results must be presented consistently. | Clearances, approvals, and recommendations appear as a result plus reason/blockers plus reference. The receiving screen does not reproduce the determining domain's full workspace. |
| Correction must feel the same everywhere. | Correction is always an authorized action on the owning record, with a mandatory reason and preserved history. The entry point and confirmation pattern should be consistent across domains. |
| Monitoring must not become a chart collection. | `SCR-TCH-03` shows threshold indicators and exceptions first; charts are secondary and only if they explain an exception. |
| Self-service screens must not expose internal operations. | Family and student screens show only the task, its status, and the information the actor needs. Internal notes, triage reasoning, clearance internals, and counselling content are excluded. |

### 7.5 Unnecessary Screens

**Finding:** No screen exists solely because an entity exists. The closest candidates are `SCR-STU-02` (student records), `SCR-FIN-04` (financial policy), and `SCR-ACD-03` (curriculum structure), which could be mistaken for entity-management screens.

**Assessment:**

- `SCR-STU-02` is an information lookup desk for certified identity and placement, supporting two maintenance use cases and serving as the shared student reference. It is operational, not a master-data screen for its own sake.
- `SCR-FIN-04` is a governance planning workspace. Without it, the billing cycle would have no authoritative policy source and the Finance Authority's approval responsibility would be invisible.
- `SCR-ACD-03` is a planning workspace for the learning structure that participation, assessment, and teaching demand depend on. It is not a curriculum entity browser; it is the place where a structure is composed, versioned, and approved.

**Decision:** Keep all three, but ensure their definitions remain task-oriented. Do not let them expand into general entity administration.

### 7.6 Navigation Consistency

**Finding:** The inventory has 36 screens across seven working environments. Without a deliberate navigation model, users could face a deep menu or a search-heavy experience.

**Decision:** Adopt a role-based landing model:

- Each staff actor lands on their primary work queue or workspace.
- A persistent student context is available from `SCR-STU-02` and from any student-related work item.
- Cross-domain handoffs use linked case references rather than duplicate worklists.
- Family and student self-service are separate entry points, not menu items inside staff operations.
- Secondary actions and historical views are reached from the owning screen, not from a global administration menu.

This model keeps navigation shallow and makes the actor's next task visible on entry.

### 7.7 Review Conclusion

The inventory is intentionally not the smallest possible set of screens. It is the smallest set that preserves distinct operational purposes, decision authorities, confidentiality boundaries, and working rhythms. The main consolidations — intake pipeline, continuing versus terminating lifecycle, domain-specific finance queues, teaching allocation and coverage, and development case separation — reduce navigation without merging unrelated work. The main separations — records versus determinations versus clearance, counselling versus pastoral support, discipline versus triage, policy versus billing — preserve clarity and authority.

---

## 8. Open Questions

The following questions remain unresolved. They should be answered with school stakeholders before detailed interaction design or implementation.

| # | Question | Why It Matters | Current Treatment |
|---|---|---|---|
| 1 | `use-cases.md` refers to 86 source workflows but enumerates 95 use cases. Which count is authoritative for planning and traceability? | Affects roadmap sizing, coverage reporting, and whether any workflow is missing or duplicated. | The inventory covers all 95 enumerated use cases. The 86 figure is treated as a source-workflow count, not a use-case count. |
| 2 | Is `UC-STD-14` Activate Alumni Status a distinct operational action or an automatic outcome of graduation? | The Students domain says alumni status is established automatically within graduation, but the use case is listed as a separate use case. | `SCR-STU-04` treats alumni activation as an automatic outcome confirmed in the graduation case, not as a separate task or screen. |
| 3 | Should School Leadership have a consolidated cross-domain approval inbox? | Leadership approvals are currently embedded in originating domain screens. A leader handling admissions, finance, operations, teaching, and discipline exceptions may face fragmented navigation. | Approvals remain in the originating workbench. A leadership inbox is a candidate addition, not assumed. |
| 4 | Is the stage-based primary actor of `SCR-STU-01` acceptable, or must admission review, entrance assessment, and enrollment be separate screens for role separation? | The combined intake pipeline assumes a shared case and role-based workbench. If policy or audit requires strict screen-level separation, the screen must split. | Combined intake pipeline with stage-sensitive workbench and role-based actions. |
| 5 | Should entrance assessment be conducted inside the intake workbench or in a separate assessment workspace? | Assessment Officers may need an isolated assessment context; applicants' assessment evidence may be sensitive. | `SCR-STU-01` includes a role-specific assessment panel. This is a candidate for separation if assessment volume or confidentiality requires it. |
| 6 | Does the school require a dedicated cohort/bulk screen for promotion and retention, separate from the continuing-student lifecycle workbench? | Bulk promotion can process many students with individual successes and failures. A general workbench may not be sufficient at scale. | `SCR-STU-03` supports cohort review within the lifecycle workbench. A dedicated progression execution screen may be needed if volume is high. |
| 7 | Should academic record completion, result publication, report card production, and result correction remain in one `SCR-ACD-07`, or be split by authority? | The screen combines Academic Coordinator and Academic Authority work. If separation of duties is strict, a single workbench may be unacceptable. | One records workbench with role-filtered work items and authority-specific actions. Split if audit or policy requires it. |
| 8 | Should financial clearance be a separate queue rather than a work item in `SCR-FIN-03`? | Clearance is a formal cross-domain determination; embedding it in account operations may reduce visibility or mix account stewardship with clearance authority. | Clearance is a work item type in the student accounts workbench. A separate queue is a candidate if clearance volume or audit requires it. |
| 9 | Is it acceptable to combine teaching assignment planning and emergency substitution in `SCR-TCH-02`? | Substitution is urgent and operationally distinct from planning. A combined workbench may slow down an emergency response. | Combined assignment and coverage workbench with an urgent work item type. A dedicated on-call coverage view may be needed. |
| 10 | Should school events and operational announcements remain in one `SCR-OPS-04`? | They share an actor and a publication context but have different approval paths and workbenches. | Combined workspace with distinct work item types. Split if communications volume or approval separation requires it. |
| 11 | What family-facing interaction should support parental consent for counselling and extracurricular participation? | Consent is a supporting-actor responsibility in `UC-DEV-13` and `UC-DEV-02`, but the inventory does not define a dedicated consent screen. | Extracurricular consent may surface in `SCR-SELF-02`; counselling consent is recorded in `SCR-DEV-04`. The exact family interaction is unresolved. |
| 12 | Are dedicated self-service screens required for viewing published results, report cards, transcripts, statements, and receipts? | The inventory supports production and issuance but not a general "view my records" experience. | Currently contextual within the relevant self-service task. A separate viewing experience is a scope question. |
| 13 | How should the system expose the correction actions for `UC-ACD-15`, `UC-OPS-13`, and `UC-DEV-16` so they are discoverable without creating generic correction screens? | Corrections are distributed across owning screens. Users must know where to go, and auditors must be able to find all corrections. | Correction is an authorized action in the owning screen. A consistent correction entry point and an audit view may be needed. |
| 14 | Which screens are required at each roadmap stage, and how should later-stage screens be hidden or disabled before their stage is active? | The inventory spans Stages 1–3. Showing all screens at once would create unnecessary navigation and premature expectations. | Screens are stage-tagged in the summary. Stage gating is a navigation/configuration decision for implementation. |
| 15 | Does the school require a separate payer experience for refunds and payment history when the payer is not the guardian? | Payer and guardian are distinct actors. `SCR-SELF-02` is family-oriented and may not fit an external sponsor or employer payer. | `SCR-SELF-02` supports payment-only payer tasks. A separate payer view is a candidate if sponsor billing is in scope. |

---

## 9. Final Validation

- [x] Every enumerated use case in `use-cases.md` is assigned to at least one screen.
- [x] Every screen has a clear operational purpose and an identifiable primary actor.
- [x] Screens are grouped by working context, not by entity or one-for-one with use cases.
- [x] Work Queue, Monitoring, Information Lookup, Schedule Management, and Planning patterns are applied according to their intended purposes.
- [x] No new business rules, actors, or use cases are introduced.
- [x] Domain ownership and decision authority are preserved.
- [x] Cross-domain results are consumed, not re-determined, by receiving screens.
- [x] Counselling confidentiality, disciplinary due process, formative-versus-summative authority, and asymmetric operational autonomy are reflected in screen boundaries.
- [x] Corrections are treated as auditable actions in owning screens rather than as generic CRUD screens.
- [x] Detailed fields, UI components, layouts, and visual styling are deliberately not specified.
- [x] Unresolved issues are documented in [Open Questions](#8-open-questions).
