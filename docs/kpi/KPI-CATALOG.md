# Elsasa SIS V3 — Management KPI Catalog

## 1. Purpose & Governance Principles

This catalog defines the recommended Key Performance Indicators (KPIs) for school leadership and operational management across the six business domains of **Elsasa SIS V3**:
1. **Student Management** (`students` schema / `Elsasa.Sis.Students`)
2. **Academic Performance** (`academics` schema / `Elsasa.Sis.Academics`)
3. **Financial Health** (`finance` schema / `Elsasa.Sis.Finance`)
4. **Teaching Performance** (`teaching` schema / `Elsasa.Sis.Teaching`)
5. **Student Development** (`development` domain — planned for Stage 3)
6. **School Operations** (`operations` schema / `Elsasa.Sis.Operations`)

### Core Design Rules
- **Strict Grounding in Actual Schema & Workflows**: Every KPI is evaluated against the actual database migrations (`src/backend/Elsasa.Sis.Database/Scripts/*.sql`), domain handlers, and frontend views currently implemented in the repository.
- **No Fabricated Thresholds or Targets**: Where a KPI requires a target, tolerance, or warning threshold not already persisted in policy tables (such as `finance.financial_policies` or `teaching.workload_policies`), it is explicitly flagged as a **School Management Policy Decision** rather than hardcoded.
- **Financial Accounting Integrity**: Financial KPIs strictly separate **Obligations Billed** (`finance.financial_obligations`), **Confirmed Payments Received** (`finance.payments` where `confirmation_status = 'confirmed'`), and **Allocated Payments Settling Obligations** (`finance.payment_allocations` where `allocation_status = 'active'`, aggregated in `finance.v_student_receivables`).
- **Teaching Workload vs. Teaching Quality**: Teaching KPIs measure staffing coverage, qualification authorization, and workload balance—never equating assignment volume or credit load with pedagogical quality.
- **Student Development Confidentiality**: Development KPIs expose only anonymized, aggregate cohort counts to general leadership, keeping counselling notes, wellbeing referrals, and sensitive case details strictly restricted to authorized pastoral roles.

### Feasibility Classification Legend
- **READY**: Existing tables, columns, views, and transactional workflows are fully implemented to calculate the KPI reliably today.
- **PARTIAL**: Core tables or status fields exist, but accurate calculation requires either additional query normalization, historical snapshot capture, or a partially unimplemented Stage 2 sub-workflow.
- **NOT AVAILABLE**: The underlying business module, database schema, or workflow is not yet implemented in the codebase (e.g., Stage 2 Benefits/Payment Arrangements, Stage 2 Room Scheduling, Stage 3 Student Development).

---

## 2. Summary Matrix of Proposed KPIs

| Code | KPI Name | Domain | Availability | Priority | Release Tier |
|---|---|---|---|---|---|
| **STD-01** | Active Student Population | Student Management | **READY** | High | Release 1 (Essential) |
| **STD-02** | New Student Enrollment Realization | Student Management | **READY** | High | Release 1 (Essential) |
| **STD-03** | Admission Funnel Conversion & Offer Yield Rate | Student Management | **READY** | High | Release 1 (Essential) |
| **STD-04** | Admission Application Review Backlog | Student Management | **READY** | Medium | Release 2 (Important) |
| **STD-05** | Student Retention & Cohort Continuation Rate | Student Management | **PARTIAL** | High | Release 2 (Important) |
| **STD-06** | Student Withdrawal & Attrition Rate | Student Management | **READY** | High | Release 1 (Essential) |
| **STD-07** | Student Population Distribution (Grade / Program / Campus) | Student Management | **READY** | High | Release 1 (Essential) |
| **STD-08** | Student Lifecycle Status & Leave Expiration Distribution | Student Management | **PARTIAL** | Medium | Release 2 (Important) |
| **ACA-01** | Academic Standing Distribution & Unsatisfactory Standing Rate | Academic Performance | **READY** | High | Release 1 (Essential) |
| **ACA-02** | Students Requiring Academic Intervention (At-Risk Rate) | Academic Performance | **READY** | High | Release 1 (Essential) |
| **ACA-03** | Learning Activity Attendance Rate | Academic Performance | **READY** | High | Release 1 (Essential) |
| **ACA-04** | Assignment Submission & Late/Missing Work Rate | Academic Performance | **READY** | Medium | Release 1 (Essential) |
| **ACA-05** | Assessment Grading & Result Publication Rate | Academic Performance | **READY** | High | Release 1 (Essential) |
| **ACA-06** | Learning Unit Mastery & Remediation Rate | Academic Performance | **READY** | Medium | Release 2 (Important) |
| **ACA-07** | Promotion, Conditional Progression & Retention Distribution | Academic Performance | **READY** | High | Release 2 (Important) |
| **ACA-08** | Report Card Publication Completeness | Academic Performance | **READY** | Medium | Release 2 (Important) |
| **ACA-09** | Normalized Assessment Mastery Score & Declining Performance | Academic Performance | **PARTIAL** | Medium | Release 2 (Important) |
| **ACA-10** | Graduation Eligibility & Requirement Completion Rate | Academic Performance | **NOT AVAILABLE** | Low | Release 3 (Advanced) |
| **FIN-01** | Total Outstanding Receivables | Financial Health | **READY** | High | Release 1 (Essential) |
| **FIN-02** | Net Fee Collection (Obligation Settlement) Rate | Financial Health | **READY** | High | Release 1 (Essential) |
| **FIN-03** | Overdue Receivables & Student Delinquency Rate | Financial Health | **READY** | High | Release 1 (Essential) |
| **FIN-04** | Receivables Aging Distribution | Financial Health | **READY** | High | Release 1 (Essential) |
| **FIN-05** | Unverified Payment Confirmation Backlog | Financial Health | **READY** | High | Release 1 (Essential) |
| **FIN-06** | Confirmed Unallocated Payment Balance | Financial Health | **READY** | Medium | Release 2 (Important) |
| **FIN-07** | Enrollment Gate Financial Clearance Rate | Financial Health | **READY** | Medium | Release 2 (Important) |
| **FIN-08** | Scholarship, Discount & Fee Waiver Distribution | Financial Health | **NOT AVAILABLE** | Medium | Release 3 (Advanced) |
| **FIN-09** | Payment Arrangement Delinquency Rate | Financial Health | **NOT AVAILABLE** | Low | Release 3 (Advanced) |
| **TCH-01** | Teaching Demand Staffing Coverage Rate | Teaching Performance | **READY** | High | Release 1 (Essential) |
| **TCH-02** | Unassigned Open Teaching Demands | Teaching Performance | **READY** | High | Release 1 (Essential) |
| **TCH-03** | Faculty Workload Distribution & Limit Breach Rate | Teaching Performance | **READY** | High | Release 1 (Essential) |
| **TCH-04** | Unresolved Teaching Capacity & Eligibility Conflicts | Teaching Performance | **READY** | High | Release 1 (Essential) |
| **TCH-05** | Teacher Qualification Verification & Authorization Coverage | Teaching Performance | **READY** | Medium | Release 2 (Important) |
| **TCH-06** | Active Teacher Substitution & Reassignment Rate | Teaching Performance | **READY** | Medium | Release 2 (Important) |
| **TCH-07** | Teaching Assignment Completion & Handover Rate | Teaching Performance | **READY** | Low | Release 2 (Important) |
| **DEV-01** | Behaviour Incident Frequency & Pending Triage Rate | Student Development | **NOT AVAILABLE** | Medium | Release 3 (Advanced) |
| **DEV-02** | Positive Behaviour Recognition Ratio | Student Development | **NOT AVAILABLE** | Low | Release 3 (Advanced) |
| **DEV-03** | Students with Active Development Interventions (Aggregated) | Student Development | **NOT AVAILABLE** | Medium | Release 3 (Advanced) |
| **DEV-04** | Extracurricular Participation Coverage | Student Development | **NOT AVAILABLE** | Low | Release 3 (Advanced) |
| **DEV-05** | Non-Academic Achievement Distribution | Student Development | **NOT AVAILABLE** | Low | Release 3 (Advanced) |
| **OPS-01** | Operational Calendar & Period Publication Readiness | School Operations | **READY** | High | Release 1 (Essential) |
| **OPS-02** | Unscheduled School Closure Days & Unreviewed Amendments | School Operations | **READY** | Medium | Release 1 (Essential) |
| **OPS-03** | Master Schedule Completion & Publication Rate | School Operations | **NOT AVAILABLE** | Medium | Release 3 (Advanced) |
| **OPS-04** | Room Utilization & Allocation Conflict Rate | School Operations | **NOT AVAILABLE** | Medium | Release 3 (Advanced) |
| **OPS-05** | Unresolved Operational Activity Requests | School Operations | **NOT AVAILABLE** | Low | Release 3 (Advanced) |
| **OPS-06** | School Event Completion & Announcement Reach | School Operations | **NOT AVAILABLE** | Low | Release 3 (Advanced) |

---

## 3. Detailed KPI Specifications by Management Area

### A. Student Management (`students` schema)

#### STD-01: Active Student Population
| Attribute | Description |
|---|---|
| **KPI Name** | Active Student Population |
| **Management Purpose** | Answers *"How many active learners are currently enrolled and placed in the school, and how does headcount compare against capacity and prior periods?"* |
| **Definition** | Total count of distinct students holding an active institutional lifecycle status (`status = 'active'`) and an active administrative placement (`placement_status = 'active'`) in the selected academic year. |
| **Formula** | $\text{Active Students} = \text{COUNT}(\text{DISTINCT } s.\text{id})$ where `ss.is_current = 1 AND ss.status = 'active'` and `ap.is_current = 1 AND ap.placement_status = 'active'`. |
| **Data Source** | `students.students` ($s$) JOIN `students.student_statuses` ($ss$) ON $s.\text{id} = ss.\text{student\_id}$ LEFT JOIN `students.administrative_placements` ($ap$) ON $s.\text{id} = ap.\text{student\_id}$ (`20261103_0301_Students_EnrollmentIdentity_Tables.sql`, `20261104_0101_Students_PlacementStatus_Tables.sql`). |
| **Domain** | Student Management (`Elsasa.Sis.Students`) |
| **Aggregation** | Live current state; historical comparison by Academic Year (`ap.academic_year`) or monthly via `ss.effective_date` / `ap.effective_start_date`. |
| **Dimensions** | Campus (`ap.campus`), School Level (`ap.school_level`), Grade (`ap.grade`), Program (`ap.program`), Cohort (`ap.cohort`), Homeroom (`ap.homeroom`). |
| **Trend** | Meaningful across academic years and monthly within a year (reconstructed from `students.student_statuses.effective_date` and `students.administrative_history_entries`). |
| **Drill-down** | Student Directory (`SCR-STD-03`, `/students/directory`) filtered to `status = active` and selected grade/campus/program. |
| **Availability** | **READY** |
| **Limitation** | `students.administrative_placements.academic_year` is stored as a code string (`NVARCHAR(32)`, e.g. `'2026/2027'`) rather than a foreign key UUID to `operations.academic_years.id`. Queries filtering by `academic_year_id` must join on `operations.academic_years.code = ap.academic_year`. |
| **Priority** | **High** (Release 1 — Executive Overview & Student Dashboard) |

#### STD-02: New Student Enrollment Realization
| Attribute | Description |
|---|---|
| **KPI Name** | New Student Enrollment Realization |
| **Management Purpose** | Tracks how many newly admitted students have completed formal enrollment activation (including mandatory documents and payment gate clearance) versus pending activations. |
| **Definition** | Count of enrollment records with `enrollment_status = 'active'` and Percentage of total initiated enrollments (`pending_activation`, `active`, `cancelled`) that reached `'active'` status for the target academic year. |
| **Formula** | $$\text{Active New Enrollments} = \text{COUNT}(e.\text{id} \mid e.\text{enrollment\_status} = \text{'active'})$$ $$\text{Enrollment Activation Rate (\%)} = \frac{\text{COUNT}(e.\text{id} \mid e.\text{enrollment\_status} = \text{'active'})}{\text{COUNT}(e.\text{id})} \times 100$$ |
| **Data Source** | `students.enrollments` ($e$) JOIN `students.admission_applications` ($aa$) ON $e.\text{admission\_application\_id} = aa.\text{id}$ (`20261103_0301_Students_EnrollmentIdentity_Tables.sql`). |
| **Domain** | Student Management (`Elsasa.Sis.Students`) |
| **Aggregation** | Academic Year (`e.academic_year` / `aa.academic_year_id`), Admission Period (`aa.admission_period_id`), Weekly/Monthly during intake window (`e.enrollment_date`). |
| **Dimensions** | Campus (`e.campus`), School Level (`e.school_level`), Grade (`e.grade`), Program (`e.program`), Enrollment Gate Status (`e.payment_gate_cleared`, `e.mandatory_documents_completed`). |
| **Trend** | Meaningful year-over-year across equivalent admission cycles and weekly cumulative intake curves. |
| **Drill-down** | Enrollment Activation Workbench (`SCR-STD-02`, `/students/enrollment`) showing pending activations blocked by `payment_gate_cleared = 0` or `mandatory_documents_completed = 0`. |
| **Availability** | **READY** |
| **Limitation** | Target intake quota per grade/program is not stored in `operations.operational_periods` or `students`; comparing realization against a target quota requires a management policy configuration. |
| **Priority** | **High** (Release 1 — Executive Overview & Student Dashboard) |

#### STD-03: Admission Funnel Conversion & Offer Yield Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Admission Funnel Conversion & Offer Yield Rate |
| **Management Purpose** | Evaluates admission pipeline effectiveness from initial application submission to admission offer (`accepted`) and final enrollment (`enrolled`). |
| **Definition** | Two linked conversion ratios across `students.admission_applications` and `students.admission_decisions`: (1) **Acceptance Rate** (accepted decisions / total submitted applications) and (2) **Offer Yield Rate** (enrolled applications / accepted applications). |
| **Formula** | $$\text{Acceptance Rate (\%)} = \frac{\text{COUNT}(aa.\text{id} \mid ad.\text{decision\_outcome} = \text{'accepted'})}{\text{COUNT}(aa.\text{id} \mid aa.\text{status} \neq \text{'draft'})} \times 100$$ $$\text{Offer Yield Rate (\%)} = \frac{\text{COUNT}(aa.\text{id} \mid aa.\text{status} = \text{'enrolled'})}{\text{COUNT}(aa.\text{id} \mid ad.\text{decision\_outcome} = \text{'accepted'})} \times 100$$ |
| **Data Source** | `students.admission_applications` ($aa$) LEFT JOIN `students.admission_decisions` ($ad$) ON $aa.\text{id} = ad.\text{admission\_application\_id} \text{ AND } ad.\text{is\_current} = 1$ (`20261101_0202_Students_AdmissionApplications.sql`, `20261103_0201_Students_Admission_Tables.sql`). |
| **Domain** | Student Management (`Elsasa.Sis.Students`) |
| **Aggregation** | Academic Year (`aa.academic_year_id`), Admission Period (`aa.admission_period_id`), Monthly (`aa.submitted_at`). |
| **Dimensions** | Target Campus (`aa.target_campus`), Target School Level (`aa.target_school_level`), Target Grade (`aa.target_grade`), Target Program (`aa.target_program`), Application Status (`draft`, `submitted`, `under_review`, `waitlisted`, `accepted`, `rejected`, `enrolled`, `withdrawn`, `expired`). |
| **Trend** | Meaningful across admission periods and academic years. |
| **Drill-down** | Admission Applications Queue (`SCR-STD-01`, `/students/admissions`) filtered by funnel stage. |
| **Availability** | **READY** |
| **Limitation** | None; all funnel statuses (`draft` through `enrolled`/`withdrawn`/`expired`) and decision outcomes (`accepted`, `waitlisted`, `rejected`) are constrained in SQL check constraints. |
| **Priority** | **High** (Release 1 — Student Dashboard) |

#### STD-04: Admission Application Review Backlog
| Attribute | Description |
|---|---|
| **KPI Name** | Admission Application Review Backlog |
| **Management Purpose** | Identifies operational bottlenecks in admissions where submitted applications are awaiting document verification, entrance assessment, or final admission decision. |
| **Definition** | Count of admission applications currently in `status IN ('submitted', 'under_review', 'waitlisted')` and average elapsed days since `submitted_at`. |
| **Formula** | $$\text{Backlog Count} = \text{COUNT}(aa.\text{id} \mid aa.\text{status} \in \{\text{'submitted'}, \text{'under\_review'}, \text{'waitlisted'}\})$$ $$\text{Avg Age (Days)} = \text{AVG}(\text{DATEDIFF}(\text{day}, aa.\text{submitted\_at}, \text{SYSDATETIMEOFFSET}()))$$ |
| **Data Source** | `students.admission_applications` ($aa$), `students.applicant_documents` ($doc$), `students.entrance_assessment_evidence` ($eae$) (`20261101_0202_Students_AdmissionApplications.sql`, `20261103_0201_Students_Admission_Tables.sql`). |
| **Domain** | Student Management (`Elsasa.Sis.Students`) |
| **Aggregation** | Daily / Real-time operational queue. |
| **Dimensions** | Target Campus (`aa.target_campus`), Target Grade (`aa.target_grade`), Readiness Blocker (`completeness_status != 'ready_for_review'`, pending `applicant_documents.verification_status = 'pending'`, or missing `entrance_assessment_evidence`). |
| **Trend** | Weekly backlog trend during active admission periods. |
| **Drill-down** | Admission Applications Queue (`SCR-STD-01`, `/students/admissions`) sorted by oldest `submitted_at`. |
| **Availability** | **READY** |
| **Limitation** | SLA threshold for maximum review days is not stored in database tables; requires a management policy parameter (e.g., flag applications pending > $N$ days). |
| **Priority** | **Medium** (Release 2 — Student Dashboard & Attention List) |

#### STD-05: Student Retention & Cohort Continuation Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Student Retention & Cohort Continuation Rate |
| **Management Purpose** | Measures the school's ability to retain existing students from one academic year/period to the next. |
| **Definition** | Percentage of non-graduating students who held an active placement in the baseline academic year ($Y_{t-1}$) and remain `active` with an active placement in the subsequent academic year ($Y_t$). |
| **Formula** | $$\text{Retention Rate (\%)} = \frac{\text{COUNT}(\text{DISTINCT } s.\text{id active in } Y_{t-1} \text{ AND active in } Y_t)}{\text{COUNT}(\text{DISTINCT } s.\text{id active in } Y_{t-1} \text{ excluding }\text{'graduated'})} \times 100$$ |
| **Data Source** | `students.administrative_placements` ($ap$), `students.student_statuses` ($ss$), `students.administrative_history_entries` ($ahe$) (`20261104_0101_Students_PlacementStatus_Tables.sql`). |
| **Domain** | Student Management (`Elsasa.Sis.Students`) |
| **Aggregation** | Annual (Academic Year to Academic Year). |
| **Dimensions** | Baseline Campus (`ap.campus`), School Level (`ap.school_level`), Grade (`ap.grade`), Program (`ap.program`). |
| **Trend** | Multi-year historical comparison. |
| **Drill-down** | Student Lifecycle & Placement Workbench (`SCR-STD-05`, `/students/lifecycle-workbench`) and Student Profile History (`SCR-STD-04`). |
| **Availability** | **PARTIAL** |
| **Limitation** | While cohort continuation can be calculated by comparing `students.administrative_placements` across consecutive `academic_year` values and inspecting `administrative_history_entries` (`event_type IN ('progression_execution', 'status_change')`), the dedicated **Annual Re-Enrollment workflow (`STD-WF-16`)** (tracking re-enrollment intent before the new academic year starts) is a Stage 2 capability not yet implemented in the database. |
| **Priority** | **High** (Release 2 — Student Dashboard) |

#### STD-06: Student Withdrawal & Attrition Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Student Withdrawal & Attrition Rate |
| **Management Purpose** | Monitors unplanned student departures (`withdrawn`, `transferred_out`, `expelled`) so management can detect retention issues early in a specific campus, grade, or program. |
| **Definition** | Count and percentage of students who transitioned from `active` to `withdrawn`, `transferred_out`, or `expelled` during the selected academic year or period. |
| **Formula** | $$\text{Attrition Count} = \text{COUNT}(\text{DISTINCT } ss.\text{student\_id} \mid ss.\text{status} \in \{\text{'withdrawn'}, \text{'transferred\_out'}, \text{'expelled'}\} \text{ AND } ss.\text{effective\_date} \in [D_{\text{start}}, D_{\text{end}}])$$ $$\text{Attrition Rate (\%)} = \frac{\text{Attrition Count}}{\text{Active Students at Period Start} + \text{New Enrollments}} \times 100$$ |
| **Data Source** | `students.student_statuses` ($ss$) JOIN `students.administrative_placements` ($ap$) ON $ss.\text{student\_id} = ap.\text{student\_id}$ (`20261104_0101_Students_PlacementStatus_Tables.sql`). |
| **Domain** | Student Management (`Elsasa.Sis.Students`) |
| **Aggregation** | Monthly, Academic Period, Academic Year (via `ss.effective_date` against `operations.academic_years.start_date` and `end_date`). |
| **Dimensions** | Departure Status (`withdrawn`, `transferred_out`, `expelled`), Reason (`ss.reason`), Campus (`ap.campus`), Grade (`ap.grade`), Program (`ap.program`). |
| **Trend** | Monthly and period-over-period comparison. |
| **Drill-down** | Student Directory (`SCR-STD-03`) filtered by `status IN ('withdrawn', 'transferred_out', 'expelled')` and Student Profile (`SCR-STD-04`) administrative history log. |
| **Availability** | **READY** |
| **Limitation** | `students.student_statuses.reason` is free text (`NVARCHAR(500)`) rather than a standardized withdrawal reason taxonomy (`financial`, `relocation`, `academic`, `dissatisfaction`), limiting structured root-cause breakdown until a reason category column or lookup is added. |
| **Priority** | **High** (Release 1 — Executive Overview & Student Dashboard) |

#### STD-07: Student Population Distribution (Grade / Program / Campus)
| Attribute | Description |
|---|---|
| **KPI Name** | Student Population Distribution |
| **Management Purpose** | Provides structural visibility into learner distribution across campuses, school levels, grades, academic programs, and homeroom cohorts to support capacity planning and class balancing. |
| **Definition** | Headcount and percentage share of active students segmented by `campus`, `school_level`, `grade`, `program`, and `cohort` / `homeroom`, plus average cohort size. |
| **Formula** | $$\text{Segment Share (\%)} = \frac{\text{COUNT}(ap.\text{student\_id in segment})}{\text{Total Active Students}} \times 100, \quad \text{Avg Cohort Size} = \frac{\text{COUNT}(ap.\text{student\_id})}{\text{COUNT}(\text{DISTINCT } ap.\text{cohort})}$$ |
| **Data Source** | `students.administrative_placements` ($ap$) JOIN `students.student_statuses` ($ss$) ON $ap.\text{student\_id} = ss.\text{student\_id}$ where $ap.\text{is\_current} = 1 \text{ AND } ap.\text{placement\_status} = \text{'active'} \text{ AND } ss.\text{is\_current} = 1 \text{ AND } ss.\text{status} = \text{'active'}$ (`20261104_0101_Students_PlacementStatus_Tables.sql`). |
| **Domain** | Student Management (`Elsasa.Sis.Students`) |
| **Aggregation** | Current snapshot / Academic Year (`ap.academic_year`). |
| **Dimensions** | Campus (`ap.campus`), School Level (`ap.school_level`), Grade (`ap.grade`), Program (`ap.program`), Cohort (`ap.cohort`), Homeroom (`ap.homeroom`). |
| **Trend** | Academic year-over-year structural shift. |
| **Drill-down** | Student Directory (`SCR-STD-03`) and Lifecycle Workbench (`SCR-STD-05`). |
| **Availability** | **READY** |
| **Limitation** | Dimension values (`campus`, `school_level`, `grade`, `program`, `cohort`) are free-text strings (`NVARCHAR`) without master reference tables, so inconsistent casing/naming must be normalized in queries. |
| **Priority** | **High** (Release 1 — Student Dashboard) |

#### STD-08: Student Lifecycle Status & Leave Expiration Distribution
| Attribute | Description |
|---|---|
| **KPI Name** | Student Lifecycle Status & Leave Expiration Distribution |
| **Management Purpose** | Tracks all students across the 10 formal lifecycle states (`enrolled_pending_activation`, `active`, `on_leave`, `leave_expired`, `suspended`, `withdrawn`, `transferred_out`, `graduated`, `expelled`, `deceased`) and flags students in exception states (`leave_expired`, `on_leave`, `enrolled_pending_activation`). |
| **Definition** | Count of students by current `students.student_statuses.status` (`is_current = 1`), highlighting exception statuses requiring administrative action. |
| **Formula** | $\text{Lifecycle Count}(k) = \text{COUNT}(ss.\text{student\_id} \mid ss.\text{is\_current} = 1 \text{ AND } ss.\text{status} = k)$ for each status $k$. |
| **Data Source** | `students.student_statuses` ($ss$) (`20261104_0101_Students_PlacementStatus_Tables.sql`). |
| **Domain** | Student Management (`Elsasa.Sis.Students`) |
| **Aggregation** | Real-time current state. |
| **Dimensions** | Lifecycle Status (`ss.status`), Campus (`ap.campus`), Grade (`ap.grade`). |
| **Trend** | Monthly distribution snapshot. |
| **Drill-down** | Student Directory (`SCR-STD-03`) filtered by selected lifecycle status. |
| **Availability** | **PARTIAL** |
| **Limitation** | All 10 statuses exist in the `CK_students_statuses_status` check constraint (`20261104_0101_Students_PlacementStatus_Tables.sql` lines 42–45), so current status counts are **READY**. However, the dedicated Leave of Absence record (`STD-WF-06`/`07`) with `expected_return_date` is not yet implemented as a separate table/column, so automatic detection of upcoming leave expirations prior to manual transition to `'leave_expired'` is not yet available. |
| **Priority** | **Medium** (Release 2 — Student Dashboard & Attention List) |

---

### B. Academic Performance (`academics` schema)

#### ACA-01: Academic Standing Distribution & Unsatisfactory Standing Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Academic Standing Distribution & Unsatisfactory Standing Rate |
| **Management Purpose** | Gives school leadership an authoritative view of how many students are in Good Standing versus Academic Warning, Academic Probation, or Unsatisfactory Standing across grades and programs. |
| **Definition** | Distribution of evaluated students across the four formal standing levels (`good_standing`, `academic_warning`, `academic_probation`, `unsatisfactory_standing`) and the percentage of evaluated students in non-good standing (`academic_warning`, `academic_probation`, `unsatisfactory_standing`). |
| **Formula** | $$\text{Non-Good Standing Rate (\%)} = \frac{\text{COUNT}(ast.\text{student\_id} \mid ast.\text{standing\_level} \in \{\text{'academic\_warning'}, \text{'academic\_probation'}, \text{'unsatisfactory\_standing'}\})}{\text{COUNT}(ast.\text{student\_id})} \times 100$$ |
| **Data Source** | `academics.academic_standings` ($ast$) JOIN `students.administrative_placements` ($ap$) ON $ast.\text{student\_id} = ap.\text{student\_id} \text{ AND } ap.\text{is\_current} = 1$ (`20261107_0401_Academics_StandingProgression_Tables.sql`). |
| **Domain** | Academic Performance (`Elsasa.Sis.Academics`) |
| **Aggregation** | Academic Period (`ast.academic_period_id`). |
| **Dimensions** | Standing Level (`ast.standing_level`), Approval Status (`draft`, `approved`, `published`), Campus (`ap.campus`), Grade (`ap.grade`), Program (`ap.program`). |
| **Trend** | Period-over-period comparison across semesters/terms (`operations.operational_periods`). |
| **Drill-down** | Progression & Standing Console (`SCR-ACA-06`, `/academics/progression`). |
| **Availability** | **READY** |
| **Limitation** | `academics.academic_standings` enforces `UQ_academics_academic_standings_student_period` (one standing per student per period), making aggregation clean and exact once standings are evaluated for the period. |
| **Priority** | **High** (Release 1 — Executive Overview & Academic Dashboard) |

#### ACA-02: Students Requiring Academic Intervention (At-Risk Learner Rate)
| Attribute | Description |
|---|---|
| **KPI Name** | Students Requiring Academic Intervention (At-Risk Learner Rate) |
| **Management Purpose** | Identifies learners exhibiting early academic distress (failing assessment results, high absenteeism, unsatisfactory progress, or warning/probation standing) and tracks whether an active `academic_intervention` has been initiated for them. |
| **Definition** | (1) **Total At-Risk Learners**: Distinct students in the academic period meeting at least one risk criterion in `GetAtRiskLearnersHandler` (`below_expectation`/`incomplete` progress, `academic_warning`/`probation`/`unsatisfactory_standing`, $\ge 3$ `absent` attendance records, or failing numeric assessment score $< \text{passing\_score}$). (2) **Unaddressed At-Risk Learners**: At-risk learners who do **not** yet have an active intervention (`academics.academic_interventions.status IN ('initiated', 'in_progress', 'monitoring')`). |
| **Formula** | $$\text{At-Risk Learner Rate (\%)} = \frac{\text{COUNT}(\text{DISTINCT } s_{\text{at-risk}})}{\text{Active Participating Students in Period}} \times 100$$ $$\text{Intervention Coverage Rate (\%)} = \frac{\text{COUNT}(\text{DISTINCT } s_{\text{at-risk with active intervention}})}{\text{COUNT}(\text{DISTINCT } s_{\text{at-risk}}) } \times 100$$ |
| **Data Source** | `academics.learning_progress`, `academics.academic_standings`, `academics.attendance_records`, `academics.academic_results`, `academics.assessments`, and `academics.academic_interventions` (`20261107_0401_Academics_StandingProgression_Tables.sql`; existing query in `src/backend/Elsasa.Sis.Academics/Features/AcademicInterventions/GetAtRiskLearners/GetAtRiskLearnersHandler.cs`). |
| **Domain** | Academic Performance (`Elsasa.Sis.Academics`) |
| **Aggregation** | Real-time within Academic Period (`academic_period_id`). |
| **Dimensions** | Risk Trigger Category (Standing, Progress, Attendance, Failing Assessment), Intervention Status (`none`, `initiated`, `in_progress`, `monitoring`, `resolved`, `escalated`), Campus (`ap.campus`), Grade (`ap.grade`), Program (`ap.program`). |
| **Trend** | Weekly/Monthly within period and period-over-period. |
| **Drill-down** | Academic Intervention Tracker (`SCR-ACA-07`, `/academics/interventions`, tab "At-Risk Early Warning"). |
| **Availability** | **READY** |
| **Limitation** | In `GetAtRiskLearnersHandler.cs` (line 53), the attendance risk trigger currently uses a hardcoded threshold (`HAVING COUNT(ar.id) >= 3` where `attendance_status = 'absent'`). Management should be able to configure this threshold via policy settings. |
| **Priority** | **High** (Release 1 — Executive Overview, Academic Dashboard & Attention List) |

#### ACA-03: Learning Activity Attendance Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Learning Activity Attendance Rate |
| **Management Purpose** | Monitors student engagement and daily instructional attendance across classes, grades, and campuses to catch chronic absenteeism early. |
| **Definition** | Percentage of recorded learning activity attendance entries marked `'present'` or `'late'` (or strictly `'present'`) out of total non-cancelled learning activity attendance records (`present`, `absent`, `late`, `excused`), paired with the **Unexcused Absence Rate** (`attendance_status = 'absent'`). |
| **Formula** | $$\text{Attendance Rate (\%)} = \frac{\text{COUNT}(ar.\text{id} \mid ar.\text{attendance\_status} \in \{\text{'present'}, \text{'late'}\})}{\text{COUNT}(ar.\text{id})} \times 100$$ $$\text{Unexcused Absence Rate (\%)} = \frac{\text{COUNT}(ar.\text{id} \mid ar.\text{attendance\_status} = \text{'absent'})}{\text{COUNT}(ar.\text{id})} \times 100$$ |
| **Data Source** | `academics.attendance_records` ($ar$) JOIN `academics.learning_activities` ($la$) ON $ar.\text{learning\_activity\_id} = la.\text{id}$ JOIN `academics.learning_units` ($lu$) ON $la.\text{learning\_unit\_id} = lu.\text{id}$ JOIN `academics.learning_structures` ($ls$) ON $lu.\text{learning\_structure\_id} = ls.\text{id}$ (`20261105_0401_Academics_ParticipationActivities_Tables.sql`, `20261104_0401_Academics_LearningStructure_Tables.sql`). |
| **Domain** | Academic Performance (`Elsasa.Sis.Academics`) |
| **Aggregation** | Daily, Weekly, Monthly (`la.activity_date`), Academic Period (`ls.academic_period_id`). |
| **Dimensions** | Attendance Status (`present`, `absent`, `late`, `excused`), Learning Unit (`lu.code`, `lu.name`), Cohort Section (`la.cohort_section`), Grade (`ls.grade_level` / `ap.grade`), Campus (`ap.campus`). |
| **Trend** | Daily/Weekly time series across `la.activity_date` and period-over-period comparison. |
| **Drill-down** | Attendance & Coursework Register (`SCR-ACA-03`, `/academics/attendance-coursework`) and At-Risk Learner list (`SCR-ACA-07`). |
| **Availability** | **READY** |
| **Limitation** | `academics.learning_activities` does not carry `academic_period_id` directly; queries must join `learning_activities` $\rightarrow$ `learning_units` $\rightarrow$ `learning_structures.academic_period_id` (or `academic_participations.academic_period_id`). Also tracks *learning activity* attendance rather than daily homeroom gate check-in. |
| **Priority** | **High** (Release 1 — Executive Overview & Academic Dashboard) |

#### ACA-04: Assignment Submission & Late/Missing Work Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Assignment Submission & Late/Missing Work Rate |
| **Management Purpose** | Tracks coursework completion discipline across cohorts and identifies subjects or grades with high rates of missing or late submissions. |
| **Definition** | Breakdown of expected student coursework submissions for published assignments (`assignments.status IN ('published', 'closed')`) across `submitted`, `late`, `missing`, and `resubmitted` statuses. |
| **Formula** | $$\text{On-Time Submission Rate (\%)} = \frac{\text{COUNT}(sub.\text{id} \mid sub.\text{submission\_status} \in \{\text{'submitted'}, \text{'resubmitted'}\})}{\text{COUNT}(sub.\text{id})} \times 100$$ $$\text{Missing Submission Rate (\%)} = \frac{\text{COUNT}(sub.\text{id} \mid sub.\text{submission\_status} = \text{'missing'})}{\text{COUNT}(sub.\text{id})} \times 100$$ |
| **Data Source** | `academics.submissions` ($sub$) JOIN `academics.assignments` ($asg$) ON $sub.\text{assignment\_id} = asg.\text{id}$ JOIN `academics.learning_units` ($lu$) ON $asg.\text{learning\_unit\_id} = lu.\text{id}$ JOIN `academics.learning_structures` ($ls$) ON $lu.\text{learning\_structure\_id} = ls.\text{id}$ (`20261105_0401_Academics_ParticipationActivities_Tables.sql`). |
| **Domain** | Academic Performance (`Elsasa.Sis.Academics`) |
| **Aggregation** | Weekly/Monthly (`asg.due_date`), Academic Period (`ls.academic_period_id`). |
| **Dimensions** | Submission Status (`submitted`, `late`, `missing`, `resubmitted`), Learning Unit (`lu.name`), Cohort Section (`asg.cohort_section`), Grade (`ls.grade_level`), Campus (`ap.campus`). |
| **Trend** | Weekly/Monthly trend within the semester and period-over-period. |
| **Drill-down** | Attendance & Coursework Register (`SCR-ACA-03`, `/academics/attendance-coursework`). |
| **Availability** | **READY** |
| **Limitation** | If a teacher has not yet recorded a `'missing'` row in `academics.submissions` for an unsubmitted assignment past `asg.due_date`, true missing work should also compare active participants (`academics.academic_participations` where `participation_status = 'enrolled'`) against recorded `submissions`. |
| **Priority** | **Medium** (Release 1 — Academic Dashboard) |

#### ACA-05: Assessment Grading & Result Publication Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Assessment Grading & Result Publication Rate |
| **Management Purpose** | Monitors whether planned assessments are being graded, approved, and published on schedule before report card generation deadlines. |
| **Definition** | Percentage of scheduled assessments (`academics.assessments`) that have reached `'published'` status, and percentage of recorded student `academics.academic_results` that are `'approved'` or `'published'` vs `'draft'` or `'under_review'`. |
| **Formula** | $$\text{Result Publication Rate (\%)} = \frac{\text{COUNT}(res.\text{id} \mid res.\text{result\_status} \in \{\text{'approved'}, \text{'published'}\})}{\text{COUNT}(res.\text{id})} \times 100$$ |
| **Data Source** | `academics.assessments` ($a$) LEFT JOIN `academics.academic_results` ($res$) ON $a.\text{id} = res.\text{assessment\_id}$ JOIN `academics.learning_units` ($lu$) ON $a.\text{learning\_unit\_id} = lu.\text{id}$ JOIN `academics.learning_structures` ($ls$) ON $lu.\text{learning\_structure\_id} = ls.\text{id}$ (`20261105_0402_Academics_AssessmentResults_Tables.sql`). |
| **Domain** | Academic Performance (`Elsasa.Sis.Academics`) |
| **Aggregation** | Academic Period (`ls.academic_period_id`), Monthly (`a.scheduled_date`). |
| **Dimensions** | Assessment Type (`diagnostic`, `formative`, `summative`, `practical`, `oral`, `project`, `portfolio`), Evaluation Model (`numeric`, `letter_grade`, `competency`, `rubric`, `pass_fail`, `narrative`), Result Status (`draft`, `under_review`, `approved`, `published`, `amended`), Learning Unit (`lu.name`), Grade (`ls.grade_level`). |
| **Trend** | Cumulative completion curve approaching mid-term and end-of-term grading locks. |
| **Drill-down** | Assessment & Gradebook Workbench (`SCR-ACA-04`, `/academics/gradebook`). |
| **Availability** | **READY** |
| **Limitation** | None; all statuses and evaluation models are explicitly tracked in `academics.assessments` and `academics.academic_results`. |
| **Priority** | **High** (Release 1 — Academic Dashboard & Attention List) |

#### ACA-06: Learning Unit Mastery & Remediation Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Learning Unit Mastery & Remediation Rate |
| **Management Purpose** | Evaluates subject-level learner progress (`on_track`, `exceeding`, `below_expectation`, `remediation_required`, `completed`, `incomplete`) to pinpoint specific subjects or learning units with high failure/remediation rates. |
| **Definition** | Percentage of student-unit progress evaluations (`academics.learning_progress`) classified as `'below_expectation'`, `'incomplete'`, or `'remediation_required'` (`remediation_flag = 1`). |
| **Formula** | $$\text{Remediation Rate (\%)} = \frac{\text{COUNT}(lp.\text{id} \mid lp.\text{remediation\_flag} = 1 \text{ OR } lp.\text{progress\_status} \in \{\text{'below\_expectation'}, \text{'incomplete'}, \text{'remediation\_required'}\})}{\text{COUNT}(lp.\text{id})} \times 100$$ |
| **Data Source** | `academics.learning_progress` ($lp$) JOIN `academics.learning_units` ($lu$) ON $lp.\text{learning\_unit\_id} = lu.\text{id}$ (`20261107_0401_Academics_StandingProgression_Tables.sql`). |
| **Domain** | Academic Performance (`Elsasa.Sis.Academics`) |
| **Aggregation** | Academic Period (`lp.academic_period_id`). |
| **Dimensions** | Progress Status (`on_track`, `exceeding`, `below_expectation`, `remediation_required`, `completed`, `incomplete`), Learning Unit (`lu.code`, `lu.name`), Subject Area (`lu.subject_area`), Grade (`ap.grade`), Campus (`ap.campus`). |
| **Trend** | Period-over-period comparison by subject area and grade. |
| **Drill-down** | Progression & Standing Console (`SCR-ACA-06`, `/academics/progression`). |
| **Availability** | **READY** |
| **Limitation** | Relies on teachers/coordinators recording periodic `learning_progress` evaluations (`evaluated_at`). |
| **Priority** | **Medium** (Release 2 — Academic Dashboard) |

#### ACA-07: Promotion, Conditional Progression & Retention Distribution
| Attribute | Description |
|---|---|
| **KPI Name** | Promotion, Conditional Progression & Retention Distribution |
| **Management Purpose** | Supports end-of-year progression boards by summarizing recommended student outcomes (`promote`, `conditional_promotion`, `retain`, `remediation_required`) and tracking alignment between academic recommendations (`academics.promotion_recommendations`) and administrative execution (`students.administrative_history_entries`). |
| **Definition** | Count and percentage breakdown of evaluated students by `recommendation_type` and `approval_status` (`draft`, `approved`, `rejected`, `forwarded_to_student_domain`). |
| **Formula** | $$\text{Outcome Share}(k) = \frac{\text{COUNT}(pr.\text{id} \mid pr.\text{recommendation\_type} = k)}{\text{COUNT}(pr.\text{id})} \times 100, \quad k \in \{\text{'promote'}, \text{'conditional\_promotion'}, \text{'retain'}, \text{'remediation\_required'}\}$$ |
| **Data Source** | `academics.promotion_recommendations` ($pr$) (`20261107_0401_Academics_StandingProgression_Tables.sql`). |
| **Domain** | Academic Performance (`Elsasa.Sis.Academics`) |
| **Aggregation** | Academic Period / End-of-Year (`pr.academic_period_id`). |
| **Dimensions** | Recommendation Type (`pr.recommendation_type`), Approval Status (`pr.approval_status`), Current Grade (`pr.current_grade_level`), Target Grade (`pr.target_grade_level`), Campus (`ap.campus`). |
| **Trend** | Annual comparison across academic years. |
| **Drill-down** | Progression & Standing Console (`SCR-ACA-06`, `/academics/progression`) and Student Lifecycle Workbench (`SCR-STD-05`, `/students/lifecycle-workbench`). |
| **Availability** | **READY** |
| **Limitation** | Meaningful primarily at the end of an academic year/progression cycle; remains near zero during mid-semester operations. |
| **Priority** | **High** (Release 2 — Academic Dashboard) |

#### ACA-08: Report Card Publication Completeness
| Attribute | Description |
|---|---|
| **KPI Name** | Report Card Publication Completeness |
| **Management Purpose** | Tracks institutional readiness at the end of a grading period by measuring how many active students have an approved and published report card. |
| **Definition** | Percentage of active participating students in the academic period whose `academics.report_cards` record has reached `status = 'published'` (versus `draft`, `under_review`, `approved`, `amended`, or not yet generated). |
| **Formula** | $$\text{Report Card Publication Rate (\%)} = \frac{\text{COUNT}(rc.\text{id} \mid rc.\text{status} = \text{'published'})}{\text{COUNT}(\text{DISTINCT } ap_{\text{part}}.\text{student\_id})} \times 100$$ |
| **Data Source** | `academics.report_cards` ($rc$) RIGHT JOIN `academics.academic_participations` ($ap_{\text{part}}$) ON $rc.\text{student\_id} = ap_{\text{part}}.\text{student\_id} \text{ AND } rc.\text{academic\_period\_id} = ap_{\text{part}}.\text{academic\_period\_id}$ (`20261105_0402_Academics_AssessmentResults_Tables.sql`). |
| **Domain** | Academic Performance (`Elsasa.Sis.Academics`) |
| **Aggregation** | Academic Period (`rc.academic_period_id`). |
| **Dimensions** | Report Card Status (`missing`, `draft`, `under_review`, `approved`, `published`, `amended`), Grade (`ap.grade`), Homeroom (`ap.homeroom`), Campus (`ap.campus`). |
| **Trend** | Daily progress during report card generation week; period-over-period. |
| **Drill-down** | Report Cards & Transcripts Studio (`SCR-ACA-05`, `/academics/reports-transcripts`). |
| **Availability** | **READY** |
| **Limitation** | None. |
| **Priority** | **Medium** (Release 2 — Academic Dashboard) |

#### ACA-09: Normalized Assessment Mastery Score & Declining Performance
| Attribute | Description |
|---|---|
| **KPI Name** | Normalized Assessment Mastery Score & Declining Performance |
| **Management Purpose** | Measures average quantitative assessment achievement across numeric assessments and identifies students whose normalized score dropped significantly compared to the previous academic period. |
| **Definition** | (1) Mean normalized percentage score ($\text{score\_numeric} / \text{score\_max} \times 100$) and Pass Rate ($\text{score\_numeric} \ge \text{passing\_score}$) across published assessments with `evaluation_model = 'numeric'`. (2) Count of students whose period-over-period mean normalized score declined by more than a management-configured delta ($\Delta\%$). |
| **Formula** | $$\text{Normalized Score}_{i,a} = \frac{res.\text{score\_numeric}}{a.\text{score\_max}} \times 100 \quad (\text{where } a.\text{evaluation\_model} = \text{'numeric'} \text{ AND } a.\text{score\_max} > 0)$$ $$\text{Assessment Pass Rate (\%)} = \frac{\text{COUNT}(res.\text{id} \mid res.\text{score\_numeric} \ge a.\text{passing\_score})}{\text{COUNT}(res.\text{id})} \times 100$$ |
| **Data Source** | `academics.academic_results` ($res$) JOIN `academics.assessments` ($a$) ON $res.\text{assessment\_id} = a.\text{id}$ (`20261105_0402_Academics_AssessmentResults_Tables.sql`). |
| **Domain** | Academic Performance (`Elsasa.Sis.Academics`) |
| **Aggregation** | Academic Period (`ls.academic_period_id`), Assessment Type (`a.assessment_type`). |
| **Dimensions** | Subject Area (`lu.subject_area`), Learning Unit (`lu.name`), Grade (`ls.grade_level`), Assessment Type (`summative`, `formative`, etc.), Campus (`ap.campus`). |
| **Trend** | Period-over-period comparison per student and per cohort. |
| **Drill-down** | Assessment & Gradebook Workbench (`SCR-ACA-04`, `/academics/gradebook`). |
| **Availability** | **PARTIAL** |
| **Limitation** | Elsasa SIS V3 intentionally supports 6 heterogeneous evaluation models (`numeric`, `letter_grade`, `competency`, `rubric`, `pass_fail`, `narrative`; see `academic-domain.md` §3.1). Numeric averaging only applies to `evaluation_model = 'numeric'` where `score_max > 0`. Furthermore, `assessments` does not store assessment weighting (`weight`), and defining what constitutes a "significant decline" (e.g., $-10\%$ vs $-15\%$) requires a school management policy decision. |
| **Priority** | **Medium** (Release 2 — Academic Dashboard) |

#### ACA-10: Graduation Eligibility & Requirement Completion Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Graduation Eligibility & Requirement Completion Rate |
| **Management Purpose** | Tracks final-year students' fulfillment of credit, mandatory unit, and competency requirements for graduation. |
| **Definition** | Percentage of final-grade students evaluated as eligible for graduation against `academics.academic_requirements`. |
| **Formula** | $\text{Graduation Eligibility Rate (\%)} = \frac{\text{Eligible Final-Year Students}}{\text{Total Active Final-Year Students}} \times 100$. |
| **Data Source** | Planned `academics.graduation_eligibilities` and `academics.academic_clearances` (currently only `academics.academic_requirements` exists in `20261104_0401_Academics_LearningStructure_Tables.sql`). |
| **Domain** | Academic Performance (`Elsasa.Sis.Academics`) |
| **Aggregation** | Academic Year / Final Period. |
| **Dimensions** | School Level, Program, Campus. |
| **Trend** | Annual. |
| **Drill-down** | Planned Graduation Eligibility Workbench (Stage 3). |
| **Availability** | **NOT AVAILABLE** |
| **Limitation** | Stage 3 feature (`sub-modul-and-development-phases.md` §3.1: `GraduationEligibility`, `AcademicClearance`, `ACA-WF-10`, `ACA-WF-11`). Tables and workflows do not exist yet. |
| **Priority** | **Low** (Release 3 — Deferred until Stage 3) |

---

### C. Financial Health (`finance` schema)

> [!IMPORTANT]
> All Financial Health KPIs strictly distinguish between:
> 1. **Net Financial Obligation** (`finance.v_student_receivables.net_obligation` / `finance.financial_obligations.original_amount` where status is `'posted'`),
> 2. **Confirmed Payment Received** (`finance.payments.payment_amount` where `confirmation_status = 'confirmed'`), and
> 3. **Allocated Payment Settling an Obligation** (`finance.payment_allocations.allocated_amount` where `allocation_status = 'active'`, summed as `total_paid` in `finance.v_student_receivables`).

#### FIN-01: Total Outstanding Receivables
| Attribute | Description |
|---|---|
| **KPI Name** | Total Outstanding Receivables |
| **Management Purpose** | Answers *"How much posted student fee revenue remains unpaid across the school right now?"* |
| **Definition** | Total unpaid balance (in IDR) across all posted, non-cancelled student financial obligations (`net_obligation - total_paid`), along with the count of distinct students with an open balance (`outstanding_amount > 0`). |
| **Formula** | $$\text{Total Outstanding Receivables (IDR)} = \sum_{r \in \text{v\_student\_receivables}} r.\text{outstanding\_amount}$$ $$\text{Students with Open Balance} = \text{COUNT}(\text{DISTINCT } r.\text{student\_id} \mid r.\text{outstanding\_amount} > 0)$$ |
| **Data Source** | `finance.v_student_receivables` ($r$) (`20261104_0301_Finance_PaymentsReceivables_Tables.sql` lines 88–137), joined with `students.administrative_placements` ($ap$) on $r.\text{student\_id} = ap.\text{student\_id} \text{ AND } ap.\text{is\_current} = 1$. |
| **Domain** | Financial Health (`Elsasa.Sis.Finance`) |
| **Aggregation** | Real-time ledger state; sliceable by Academic Period (`r.academic_period_id`) and Due Date (`r.due_date`). |
| **Dimensions** | Academic Period (`r.academic_period_id`), Charge Type (`r.charge_type`: `tuition`, `admission_fee`, `enrollment_fee`, `reregistration_fee`, `activity_fee`, `material_fee`, `examination_fee`, `administrative_fee`), Campus (`ap.campus`), Grade (`ap.grade`), Program (`ap.program`). |
| **Trend** | Monthly and period-over-period comparison (requires periodic snapshot for historical point-in-time balance). |
| **Drill-down** | Receivables & Collection Monitor (`SCR-FIN-05`, `/finance/receivables`) and Student Financial Account Ledger (`SCR-FIN-02`, `/finance/accounts/:studentId`). |
| **Availability** | **READY** |
| **Limitation** | `finance.v_student_receivables` already exists in SQL Server and filters `fo.obligation_status = 'posted'`. Point-in-time historical trend requires either reconstructing allocations by `allocated_at` or storing monthly snapshots. |
| **Priority** | **High** (Release 1 — Executive Overview & Finance Dashboard) |

#### FIN-02: Net Fee Collection (Obligation Settlement) Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Net Fee Collection (Obligation Settlement) Rate |
| **Management Purpose** | Measures the percentage of billed student financial obligations for an academic period that have been formally settled via confirmed, active payment allocations. |
| **Definition** | Ratio of total actively allocated payments (`SUM(r.total_paid)`) to total net posted obligations (`SUM(r.net_obligation)`) for the selected academic period. Also provides **Matured Collection Rate** restricted to obligations whose `due_date <= CAST(SYSDATETIMEOFFSET() AS DATE)`. |
| **Formula** | $$\text{Period Collection Rate (\%)} = \frac{\sum r.\text{total\_paid}}{\sum r.\text{net\_obligation}} \times 100$$ $$\text{Matured Collection Rate (\%)} = \frac{\sum_{r.\text{due\_date} \le \text{Today}} r.\text{total\_paid}}{\sum_{r.\text{due\_date} \le \text{Today}} r.\text{net\_obligation}} \times 100$$ |
| **Data Source** | `finance.v_student_receivables` ($r$) (`20261104_0301_Finance_PaymentsReceivables_Tables.sql`). |
| **Domain** | Financial Health (`Elsasa.Sis.Finance`) |
| **Aggregation** | Academic Period (`r.academic_period_id`), Monthly (`r.due_date`). |
| **Dimensions** | Charge Type (`r.charge_type`), Campus (`ap.campus`), School Level (`ap.school_level`), Grade (`ap.grade`), Program (`ap.program`). |
| **Trend** | Cumulative collection curve across the semester and period-over-period comparison against equivalent periods. |
| **Drill-down** | Receivables & Collection Monitor (`SCR-FIN-05`, `/finance/receivables`) and Obligations & Billing Center (`SCR-FIN-03`, `/finance/obligations-billing`). |
| **Availability** | **READY** |
| **Limitation** | Correctly uses `r.total_paid` (sum of `finance.payment_allocations.allocated_amount` where `allocation_status = 'active'`), so unallocated or unconfirmed payments do **not** artificially inflate the collection rate. |
| **Priority** | **High** (Release 1 — Executive Overview & Finance Dashboard) |

#### FIN-03: Overdue Receivables & Student Delinquency Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Overdue Receivables & Student Delinquency Rate |
| **Management Purpose** | Highlights past-due financial obligations (`due_date < Today` and `outstanding_amount > 0`) requiring immediate collection follow-up. |
| **Definition** | (1) **Overdue Receivables Amount (IDR)**: Sum of `outstanding_amount` where `is_overdue = 1` (`receivable_status = 'overdue'`). (2) **Overdue Share of Receivables (%)**: Overdue amount divided by total outstanding receivables. (3) **Delinquent Student Count**: Distinct students with at least one overdue obligation. |
| **Formula** | $$\text{Overdue Receivables (IDR)} = \sum_{r.\text{is\_overdue} = 1} r.\text{outstanding\_amount}$$ $$\text{Overdue Ratio (\%)} = \frac{\sum_{r.\text{is\_overdue} = 1} r.\text{outstanding\_amount}}{\sum r.\text{outstanding\_amount}} \times 100$$ |
| **Data Source** | `finance.v_student_receivables` ($r$) (`20261104_0301_Finance_PaymentsReceivables_Tables.sql` lines 113–136). |
| **Domain** | Financial Health (`Elsasa.Sis.Finance`) |
| **Aggregation** | Real-time daily status; Academic Period (`r.academic_period_id`). |
| **Dimensions** | Aging Bracket (`1-30 Days`, `31-60 Days`, `61-90 Days`, `90+ Days`), Charge Type (`r.charge_type`), Campus (`ap.campus`), Grade (`ap.grade`), Program (`ap.program`). |
| **Trend** | Weekly/Monthly overdue balance trajectory. |
| **Drill-down** | Receivables & Collection Monitor (`SCR-FIN-05`, `/finance/receivables`) filtered by `receivable_status = 'overdue'`. |
| **Availability** | **READY** |
| **Limitation** | Note that `finance.v_student_receivables` computes `is_overdue` against `fo.due_date` (`CAST(SYSDATETIMEOFFSET() AS DATE) > fo.due_date`). If an obligation uses multi-installment `finance.obligation_due_schedules` or a policy `overdue_grace_days` (`finance.financial_policies.overdue_grace_days`), enhancing the query to incorporate `overdue_grace_days` is recommended in Release 2. |
| **Priority** | **High** (Release 1 — Executive Overview, Finance Dashboard & Attention List) |

#### FIN-04: Receivables Aging Distribution
| Attribute | Description |
|---|---|
| **KPI Name** | Receivables Aging Distribution |
| **Management Purpose** | Breaks down unpaid receivables by delinquency duration (`Current`, `1-30 Days`, `31-60 Days`, `61-90 Days`, `90+ Days`) to assess collection risk and cash flow exposure. |
| **Definition** | Sum of `outstanding_amount` and count of obligations/students grouped by `finance.v_student_receivables.aging_bracket`. |
| **Formula** | $$\text{Aging Bucket Total}(b) = \sum_{r.\text{aging\_bracket} = b, \, r.\text{outstanding\_amount} > 0} r.\text{outstanding\_amount}$$ for $b \in \{\text{'Current'}, \text{'1-30 Days'}, \text{'31-60 Days'}, \text{'61-90 Days'}, \text{'90+ Days'}\}$. |
| **Data Source** | `finance.v_student_receivables` ($r$) (`20261104_0301_Finance_PaymentsReceivables_Tables.sql` lines 124–131). |
| **Domain** | Financial Health (`Elsasa.Sis.Finance`) |
| **Aggregation** | Real-time daily status; Academic Period (`r.academic_period_id`). |
| **Dimensions** | Aging Bracket (`r.aging_bracket`), Charge Type (`r.charge_type`), Campus (`ap.campus`), Grade (`ap.grade`), Program (`ap.program`). |
| **Trend** | Monthly shift across aging brackets. |
| **Drill-down** | Receivables & Collection Monitor (`SCR-FIN-05`, `/finance/receivables`) filtered by selected `aging_bracket`. |
| **Availability** | **READY** |
| **Limitation** | None; `aging_bracket` is already calculated natively inside `finance.v_student_receivables`. |
| **Priority** | **High** (Release 1 — Finance Dashboard) |

#### FIN-05: Unverified Payment Confirmation Backlog
| Attribute | Description |
|---|---|
| **KPI Name** | Unverified Payment Confirmation Backlog |
| **Management Purpose** | Monitors operational responsiveness in the finance office by tracking submitted payments awaiting cashier/finance officer verification (`confirmation_status = 'submitted'`). Delayed confirmation blocks payment allocation and student enrollment activation. |
| **Definition** | Total count and IDR amount of payment records in `finance.payments` with `confirmation_status = 'submitted'`, plus average waiting time since `created_at`. |
| **Formula** | $$\text{Unverified Payment Count} = \text{COUNT}(p.\text{id} \mid p.\text{confirmation\_status} = \text{'submitted'})$$ $$\text{Unverified Payment Volume (IDR)} = \sum_{p.\text{confirmation\_status} = \text{'submitted'}} p.\text{payment\_amount}$$ |
| **Data Source** | `finance.payments` ($p$) (`20261104_0301_Finance_PaymentsReceivables_Tables.sql`). |
| **Domain** | Financial Health (`Elsasa.Sis.Finance`) |
| **Aggregation** | Real-time operational queue. |
| **Dimensions** | Payment Method (`bank_transfer`, `virtual_account`, `cash`, `card`, `e_wallet`, `cheque`, `other`), Campus (via `student_financial_accounts` $\rightarrow$ `administrative_placements.campus`). |
| **Trend** | Daily/Weekly backlog age. |
| **Drill-down** | Payments & Allocation Desk (`SCR-FIN-04`, `/finance/payments`) filtered to `confirmation_status = 'submitted'`. |
| **Availability** | **READY** |
| **Limitation** | None. |
| **Priority** | **High** (Release 1 — Finance Dashboard & Attention List) |

#### FIN-06: Confirmed Unallocated Payment Balance
| Attribute | Description |
|---|---|
| **KPI Name** | Confirmed Unallocated Payment Balance |
| **Management Purpose** | Enforces the domain rule that *"payment received $\neq$ obligation settled"* by surfacing confirmed payments that have been received in cash/bank (`confirmation_status = 'confirmed'`) but have not yet been allocated to settle student obligations. |
| **Definition** | Total IDR amount and count of confirmed payments (`finance.payments.confirmation_status = 'confirmed'`) where `payment_amount` exceeds the sum of active allocations (`finance.payment_allocations` where `allocation_status = 'active'`). |
| **Formula** | $$\text{Unallocated Balance (IDR)} = \sum_{p.\text{confirmation\_status} = \text{'confirmed'}} \left(p.\text{payment\_amount} - \text{COALESCE}\left(\sum_{pa.\text{allocation\_status} = \text{'active'}} pa.\text{allocated\_amount}, 0\right)\right)$$ |
| **Data Source** | `finance.payments` ($p$) LEFT JOIN `finance.payment_allocations` ($pa$) ON $p.\text{id} = pa.\text{payment\_id} \text{ AND } pa.\text{allocation\_status} = \text{'active'}$ (`20261104_0301_Finance_PaymentsReceivables_Tables.sql`). |
| **Domain** | Financial Health (`Elsasa.Sis.Finance`) |
| **Aggregation** | Real-time ledger state. |
| **Dimensions** | Payment Method (`p.payment_method`), Student Financial Account (`p.student_financial_account_id`), Campus (`ap.campus`). |
| **Trend** | Weekly/Monthly unallocated balance. |
| **Drill-down** | Payments & Allocation Desk (`SCR-FIN-04`, `/finance/payments`). |
| **Availability** | **READY** |
| **Limitation** | Overpayments that remain after all posted obligations are settled will appear here until Stage 2 Credit Balances (`finance.credit_balances`) is implemented. |
| **Priority** | **Medium** (Release 2 — Finance Dashboard & Attention List) |

#### FIN-07: Enrollment Gate Financial Clearance Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Enrollment Gate Financial Clearance Rate |
| **Management Purpose** | Tracks how many newly admitted students have cleared the mandatory admission/enrollment fee payment gate (`enrollment_gate_flag = 1` / `payment_gate_cleared = 1`) versus those blocked from enrollment activation due to unpaid intake obligations. |
| **Definition** | Percentage of enrollment records (`students.enrollments`) where `payment_gate_cleared = 1`, paired with the unpaid IDR balance on obligations marked `enrollment_gate_flag = 1`. |
| **Formula** | $$\text{Enrollment Financial Clearance Rate (\%)} = \frac{\text{COUNT}(e.\text{id} \mid e.\text{payment\_gate\_cleared} = 1)}{\text{COUNT}(e.\text{id} \mid e.\text{enrollment\_status} \neq \text{'cancelled'})} \times 100$$ |
| **Data Source** | `students.enrollments` ($e$) and `finance.financial_obligations` ($fo$) where `fo.enrollment_gate_flag = 1` (`20261103_0301_Students_EnrollmentIdentity_Tables.sql`, `20261104_0201_Finance_ObligationsBilling_Tables.sql`). |
| **Domain** | Financial Health (`Elsasa.Sis.Finance`) / Cross-domain with `Students` |
| **Aggregation** | Academic Year (`e.academic_year`), Academic Period (`fo.academic_period_id`). |
| **Dimensions** | Campus (`e.campus`), School Level (`e.school_level`), Grade (`e.grade`), Program (`e.program`). |
| **Trend** | Weekly during admission/enrollment intake windows. |
| **Drill-down** | Enrollment Activation Workbench (`SCR-STD-02`, `/students/enrollment`) and Obligations & Billing Center (`SCR-FIN-03`). |
| **Availability** | **READY** |
| **Limitation** | None; both `finance.financial_obligations.enrollment_gate_flag` and `students.enrollments.payment_gate_cleared` are implemented. |
| **Priority** | **Medium** (Release 2 — Finance & Student Dashboards) |

#### FIN-08: Scholarship, Discount & Fee Waiver Distribution
| Attribute | Description |
|---|---|
| **KPI Name** | Scholarship, Discount & Fee Waiver Distribution |
| **Management Purpose** | Monitors total fee reductions granted via scholarships, sibling/staff discounts, and financial aid waivers against gross tuition revenue. |
| **Definition** | Total IDR value of active scholarships, discounts, and waivers as a percentage of gross financial obligations, segmented by benefit type and grade/program. |
| **Formula** | $\text{Fee Reduction Rate (\%)} = \frac{\sum \text{Approved Benefit Reductions}}{\sum \text{Gross Original Obligations}} \times 100$. |
| **Data Source** | Planned Stage 2 tables: `finance.scholarships`, `finance.discounts`, `finance.waivers` (currently only referenced in `finance-domain.md` §3.2 and `billing_document_items.item_type = 'benefit_reduction'`). |
| **Domain** | Financial Health (`Elsasa.Sis.Finance`) |
| **Aggregation** | Academic Period, Academic Year. |
| **Dimensions** | Benefit Type (Scholarship, Discount, Waiver), Campus, Grade, Program. |
| **Trend** | Annual / Period-over-period. |
| **Drill-down** | Planned Benefits & Aid Management View (Stage 2). |
| **Availability** | **NOT AVAILABLE** |
| **Limitation** | Stage 2 sub-module (`sub-modul-and-development-phases.md` §2.5: `Scholarship`, `Discount`, `Waiver`, `FIN-WF-04`). In `finance.v_student_receivables` (line 99), `original_amount AS net_obligation` currently has no benefit deduction join because the tables do not exist yet. |
| **Priority** | **Medium** (Release 3 — Deferred until Stage 2 Finance Benefits is implemented) |

#### FIN-09: Payment Arrangement Delinquency Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Payment Arrangement Delinquency Rate |
| **Management Purpose** | Tracks compliance with negotiated installment plans (`PaymentArrangement`) for families experiencing financial difficulty. |
| **Definition** | Percentage of active negotiated payment arrangements that have missed one or more agreed installment dates (`defaulted` / `delinquent`). |
| **Formula** | $\text{Arrangement Delinquency Rate (\%)} = \frac{\text{Delinquent Arrangements}}{\text{Total Active Arrangements}} \times 100$. |
| **Data Source** | Planned Stage 2 `finance.payment_arrangements` table (`sub-modul-and-development-phases.md` §2.5, `FIN-WF-06`). Note: `finance.obligation_due_schedules` (`20261104_0201_Finance_ObligationsBilling_Tables.sql`) exists for initial obligation schedules, but the post-billing negotiated `PaymentArrangement` entity is not yet implemented. |
| **Domain** | Financial Health (`Elsasa.Sis.Finance`) |
| **Aggregation** | Monthly, Academic Period. |
| **Dimensions** | Campus, Grade, Program. |
| **Trend** | Monthly. |
| **Drill-down** | Planned Payment Arrangements Workbench. |
| **Availability** | **NOT AVAILABLE** |
| **Limitation** | Requires Stage 2 `PaymentArrangement` workflow (`FIN-WF-06`) and individual installment settlement tracking. |
| **Priority** | **Low** (Release 3 — Deferred until Stage 2 Finance Payment Arrangements is implemented) |

---

### D. Teaching Performance (`teaching` schema)

> [!NOTE]
> Per `teaching-domain.md` §1.4, Teaching Performance KPIs strictly measure **staffing coverage, authorization compliance, and workload distribution**—never equating workload volume or assignment count with teaching quality.

#### TCH-01: Teaching Demand Staffing Coverage Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Teaching Demand Staffing Coverage Rate |
| **Management Purpose** | Answers *"What percentage of planned instructional demand for the academic period has been staffed with an eligible, assigned teacher?"* |
| **Definition** | Percentage of non-cancelled `teaching.teaching_demands` records (and percentage of total required `load_units`) that have reached `status = 'staffed'` for the selected academic period. |
| **Formula** | $$\text{Demand Coverage Rate (\%)} = \frac{\text{COUNT}(td.\text{id} \mid td.\text{status} = \text{'staffed'})}{\text{COUNT}(td.\text{id} \mid td.\text{status} \neq \text{'cancelled'})} \times 100$$ $$\text{Load Unit Coverage Rate (\%)} = \frac{\sum_{td.\text{status} = \text{'staffed'}} td.\text{load\_units}}{\sum_{td.\text{status} \neq \text{'cancelled'}} td.\text{load\_units}} \times 100$$ |
| **Data Source** | `teaching.teaching_demands` ($td$) (`20261110_0502_Teaching_AssignmentWorkload_Tables.sql`). |
| **Domain** | Teaching Performance (`Elsasa.Sis.Teaching`) |
| **Aggregation** | Academic Period (`td.academic_period_id`). |
| **Dimensions** | Demand Status (`open`, `partially_staffed`, `staffed`, `cancelled`), Subject Code (`td.subject_code`), School Level (`td.school_level`), Grade (`td.grade_level`), Required Role (`td.required_role_id` $\rightarrow$ `teaching.assignment_roles.name`). |
| **Trend** | Daily/Weekly readiness progression leading up to semester start; period-over-period. |
| **Drill-down** | Teaching Demand & Assignment Board (`SCR-TCH-02`, `/teaching/assignments`). |
| **Availability** | **READY** |
| **Limitation** | `teaching.teaching_demands` does not have a dedicated `campus` column; campus filtering must infer campus from `cohort_section` or assigned teacher context unless `campus` is added to `teaching_demands`. |
| **Priority** | **High** (Release 1 — Executive Overview & Teaching Dashboard) |

#### TCH-02: Unassigned Open Teaching Demands
| Attribute | Description |
|---|---|
| **KPI Name** | Unassigned Open Teaching Demands |
| **Management Purpose** | Surfaces immediate instructional gaps—classes and learning units that still lack a primary teacher (`open` or `partially_staffed`). |
| **Definition** | Count of `teaching.teaching_demands` where `status IN ('open', 'partially_staffed')` and sum of unstaffed `load_units`. |
| **Formula** | $$\text{Unassigned Demand Count} = \text{COUNT}(td.\text{id} \mid td.\text{status} \in \{\text{'open'}, \text{'partially\_staffed'}\})$$ |
| **Data Source** | `teaching.teaching_demands` ($td$) LEFT JOIN `teaching.teaching_assignments` ($ta$) ON $td.\text{id} = ta.\text{teaching\_demand\_id} \text{ AND } ta.\text{assignment\_status} \in (\text{'approved'}, \text{'active'})$ (`20261110_0502_Teaching_AssignmentWorkload_Tables.sql`). |
| **Domain** | Teaching Performance (`Elsasa.Sis.Teaching`) |
| **Aggregation** | Real-time within Academic Period (`td.academic_period_id`). |
| **Dimensions** | Subject Code (`td.subject_code`), School Level (`td.school_level`), Grade (`td.grade_level`), Cohort Section (`td.cohort_section`). |
| **Trend** | Countdown burn-down chart prior to semester start. |
| **Drill-down** | Teaching Demand & Assignment Board (`SCR-TCH-02`, `/teaching/assignments`) filtered to `status IN ('open', 'partially_staffed')`. |
| **Availability** | **READY** |
| **Limitation** | Same campus column note as `TCH-01`. |
| **Priority** | **High** (Release 1 — Executive Overview, Teaching Dashboard & Attention List) |

#### TCH-03: Faculty Workload Distribution & Limit Breach Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Faculty Workload Distribution & Limit Breach Rate |
| **Management Purpose** | Monitors faculty workload balance against approved institutional `workload_policies` (`min_load_units`, `standard_load_units`, `warning_threshold_units`, `max_load_units`) to prevent teacher burnout or under-utilization. |
| **Definition** | Distribution of active faculty across the 5 authoritative `workload_status` states in `teaching.workload_commitments`: `under_allocated`, `normal`, `approaching_limit`, `overloaded_with_exception`, and `breached`. |
| **Formula** | $$\text{Workload Status Share}(s) = \frac{\text{COUNT}(wc.\text{teacher\_id} \mid wc.\text{workload\_status} = s)}{\text{COUNT}(wc.\text{teacher\_id})} \times 100$$ $$\text{Imbalance Rate (\%)} = \frac{\text{COUNT}(wc.\text{teacher\_id} \mid wc.\text{workload\_status} \in \{\text{'under\_allocated'}, \text{'overloaded\_with\_exception'}, \text{'breached'}\})}{\text{COUNT}(wc.\text{teacher\_id})} \times 100$$ |
| **Data Source** | `teaching.workload_commitments` ($wc$) JOIN `teaching.teacher_profiles` ($tp$) ON $wc.\text{teacher\_id} = tp.\text{id}$ JOIN `teaching.workload_policies` ($wp$) ON $wc.\text{workload\_policy\_id} = wp.\text{id}$ (`20261110_0502_Teaching_AssignmentWorkload_Tables.sql`; already rendered in `src/frontend/src/features/teaching/workload/components/WorkloadKpiCards.vue`). |
| **Domain** | Teaching Performance (`Elsasa.Sis.Teaching`) |
| **Aggregation** | Academic Period (`wc.academic_period_id`). |
| **Dimensions** | Workload Status (`under_allocated`, `normal`, `approaching_limit`, `overloaded_with_exception`, `breached`), Employment Type (`tp.employment_type`: `full_time`, `part_time`, `visiting`, `adjunct`), Teaching Role (`tp.primary_role_id`). |
| **Trend** | Period-over-period comparison. |
| **Drill-down** | Workload & Capacity Monitor (`SCR-TCH-03`, `/teaching/workload`). |
| **Availability** | **READY** |
| **Limitation** | Thresholds (`min_load_units`, `standard_load_units`, `warning_threshold_units`, `max_load_units`) are already governed by `teaching.workload_policies`—no hardcoded thresholds needed. |
| **Priority** | **High** (Release 1 — Executive Overview & Teaching Dashboard) |

#### TCH-04: Unresolved Teaching Capacity & Eligibility Conflicts
| Attribute | Description |
|---|---|
| **KPI Name** | Unresolved Teaching Capacity & Eligibility Conflicts |
| **Management Purpose** | Alerts academic and teaching coordinators to active assignments that violate teacher availability, exceed workload limits without an approved exception, or breach time windows. |
| **Definition** | Count of records in `teaching.capacity_conflicts` where `resolution_status = 'open'`, grouped by `conflict_type` and `severity`. |
| **Formula** | $\text{Open Capacity Conflicts} = \text{COUNT}(cc.\text{id} \mid cc.\text{resolution\_status} = \text{'open'})$. |
| **Data Source** | `teaching.capacity_conflicts` ($cc$) JOIN `teaching.teaching_demands` ($td$) ON $cc.\text{teaching\_demand\_id} = td.\text{id}$ (`20261110_0502_Teaching_AssignmentWorkload_Tables.sql`). |
| **Domain** | Teaching Performance (`Elsasa.Sis.Teaching`) |
| **Aggregation** | Real-time within Academic Period (`td.academic_period_id`). |
| **Dimensions** | Conflict Type (`availability_mismatch`, `workload_exceeded`, `schedule_overlap`, `unavailability_window_breach`), Severity (`warning`, `blocking`), School Level (`td.school_level`). |
| **Trend** | Weekly open conflict count. |
| **Drill-down** | Workload & Capacity Monitor (`SCR-TCH-03`, `/teaching/workload`) conflict resolution queue. |
| **Availability** | **READY** |
| **Limitation** | None. |
| **Priority** | **High** (Release 1 — Teaching Dashboard & Attention List) |

#### TCH-05: Teacher Qualification Verification & Authorization Coverage
| Attribute | Description |
|---|---|
| **KPI Name** | Teacher Qualification Verification & Authorization Coverage |
| **Management Purpose** | Ensures compliance with academic accreditation and school governance by tracking whether active teachers hold verified credentials and active teaching authorizations without unverified warnings or expired licenses. |
| **Definition** | (1) Percentage of active `teaching.teacher_profiles` with at least one `'active'` `teaching.teaching_authorizations` and `unverified_credential_warning = 0`. (2) Count of pending/expired `teaching.qualification_evidence` (`verification_status IN ('pending', 'expired')`) and active `teaching.supervision_requirements`. |
| **Formula** | $$\text{Verified Authorization Rate (\%)} = \frac{\text{COUNT}(\text{DISTINCT } tp.\text{id} \mid ta.\text{authorization\_status} = \text{'active'} \text{ AND } ta.\text{unverified\_credential\_warning} = 0)}{\text{COUNT}(\text{DISTINCT } tp.\text{id} \mid tp.\text{status} = \text{'active'})} \times 100$$ |
| **Data Source** | `teaching.teacher_profiles` ($tp$), `teaching.teaching_authorizations` ($ta$), `teaching.qualification_evidence` ($qe$), `teaching.supervision_requirements` ($sr$) (`20261110_0501_Teaching_TeacherProfile_Tables.sql`). |
| **Domain** | Teaching Performance (`Elsasa.Sis.Teaching`) |
| **Aggregation** | Current compliance state. |
| **Dimensions** | Authorization Status (`active`, `suspended`, `revoked`, `expired`), Verification Status (`pending`, `verified`, `rejected`, `expired`), School Level (`ta.school_level`), Subject (`ta.subject_code`), Employment Type (`tp.employment_type`). |
| **Trend** | Monthly/Period compliance trend. |
| **Drill-down** | Teacher Registry & Profile (`SCR-TCH-01`, `/teaching/teachers`). |
| **Availability** | **READY** |
| **Limitation** | None; all credential verification states, expiry dates (`qe.expiry_date`, `ta.effective_to`), and `unverified_credential_warning` flags exist in `20261110_0501_Teaching_TeacherProfile_Tables.sql`. |
| **Priority** | **Medium** (Release 2 — Teaching Dashboard & Attention List) |

#### TCH-06: Active Teacher Substitution & Reassignment Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Active Teacher Substitution & Reassignment Rate |
| **Management Purpose** | Monitors instructional continuity disruptions caused by temporary teacher absences (`teaching_substitutions`) or mid-period permanent teacher changes (`teaching_reassignments`). |
| **Definition** | Count of active/completed `teaching.teaching_substitutions` and `teaching.teaching_reassignments` in the selected academic period, and percentage of active `teaching_assignments` affected by a substitution or reassignment. |
| **Formula** | $$\text{Disrupted Assignment Rate (\%)} = \frac{\text{COUNT}(\text{DISTINCT } ta.\text{id with substitution or reassignment})}{\text{COUNT}(ta.\text{id} \mid ta.\text{assignment\_status} \in \{\text{'active'}, \text{'completed'}\})} \times 100$$ |
| **Data Source** | `teaching.teaching_substitutions` ($ts$), `teaching.teaching_reassignments` ($tr$), `teaching.teaching_assignments` ($ta$) (`20261110_0502_Teaching_AssignmentWorkload_Tables.sql`). |
| **Domain** | Teaching Performance (`Elsasa.Sis.Teaching`) |
| **Aggregation** | Monthly, Academic Period (`ta.academic_period_id`). |
| **Dimensions** | Subject Code (`td.subject_code`), School Level (`td.school_level`), Grade (`td.grade_level`), Substitution Status (`active`, `completed`, `cancelled`). |
| **Trend** | Monthly and period-over-period comparison. |
| **Drill-down** | Substitutions, Reassignments & Closeout (`SCR-TCH-04`, `/teaching/continuity`). |
| **Availability** | **READY** |
| **Limitation** | None. |
| **Priority** | **Medium** (Release 2 — Teaching Dashboard) |

#### TCH-07: Teaching Assignment Completion & Handover Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Teaching Assignment Completion & Handover Rate |
| **Management Purpose** | Verifies that at the close of an academic period, all teaching assignments have formally completed instructional handover and closeout (`assignment_completions`). |
| **Definition** | Percentage of active/completed `teaching.teaching_assignments` in the period that have a recorded `teaching.assignment_completions` entry with `completion_status = 'completed'`. |
| **Formula** | $$\text{Assignment Closeout Rate (\%)} = \frac{\text{COUNT}(ac.\text{id} \mid ac.\text{completion\_status} = \text{'completed'})}{\text{COUNT}(ta.\text{id} \mid ta.\text{assignment\_status} \in \{\text{'active'}, \text{'completed'}\})} \times 100$$ |
| **Data Source** | `teaching.teaching_assignments` ($ta$) LEFT JOIN `teaching.assignment_completions` ($ac$) ON $ta.\text{id} = ac.\text{teaching\_assignment\_id}$ (`20261110_0502_Teaching_AssignmentWorkload_Tables.sql`). |
| **Domain** | Teaching Performance (`Elsasa.Sis.Teaching`) |
| **Aggregation** | End of Academic Period (`ta.academic_period_id`). |
| **Dimensions** | Completion Status (`completed`, `partially_completed`, `terminated_early`), School Level (`td.school_level`), Subject (`td.subject_code`). |
| **Trend** | Period-over-period closeout compliance. |
| **Drill-down** | Substitutions, Reassignments & Closeout (`SCR-TCH-04`, `/teaching/continuity`). |
| **Availability** | **READY** |
| **Limitation** | Primarily an end-of-period closeout metric. |
| **Priority** | **Low** (Release 2 — Teaching Dashboard) |

---

### E. Student Development (`development` domain — Stage 3)

> [!CAUTION]
> **Confidentiality & Implementation Status**:
> 1. **Implementation Status**: Per `sub-modul-and-development-phases.md` §3.2, the entire **Student Development (`Development`)** domain is scheduled for **Stage 3**. No `development` SQL schema or `Elsasa.Sis.Development` backend project exists in the repository today. All KPIs in this domain are classified as **NOT AVAILABLE** for immediate implementation and are designed for Release 3.
> 2. **Strict Privacy Boundary**: Per `development-domain.md` §1.4 and §9.5, general management dashboards must **never** expose counselling notes, wellbeing referrals (`WellbeingSupportCase`), confidential discipline narratives, or individual pastoral case identities. Drill-down to individual records must be restricted to authorized `Counselor` / `Student Affairs Coordinator` roles (`development.cases.read_confidential`).

#### DEV-01: Behaviour Incident Frequency & Pending Triage Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Behaviour Incident Frequency & Pending Triage Rate |
| **Management Purpose** | Monitors school climate trends across grades and campuses and ensures reported negative behaviour observations are triaged promptly. |
| **Definition** | Aggregate count of negative `BehaviourObservation` records and `DisciplineCase` records per 100 active students in the period, plus percentage of observations pending coordinator triage (`DEV-WF-02`). |
| **Formula** | $$\text{Incident Rate per 100 Students} = \frac{\text{COUNT}(\text{Negative Behaviour Observations})}{\text{Active Students}} \times 100$$ |
| **Data Source** | Planned Stage 3 `development.behaviour_observations` and `development.discipline_cases` (`development-domain.md` §3.1). |
| **Domain** | Student Development (`Development`) |
| **Aggregation** | Weekly, Monthly, Academic Period. |
| **Dimensions** | Campus, School Level, Grade, Severity Tier (Minor, Moderate, Major — non-confidential aggregate only). |
| **Trend** | Monthly and period-over-period comparison. |
| **Drill-down** | Aggregate breakdown by grade/campus for general leadership; individual case list restricted to `Student Affairs Coordinator`. |
| **Availability** | **NOT AVAILABLE** |
| **Limitation** | Entire `Development` domain is unimplemented (Stage 3). |
| **Priority** | **Medium** (Release 3 — Deferred until Stage 3 Development module is built) |

#### DEV-02: Positive Behaviour Recognition Ratio
| Attribute | Description |
|---|---|
| **KPI Name** | Positive Behaviour Recognition Ratio |
| **Management Purpose** | Tracks whether the school culture balances corrective discipline with positive character recognition (`PositiveBehaviourRecognition`, `DEV-WF-03`). |
| **Definition** | Ratio of recorded `PositiveBehaviourRecognition` entries to negative `BehaviourObservation` entries across the school or grade level. |
| **Formula** | $\text{Positive-to-Negative Ratio} = \frac{\text{COUNT}(\text{Positive Recognitions})}{\text{ MAX}(\text{COUNT}(\text{Negative Observations}), 1)}$. |
| **Data Source** | Planned Stage 3 `development.positive_recognitions` and `development.behaviour_observations` (`development-domain.md` §3.1). |
| **Domain** | Student Development (`Development`) |
| **Aggregation** | Monthly, Academic Period. |
| **Dimensions** | Campus, School Level, Grade. |
| **Trend** | Monthly and period-over-period. |
| **Drill-down** | Aggregate summary by grade/homeroom. |
| **Availability** | **NOT AVAILABLE** |
| **Limitation** | Unimplemented Stage 3 module. |
| **Priority** | **Low** (Release 3) |

#### DEV-03: Students with Active Development Interventions (Aggregated)
| Attribute | Description |
|---|---|
| **KPI Name** | Students with Active Development Interventions (Aggregated) |
| **Management Purpose** | Gives leadership an anonymized, aggregate count of students receiving structured behavioural or pastoral intervention (`DevelopmentIntervention`, `DEV-WF-06`) and tracks follow-up review completion without exposing sensitive case details. |
| **Definition** | Anonymized count of active `DevelopmentIntervention` plans (`status IN ('active', 'monitoring')`) and percentage of interventions with overdue review dates. **Excludes confidential counselling notes and wellbeing referral identities.** |
| **Formula** | $$\text{Active Development Interventions} = \text{COUNT}(di.\text{id} \mid di.\text{status} \in \{\text{'active'}, \text{'monitoring'}\})$$ $$\text{On-Time Review Rate (\%)} = \frac{\text{COUNT}(di.\text{id} \mid di.\text{next\_review\_date} \ge \text{Today})}{\text{Active Development Interventions}} \times 100$$ |
| **Data Source** | Planned Stage 3 `development.development_interventions` (`development-domain.md` §3.2). |
| **Domain** | Student Development (`Development`) |
| **Aggregation** | Academic Period, Monthly. |
| **Dimensions** | School Level, Grade, Campus, Intervention Status (Aggregate counts only; suppress cell counts $< 5$ to prevent re-identification). |
| **Trend** | Period-over-period aggregate trend. |
| **Drill-down** | **Disabled for general management roles**; only users with pastoral/student-affairs clearance can drill down to non-confidential intervention records. |
| **Availability** | **NOT AVAILABLE** |
| **Limitation** | Unimplemented Stage 3 module. |
| **Priority** | **Medium** (Release 3) |

#### DEV-04: Extracurricular Participation Coverage
| Attribute | Description |
|---|---|
| **KPI Name** | Extracurricular Participation Coverage |
| **Management Purpose** | Measures holistic student engagement outside classroom academics (`StudentActivityParticipation`, `DEV-WF-08`). |
| **Definition** | Percentage of active students enrolled in at least one active extracurricular, club, or leadership activity during the academic period. |
| **Formula** | $\text{Extracurricular Coverage (\%)} = \frac{\text{COUNT}(\text{DISTINCT Active Students with } \ge 1 \text{ Activity Participation})}{\text{Total Active Students}} \times 100$. |
| **Data Source** | Planned Stage 3 `development.student_activity_participations` (`development-domain.md` §3.3). |
| **Domain** | Student Development (`Development`) |
| **Aggregation** | Academic Period. |
| **Dimensions** | Activity Category (Sports, Arts, STEM, Leadership, Community Service), Campus, School Level, Grade. |
| **Trend** | Period-over-period comparison. |
| **Drill-down** | Activity participation summary by club/grade. |
| **Availability** | **NOT AVAILABLE** |
| **Limitation** | Unimplemented Stage 3 module. |
| **Priority** | **Low** (Release 3) |

#### DEV-05: Non-Academic Achievement Distribution
| Attribute | Description |
|---|---|
| **KPI Name** | Non-Academic Achievement Distribution |
| **Management Purpose** | Tracks verified student accomplishments in external competitions, leadership, arts, athletics, and community service (`NonAcademicAchievement`, `DEV-WF-09`). |
| **Definition** | Count of verified non-academic achievements recorded in the academic period, segmented by competition level (School, Regional, National, International) and category. |
| **Formula** | $\text{Verified Non-Academic Achievements} = \text{COUNT}(naa.\text{id} \mid naa.\text{verification\_status} = \text{'verified'})$. |
| **Data Source** | Planned Stage 3 `development.non_academic_achievements` (`development-domain.md` §3.3). *(Note: `academics.academic_achievements` in `20261107_0401_Academics_StandingProgression_Tables.sql` already exists for **academic** awards and honours).* |
| **Domain** | Student Development (`Development`) |
| **Aggregation** | Academic Period, Academic Year. |
| **Dimensions** | Achievement Category, Competition Level, Campus, Grade. |
| **Trend** | Period-over-period and annual. |
| **Drill-down** | Achievement registry list. |
| **Availability** | **NOT AVAILABLE** |
| **Limitation** | Unimplemented Stage 3 module for non-academic achievements (though academic achievements in `academics.academic_achievements` are **READY**). |
| **Priority** | **Low** (Release 3) |

---

### F. School Operations (`operations` schema)

#### OPS-01: Operational Calendar & Period Publication Readiness
| Attribute | Description |
|---|---|
| **KPI Name** | Operational Calendar & Period Publication Readiness |
| **Management Purpose** | Ensures that the foundational institutional time structures (`academic_years`, `operational_periods`, `school_calendars`) required by Admissions, Finance, Academics, and Teaching are activated and published on time. |
| **Definition** | (1) Percentage of defined `operations.operational_periods` for the target academic year that are `'approved'` or `'active'` (vs `'draft'`). (2) Percentage of campus/level `operations.school_calendars` for the academic year that are `'published'` (vs `'draft'` or `'approved'`), along with count of scheduled `'instructional_day'` vs `'assessment_window'` and `'holiday'` entries. |
| **Formula** | $$\text{Calendar Publication Rate (\%)} = \frac{\text{COUNT}(sc.\text{id} \mid sc.\text{status} = \text{'published'})}{\text{COUNT}(sc.\text{id})} \times 100$$ $$\text{Period Readiness Rate (\%)} = \frac{\text{COUNT}(op.\text{id} \mid op.\text{status} \in \{\text{'approved'}, \text{'active'}, \text{'closed'}\})}{\text{COUNT}(op.\text{id})} \times 100$$ |
| **Data Source** | `operations.academic_years` ($ay$), `operations.operational_periods` ($op$), `operations.school_calendars` ($sc$), `operations.calendar_entries` ($ce$) (`20261101_0101_Operations_AcademicYears.sql`, `20261102_0101_Operations_CalendarPeriods_Tables.sql`). |
| **Domain** | School Operations (`Elsasa.Sis.Operations`) |
| **Aggregation** | Academic Year (`ay.id`). |
| **Dimensions** | Period Type (`semester`, `term`, `quarter`, `admission_period`, `grading_period`, `financial_period`), Campus Context (`op.campus_context`, `sc.campus_context`), School Level (`sc.school_level_context`), Entry Type (`instructional_day`, `holiday`, `school_closure`, `assessment_window`, `enrollment_milestone`, `reporting_deadline`). |
| **Trend** | Annual readiness comparison. |
| **Drill-down** | Academic Years & Periods Setup (`SCR-OPS-01`, `/operations/academic-years`) and School Calendar Studio (`SCR-OPS-02`, `/operations/calendars`). |
| **Availability** | **READY** |
| **Limitation** | None; all Stage 1 Operations calendar and period tables are fully implemented. |
| **Priority** | **High** (Release 1 — Operations Dashboard & Attention List) |

#### OPS-02: Unscheduled School Closure Days & Unreviewed Amendments
| Attribute | Description |
|---|---|
| **KPI Name** | Unscheduled School Closure Days & Unreviewed Amendments |
| **Management Purpose** | Tracks instructional day loss due to emergency or unscheduled school closures (`operations.school_closures`) and flags calendar amendments (`operations.calendar_amendments`) whose downstream impact on Academics/Finance/Teaching has not yet been reviewed (`downstream_impact_reviewed = 0`). |
| **Definition** | (1) Total days impacted by active `operations.school_closures` (`closure_type IN ('unscheduled', 'emergency')`). (2) Count of `operations.calendar_amendments` where `downstream_impact_reviewed = 0`. |
| **Formula** | $$\text{Unscheduled Closure Days} = \sum_{cl.\text{status} = \text{'active'}, \, cl.\text{closure\_type} \in \{\text{'unscheduled'}, \text{'emergency'}\}} \left(\text{DATEDIFF}(\text{day}, cl.\text{start\_date}, cl.\text{end\_date}) + 1\right)$$ $$\text{Unreviewed Calendar Amendments} = \text{COUNT}(ca.\text{id} \mid ca.\text{downstream\_impact\_reviewed} = 0)$$ |
| **Data Source** | `operations.school_closures` ($cl$) and `operations.calendar_amendments` ($ca$) (`20261102_0101_Operations_CalendarPeriods_Tables.sql`). |
| **Domain** | School Operations (`Elsasa.Sis.Operations`) |
| **Aggregation** | Academic Year (`sc.academic_year_id`), Academic Period (`cl.operational_period_id`). |
| **Dimensions** | Closure Type (`planned`, `unscheduled`, `emergency`), Affected Scope (`cl.affected_scope`), Campus Context (`sc.campus_context`). |
| **Trend** | Period-over-period and annual comparison. |
| **Drill-down** | School Calendar Studio (`SCR-OPS-02`, `/operations/calendars`). |
| **Availability** | **READY** |
| **Limitation** | None; `closure_type` and `downstream_impact_reviewed` are explicit columns in `20261102_0101_Operations_CalendarPeriods_Tables.sql`. |
| **Priority** | **Medium** (Release 1 — Operations Dashboard & Attention List) |

#### OPS-03: Master Schedule Completion & Publication Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Master Schedule Completion & Publication Rate |
| **Management Purpose** | Tracks whether approved teaching assignments and operational activity requests have been scheduled into conflict-free time slots and published for the semester. |
| **Definition** | Percentage of approved `ScheduledOccurrence` / teaching demand sessions that have been assigned a time slot and room and published. |
| **Formula** | $\text{Schedule Publication Rate (\%)} = \frac{\text{Published Scheduled Occurrences}}{\text{Total Required Scheduled Occurrences}} \times 100$. |
| **Data Source** | Planned Stage 2 tables: `operations.scheduled_occurrences`, `operations.operational_activity_requests` (`operational-domain.md` §3.2, `sub-modul-and-development-phases.md` §2.6). |
| **Domain** | School Operations (`Elsasa.Sis.Operations`) |
| **Aggregation** | Academic Period. |
| **Dimensions** | Campus, School Level, Grade, Room. |
| **Trend** | Pre-semester readiness curve. |
| **Drill-down** | Planned Master Schedule Workbench (Stage 2). |
| **Availability** | **NOT AVAILABLE** |
| **Limitation** | Stage 2 sub-module (`Scheduling & Rooms` in `sub-modul-and-development-phases.md` §2.6) is not yet implemented in `Elsasa.Sis.Database` or `Elsasa.Sis.Operations`. |
| **Priority** | **Medium** (Release 3 — Deferred until Stage 2 Scheduling & Rooms is implemented) |

#### OPS-04: Room Utilization & Allocation Conflict Rate
| Attribute | Description |
|---|---|
| **KPI Name** | Room Utilization & Allocation Conflict Rate |
| **Management Purpose** | Evaluates physical classroom/lab capacity utilization across campuses and surfaces unresolved room double-bookings (`OperationalConflict`). |
| **Definition** | (1) Ratio of booked room hours (`RoomAllocation`) to total available instructional room hours (`RoomAvailability`). (2) Count of open `OperationalConflict` records (`OPS-WF-09`). |
| **Formula** | $\text{Room Utilization (\%)} = \frac{\sum \text{Allocated Room Hours}}{\sum \text{Available Instructional Room Hours}} \times 100$. |
| **Data Source** | Planned Stage 2 tables: `operations.rooms`, `operations.room_availabilities`, `operations.room_allocations`, `operations.operational_conflicts` (`operational-domain.md` §3.2). |
| **Domain** | School Operations (`Elsasa.Sis.Operations`) |
| **Aggregation** | Weekly, Academic Period. |
| **Dimensions** | Campus, Room Type (Classroom, Laboratory, Auditorium, Sports Facility), Day of Week, Time Slot. |
| **Trend** | Period-over-period utilization. |
| **Drill-down** | Planned Room Allocation & Conflict Monitor (Stage 2). |
| **Availability** | **NOT AVAILABLE** |
| **Limitation** | Stage 2 `Scheduling & Rooms` tables do not exist yet. |
| **Priority** | **Medium** (Release 3 — Deferred until Stage 2 Scheduling & Rooms is implemented) |

#### OPS-05: Unresolved Operational Activity Requests
| Attribute | Description |
|---|---|
| **KPI Name** | Unresolved Operational Activity Requests |
| **Management Purpose** | Tracks pending room/slot booking requests from Academic, Teaching, and Student Development coordinators (`OperationalActivityRequest`, `OPS-WF-07`). |
| **Definition** | Count of `OperationalActivityRequest` records in `'submitted'` or `'pending_review'` status and average age in days. |
| **Formula** | $\text{Pending Requests} = \text{COUNT}(\text{requests where status} = \text{'pending'})$. |
| **Data Source** | Planned Stage 2 `operations.operational_activity_requests` (`operational-domain.md` §3.2). |
| **Domain** | School Operations (`Elsasa.Sis.Operations`) |
| **Aggregation** | Daily / Real-time queue. |
| **Dimensions** | Requesting Domain, Campus, Facility Type. |
| **Trend** | Weekly backlog trend. |
| **Drill-down** | Planned Operational Activity Requests Queue. |
| **Availability** | **NOT AVAILABLE** |
| **Limitation** | Stage 2 `Scheduling & Rooms` is not yet implemented. |
| **Priority** | **Low** (Release 3) |

#### OPS-06: School Event Completion & Announcement Reach
| Attribute | Description |
|---|---|
| **KPI Name** | School Event Completion & Announcement Reach |
| **Management Purpose** | Monitors execution of scheduled school-wide events (`SchoolEvent`) and publication of operational notices (`OperationalAnnouncement`). |
| **Definition** | Percentage of scheduled `SchoolEvent` records completed without cancellation and count of active published `OperationalAnnouncement` records. |
| **Formula** | $\text{Event Completion Rate (\%)} = \frac{\text{Completed School Events}}{\text{Total Planned School Events}} \times 100$. |
| **Data Source** | Planned Stage 3 `operations.school_events` and `operations.operational_announcements` (`sub-modul-and-development-phases.md` §3.5). |
| **Domain** | School Operations (`Elsasa.Sis.Operations`) |
| **Aggregation** | Monthly, Academic Period. |
| **Dimensions** | Campus, School Level, Event Type. |
| **Trend** | Period-over-period. |
| **Drill-down** | Planned Events & Announcements Console (Stage 3). |
| **Availability** | **NOT AVAILABLE** |
| **Limitation** | Stage 3 `Events & Announcements` sub-module is not yet implemented. |
| **Priority** | **Low** (Release 3) |

---

## 4. Prioritization Rationale & Avoidance of Vanity Metrics

To keep the dashboard high-signal and actionable for school leadership:
1. **No Raw Transaction Volume Vanity Metrics**: We exclude metrics like *"Total Attendance Rows Logged"*, *"Total Invoices Generated"*, or *"Total Gradebook Entries"* because raw row counts grow naturally with school size without indicating whether the school is healthy or deteriorating. Every KPI is expressed as a **rate/percentage**, **unsettled exposure (IDR)**, or **exception/backlog count**.
2. **Asymmetric Domain Density by Design**: Because **Student Management**, **Academic Performance**, **Financial Health**, and **Teaching Performance** have rich Stage 1 and Stage 2 schemas already live in SQL Server, they carry the core Release 1 KPIs. Conversely, **Student Development** (0% implemented, Stage 3) and **School Operations** (only Stage 1 Calendar & Periods implemented) have only the KPIs their current data genuinely supports, avoiding artificial filler indicators.
3. **Policy Decisions Explicitly Separated from Data**:
   - **Already Governed in DB**: Workload thresholds (`teaching.workload_policies`), assessment passing scores (`academics.assessments.passing_score`), and obligation due dates (`finance.financial_obligations.due_date`).
   - **Requires Management Policy Configuration**: Target enrollment quotas per grade/campus (`STD-02`), maximum admission review SLA days (`STD-04`), chronic absence threshold (`ACA-02`/`ACA-03`), target collection rate milestones (`FIN-02`), and tolerable overdue ratio (`FIN-03`).
