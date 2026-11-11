# Elsasa SIS V3 — KPI Implementation Feasibility & Data Gap Analysis

## 1. Executive Summary of Codebase & Schema Readiness

An exhaustive inspection of the **Elsasa SIS V3** codebase—including all 18 SQL migration scripts in `src/backend/Elsasa.Sis.Database/Scripts/`, the 5 implemented backend domain assemblies (`Elsasa.Sis.Operations`, `Elsasa.Sis.Students`, `Elsasa.Sis.Finance`, `Elsasa.Sis.Academics`, `Elsasa.Sis.Teaching`), and the Vue 3 frontend (`src/frontend/src/features/`)—reveals the following implementation baseline:

| Business Domain | Backend Assembly & SQL Schema | Implemented Sub-Modules (Live in DB & API) | Unimplemented Sub-Modules (Planned) | Proposed KPIs (`READY` / `PARTIAL` / `NOT AVAILABLE`) |
|---|---|---|---|---|
| **Student Management** | `Elsasa.Sis.Students` (`students`) | Stage 1 Admission, Enrollment & Identity, Placement & Status; Stage 2 Promotion/Retention Execution | Stage 2 Dedicated Re-Enrollment (`STD-WF-16`), Dedicated Leave of Absence table (`STD-WF-06`), Stage 3 External Transfer & Graduation/Alumni | **6 READY**, **2 PARTIAL**, **0 NOT AVAILABLE** |
| **Academic Performance** | `Elsasa.Sis.Academics` (`academics`) | Stage 2 Learning Structure, Participation & Activities, Assessment & Results, Standing & Progression, Student Learning Plan (SLP) | Stage 3 Graduation Eligibility (`ACA-WF-10`), Academic Clearance (`ACA-WF-11`) | **8 READY**, **1 PARTIAL**, **1 NOT AVAILABLE** |
| **Financial Health** | `Elsasa.Sis.Finance` (`finance`) | Stage 1 Obligations & Billing, Payments & Receivables (including `finance.v_student_receivables`) | Stage 2 Benefits (`Scholarship`, `Discount`, `Waiver`), Payment Arrangements, Credit Balances & Refunds; Stage 3 Financial Clearance | **7 READY**, **0 PARTIAL**, **2 NOT AVAILABLE** |
| **Teaching Performance** | `Elsasa.Sis.Teaching` (`teaching`) | Stage 2 Teacher Profile & Authorization, Assignment & Workload | None (Domain 100% schema-complete) | **7 READY**, **0 PARTIAL**, **0 NOT AVAILABLE** |
| **Student Development** | *None* (No `development` schema) | *None* | Stage 3 Behaviour & Discipline, Wellbeing & Interventions, Activities & Achievements | **0 READY**, **0 PARTIAL**, **5 NOT AVAILABLE** |
| **School Operations** | `Elsasa.Sis.Operations` (`operations`) | Stage 1 Calendar & Periods (`academic_years`, `operational_periods`, `school_calendars`, `school_closures`, `calendar_entries`, `calendar_amendments`) | Stage 2 Scheduling & Rooms (`rooms`, `room_allocations`, `scheduled_occurrences`, `operational_conflicts`); Stage 3 Events & Announcements | **2 READY**, **0 PARTIAL**, **4 NOT AVAILABLE** |
| **Total** | **5 Assemblies / 5 Schemas** | **11 Sub-Modules Live** | **9 Sub-Modules Unimplemented** | **30 READY**, **3 PARTIAL**, **12 NOT AVAILABLE** |

### Current Dashboard State in the Codebase
- **Main Dashboard (`src/frontend/src/features/dashboard/views/DashboardView.vue`)**: Currently a **static module launcher** with hardcoded navigation cards and zero backend API queries.
- **Existing KPI Component (`src/frontend/src/features/teaching/workload/components/WorkloadKpiCards.vue`)**: The only live KPI card strip in the frontend today, computing 5 metrics (`Total Faculty`, `Normal Load`, `Approaching Limit`, `Overloaded (w/ Exception)`, `Limit Breached`) client-side from `GET /api/teaching/workload/commitments`.
- **Existing Early-Warning Query (`src/backend/Elsasa.Sis.Academics/Features/AcademicInterventions/GetAtRiskLearners/GetAtRiskLearnersHandler.cs`)**: Implements a live multi-CTE SQL query at `GET /api/academics/interventions/at-risk-learners` combining `learning_progress`, `academic_standings`, `attendance_records`, and `academic_results` with `academic_interventions`.
- **Existing Financial Aggregation View (`src/backend/Elsasa.Sis.Database/Scripts/20261104_0301_Finance_PaymentsReceivables_Tables.sql`)**: Defines `finance.v_student_receivables`, which computes `net_obligation`, `total_paid` (active allocations only), `outstanding_amount`, `is_overdue`, `days_overdue`, `aging_bracket`, and `receivable_status`.

---

## 2. Category 1 — Immediately Implementable KPIs (`READY` — 30 KPIs)

These 30 KPIs have complete transactional tables, status constraints, and timestamps in the current SQL Server schema and require only read-only Dapper aggregation endpoints and frontend visualization components.

### 2.1 Student Management (`READY`)
| KPI | Authoritative SQL Tables & Key Columns | Existing Codebase Reference |
|---|---|---|
| **STD-01: Active Student Population** | `students.students` (`id`), `students.student_statuses` (`student_id`, `status = 'active'`, `is_current = 1`), `students.administrative_placements` (`student_id`, `academic_year`, `campus`, `school_level`, `grade`, `program`, `cohort`, `homeroom`, `placement_status = 'active'`, `is_current = 1`) | `20261103_0301_Students_EnrollmentIdentity_Tables.sql`, `20261104_0101_Students_PlacementStatus_Tables.sql`, `Elsasa.Sis.Students/Features/Students/GetStudents/` |
| **STD-02: New Student Enrollment Realization** | `students.enrollments` (`id`, `admission_application_id`, `academic_year`, `campus`, `school_level`, `grade`, `program`, `enrollment_status`, `payment_gate_cleared`, `mandatory_documents_completed`, `enrollment_date`) | `20261103_0301_Students_EnrollmentIdentity_Tables.sql`, `Elsasa.Sis.Students/Features/Enrollments/` |
| **STD-03: Admission Funnel Conversion & Offer Yield Rate** | `students.admission_applications` (`id`, `academic_year_id`, `admission_period_id`, `target_campus`, `target_school_level`, `target_grade`, `target_program`, `status`, `submitted_at`), `students.admission_decisions` (`admission_application_id`, `decision_outcome`, `is_current = 1`) | `20261101_0202_Students_AdmissionApplications.sql`, `20261103_0201_Students_Admission_Tables.sql` |
| **STD-04: Admission Application Review Backlog** | `students.admission_applications` (`status IN ('submitted', 'under_review', 'waitlisted')`, `completeness_status`, `submitted_at`), `students.applicant_documents` (`verification_status`), `students.entrance_assessment_evidence` | `20261101_0202_Students_AdmissionApplications.sql`, `20261103_0201_Students_Admission_Tables.sql` |
| **STD-06: Student Withdrawal & Attrition Rate** | `students.student_statuses` (`status IN ('withdrawn', 'transferred_out', 'expelled')`, `effective_date`, `reason`), `students.administrative_history_entries` (`event_type = 'status_change'`, `effective_date`) | `20261104_0101_Students_PlacementStatus_Tables.sql` |
| **STD-07: Student Population Distribution** | `students.administrative_placements` (`campus`, `school_level`, `grade`, `program`, `cohort`, `homeroom`, `is_current = 1`, `placement_status = 'active'`) | `20261104_0101_Students_PlacementStatus_Tables.sql` |

### 2.2 Academic Performance (`READY`)
| KPI | Authoritative SQL Tables & Key Columns | Existing Codebase Reference |
|---|---|---|
| **ACA-01: Academic Standing Distribution** | `academics.academic_standings` (`student_id`, `academic_period_id`, `standing_level`, `status`) | `20261107_0401_Academics_StandingProgression_Tables.sql`, `Elsasa.Sis.Academics/Features/AcademicStandings/` |
| **ACA-02: Students Requiring Academic Intervention** | `academics.academic_interventions` (`student_id`, `academic_period_id`, `status`), `academics.learning_progress`, `academics.academic_standings`, `academics.attendance_records`, `academics.academic_results` | `Elsasa.Sis.Academics/Features/AcademicInterventions/GetAtRiskLearners/GetAtRiskLearnersHandler.cs` |
| **ACA-03: Learning Activity Attendance Rate** | `academics.attendance_records` (`learning_activity_id`, `student_id`, `attendance_status`), `academics.learning_activities` (`learning_unit_id`, `cohort_section`, `activity_date`, `status`), `academics.learning_units` (`learning_structure_id`), `academics.learning_structures` (`academic_period_id`, `grade_level`) | `20261105_0401_Academics_ParticipationActivities_Tables.sql`, `20261104_0401_Academics_LearningStructure_Tables.sql` |
| **ACA-04: Assignment Submission & Late/Missing Work Rate** | `academics.assignments` (`id`, `learning_unit_id`, `cohort_section`, `due_date`, `status`), `academics.submissions` (`assignment_id`, `student_id`, `submission_status`, `submitted_at`) | `20261105_0401_Academics_ParticipationActivities_Tables.sql` |
| **ACA-05: Assessment Grading & Result Publication Rate** | `academics.assessments` (`id`, `learning_unit_id`, `assessment_type`, `evaluation_model`, `scheduled_date`, `status`), `academics.academic_results` (`assessment_id`, `student_id`, `result_status`, `published_at`) | `20261105_0402_Academics_AssessmentResults_Tables.sql` |
| **ACA-06: Learning Unit Mastery & Remediation Rate** | `academics.learning_progress` (`student_id`, `academic_period_id`, `learning_unit_id`, `progress_status`, `remediation_flag`, `evaluated_at`) | `20261107_0401_Academics_StandingProgression_Tables.sql` |
| **ACA-07: Promotion & Retention Distribution** | `academics.promotion_recommendations` (`student_id`, `academic_period_id`, `current_grade_level`, `target_grade_level`, `recommendation_type`, `approval_status`) | `20261107_0401_Academics_StandingProgression_Tables.sql` |
| **ACA-08: Report Card Publication Completeness** | `academics.report_cards` (`student_id`, `academic_period_id`, `status`, `published_at`), `academics.academic_participations` (`student_id`, `academic_period_id`, `participation_status`) | `20261105_0402_Academics_AssessmentResults_Tables.sql`, `20261105_0401_Academics_ParticipationActivities_Tables.sql` |

### 2.3 Financial Health (`READY`)
| KPI | Authoritative SQL Tables & Key Columns | Existing Codebase Reference |
|---|---|---|
| **FIN-01: Total Outstanding Receivables** | `finance.v_student_receivables` (`obligation_id`, `student_id`, `academic_period_id`, `charge_type`, `net_obligation`, `total_paid`, `outstanding_amount`, `receivable_status`) | `20261104_0301_Finance_PaymentsReceivables_Tables.sql` (lines 88–137), `Elsasa.Sis.Finance/Features/Receivables/` |
| **FIN-02: Net Fee Collection Rate** | `finance.v_student_receivables` (`SUM(total_paid) / SUM(net_obligation)`, `due_date`, `academic_period_id`, `charge_type`) | `20261104_0301_Finance_PaymentsReceivables_Tables.sql` |
| **FIN-03: Overdue Receivables & Delinquency Rate** | `finance.v_student_receivables` (`is_overdue = 1`, `days_overdue`, `outstanding_amount`, `receivable_status = 'overdue'`) | `20261104_0301_Finance_PaymentsReceivables_Tables.sql` |
| **FIN-04: Receivables Aging Distribution** | `finance.v_student_receivables` (`aging_bracket` IN `'Current'`, `'1-30 Days'`, `'31-60 Days'`, `'61-90 Days'`, `'90+ Days'`, `outstanding_amount`) | `20261104_0301_Finance_PaymentsReceivables_Tables.sql` (lines 124–131) |
| **FIN-05: Unverified Payment Confirmation Backlog** | `finance.payments` (`id`, `student_financial_account_id`, `payment_amount`, `payment_method`, `confirmation_status = 'submitted'`, `created_at`) | `20261104_0301_Finance_PaymentsReceivables_Tables.sql` |
| **FIN-06: Confirmed Unallocated Payment Balance** | `finance.payments` (`confirmation_status = 'confirmed'`, `payment_amount`) LEFT JOIN `finance.payment_allocations` (`payment_id`, `allocated_amount`, `allocation_status = 'active'`) | `20261104_0301_Finance_PaymentsReceivables_Tables.sql` |
| **FIN-07: Enrollment Gate Financial Clearance Rate** | `students.enrollments` (`payment_gate_cleared`, `enrollment_status`), `finance.financial_obligations` (`enrollment_gate_flag = 1`, `obligation_status = 'posted'`) | `20261103_0301_Students_EnrollmentIdentity_Tables.sql`, `20261104_0201_Finance_ObligationsBilling_Tables.sql` |

### 2.4 Teaching Performance (`READY`)
| KPI | Authoritative SQL Tables & Key Columns | Existing Codebase Reference |
|---|---|---|
| **TCH-01: Teaching Demand Staffing Coverage Rate** | `teaching.teaching_demands` (`id`, `academic_period_id`, `subject_code`, `school_level`, `grade_level`, `load_units`, `status`) | `20261110_0502_Teaching_AssignmentWorkload_Tables.sql`, `Elsasa.Sis.Teaching/Features/TeachingDemands/` |
| **TCH-02: Unassigned Open Teaching Demands** | `teaching.teaching_demands` (`status IN ('open', 'partially_staffed')`, `academic_period_id`, `load_units`) | `20261110_0502_Teaching_AssignmentWorkload_Tables.sql` |
| **TCH-03: Faculty Workload Distribution & Limit Breach Rate** | `teaching.workload_commitments` (`teacher_id`, `academic_period_id`, `total_allocated_units`, `workload_status`), `teaching.workload_policies` (`min_load_units`, `standard_load_units`, `warning_threshold_units`, `max_load_units`) | `20261110_0502_Teaching_AssignmentWorkload_Tables.sql`, `src/frontend/src/features/teaching/workload/components/WorkloadKpiCards.vue` |
| **TCH-04: Unresolved Teaching Capacity & Eligibility Conflicts** | `teaching.capacity_conflicts` (`teacher_id`, `teaching_demand_id`, `conflict_type`, `severity`, `resolution_status = 'open'`) | `20261110_0502_Teaching_AssignmentWorkload_Tables.sql` |
| **TCH-05: Teacher Qualification Verification & Authorization Coverage** | `teaching.teacher_profiles` (`status = 'active'`), `teaching.teaching_authorizations` (`authorization_status`, `unverified_credential_warning`, `effective_to`), `teaching.qualification_evidence` (`verification_status`, `expiry_date`) | `20261110_0501_Teaching_TeacherProfile_Tables.sql` |
| **TCH-06: Active Teacher Substitution & Reassignment Rate** | `teaching.teaching_substitutions` (`teaching_assignment_id`, `status`, `start_date`, `end_date`), `teaching.teaching_reassignments` (`original_assignment_id`, `effective_date`) | `20261110_0502_Teaching_AssignmentWorkload_Tables.sql` |
| **TCH-07: Teaching Assignment Completion & Handover Rate** | `teaching.assignment_completions` (`teaching_assignment_id`, `completion_status`, `completed_at`), `teaching.teaching_assignments` (`academic_period_id`, `assignment_status`) | `20261110_0502_Teaching_AssignmentWorkload_Tables.sql` |

### 2.5 School Operations (`READY`)
| KPI | Authoritative SQL Tables & Key Columns | Existing Codebase Reference |
|---|---|---|
| **OPS-01: Operational Calendar & Period Publication Readiness** | `operations.academic_years` (`id`, `code`, `status`), `operations.operational_periods` (`academic_year_id`, `period_type`, `status`), `operations.school_calendars` (`academic_year_id`, `campus_context`, `school_level_context`, `status`) | `20261101_0101_Operations_AcademicYears.sql`, `20261102_0101_Operations_CalendarPeriods_Tables.sql` |
| **OPS-02: Unscheduled School Closure Days & Unreviewed Amendments** | `operations.school_closures` (`school_calendar_id`, `closure_type`, `start_date`, `end_date`, `status = 'active'`), `operations.calendar_amendments` (`school_calendar_id`, `downstream_impact_reviewed = 0`) | `20261102_0101_Operations_CalendarPeriods_Tables.sql` |

---

## 3. Category 2 — KPIs Requiring Additional Query Normalization or Partial Workflow Enhancements (`PARTIAL` — 3 KPIs + Cross-Domain Filter Normalization)

### 3.1 Partial KPIs
1. **STD-05: Student Retention & Cohort Continuation Rate (`PARTIAL`)**:
   - **What Exists**: `students.administrative_placements` tracks `academic_year` per student with `is_current` and historical placements, and `students.administrative_history_entries` records `event_type = 'progression_execution'` and `'status_change'`. Year-over-year retention can be computed retrospectively once placements for the new academic year are created.
   - **What is Missing**: The Stage 2 **Annual Re-Enrollment workflow (`STD-WF-16`)** (`sub-modul-and-development-phases.md` §2.2) is not yet implemented, so management cannot track *pre-year re-enrollment intent/confirmation rate* (only actual post-placement continuation).
2. **STD-08: Student Lifecycle Status & Leave Expiration Distribution (`PARTIAL`)**:
   - **What Exists**: `students.student_statuses` enforces all 10 lifecycle statuses (`enrolled_pending_activation`, `active`, `on_leave`, `leave_expired`, `suspended`, `withdrawn`, `transferred_out`, `graduated`, `expelled`, `deceased`), so current status counts are immediately queryable.
   - **What is Missing**: The dedicated Leave of Absence record (`STD-WF-06`/`07`) with an explicit `expected_return_date` column does not exist yet in `students.student_statuses` (which only has `effective_date` and `reason`). Consequently, the system cannot automatically calculate *"Leaves Expiring in the Next 14 Days"* until `expected_return_date` is added to `students.student_statuses` or a `students.student_leaves` table is created.
3. **ACA-09: Normalized Assessment Mastery Score & Declining Performance (`PARTIAL`)**:
   - **What Exists**: `academics.assessments` (`evaluation_model`, `score_max`, `passing_score`) and `academics.academic_results` (`score_numeric`, `result_status`) allow computing normalized percentage scores ($\text{score\_numeric} / \text{score\_max} \times 100$) and pass rates for numeric assessments.
   - **What is Missing**: Non-numeric evaluation models (`letter_grade`, `competency`, `rubric`, `pass_fail`, `narrative`) cannot be averaged into a single numeric score without a grade-point mapping table, and `academics.assessments` does not have a `weight` column for weighted course averages.

### 3.2 Structural Schema & Filter Gaps Discovered Across Domains

Even for `READY` KPIs, our schema inspection identified **five critical cross-domain data linkage nuances** that KPI query handlers must handle carefully:

#### Gap A — `academic_year` String Code vs. `academic_year_id` UUID
- **Observation**:
  - `operations.academic_years` has both `id` (`UNIQUEIDENTIFIER`) and `code` (`NVARCHAR(20)`, e.g., `'2026/2027'`).
  - `students.admission_applications` and `operations.operational_periods` reference `academic_year_id` (`UNIQUEIDENTIFIER`).
  - However, `students.enrollments` (`academic_year NVARCHAR(32)`) and `students.administrative_placements` (`academic_year NVARCHAR(32)`) store the **string code** (`'2026/2027'`), **not** `academic_year_id`!
  - Meanwhile, all tables in `finance`, `academics`, and `teaching` link to `academic_period_id` (`UNIQUEIDENTIFIER` referencing `operations.operational_periods.id`).
- **Required Query Resolution**:
  - Any KPI query filtered by `academic_year_id` or `academic_period_id` must resolve `operations.operational_periods` $\rightarrow$ `operations.academic_years` so it can filter `finance`/`academics`/`teaching` by `academic_period_id` (`UNIQUEIDENTIFIER`) and `students.administrative_placements`/`students.enrollments` by `academic_year = ay.code`.

#### Gap B — Unstandardized Campus & Grade String Representations
- **Observation**:
  - There are no master `campuses`, `school_levels`, `grades`, or `programs` tables in the database.
  - In `LoginEndpoint.cs` and `AppLayout.vue`, the frontend campus selector uses synthetic IDs (`'campus-main'`, `'campus-east'`), while `operations.operational_periods.campus_context` defaults to `'All'`, and `students.administrative_placements.campus` / `students.admission_applications.target_campus` store free-text strings (`NVARCHAR(100)`, e.g., `'Main Campus'`, `'Kampus Utama'`).
- **Required Query Resolution**:
  - Dashboard dimension filters (`Campus`, `School Level`, `Grade`, `Program`) should be populated dynamically from a `DISTINCT` lookup endpoint over `students.administrative_placements` and `operations.school_calendars`, and the frontend `appContext.store.ts` campus selector should pass the canonical campus name or `'All'`.

#### Gap C — Indirect `academic_period_id` on Learning Activities, Attendance, Assignments, and Assessments
- **Observation**:
  - `academics.learning_activities`, `academics.attendance_records`, `academics.assignments`, `academics.submissions`, `academics.assessments`, and `academics.academic_results` do **not** have an `academic_period_id` column on their own tables.
- **Required Query Resolution**:
  - KPI queries for `ACA-03`, `ACA-04`, `ACA-05`, and `ACA-09` must join through `academics.learning_units` ($lu$) $\rightarrow$ `academics.learning_structures` ($ls$) where `ls.academic_period_id = @AcademicPeriodId`. (Note: `IX_academics_learning_units_learning_structure_id` and `IX_academics_learning_structures_academic_period_id` are already indexed in `20261104_0401_Academics_LearningStructure_Tables.sql`).

#### Gap D — Missing `campus` Column on `teaching.teaching_demands` and `teaching.teacher_profiles`
- **Observation**:
  - Neither `teaching.teacher_profiles` nor `teaching.teaching_demands` has a `campus` column (`teaching_demands` has `school_level`, `grade_level`, and `cohort_section`).
- **Required Query Resolution**:
  - In Release 1, Teaching KPIs (`TCH-01` to `TCH-07`) support filtering by `Academic Period`, `School Level`, `Grade Level`, and `Subject Code`, while ignoring or greying out the `Campus` filter unless `campus` is inferred from `cohort_section` or added as a nullable column on `teaching.teaching_demands` in a future migration.

#### Gap E — Point-in-Time Historical Snapshots for Mutable State
- **Observation**:
  - While event-dated tables exist for payments (`payment_date`), allocations (`allocated_at`), attendance (`activity_date`), and student status changes (`effective_date`), tables like `teaching.workload_commitments` and `teaching.capacity_conflicts` overwrite mutable state in place.
- **Required Query Resolution**:
  - Period-over-period comparisons across `academic_period_id` work out of the box because `workload_commitments`, `financial_obligations`, `academic_standings`, `learning_progress`, and `teaching_demands` are all partitioned by `academic_period_id`. Within-period weekly/monthly snapshots for mutable counters are addressed in Release 2 via a lightweight snapshot mechanism (detailed in `KPI-ARCHITECTURE.md`).

---

## 4. Category 3 — KPIs Requiring New Data Fields or Unimplemented Workflows (`NOT AVAILABLE` — 12 KPIs)

These 12 KPIs depend on Stage 2 or Stage 3 domain modules documented in `docs/sub-modul-and-development-phases.md` that have **not yet been built** in the codebase.

| KPI Code & Name | Missing Domain / Sub-Module | Missing Tables / Workflows | Development Stage |
|---|---|---|---|
| **ACA-10: Graduation Eligibility Rate** | `Academics` — Standing & Progression (Stage 3 extensions) | `academics.graduation_eligibilities`, `academics.academic_clearances` (`ACA-WF-10`, `ACA-WF-11`) | Stage 3 |
| **FIN-08: Scholarship, Discount & Fee Waiver Distribution** | `Finance` — Benefits | `finance.scholarships`, `finance.discounts`, `finance.waivers` (`FIN-WF-04`), plus deduction logic in `finance.v_student_receivables` | Stage 2 (Unimplemented) |
| **FIN-09: Payment Arrangement Delinquency Rate** | `Finance` — Payment Arrangements | `finance.payment_arrangements` (`FIN-WF-06`) and installment delinquency tracking | Stage 2 (Unimplemented) |
| **DEV-01: Behaviour Incident Frequency & Triage Rate** | `Development` — Behaviour & Discipline | Entire `development` schema: `development.behaviour_observations`, `development.discipline_cases` (`DEV-WF-01`, `DEV-WF-02`, `DEV-WF-04`) | Stage 3 |
| **DEV-02: Positive Behaviour Recognition Ratio** | `Development` — Behaviour & Discipline | `development.positive_recognitions` (`DEV-WF-03`) | Stage 3 |
| **DEV-03: Students with Active Development Interventions** | `Development` — Wellbeing & Interventions | `development.development_interventions`, `development.wellbeing_support_cases` (`DEV-WF-05`, `DEV-WF-06`, `DEV-WF-07`) | Stage 3 |
| **DEV-04: Extracurricular Participation Coverage** | `Development` — Activities & Achievements | `development.student_activity_participations` (`DEV-WF-08`) | Stage 3 |
| **DEV-05: Non-Academic Achievement Distribution** | `Development` — Activities & Achievements | `development.non_academic_achievements` (`DEV-WF-09`) | Stage 3 |
| **OPS-03: Master Schedule Completion & Publication Rate** | `Operations` — Scheduling & Rooms | `operations.scheduled_occurrences`, `operations.operational_activity_requests` (`OPS-WF-07`, `OPS-WF-08`) | Stage 2 (Unimplemented) |
| **OPS-04: Room Utilization & Allocation Conflict Rate** | `Operations` — Scheduling & Rooms | `operations.rooms`, `operations.room_availabilities`, `operations.room_allocations`, `operations.operational_conflicts` (`OPS-WF-06`, `OPS-WF-09`) | Stage 2 (Unimplemented) |
| **OPS-05: Unresolved Operational Activity Requests** | `Operations` — Scheduling & Rooms | `operations.operational_activity_requests` (`OPS-WF-07`) | Stage 2 (Unimplemented) |
| **OPS-06: School Event Completion & Announcement Reach** | `Operations` — Events & Announcements | `operations.school_events`, `operations.operational_announcements` (`OPS-WF-10`, `OPS-WF-11`) | Stage 3 |

---

## 5. Summary of Minor Schema Enhancements That Unlock High Value in Release 2

While **Release 1 requires zero breaking schema changes** (all 15 Release 1 KPIs are 100% `READY`), the following small, non-breaking additive columns in Release 2 will significantly enrich KPI slicing and exception monitoring:
1. **`students.student_statuses.expected_return_date` (`DATE NULL`)**: Enables proactive alerting on student leaves of absence approaching expiration (`STD-08`).
2. **`students.student_statuses.reason_category` (`NVARCHAR(50) NULL`)**: Standardizes withdrawal/transfer reasons (`relocation`, `financial`, `academic`, `health`, `other`) for `STD-06` attrition root-cause analysis.
3. **`teaching.teaching_demands.campus` (`NVARCHAR(100) NULL`)**: Allows Teaching Performance KPIs (`TCH-01`, `TCH-02`, `TCH-04`) to be filtered by `Campus` alongside Student and Finance KPIs.
4. **Update `finance.v_student_receivables`**: Incorporate `finance.financial_policies.overdue_grace_days` when computing `is_overdue` and `days_overdue`.
