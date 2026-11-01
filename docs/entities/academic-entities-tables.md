# Academic Domain — Database Tables by Submodule

## Source Documents

- [academic-domain.md](file:///d:/Project.Private/elsasa-sis-v3/domains/academic-domain.md) — Academic Domain definition
- [academic-entities.md](file:///d:/Project.Private/elsasa-sis-v3/entities/academic-entities.md) — Entity identification by submodule
- [sub-modul-and-development-phases.md](file:///d:/Project.Private/elsasa-sis-v3/sub-modul-and-development-phases.md) — Module decomposition and roadmap

## Conventions

- **PK** — Primary Key
- **FK** — Foreign Key (logical; referencing another table in this domain or an external domain)
- **UQ** — Unique constraint
- All tables include `created_at`, `updated_at`, and `created_by` audit columns (omitted from key-column listings for brevity)
- UUID is used as the primary key type throughout
- Columns marked *(nullable)* are not required at creation time, supporting progressive information collection
- External domain references (Students, Teaching, Operations) are stored as opaque identifiers; this domain does not own or define those tables
- Entity status values and type enumerations are stored as VARCHAR with application-level validation, consistent with the Student domain convention

---

## Submodule 1 — Learning Structure & Requirements

### 1.1 `learning_structures` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | The top-level organizational container describing the learning recognized by the school. Groups Learning Units into a coherent curriculum framework for a given academic context (program, level, or school-wide curriculum). |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `name` | VARCHAR | NOT NULL | Human-readable name (e.g., "Kurikulum Merdeka SD 2024") |
| `description` | TEXT | nullable | Description of the learning structure |
| `academic_period_id` | UUID | FK → external (Operations), NOT NULL | Academic period this structure is effective within |
| `academic_period_label` | VARCHAR | NOT NULL | Human-readable period label (e.g., "TP 2024/2025") |
| `version` | VARCHAR | NOT NULL | Version identifier (e.g., "2024.1") |
| `effective_date` | DATE | NOT NULL | Date this structure becomes effective |
| `end_date` | DATE | nullable | Date this structure ceases to be effective (null = current) |
| `status` | VARCHAR | NOT NULL | Status: `draft`, `active`, `superseded`, `archived` |
| `approved_by` | VARCHAR | nullable | Person who approved this structure |
| `approved_date` | DATE | nullable | Date approved |
| `supersedes_id` | UUID | FK → `learning_structures.id`, nullable | Previous version this supersedes |
| `notes` | TEXT | nullable | Internal notes |

**Example:**

| name | version | effective_date | status | approved_by |
|---|---|---|---|---|
| Kurikulum Merdeka SD 2024 | 2024.1 | 2024-07-01 | active | Kepala Sekolah |

> [!NOTE]
> Versioning is handled directly on this table via `version`, `supersedes_id`, and the `effective_date`/`end_date` pair. This eliminates the need for a separate `curriculum_versions` table. The entity document lists Curriculum Version as a supporting entity with an implementation note that it may be modeled as attributes — this table follows that pragmatic approach.

---

### 1.2 `learning_units` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | The single curriculum-neutral abstraction for a defined body of learning. Represents a subject, course, module, PACE, project, or any class-based learning element. The central building block of all academic workflows. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `learning_structure_id` | UUID | FK → `learning_structures.id`, NOT NULL | Parent learning structure |
| `parent_unit_id` | UUID | FK → `learning_units.id`, nullable | Parent learning unit for hierarchical composition (e.g., course → modules) |
| `code` | VARCHAR | UQ (within structure), NOT NULL | Unit code (e.g., "MAT-4-S1") |
| `name` | VARCHAR | NOT NULL | Human-readable name (e.g., "Matematika Kelas 4 Semester 1") |
| `unit_type` | VARCHAR | NOT NULL | Curriculum expression: `subject`, `course`, `module`, `pace`, `project`, `competency`, `unit`, `other` |
| `level` | VARCHAR | nullable | Grade or level context (e.g., "Grade 4") |
| `description` | TEXT | nullable | Description of what this unit covers |
| `credit_weight` | DECIMAL | nullable | Credit or weight as defined by school policy |
| `status` | VARCHAR | NOT NULL | Status: `draft`, `active`, `archived` |
| `notes` | TEXT | nullable | Internal notes |

**Example:**

| code | name | unit_type | level | learning_structure_id | credit_weight | status |
|---|---|---|---|---|---|---|
| MAT-4-S1 | Matematika Kelas 4 Semester 1 | subject | Grade 4 | `ls-merdeka-sd-2024...` | 5 | active |
| BI-4-S1 | Bahasa Indonesia Kelas 4 Semester 1 | subject | Grade 4 | `ls-merdeka-sd-2024...` | 5 | active |
| MAT-4-S1-M1 | Pecahan Biasa | module | Grade 4 | `ls-merdeka-sd-2024...` | *(null)* | active |

> [!IMPORTANT]
> Subject, course, module, PACE, project, and fine-grained competencies are **not** separate tables — they are `unit_type` values on this single table. The domain explicitly prohibits introducing separate root concepts for curriculum variants. Hierarchical composition is supported through `parent_unit_id` (e.g., `unit_type = 'competency'` child under a parent subject, per Resolved Question 1).

---

### 1.3 `academic_requirements` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Rules describing the learning, evidence, completion, credit, or mastery expected for a program, stage, or level. Governs completion, progression, and graduation criteria. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `name` | VARCHAR | NOT NULL | Human-readable name (e.g., "Grade 4 Promotion Requirements — TP 2026/2027") |
| `requirement_scope` | VARCHAR | NOT NULL | Scope: `program`, `level`, `learning_unit` |
| `learning_structure_id` | UUID | FK → `learning_structures.id`, nullable | Applicable learning structure |
| `learning_unit_id` | UUID | FK → `learning_units.id`, nullable | Applicable learning unit (when scope is `learning_unit`) |
| `level` | VARCHAR | nullable | Applicable grade/level (when scope is `level`) |
| `requirement_type` | VARCHAR | NOT NULL | Type: `completion`, `prerequisite`, `credit`, `progression`, `graduation`, `assessment_policy` |
| `description` | TEXT | NOT NULL | Human-readable description of what must be satisfied |
| `version` | VARCHAR | NOT NULL | Version identifier |
| `effective_date` | DATE | NOT NULL | Date this requirement becomes effective |
| `end_date` | DATE | nullable | Date this requirement ceases to be effective (null = current) |
| `status` | VARCHAR | NOT NULL | Status: `draft`, `active`, `superseded`, `archived` |
| `approved_by` | VARCHAR | nullable | Person who approved |
| `approved_date` | DATE | nullable | Date approved |
| `supersedes_id` | UUID | FK → `academic_requirements.id`, nullable | Previous version this supersedes |
| `notes` | TEXT | nullable | Notes |

**Example:**

| name | requirement_scope | requirement_type | version | effective_date | status |
|---|---|---|---|---|---|
| Grade 4 Promotion Requirements — TP 2026/2027 | level | progression | 2026.1 | 2026-07-01 | active |
| Matematika Completion Criteria | learning_unit | completion | 2026.1 | 2026-07-01 | active |
| SD Graduation Requirements v2032.1 | program | graduation | 2032.1 | 2032-01-01 | active |

> [!NOTE]
> Versioning is handled inline through `version`, `supersedes_id`, and effective dating. This eliminates the separate Curriculum Version entity. Requirements and Learning Structures both carry their own version tracking, enabling independent versioning as the domain requires.

---

### Learning Structure & Requirements — Table Relationships

```mermaid
erDiagram
    learning_structures ||--o{ learning_units : "contains"
    learning_units ||--o{ learning_units : "parent-child composition"
    learning_structures ||--o{ academic_requirements : "applies to"
    learning_units ||--o{ academic_requirements : "applies to"
    learning_structures }o--|| ACADEMIC_PERIOD_EXT : "effective within (external)"
    learning_structures ||--o| learning_structures : "supersedes"
    academic_requirements ||--o| academic_requirements : "supersedes"
```

---

## Submodule 2 — Participation & Learning Activities

### 2.1 `academic_participations` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Records the relationship between a student and a specific Learning Unit — that the student is undertaking, has paused, has withdrawn from, or has completed that learning. Answers "What learning is this student undertaking?" |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student |
| `learning_unit_id` | UUID | FK → `learning_units.id`, NOT NULL | The learning unit being undertaken |
| `academic_period_id` | UUID | FK → external (Operations), NOT NULL | Academic period |
| `academic_period_label` | VARCHAR | NOT NULL | Human-readable period label |
| `participation_type` | VARCHAR | NOT NULL, default 'standard' | Type: `standard`, `repeat`, `transferred_in`, `recognized_prior` |
| `attempt_number` | INTEGER | NOT NULL, default 1 | Attempt counter for repeated learning (increments on retakes) |
| `source` | VARCHAR | NOT NULL, default 'assigned' | Origin: `assigned`, `student_selected`, `learning_plan` |
| `section_label` | VARCHAR | nullable | Human-readable section or cohort partition (e.g., "4A") |
| `status` | VARCHAR | NOT NULL | Lifecycle: `in_progress`, `paused`, `withdrawn`, `completed` |
| `start_date` | DATE | NOT NULL | Date participation began |
| `end_date` | DATE | nullable | Date participation ended (null = ongoing) |
| `completion_date` | DATE | nullable | Date learning was completed (null if not completed) |
| `source_institution` | VARCHAR | nullable | Originating institution for transferred/prior recognized learning |
| `external_grade_or_credit` | VARCHAR | nullable | External grade, score, or credits recognized from source institution |
| `recognition_basis` | TEXT | nullable | Documented basis or rationale for credit recognition |
| `approved_by` | VARCHAR | nullable | Academic Authority who approved transfer or prior credit recognition |
| `student_learning_plan_id` | UUID | FK → `student_learning_plans.id`, nullable | SLP governing this participation, if any |
| `notes` | TEXT | nullable | Notes |

**Example:**

| student_id | learning_unit_id | academic_period_label | participation_type | attempt_number | source | status | source_institution |
|---|---|---|---|---|---|---|---|
| `std-108...` | `lu-mat-4-s1...` | Semester 1 TP 2029/2030 | standard | 1 | assigned | in_progress | *(null)* |
| `std-205...` | `lu-mat-3-s1...` | Semester 1 TP 2029/2030 | repeat | 2 | learning_plan | in_progress | *(null)* |
| `std-108...` | `lu-ipa-transfer...` | Semester 1 TP 2029/2030 | transferred_in | 1 | assigned | completed | SD Harapan Bangsa |

> [!IMPORTANT]
> Administrative enrollment does **not** automatically create an `academic_participations` row. Participation must be established through Academics workflows. A student may be enrolled in Students but have no active participation records here.
> 
> Per Resolved Questions:
> - **Selection vs. Assignment (Q4):** Captured via `source` (`assigned`, `student_selected`, `learning_plan`).
> - **Repeated & Paused Learning (Q5):** Repeated learning creates a new record with `participation_type = 'repeat'` and incremented `attempt_number`. Paused learning uses `status = 'paused'`, returning to `'in_progress'` upon resumption.
> - **Cohort / Class Grouping (Q6):** Optional `section_label` partitions cohorts without requiring a standalone class table.
> - **Transferred & Prior Learning (Q14):** Recorded with `participation_type = 'transferred_in'` or `'recognized_prior'`, immediately completed with `source_institution`, `external_grade_or_credit`, `recognition_basis`, and `approved_by`. No synthetic activities or attendance rows are generated.

---

### 2.2 `learning_activities` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Represents an occurrence in which learning is undertaken, supported, or demonstrated. The container for actual learning work: attendance, assignments, and assessment evidence trace back here. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `learning_unit_id` | UUID | FK → `learning_units.id`, NOT NULL | The learning unit this activity belongs to |
| `activity_type` | VARCHAR | NOT NULL | Type: `lesson`, `tutorial`, `laboratory`, `independent_work`, `project`, `discussion`, `practice`, `examination`, `remediation`, `homeroom_session`, `other` |
| `title` | VARCHAR | NOT NULL | Human-readable title (e.g., "Lesson — Pecahan Biasa") |
| `description` | TEXT | nullable | Description of the activity |
| `section_label` | VARCHAR | nullable | Human-readable section or cohort partition (e.g., "4A") |
| `activity_date` | DATE | NOT NULL | Date the activity occurs or occurred |
| `start_time` | TIME | nullable | Start time (if applicable) |
| `end_time` | TIME | nullable | End time (if applicable) |
| `teacher_assignment_id` | UUID | FK → external (Teaching), nullable | Reference to teacher assignment from Teaching domain |
| `status` | VARCHAR | NOT NULL | Status: `planned`, `conducted`, `cancelled` |
| `is_attendance_required` | BOOLEAN | NOT NULL, default true | Whether attendance must be recorded for this activity |
| `notes` | TEXT | nullable | Notes |

**Example:**

| learning_unit_id | activity_type | title | section_label | activity_date | status | is_attendance_required |
|---|---|---|---|---|---|---|
| `lu-mat-4-s1...` | lesson | Lesson — Pecahan Biasa | 4A | 2029-08-12 | conducted | true |
| `lu-mat-4-s1...` | examination | Ulangan Harian Pecahan | 4A | 2029-09-10 | conducted | true |
| `lu-mat-4-s1...` | independent_work | Latihan Mandiri Pecahan | *(null)* | 2029-08-15 | conducted | false |
| `lu-hr-4...` | homeroom_session | Daily Homeroom Roll Call | 4A | 2029-08-12 | conducted | true |

> [!NOTE]
> Academics owns the educational purpose, expected outcomes, student participation, and academic result of an activity. Teaching owns the teacher assignment (referenced by `teacher_assignment_id`). Operations owns scheduling and room allocation (not stored in this table). The activity may be individual, asynchronous, or group-based.
> 
> Per Resolved Questions:
> - **Class / Cohort Partitioning (Q6):** Dynamic reference to administrative homerooms via `section_label` without requiring a standalone class table.
> - **Attendance Mandate (Q7):** Controlled via `is_attendance_required`. Defaults to `true` for synchronous activities and `false` for self-paced/asynchronous activities.
> - **Daily Statutory Attendance (Q19):** Daily homeroom roll call is conducted as a scheduled `Learning Activity` with `activity_type = 'homeroom_session'` and `is_attendance_required = true` to produce statutory report card day totals. General physical gate presence belongs to Operations.

---

### 2.3 `attendance_records` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Records the participation of a student in a required learning activity. Captures observed attendance status, correction history, and authorization. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `learning_activity_id` | UUID | FK → `learning_activities.id`, NOT NULL | The learning activity attended |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student |
| `status` | VARCHAR | NOT NULL | Attendance status: `present`, `absent`, `late`, `excused_absence`, `partial` |
| `recorded_by` | VARCHAR | NOT NULL | Person who recorded attendance |
| `recorded_date` | DATE | NOT NULL | Date the attendance was recorded |
| `evidence` | TEXT | nullable | Supporting evidence (e.g., parent notification for excused absence) |
| `notes` | TEXT | nullable | Notes |

**Example:**

| learning_activity_id | student_id | status | recorded_by | recorded_date |
|---|---|---|---|---|
| `la-pecahan-lesson...` | `std-108...` | present | Ibu Dewi | 2029-08-12 |
| `la-pecahan-lesson...` | `std-205...` | absent | Ibu Dewi | 2029-08-12 |

---

### 2.4 `attendance_corrections` — Supporting Table

| Aspect | Description |
|---|---|
| **Purpose** | Preserves the audit trail when an attendance record is corrected. Each correction records what changed, why, when, and who authorized it. The domain requires corrections to never silently overwrite. |
| **Type** | Supporting |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `attendance_record_id` | UUID | FK → `attendance_records.id`, NOT NULL | The attendance record being corrected |
| `previous_status` | VARCHAR | NOT NULL | Status value before correction |
| `new_status` | VARCHAR | NOT NULL | Status value after correction |
| `reason` | TEXT | NOT NULL | Reason for correction |
| `corrected_by` | VARCHAR | NOT NULL | Person who authorized the correction |
| `corrected_date` | DATE | NOT NULL | Date correction was made |
| `evidence` | TEXT | nullable | Supporting evidence for the correction |

**Example:**

| attendance_record_id | previous_status | new_status | reason | corrected_by | corrected_date |
|---|---|---|---|---|---|
| `att-205-absent...` | absent | excused_absence | Parent submitted medical certificate | Ibu Dewi | 2029-08-14 |

---

### 2.5 `assignments` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Academic work requested from students. Must always originate from a Learning Activity — the domain enforces `Learning Activity → Assignment → Submission`. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `learning_activity_id` | UUID | FK → `learning_activities.id`, NOT NULL | The learning activity this assignment originates from |
| `title` | VARCHAR | NOT NULL | Assignment title (e.g., "Latihan Pecahan Biasa — Halaman 42") |
| `description` | TEXT | nullable | Assignment requirements and expectations |
| `release_date` | DATE | NOT NULL | Date assignment is released to students |
| `due_date` | DATE | nullable | Due date (null if no fixed deadline) |
| `status` | VARCHAR | NOT NULL | Status: `draft`, `released`, `closed` |
| `notes` | TEXT | nullable | Notes |

**Example:**

| learning_activity_id | title | release_date | due_date | status |
|---|---|---|---|---|
| `la-pecahan-lesson...` | Latihan Pecahan Biasa — Halaman 42 | 2029-08-12 | 2029-08-14 | released |

> [!IMPORTANT]
> Assignments must **not** exist independently of a Learning Activity. The `learning_activity_id` FK is NOT NULL, enforcing the domain rule.

---

### 2.6 `submissions` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Records a student's response to an assignment. Tracks what was delivered, its status, and allows resubmission. Submission status and academic evaluation are explicitly separate concerns. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `assignment_id` | UUID | FK → `assignments.id`, NOT NULL | The assignment this submission responds to |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student submitting |
| `status` | VARCHAR | NOT NULL | Submission status: `submitted`, `late`, `missing`, `excused`, `exempted` |
| `submitted_date` | TIMESTAMP | nullable | Date and time of submission (null for missing/excused/exempted) |
| `submission_reference` | VARCHAR | nullable | Reference to submitted work (file path, URL, or description) |
| `is_resubmission` | BOOLEAN | NOT NULL, default false | Whether this is a resubmission |
| `previous_submission_id` | UUID | FK → `submissions.id`, nullable | Previous submission this resubmits |
| `notes` | TEXT | nullable | Notes |

**Example:**

| assignment_id | student_id | status | submitted_date | is_resubmission |
|---|---|---|---|---|
| `asg-pecahan...` | `std-108...` | submitted | 2029-08-13 10:30:00 | false |
| `asg-pecahan...` | `std-205...` | missing | *(null)* | false |

> [!NOTE]
> Missing submissions do **not** automatically become zero scores. Grading consequences are determined by assessment policy, not by submission status. The `excused` and `exempted` statuses are policy-driven, not automatic.

---

### Participation & Learning Activities — Table Relationships

```mermaid
erDiagram
    STUDENT_EXT ||--o{ academic_participations : "undertakes"
    academic_participations ||--|| learning_units : "in"
    academic_participations }o--|| ACADEMIC_PERIOD_EXT : "within (external)"
    academic_participations ||--o| student_learning_plans : "governed by"
    learning_units ||--o{ learning_activities : "contains"
    learning_activities ||--o| TEACHER_ASSIGNMENT_EXT : "references (external)"
    learning_activities ||--o{ attendance_records : "generates"
    attendance_records ||--|| STUDENT_EXT : "for"
    attendance_records ||--o{ attendance_corrections : "corrected by"
    learning_activities ||--o{ assignments : "issues"
    assignments ||--o{ submissions : "receives"
    submissions ||--|| STUDENT_EXT : "from"
    submissions ||--o| submissions : "resubmits"
```

---

## Submodule 3 — Assessment & Results

### 3.1 `assessments` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | A planned or conducted evaluation event designed to collect evidence of learning. Defines the assessment purpose, method, criteria, and applicable policy. The organizing concept for evidence collection and evaluation. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `learning_unit_id` | UUID | FK → `learning_units.id`, NOT NULL | The learning unit being assessed |
| `learning_activity_id` | UUID | FK → `learning_activities.id`, nullable | The learning activity this assessment is linked to (nullable because some assessments may span multiple activities) |
| `title` | VARCHAR | NOT NULL | Assessment title (e.g., "Ulangan Harian Pecahan — Matematika Kelas 4") |
| `assessment_type` | VARCHAR | NOT NULL | Type: `formative`, `summative`, `diagnostic`, `portfolio`, `project`, `other` |
| `assessment_method` | VARCHAR | NOT NULL | Method: `written_test`, `oral`, `practical`, `project`, `portfolio`, `observation`, `other` |
| `max_score` | DECIMAL | nullable | Maximum possible score (e.g., 100.0) |
| `passing_score` | DECIMAL | nullable | Minimum passing score / mastery threshold (KKM), used for completion and capped reassessment |
| `reassessment_policy` | VARCHAR | NOT NULL, default 'highest_score' | Reassessment selection strategy: `capped_at_passing`, `highest_score`, `latest_attempt`, `average`, `none` |
| `criteria` | TEXT | nullable | Assessment criteria or rubric description |
| `assessment_date` | DATE | nullable | Date assessment is conducted |
| `assessor_reference` | VARCHAR | nullable | Teacher or assessor reference (from Teaching domain) |
| `status` | VARCHAR | NOT NULL | Status: `planned`, `conducted`, `cancelled` |
| `notes` | TEXT | nullable | Notes |

**Example:**

| learning_unit_id | title | assessment_type | assessment_method | max_score | passing_score | reassessment_policy | status |
|---|---|---|---|---|---|---|---|
| `lu-mat-4-s1...` | Ulangan Harian Pecahan — Matematika Kelas 4 | summative | written_test | 100 | 75 | capped_at_passing | conducted |
| `lu-mat-4-s1...` | Pengamatan Diskusi Kelompok | formative | observation | *(null)* | *(null)* | none | conducted |

> [!NOTE]
> Per Resolved Questions:
> - **Core Assessment Models (Q10):** Supports `numeric`, `letter_grade`, `pass_fail` / `completion`, and `narrative` feedback. Rubrics record qualitative narrative feedback and overall scores.
> - **Reassessment Strategy (Q12):** Governed by `reassessment_policy`. When a student retakes an assessment, the policy dictates whether the operative result is capped at `passing_score`, takes the highest score, uses the latest attempt, or averages attempts.

---

### 3.2 `assessment_evidence` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Student work, observation, response, performance, or other evidence used to evaluate learning. The raw input to evaluation — distinct from the resulting academic result. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `assessment_id` | UUID | FK → `assessments.id`, NOT NULL | The assessment this evidence belongs to |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student who produced this evidence |
| `submission_id` | UUID | FK → `submissions.id`, nullable | Submission that produced this evidence (null when evidence is from observation, oral, etc.) |
| `evidence_type` | VARCHAR | NOT NULL | Type: `test_paper`, `oral_response`, `project_artifact`, `portfolio_item`, `observation`, `practical_demonstration`, `other` |
| `collected_date` | DATE | NOT NULL | Date evidence was collected |
| `evidence_reference` | VARCHAR | nullable | Reference to evidence artifact (file, URL, or description) |
| `notes` | TEXT | nullable | Notes |

**Example:**

| assessment_id | student_id | evidence_type | collected_date | submission_id |
|---|---|---|---|---|
| `asm-ulangan-pecahan...` | `std-108...` | test_paper | 2029-09-10 | *(null)* |
| `asm-diskusi...` | `std-108...` | observation | 2029-08-20 | *(null)* |

> [!NOTE]
> Raw evidence, provisional evaluation, approved result, and published result must **not** be treated as automatically equivalent. This table holds evidence only — evaluation happens in `academic_results`.

---

### 3.3 `academic_results` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | An authorized evaluation of learning evidence under an applicable assessment policy. The primary academic record. Supports multiple evaluation models and distinguishes between provisional, approved, and published states. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `assessment_id` | UUID | FK → `assessments.id`, NOT NULL | The assessment this result evaluates |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student |
| `attempt_number` | INTEGER | NOT NULL, default 1 | Reassessment attempt number (1, 2, 3...) |
| `replaces_result_id` | UUID | FK → `academic_results.id`, nullable | Prior result record replaced/superseded by this retake attempt |
| `is_final` | BOOLEAN | NOT NULL, default true | Whether this is the operative/final result determined by the assessment's reassessment policy |
| `score_numeric` | DECIMAL | nullable | Numeric score (when applicable) |
| `score_max` | DECIMAL | nullable | Maximum possible score (for context) |
| `grade_letter` | VARCHAR | nullable | Letter grade (when applicable, e.g., "A", "B+") |
| `grade_label` | VARCHAR | nullable | Qualitative grade label (e.g., "Kompeten", "Pass", "Distinction") |
| `narrative_feedback` | TEXT | nullable | Narrative or descriptive feedback |
| `evaluation_model` | VARCHAR | NOT NULL | Model used: `numeric`, `letter_grade`, `pass_fail`, `competency`, `rubric`, `narrative`, `completion` |
| `evaluated_by` | VARCHAR | NOT NULL | Teacher or assessor who evaluated |
| `evaluated_date` | DATE | NOT NULL | Date of evaluation |
| `result_status` | VARCHAR | NOT NULL | Lifecycle: `provisional`, `approved`, `published` |
| `approved_by` | VARCHAR | nullable | Academic Authority (or delegated Coordinator) who approved result |
| `approved_date` | DATE | nullable | Date approved |
| `published_date` | DATE | nullable | Date published to authorized audiences |
| `notes` | TEXT | nullable | Notes |

**Example:**

| assessment_id | student_id | attempt_number | is_final | score_numeric | score_max | grade_letter | evaluation_model | result_status | evaluated_by |
|---|---|---|---|---|---|---|---|---|---|
| `asm-ulangan...` | `std-108...` | 1 | true | 85 | 100 | A | numeric | published | Ibu Dewi |
| `asm-ulangan...` | `std-205...` | 1 | false | 55 | 100 | D | numeric | approved | Ibu Dewi |
| `asm-ulangan...` | `std-205...` | 2 | true | 75 | 100 | C | numeric | approved | Ibu Dewi |
| `asm-diskusi...` | `std-108...` | 1 | true | *(null)* | *(null)* | *(null)* | competency | approved | Ibu Dewi |

> [!IMPORTANT]
> Provisional, approved, and published states have different meanings and must remain distinguishable. A provisional result must **not** be consumed by progression or reporting workflows as if it were approved. Approved results may change only through the correction workflow recorded in `academic_record_corrections`.
> 
> Per Resolved Questions:
> - **Governance Lifecycle (Q11):** Assigned Teacher records provisional evaluations (`provisional`). Academic Coordinator reviews assessments across classes. Academic Authority (or delegated Coordinator for routine formative work) approves results (`approved`). Only approved results feed progression, standing, and report cards. Post-approval edits are prohibited in place and require formal `academic_record_corrections` authorized by Academic Authority. Academic Authority or authorized Coordinator releases results (`published`).
> - **Reassessment Attempts (Q12):** Every retake produces a distinct immutable `academic_results` record (`attempt_number = 2, 3...`, referencing `replaces_result_id`). The operative result is flagged `is_final = true` in accordance with the parent assessment's `reassessment_policy`. Preserves 100% of remediation audit history.

---

### 3.4 `academic_record_corrections` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Explicit authorized corrections to academic records. Ensures approved records are never silently overwritten. Applies to all correctable academic records: results, assignments, submissions, submission evaluations, academic standing, promotion recommendations, graduation eligibility, and academic clearance. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `record_type` | VARCHAR | NOT NULL | Type of record being corrected: `academic_result`, `assignment`, `submission`, `academic_standing`, `promotion_recommendation`, `graduation_eligibility`, `academic_clearance` |
| `record_id` | UUID | NOT NULL | ID of the record being corrected |
| `original_value` | TEXT | NOT NULL | Original value before correction (serialized for auditability) |
| `corrected_value` | TEXT | NOT NULL | New value after correction (serialized) |
| `reason` | TEXT | NOT NULL | Reason for correction |
| `evidence` | TEXT | nullable | Supporting evidence |
| `corrected_by` | VARCHAR | NOT NULL | Person who authorized the correction |
| `corrected_date` | DATE | NOT NULL | Date of correction |
| `is_material` | BOOLEAN | NOT NULL, default false | Whether this is a material correction requiring Academic Authority |
| `notes` | TEXT | nullable | Notes |

**Example:**

| record_type | record_id | original_value | corrected_value | reason | corrected_by |
|---|---|---|---|---|---|
| academic_result | `res-108-ulangan...` | score: 85 | score: 88 | Scoring error on question 5 | Ibu Ratna (Academic Authority) |
| submission | `sub-205-pecahan...` | status: missing | status: excused | Medical certificate provided | Ibu Dewi |

> [!NOTE]
> This is a unified correction table using a `record_type` discriminator, per entity document assumption 4. The domain defines multiple correction workflows (Correct Approved Academic Record, Correct Academic Clearance, Correct Phase-2 Academic Record). Application-level logic determines which correction rules apply based on `record_type`. Routine corrections may be made by teachers; material corrections and standing/eligibility corrections require Academic Authority.

---

### 3.5 `report_cards` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | An authorized academic document communicating a student's approved results, attendance, and progress for a reporting period. Produced from operational academic records — the report card does not own grades or progress. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student |
| `academic_period_id` | UUID | FK → external (Operations), NOT NULL | Reporting period |
| `academic_period_label` | VARCHAR | NOT NULL | Human-readable period label |
| `report_type` | VARCHAR | NOT NULL | Periodic report type: `mid_term`, `end_of_term` |
| `template_name` | VARCHAR | NOT NULL | Layout and curriculum template (e.g., "Buku Rapor Merdeka", "PACE Modular") |
| `status` | VARCHAR | NOT NULL | Status: `draft`, `approved`, `published` |
| `approved_by` | VARCHAR | nullable | Person who approved |
| `approved_date` | DATE | nullable | Date approved |
| `published_date` | DATE | nullable | Date published |
| `narrative_comments` | TEXT | nullable | Teacher or coordinator narrative comments |
| `content_snapshot` | TEXT | nullable | Immutable frozen snapshot (JSON payload / rendered archive) guaranteeing historical reproducibility |
| `notes` | TEXT | nullable | Notes |

**Example:**

| student_id | academic_period_label | report_type | template_name | status | published_date |
|---|---|---|---|---|---|
| `std-108...` | Semester 1 TP 2029/2030 | end_of_term | Buku Rapor Merdeka | published | 2029-12-22 |
| `std-108...` | Mid-Term TP 2029/2030 | mid_term | Mid-Term Progress Report | published | 2029-10-05 |

> [!NOTE]
> Per Resolved Question 18, the system supports periodic report suites: Mid-Term Progress Reports (`mid_term`) and End-of-Semester Report Cards (`end_of_term` — *Buku Rapor*). Upon approval/publication, an immutable document payload is frozen into `content_snapshot` to guarantee exact historical reproducibility without relying on live queries against mutable operational tables.

---

### 3.6 `report_card_entries` — Supporting Table

| Aspect | Description |
|---|---|
| **Purpose** | Links a report card to the individual approved academic results it contains. Each entry represents one learning unit's result within the report card. |
| **Type** | Supporting |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `report_card_id` | UUID | FK → `report_cards.id`, NOT NULL | The report card |
| `learning_unit_id` | UUID | FK → `learning_units.id`, NOT NULL | The learning unit |
| `academic_result_id` | UUID | FK → `academic_results.id`, nullable | Reference to the approved academic result (null if no result available for this unit) |
| `display_order` | INTEGER | NOT NULL | Display sequence on the report card |
| `teacher_comment` | TEXT | nullable | Subject-specific teacher comment |

**Example:**

| report_card_id | learning_unit_id | academic_result_id | display_order | teacher_comment |
|---|---|---|---|---|
| `rc-108-s1...` | `lu-mat-4-s1...` | `res-108-mat-final...` | 1 | Rina menunjukkan kemajuan yang baik |
| `rc-108-s1...` | `lu-bi-4-s1...` | `res-108-bi-final...` | 2 | *(null)* |

---

### 3.7 `transcripts` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | An authorized cumulative academic document certifying a student's academic history across multiple periods or their entire enrollment. Distinct from Report Card in scope (cumulative vs. periodic) and purpose (official transfer/graduation vs. periodic communication). |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student |
| `transcript_type` | VARCHAR | NOT NULL | Type: `official`, `unofficial` |
| `period_start` | DATE | NOT NULL | Start of covered period |
| `period_end` | DATE | NOT NULL | End of covered period |
| `status` | VARCHAR | NOT NULL | Status: `draft`, `approved`, `issued` |
| `approved_by` | VARCHAR | nullable | Person who approved |
| `approved_date` | DATE | nullable | Date approved |
| `issued_date` | DATE | nullable | Date issued |
| `purpose` | VARCHAR | nullable | Purpose of issuance (e.g., "Graduation", "Transfer Out", "Advising") |
| `content_snapshot` | TEXT | nullable | Immutable frozen snapshot (JSON payload / rendered archive) guaranteeing historical reproducibility |
| `notes` | TEXT | nullable | Notes |

**Example:**

| student_id | transcript_type | period_start | period_end | status | issued_date | purpose |
|---|---|---|---|---|---|---|
| `std-108...` | official | 2026-07-15 | 2030-06-20 | issued | 2030-06-25 | Graduation |
| `std-205...` | unofficial | 2028-07-15 | 2029-12-20 | issued | 2029-12-22 | Advising |

> [!NOTE]
> Per Resolved Questions 14 and 18:
> - **Transcript Scope & Snapshots (Q18):** Supports Official Cumulative Transcripts (`official` — certified for graduation or transfer-out) and Unofficial Advising Transcripts (`unofficial`). Official issuances archive an immutable snapshot in `content_snapshot`.
> - **Transferred Learning (Q14):** Transferred and recognized prior learning units are distinguished with a transfer tag `[T]` on official transcripts.

---

### Assessment & Results — Table Relationships

```mermaid
erDiagram
    assessments ||--|| learning_units : "assesses"
    assessments ||--o| learning_activities : "linked to"
    assessments ||--o{ assessment_evidence : "collects"
    assessment_evidence ||--|| STUDENT_EXT : "from"
    assessment_evidence ||--o| submissions : "may originate from"
    assessments ||--o{ academic_results : "produces"
    academic_results ||--|| STUDENT_EXT : "for"
    academic_results ||--o| academic_results : "replaces (reassessment)"
    academic_results ||--o{ academic_record_corrections : "corrected by"
    STUDENT_EXT ||--o{ report_cards : "receives"
    report_cards ||--o{ report_card_entries : "contains"
    report_card_entries ||--o| academic_results : "references"
    report_card_entries ||--|| learning_units : "for"
    STUDENT_EXT ||--o{ transcripts : "receives"
```

---

## Submodule 4 — Standing & Progression

### 4.1 `learning_progress` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Records the student's current advancement toward defined learning expectations for a specific Learning Unit. Uses the Phase-2 minimal progress model. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student |
| `learning_unit_id` | UUID | FK → `learning_units.id`, NOT NULL | The learning unit |
| `academic_period_id` | UUID | FK → external (Operations), NOT NULL | Academic period |
| `status` | VARCHAR | NOT NULL | Progress status: `not_started`, `in_progress`, `completed`, `needs_remediation`, `exempted` |
| `basis` | VARCHAR | NOT NULL | What this progress is based on: `assessment_evidence`, `learning_work`, `authorized_decision` |
| `completion_date` | DATE | nullable | Date learning requirements were completed (null if not completed; may occur outside period boundaries) |
| `last_updated` | DATE | NOT NULL | Date progress was last determined |
| `determined_by` | VARCHAR | NOT NULL | Person who determined or updated progress |
| `notes` | TEXT | nullable | Notes |

**Example:**

| student_id | learning_unit_id | status | basis | completion_date | last_updated |
|---|---|---|---|---|---|
| `std-108...` | `lu-mat-4-s1...` | in_progress | assessment_evidence | *(null)* | 2029-10-15 |
| `std-108...` | `lu-bi-4-s1...` | completed | assessment_evidence | 2029-12-18 | 2029-12-18 |
| `std-205...` | `lu-mat-3-s1...` | needs_remediation | assessment_evidence | *(null)* | 2029-10-20 |

> [!NOTE]
> Progress states follow the Phase-2 minimal model without percentage-based tracking. The domain prohibits inferring completion merely from calendar period boundaries, administrative placement, or attendance alone.
> 
> Per Resolved Questions:
> - **Completion Outside Period Boundaries (Q13):** Completion is evidence-driven and stamped with an explicit `completion_date`. A student may finish a Learning Unit mid-term (e.g. PACE packet in October), during holidays, or post-term during remediation.
> - **Transferred / Recognized Learning (Q14):** Created directly with `status = 'completed'` and `basis = 'authorized_decision'`.

---

### 4.2 `academic_standings` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Records an academic conclusion about the student's overall academic condition. An academic determination, not grades. Serves as input to progression and graduation decisions. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student |
| `academic_period_id` | UUID | FK → external (Operations), NOT NULL | Academic period this standing applies to |
| `academic_period_label` | VARCHAR | NOT NULL | Human-readable period label |
| `standing` | VARCHAR | NOT NULL | Standing: `good_standing`, `conditional_standing`, `not_in_good_standing` |
| `policy_reference` | VARCHAR | NOT NULL | Policy and evidence criteria used to determine standing |
| `conditions` | TEXT | nullable | Mandatory stipulations when standing is `conditional_standing` |
| `is_exception` | BOOLEAN | NOT NULL, default false | Whether this standing determination is an authorized exception |
| `exception_reason` | TEXT | nullable | Mandatory rationale when `is_exception` is true |
| `committee_reference` | VARCHAR | nullable | Formal decree or meeting minutes reference (e.g., SK Dewan Guru) |
| `authorized_by` | VARCHAR | NOT NULL | Academic Authority who approved standing |
| `determined_date` | DATE | NOT NULL | Date standing was determined |
| `notes` | TEXT | nullable | Notes |

**Example:**

| student_id | academic_period_label | standing | is_exception | committee_reference | authorized_by | determined_date |
|---|---|---|---|---|---|---|
| `std-108...` | TP 2029/2030 | good_standing | false | *(null)* | Academic Coordinator | 2030-06-10 |
| `std-205...` | TP 2029/2030 | conditional_standing | true | SK-DG/2030/06-12 | Academic Authority | 2030-06-10 |

> [!NOTE]
> Per Resolved Question 16, governance fields (`is_exception`, `exception_reason`, `conditions`, `committee_reference`, `authorized_by`) are embedded directly on decision tables, avoiding unnecessary committee subsystem overhead.

---

### 4.3 `promotion_recommendations` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Records Academics' recommendation regarding a student's advancement to the next level or grade. This is a recommendation — Students executes official promotion or retention. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student |
| `academic_period_id` | UUID | FK → external (Operations), NOT NULL | Academic period |
| `academic_period_label` | VARCHAR | NOT NULL | Human-readable period label |
| `recommendation` | VARCHAR | NOT NULL | Recommendation: `promotion_eligible`, `retention_recommended`, `conditional_promotion` |
| `from_level` | VARCHAR | NOT NULL | Current grade/level |
| `recommended_to_level` | VARCHAR | nullable | Recommended next grade/level (null for retention) |
| `conditions` | TEXT | nullable | Conditions for conditional promotion |
| `is_exception` | BOOLEAN | NOT NULL, default false | Whether this recommendation is an authorized exception |
| `exception_reason` | TEXT | nullable | Mandatory justification when `is_exception` is true |
| `committee_reference` | VARCHAR | nullable | Formal decree or meeting minutes reference (e.g., SK Dewan Guru) |
| `academic_standing_id` | UUID | FK → `academic_standings.id`, nullable | Supporting academic standing |
| `supporting_evidence` | TEXT | NOT NULL | Summary of evidence supporting this recommendation |
| `authorized_by` | VARCHAR | NOT NULL | Academic Authority who approved recommendation |
| `determined_date` | DATE | NOT NULL | Date determined |
| `notes` | TEXT | nullable | Notes |

**Example:**

| student_id | academic_period_label | recommendation | from_level | recommended_to_level | is_exception | authorized_by |
|---|---|---|---|---|---|---|
| `std-108...` | TP 2029/2030 | promotion_eligible | Grade 4 | Grade 5 | false | Academic Authority |
| `std-205...` | TP 2029/2030 | conditional_promotion | Grade 4 | Grade 5 | true | Academic Authority |

> [!NOTE]
> Per Resolved Question 16, exceptions and conditional promotion stipulations embed governance attributes directly (`is_exception`, `exception_reason`, `conditions`, `committee_reference`, `authorized_by`).

---

### 4.4 `graduation_eligibilities` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Records Academics' determination of whether a student has satisfied academic graduation requirements. Students executes official graduation. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student |
| `determination` | VARCHAR | NOT NULL | Determination: `graduation_eligible`, `graduation_not_eligible`, `conditional_graduation` |
| `academic_requirement_id` | UUID | FK → `academic_requirements.id`, nullable | Reference to versioned graduation requirement entity |
| `requirement_reference` | VARCHAR | NOT NULL | Applicable graduation requirements label (e.g., "SD Graduation Requirements v2032.1") |
| `conditions` | TEXT | nullable | Conditions for conditional graduation |
| `supporting_evidence` | TEXT | NOT NULL | Summary of evidence: completed requirements, credits, cumulative standing |
| `is_exception` | BOOLEAN | NOT NULL, default false | Whether this determination is an authorized exception |
| `exception_reason` | TEXT | nullable | Mandatory rationale when `is_exception` is true |
| `committee_reference` | VARCHAR | nullable | Formal decree or meeting minutes reference (e.g., SK Dewan Guru / Academic Committee) |
| `authorized_by` | VARCHAR | NOT NULL | Academic Authority or Authorized Academic Committee |
| `determined_date` | DATE | NOT NULL | Date determined |
| `notes` | TEXT | nullable | Notes |

**Example:**

| student_id | determination | requirement_reference | is_exception | authorized_by | determined_date |
|---|---|---|---|---|---|
| `std-108...` | graduation_eligible | SD Graduation Requirements v2032.1 | false | Academic Authority / Academic Committee | 2032-05-20 |

> [!NOTE]
> Per Resolved Questions 16 and 17:
> - **Exclusively Academic Evaluation (Q17):** Academics determines eligibility against versioned `academic_requirements` (units completed, cumulative standing, culminating assessments). Non-academic clearances (finance, conduct) are assembled independently by the Students domain.
> - **Embedded Governance (Q16):** Exceptions embed `is_exception`, `exception_reason`, and `committee_reference` directly.

---

### 4.5 `academic_clearances` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Purpose-specific determination stating whether defined academic prerequisites have been satisfied. Phase-2 purpose is Transfer Out only. Students requests; Academics determines; Students consumes. Distinct from standing and eligibility. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student |
| `purpose` | VARCHAR | NOT NULL | Clearance purpose: `transfer_out` (Phase-2; additional purposes configurable later) |
| `applicable_policy` | VARCHAR | NOT NULL | Policy used for evaluation |
| `evaluated_facts` | TEXT | NOT NULL | Academic facts evaluated for this purpose |
| `blocking_items` | TEXT | nullable | Items blocking clearance (null if none) |
| `result` | VARCHAR | NOT NULL | Result: `granted`, `denied`, `conditional`, `cannot_yet_be_determined`, `expired`, `revoked` |
| `determination_date` | DATE | NOT NULL | Date determination was made |
| `valid_until` | DATE | nullable | Expiry date (null if no expiry) |
| `determined_by` | VARCHAR | NOT NULL | Academic Coordinator (routine) or Academic Authority (exception) |
| `is_exception` | BOOLEAN | NOT NULL, default false | Whether this is an exception determination |
| `exception_reason` | TEXT | nullable | Mandatory reason when `is_exception` is true |
| `notes` | TEXT | nullable | Notes |

**Example:**

| student_id | purpose | result | determination_date | valid_until | determined_by | is_exception |
|---|---|---|---|---|---|---|
| `std-faisal...` | transfer_out | granted | 2028-02-15 | 2028-03-15 | Academic Coordinator | false |
| `std-another...` | transfer_out | denied | 2028-03-01 | *(null)* | Academic Coordinator | false |

---

### 4.6 `academic_interventions` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Records academic action taken when a student needs additional learning support or challenge. Covers remediation, reassessment, tutoring recommendation, extension work, recovery plan, learning-plan adjustment, and academic warning. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student |
| `learning_unit_id` | UUID | FK → `learning_units.id`, NOT NULL | The learning unit requiring intervention |
| `intervention_type` | VARCHAR | NOT NULL | Type: `remediation`, `reassessment`, `tutoring_recommendation`, `extension_work`, `recovery_plan`, `learning_plan_adjustment`, `academic_warning` |
| `trigger_type` | VARCHAR | NOT NULL | Dual-trigger category: `assessment_below_threshold`, `missing_work`, `attendance_concern`, `standing_concern`, `teacher_referral` |
| `trigger_description` | TEXT | NOT NULL | Detailed narrative of what triggered the intervention |
| `plan` | TEXT | NOT NULL | Description of the intervention plan |
| `status` | VARCHAR | NOT NULL | Status: `planned`, `in_progress`, `completed`, `cancelled` |
| `initiated_by` | VARCHAR | NOT NULL | Person who initiated the intervention |
| `initiated_date` | DATE | NOT NULL | Date initiated |
| `completed_date` | DATE | nullable | Date completed |
| `outcome` | TEXT | nullable | Outcome of the intervention |
| `notes` | TEXT | nullable | Notes |

**Example:**

| student_id | learning_unit_id | intervention_type | trigger_type | trigger_description | status | initiated_by |
|---|---|---|---|---|---|---|
| `std-205...` | `lu-mat-4-s1...` | remediation | assessment_below_threshold | Assessment score below minimum threshold (45/100) | in_progress | Academic Coordinator |
| `std-108...` | `lu-mat-4-s1...` | extension_work | teacher_referral | Student demonstrates mastery ahead of cohort | planned | Ibu Dewi |

> [!NOTE]
> Per Resolved Question 15, academic interventions follow a **dual-trigger model**:
> - **System Early-Warning Alerts:** Triggered by objective criteria: `assessment_below_threshold`, `missing_work` (accumulated missing submissions), `attendance_concern` (attendance below policy threshold), or `standing_concern`.
> - **Educator Referrals:** Staff-initiated via `teacher_referral` based on qualitative classroom observation, special learning support, or extension/gifted needs.
> Behavioural, wellbeing, counselling, and non-academic interventions belong strictly to the Development domain.

---

### 4.7 `academic_achievements` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Records achievements derived from academic learning or assessment: subject distinction, academic honors, mastery recognition, academic certification, and curriculum-recognized academic competition achievement. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student |
| `achievement_type` | VARCHAR | NOT NULL | Type: `subject_distinction`, `academic_honors`, `mastery_recognition`, `academic_certification`, `academic_competition` |
| `title` | VARCHAR | NOT NULL | Achievement title |
| `learning_unit_id` | UUID | FK → `learning_units.id`, nullable | Related learning unit (if applicable) |
| `academic_period_id` | UUID | FK → external (Operations), nullable | Academic period |
| `criteria` | TEXT | nullable | Criteria for the achievement |
| `supporting_evidence` | TEXT | nullable | Reference to academic results or evidence supporting this achievement |
| `recognized_by` | VARCHAR | NOT NULL | Person who recognized the achievement |
| `recognized_date` | DATE | NOT NULL | Date recognized |
| `notes` | TEXT | nullable | Notes |

**Example:**

| student_id | achievement_type | title | learning_unit_id | recognized_by | recognized_date |
|---|---|---|---|---|---|
| `std-108...` | subject_distinction | Nilai Tertinggi Matematika | `lu-mat-4-s1...` | Academic Authority | 2030-06-15 |
| `std-108...` | academic_honors | Juara Umum Grade 4 | *(null)* | Academic Authority | 2030-06-15 |

---

### Standing & Progression — Table Relationships

```mermaid
erDiagram
    STUDENT_EXT ||--o{ learning_progress : "has"
    learning_progress ||--|| learning_units : "for"
    learning_progress }o--|| ACADEMIC_PERIOD_EXT : "within (external)"
    STUDENT_EXT ||--o{ academic_standings : "receives"
    academic_standings }o--|| ACADEMIC_PERIOD_EXT : "within (external)"
    STUDENT_EXT ||--o{ promotion_recommendations : "receives"
    promotion_recommendations ||--o| academic_standings : "informed by"
    STUDENT_EXT ||--o{ graduation_eligibilities : "receives"
    academic_requirements ||--o{ graduation_eligibilities : "evaluated against"
    STUDENT_EXT ||--o{ academic_clearances : "determined for"
    STUDENT_EXT ||--o{ academic_interventions : "receives"
    academic_interventions ||--|| learning_units : "for"
    STUDENT_EXT ||--o{ academic_achievements : "earns"
    academic_achievements ||--o| learning_units : "for"
    academic_record_corrections }o--|| academic_standings : "may correct"
    academic_record_corrections }o--|| promotion_recommendations : "may correct"
    academic_record_corrections }o--|| graduation_eligibilities : "may correct"
    academic_record_corrections }o--|| academic_clearances : "may correct"
```

---

## Submodule 5 — Student Learning Plan

### 5.1 `student_learning_plans` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | The approved set or sequence of learning expectations for a student in exceptional or individualized cases. Optional — default academic operation works without one. At most one Active SLP per student per Academic Period. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `student_id` | UUID | FK → external (Students), NOT NULL | The student |
| `academic_period_id` | UUID | FK → external (Operations), NOT NULL | Effective academic period |
| `academic_period_label` | VARCHAR | NOT NULL | Human-readable period label |
| `status` | VARCHAR | NOT NULL | Status: `draft`, `active`, `revised`, `completed`, `archived` |
| `version` | INTEGER | NOT NULL, default 1 | Plan version number (incremented on revision) |
| `notes` | TEXT | nullable | Plan notes (e.g., rationale for individualization) |

**Example:**

| student_id | academic_period_label | status | version | notes |
|---|---|---|---|---|
| `std-205...` | Semester 1 TP 2029/2030 | active | 2 | Student repeating Matematika at Grade 3 level per Academic Authority recommendation |

> [!IMPORTANT]
> A student may have at most **one Active** SLP per Academic Period. This is enforced at the application level (partial unique index on `student_id` + `academic_period_id` where `status = 'active'` may also be used). Historical plans (`revised`, `completed`, `archived`) are preserved. A learning plan is **not** the student's administrative placement.

---

### 5.2 `slp_learning_units` — Supporting Table

| Aspect | Description |
|---|---|
| **Purpose** | Links a Student Learning Plan to its constituent Learning Units. Captures the specific learning expected under this plan. |
| **Type** | Supporting |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `student_learning_plan_id` | UUID | FK → `student_learning_plans.id`, NOT NULL | The SLP |
| `learning_unit_id` | UUID | FK → `learning_units.id`, NOT NULL | The learning unit included in the plan |
| `sequence_order` | INTEGER | nullable | Required sequence (null if no sequencing required) |
| `is_required` | BOOLEAN | NOT NULL, default true | Whether this unit is required or optional within the plan |
| `notes` | TEXT | nullable | Notes (e.g., level explanation) |

**Example:**

| student_learning_plan_id | learning_unit_id | sequence_order | is_required | notes |
|---|---|---|---|---|
| `slp-205...` | `lu-mat-3-s1...` | 1 | true | Remediation: Grade 3 level |
| `slp-205...` | `lu-bi-4-s1...` | *(null)* | true | Normal Grade 4 level |
| `slp-205...` | `lu-ipa-4-s1...` | *(null)* | true | Normal Grade 4 level |

---

### 5.3 `academic_plan_adjustments` — Main Table

| Aspect | Description |
|---|---|
| **Purpose** | Records an accommodation, substitution, or exemption applied to a Student Learning Plan. All three types are treated as a single category. Each requires explicit Academic Authority approval. |
| **Type** | Core |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `student_learning_plan_id` | UUID | FK → `student_learning_plans.id`, NOT NULL | The SLP this adjustment applies to |
| `adjustment_type` | VARCHAR | NOT NULL | Type: `accommodation`, `substitution`, `exemption` |
| `description` | TEXT | NOT NULL | Description of the adjustment |
| `rationale` | TEXT | NOT NULL | Rationale justifying the adjustment |
| `effective_date` | DATE | NOT NULL | Date adjustment becomes effective |
| `end_date` | DATE | nullable | Date adjustment ends (null = indefinite within the SLP period) |
| `approved_by` | VARCHAR | NOT NULL | Academic Authority who approved |
| `approved_date` | DATE | NOT NULL | Date approved |
| `notes` | TEXT | nullable | Notes |

**Example:**

| student_learning_plan_id | adjustment_type | description | rationale | approved_by |
|---|---|---|---|---|
| `slp-205...` | accommodation | Extended time for written assessments (50% additional) | Documented learning need assessment | Ibu Ratna (VP Academics) |
| `slp-205...` | substitution | Replace PJOK regular with adaptive PJOK | Medical recommendation for physical activity modification | Ibu Ratna (VP Academics) |

---

### 5.4 `slp_approvals` — Supporting Table

| Aspect | Description |
|---|---|
| **Purpose** | Records the approval or re-approval of a Student Learning Plan or its revision. Preserves the approval chain — every revision requires re-approval by an Academic Authority. |
| **Type** | Supporting |

**Key Columns:**

| Column | Type | Constraint | Description |
|---|---|---|---|
| `id` | UUID | PK | Unique identifier |
| `student_learning_plan_id` | UUID | FK → `student_learning_plans.id`, NOT NULL | The SLP being approved |
| `plan_version` | INTEGER | NOT NULL | Version of the plan being approved |
| `approved_by` | VARCHAR | NOT NULL | Academic Authority who approved |
| `approved_date` | DATE | NOT NULL | Date approved |
| `approval_type` | VARCHAR | NOT NULL | Type: `initial_approval`, `revision_approval` |
| `notes` | TEXT | nullable | Notes |

**Example:**

| student_learning_plan_id | plan_version | approved_by | approved_date | approval_type |
|---|---|---|---|---|
| `slp-205...` | 1 | Ibu Ratna (VP Academics) | 2029-07-20 | initial_approval |
| `slp-205...` | 2 | Ibu Ratna (VP Academics) | 2029-07-22 | revision_approval |

---

### Student Learning Plan — Table Relationships

```mermaid
erDiagram
    STUDENT_EXT ||--o{ student_learning_plans : "may have"
    student_learning_plans }o--|| ACADEMIC_PERIOD_EXT : "effective within (external)"
    student_learning_plans ||--o{ slp_learning_units : "includes"
    slp_learning_units ||--|| learning_units : "references"
    student_learning_plans ||--o{ academic_plan_adjustments : "contains"
    student_learning_plans ||--o{ slp_approvals : "approved by"
```

---

## Cross-Module Table Summary

| # | Table | Type | Submodule | Notes |
|---|---|---|---|---|
| 1.1 | `learning_structures` | Main | Learning Structure & Requirements | Curriculum framework with inline versioning |
| 1.2 | `learning_units` | Main | Learning Structure & Requirements | Central learning abstraction; self-referencing hierarchy |
| 1.3 | `academic_requirements` | Main | Learning Structure & Requirements | Rules for completion, progression, graduation |
| 2.1 | `academic_participations` | Main | Participation & Learning Activities | Student ↔ Learning Unit relationship |
| 2.2 | `learning_activities` | Main | Participation & Learning Activities | Learning occurrences (lesson, exam, etc.) |
| 2.3 | `attendance_records` | Main | Participation & Learning Activities | Per-student per-activity attendance |
| 2.4 | `attendance_corrections` | Supporting | Participation & Learning Activities | Attendance correction audit trail |
| 2.5 | `assignments` | Main | Participation & Learning Activities | Academic work assigned from activities |
| 2.6 | `submissions` | Main | Participation & Learning Activities | Student work submitted for assignments |
| 3.1 | `assessments` | Main | Assessment & Results | Evaluation events |
| 3.2 | `assessment_evidence` | Main | Assessment & Results | Raw learning evidence |
| 3.3 | `academic_results` | Main | Assessment & Results | Evaluations with provisional → approved → published lifecycle |
| 3.4 | `academic_record_corrections` | Main | Assessment & Results | Unified correction audit trail |
| 3.5 | `report_cards` | Main | Assessment & Results | Periodic academic reports |
| 3.6 | `report_card_entries` | Supporting | Assessment & Results | Report card ↔ result links |
| 3.7 | `transcripts` | Main | Assessment & Results | Cumulative academic documents |
| 4.1 | `learning_progress` | Main | Standing & Progression | Progress toward learning expectations |
| 4.2 | `academic_standings` | Main | Standing & Progression | Academic condition determination |
| 4.3 | `promotion_recommendations` | Main | Standing & Progression | Promotion/retention recommendations |
| 4.4 | `graduation_eligibilities` | Main | Standing & Progression | Graduation eligibility determinations |
| 4.5 | `academic_clearances` | Main | Standing & Progression | Purpose-specific clearance |
| 4.6 | `academic_interventions` | Main | Standing & Progression | Academic support actions |
| 4.7 | `academic_achievements` | Main | Standing & Progression | Academic achievement records |
| 5.1 | `student_learning_plans` | Main | Student Learning Plan | Individualized learning plans |
| 5.2 | `slp_learning_units` | Supporting | Student Learning Plan | SLP ↔ Learning Unit links |
| 5.3 | `academic_plan_adjustments` | Main | Student Learning Plan | Accommodations, substitutions, exemptions |
| 5.4 | `slp_approvals` | Supporting | Student Learning Plan | SLP approval chain |

**Total: 27 tables** (22 Main, 5 Supporting, 0 Reference)

> [!NOTE]
> No reference tables are created within this domain. Academic Period and other Operations concepts are referenced via external foreign keys. Enumeration values (status types, activity types, etc.) are stored as VARCHAR with application-level validation, consistent with the Student domain convention. Separate reference tables can be introduced later if configuration-driven enumerations are needed.

---

## Cross-Module Relationship Overview

```mermaid
flowchart TD
    subgraph "Submodule 1: Learning Structure & Requirements"
        LS[learning_structures]
        LU[learning_units]
        AR[academic_requirements]
        LS --> LU
        LU -->|self-ref| LU
        LS --> AR
        LU --> AR
    end

    subgraph "Submodule 5: Student Learning Plan"
        SLP[student_learning_plans]
        SLPLU[slp_learning_units]
        APA[academic_plan_adjustments]
        SLPA[slp_approvals]
        SLP --> SLPLU
        SLPLU --> LU
        SLP --> APA
        SLP --> SLPA
    end

    subgraph "Submodule 2: Participation & Learning Activities"
        AP[academic_participations]
        LA[learning_activities]
        ATT[attendance_records]
        ATTC[attendance_corrections]
        ASG[assignments]
        SUB[submissions]
        AP -->|references| LU
        LU --> LA
        LA --> ATT
        ATT --> ATTC
        LA --> ASG
        ASG --> SUB
    end

    subgraph "Submodule 3: Assessment & Results"
        ASM[assessments]
        AE[assessment_evidence]
        RES[academic_results]
        ARC[academic_record_corrections]
        RC[report_cards]
        RCE[report_card_entries]
        TR[transcripts]
        ASM --> AE --> RES
        RES --> ARC
        RC --> RCE
    end

    subgraph "Submodule 4: Standing & Progression"
        LP[learning_progress]
        AS[academic_standings]
        PR[promotion_recommendations]
        GE[graduation_eligibilities]
        AC[academic_clearances]
        AI[academic_interventions]
        AA[academic_achievements]
        LP --> AS --> PR
        AS --> GE
    end

    LU --> ASM
    LU --> AP
    AR -.->|evaluated against| GE
    SUB -.->|evidence| AE
    RES --> LP
    RES -->|replaces| RES
    SLP -.->|governs exceptional| AP
    AI -.->|may adjust| AP
    RES -.-> AA

    PR -->|recommendation| EXT_S["Students Domain (external)"]
    GE -->|eligibility| EXT_S
    AC -->|clearance| EXT_S
```

---

## External Domain References

This domain references but does **not** own the following external concepts:

| External Concept | Owning Domain | Referenced By (Tables) | How Referenced |
|---|---|---|---|
| Student identity | Students | All tables with `student_id` | FK → external `students.id` |
| Academic Period | Operations | `learning_structures`, `academic_participations`, `academic_standings`, `promotion_recommendations`, `academic_achievements`, `student_learning_plans`, `report_cards`, `learning_progress` | FK → external period ID + label column |
| Teacher Assignment | Teaching | `learning_activities` | FK → external `teacher_assignment_id` |
| Administrative Placement | Students | *(not directly referenced — used as context in workflows)* | Consumed via domain event, not FK |

> [!IMPORTANT]
> Academics does **not** own student identity, administrative status, teacher assignment, or operational scheduling. These are consumed as external references. Administrative enrollment does not automatically create Academic Participation. Academic standing does not change official student status. Promotion and graduation recommendations are consumed and executed by Students.

---

## Resolved Business Decisions & Database Realization

All 19 architectural open questions from the domain document have been formally resolved in [academic-entities.md](file:///d:/Project.Private/elsasa-sis-v3/entities/academic-entities.md) and are realized in the database table definitions as follows:

| # | Question & Decision | Realization in Database Schema | Affected Table(s) | Status |
|---|---|---|---|---|
| 1 | **Competencies and outcomes as descriptive metadata and hierarchy.** Fine-grained competencies use parent-child unit composition. | `learning_units.unit_type = 'competency'` with `parent_unit_id` hierarchy; criteria text on `academic_requirements`. No separate competency tables. | `learning_units`, `academic_requirements` | **Closed & Aligned** |
| 2 | **Dual-track learning structures (standard subjects + modular/PACE).** System validates both conventional grade-level subjects and self-paced modular units. | `learning_units.unit_type` supports `subject`, `module`, `pace`, `course`, etc., within the same unified schema. | `learning_structures`, `learning_units` | **Closed & Aligned** |
| 3 | **Explicit state transition for requirements and curriculum versions.** Academic Coordinator drafts; Academic Authority approves and activates. | Lifecycle columns `status` (`draft`, `active`, `superseded`), `approved_by`, `approved_date`, `effective_date`, `end_date`, `supersedes_id` inline on both tables. | `learning_structures`, `academic_requirements` | **Closed & Aligned** |
| 4 | **Learning assignment by default with student self-selection for electives/PACE.** | `academic_participations.source` (`assigned`, `student_selected`, `learning_plan`). | `academic_participations` | **Closed & Aligned** |
| 5 | **Unified lifecycle for repeated, paused, transferred, and recognized learning.** | `academic_participations.participation_type` (`standard`, `repeat`, `transferred_in`, `recognized_prior`), `attempt_number` counter, and `status` (`in_progress`, `paused`, `withdrawn`, `completed`). | `academic_participations`, `learning_progress` | **Closed & Aligned** |
| 6 | **Decomposed class concept across domains — no standalone class entity.** | Dynamic references to administrative homerooms via optional `section_label` on activities and participations. Administrative groups stay in Students, rooms/schedules in Operations. | `academic_participations`, `learning_activities` | **Closed & Aligned** |
| 7 | **Activity-level attendance mandate flag.** Synchronous activities default to true; asynchronous/self-directed default to false. | `learning_activities.is_attendance_required` (BOOLEAN NOT NULL, default true). | `learning_activities` | **Closed & Aligned** |
| 8 | **Standard 5 attendance categories + grace window + coordinator correction.** | `attendance_records.status` (`present`, `absent`, `late`, `excused_absence`, `partial`); post-grace corrections logged immutably in `attendance_corrections`. | `attendance_records`, `attendance_corrections` | **Closed & Aligned** |
| 9 | **Deliverable & milestone evidence for asynchronous participation.** | `is_attendance_required = false` on asynchronous activities; participation evidenced through `submissions` and milestone completions without synthetic attendance rows. | `learning_activities`, `submissions`, `attendance_records` | **Closed & Aligned** |
| 10 | **Core 4 assessment models supported initially.** Numeric, letter grade, pass/fail, and narrative feedback. | `assessments.max_score`, `passing_score`; `academic_results.evaluation_model` (`numeric`, `letter_grade`, `pass_fail`, `narrative`, etc.). | `assessments`, `academic_results` | **Closed & Aligned** |
| 11 | **Role-separated governance lifecycle for assessment results.** Teacher records provisional; Coordinator reviews; Authority approves; Authority/Coordinator publishes; post-approval edits require formal correction. | `academic_results.result_status` (`provisional`, `approved`, `published`), `approved_by`, `approved_date`, `published_date`; audited via `academic_record_corrections`. | `academic_results`, `academic_record_corrections` | **Closed & Aligned** |
| 12 | **Immutable reassessment attempts governed by assessment policy strategy.** | `assessments.reassessment_policy` (`capped_at_passing`, `highest_score`, `latest_attempt`, `average`, `none`); `academic_results.attempt_number`, `replaces_result_id`, `is_final`. | `assessments`, `academic_results` | **Closed & Aligned** |
| 13 | **Evidence/milestone-driven completion outside academic-period boundaries.** | Explicit `completion_date` on `learning_progress` and `academic_participations`, independent of period calendar boundaries. | `learning_progress`, `academic_participations` | **Closed & Aligned** |
| 14 | **Authorized recognition decision with external attribution.** Academic Authority approves transfer/prior learning without dummy lessons or grades. | `academic_participations.participation_type` (`transferred_in`, `recognized_prior`), `source_institution`, `external_grade_or_credit`, `recognition_basis`, `approved_by`; `learning_progress.status = 'completed'`; `[T]` display tag on official transcripts. | `academic_participations`, `learning_progress`, `transcripts` | **Closed & Aligned** |
| 15 | **Dual-trigger model for academic interventions.** Early-warning system alerts + educator manual referrals. | `academic_interventions.trigger_type` (`assessment_below_threshold`, `missing_work`, `attendance_concern`, `standing_concern`, `teacher_referral`) and `trigger_description`. | `academic_interventions` | **Closed & Aligned** |
| 16 | **Embedded governance attributes on decision entities.** Avoids dedicated committee subsystem. | `is_exception`, `exception_reason`, `conditions`, `committee_reference`, `authorized_by` embedded directly on decision tables. | `academic_standings`, `promotion_recommendations`, `graduation_eligibilities` | **Closed & Aligned** |
| 17 | **Versioned academic requirements determine graduation eligibility.** Evaluated exclusively against academic criteria. | `graduation_eligibilities.academic_requirement_id` FK → `academic_requirements.id`; non-academic clearances assembled externally by Students. | `graduation_eligibilities`, `academic_requirements` | **Closed & Aligned** |
| 18 | **Periodic report suite + cumulative transcripts with frozen snapshots.** | `report_cards.report_type` (`mid_term`, `end_of_term`), `template_name`; `transcripts.transcript_type` (`official`, `unofficial`); immutable `content_snapshot` on both. | `report_cards`, `transcripts` | **Closed & Aligned** |
| 19 | **Academics owns learning-activity attendance; daily homeroom roll call conducted via homeroom session.** Gate presence belongs to Operations. | `learning_activities.activity_type = 'homeroom_session'` with `is_attendance_required = true` for statutory daily attendance roll call. | `learning_activities`, `attendance_records` | **Closed & Aligned** |

> [!NOTE]
> All 19 architectural questions are formally resolved and fully represented in the technical table definitions. No unresolved open questions remain between `academic-entities.md` and `academic-entities-tables.md`.

---

## Assumptions

1. **Inline Versioning** — Curriculum Version (entity 1.4) is modeled as inline attributes (`version`, `supersedes_id`, `effective_date`/`end_date`) on both `learning_structures` and `academic_requirements` rather than as a separate table. This eliminates an entity that would add complexity without clear workflow benefit.
2. **Attendance Corrections as Separate Table** — Split from `attendance_records` because the domain explicitly requires correction history with "what changed, why, when, who." A single table would require complex versioning; a correction-log table is cleaner and consistent with the `academic_record_corrections` pattern.
3. **Report Card Entries as Junction Table** — Introduced as a supporting table to properly link report cards to their constituent academic results. Without this, the report card would have no structured content reference. The domain requires that reports be generated from operational academic records and remain reproducible.
4. **SLP Learning Units as Junction Table** — Introduced to represent the many-to-many relationship between Student Learning Plans and Learning Units with sequence and requirement attributes. The entity document defines "Learning Units" as a component of SLP minimum scope.
5. **Unified Academic Record Corrections** — One correction table with a `record_type` discriminator covers all correctable record types, per entity document assumption 4. Application logic determines which correction rules apply.
6. **Decomposed Class / No Standalone Learning Group Table** — Per the resolution of Question 6, "class" is decomposed across domains: administrative homeroom in Students, scheduling and room allocations in Operations, teacher authorization in Teaching, and academic learning anchored to `learning_units` and `academic_participations` with optional `section_label` on activities and participations. No standalone `learning_groups` table is introduced in Phase 2.
7. **No Grading Policy / Rubric Table** — The domain treats these as configuration within assessments and requirements. A separate table may emerge if complex policy management is needed, but it exceeds Phase-2 scope.
8. **No Submission Evaluation Table** — The domain mentions "submission evaluation" in correction workflows, but it is effectively the same as an `academic_result` linked to an assessment whose evidence traces to a submission. A separate table would duplicate the result lifecycle without clear benefit.
9. **UUID Primary Keys** — Consistent with the Student domain convention for future multi-campus/multi-tenant scenarios.
10. **VARCHAR for Enumerations** — Consistent with the Student domain convention. Reference tables can be introduced later if configuration-driven enumeration management is needed.
