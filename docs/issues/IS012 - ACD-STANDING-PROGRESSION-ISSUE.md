# ISSUE

## Metadata

ID: CR-ACD-STANDING-PROGRESSION
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage-2 Academic Standing & Progression Core Scope

## Source

Reported By: Drury
Reported Date: 2026-11-07

## Description

Implement the core scope of Stage 2 "Standing & Progression" under the Academic domain as planned in the SIS implementation roadmap (`docs/sub-modul-and-development-phases.md`).

The scope encompasses five cohesive submodules:

1. **Learning Progress & Completion (`ACD-WF-11`, `UC-ACD-12`, `SCR-ACD-07`):**
   - Record and track student advancement toward defined learning expectations for specific learning units (`learning_progress`).
   - Support the Phase-2 minimal progress model with required and optional business states: `not_started`, `in_progress`, `completed`, `needs_remediation`, `exempted` (percentage-based progress tracking is prohibited in Phase 2).
   - Ensure learning progress strictly originates from authorized operational evidence (`assessment_evidence`, `learning_work`, or an explicit `authorized_decision`); completion must never be inferred solely from period boundaries, administrative placement, or attendance alone.
   - Support completion recorded outside academic period boundaries with an explicit completion date (`completion_date`), accommodating self-paced modular packets, holiday completion, and post-term remediation.
   - Support direct recording of transferred or externally recognized learning (`status = 'completed'`, `basis = 'authorized_decision'`).
   - Enable Academic Coordinators to review completion criteria and formally conclude learning unit participation (`UC-ACD-12`).

2. **Academic Standing Determination (`ACD-WF-13`, `UC-ACD-17`, `SCR-ACD-09`):**
   - Record formal academic conclusions regarding a student's overall academic condition within an academic period (`academic_standings`).
   - Enforce the foundational domain principle that Academic Standing is an academic determination based on policy and evidence, not a simple numerical grade or GPA.
   - Support the standard standing classification model: `good_standing`, `conditional_standing`, and `not_in_good_standing`.
   - Capture applicable governing policy references (`policy_reference`) and mandatory stipulations/conditions when standing is conditional (`conditions`).
   - Embed governance attributes directly on decision records for exceptions: `is_exception`, `exception_reason`, `committee_reference` (e.g., formal decree or meeting minutes reference such as *SK Dewan Guru*), and approving `authorized_by`.
   - Provide authoritative academic standing records to downstream progression, retention, and graduation processes without altering the student's administrative status.

3. **Promotion & Retention Recommendation (`ACD-WF-13`, `UC-ACD-17`, `SCR-ACD-09`, `SCR-STU-03`):**
   - Record Academics' formal evaluation and recommendations regarding student advancement to the next educational level or grade (`promotion_recommendations`).
   - Support standard recommendation outcomes: `promotion_eligible`, `retention_recommended`, and `conditional_promotion`.
   - Record current level (`from_level`), recommended destination level (`recommended_to_level`, nullable for retention), mandatory conditional requirements (`conditions`), supporting evidence summary (`supporting_evidence`), and linked academic standing (`academic_standing_id`).
   - Embed governance attributes directly on exception records (`is_exception`, `exception_reason`, `committee_reference`, `authorized_by`).
   - Enforce the strict cross-domain boundary rule: Academics owns promotion and retention eligibility recommendations; the Students domain is the sole authority that executes official administrative promotion or retention transitions (`STD-WF-08`, `SCR-STU-03`).

4. **Academic Intervention (`ACD-WF-10`, `UC-ACD-11`, `SCR-ACD-06`):**
   - Record and manage structured academic actions taken when a student requires additional learning support or educational challenge (`academic_interventions`).
   - Support standard intervention types: `remediation`, `reassessment`, `tutoring_recommendation`, `extension_work`, `recovery_plan`, `learning_plan_adjustment`, and `academic_warning`.
   - Implement the dual-trigger intake model:
     - System Early-Warning alerts triggered by objective academic indicators (`assessment_below_threshold`, `missing_work`, `attendance_concern`, `standing_concern`).
     - Staff-initiated educator referrals (`teacher_referral`) based on classroom observations, learning difficulties, or gifted/accelerated learning needs.
   - Manage the intervention lifecycle: `planned`, `in_progress`, `completed`, `cancelled`.
   - Capture detailed intervention plans, target learning units, initiating actors, milestone dates, recorded outcomes, and notes.
   - Enforce domain boundaries: Academics owns interventions directly tied to academic performance and learning; behavioural, psychological, counselling, and pastoral interventions belong strictly to the Development domain (`Development Referral & Wellbeing`). Academic interventions do not independently alter official administrative enrollment status.

5. **Academic Achievement Recognition (`ACD-WF-19`, `UC-ACD-21`, `SCR-ACD-09`):**
   - Formally record and confer achievements derived from academic learning or assessment (`academic_achievements`).
   - Support standard academic achievement types: `subject_distinction`, `academic_honors` (e.g., honor roll / *Juara Umum*), `mastery_recognition`, `academic_certification`, and `academic_competition` (curriculum-recognized academic contests).
   - Capture achievement title, linked learning unit (where applicable), academic period, evaluation criteria, supporting academic result evidence, recognizing authority, and recognition date.
   - Enforce domain boundaries: Academics owns achievements derived from academic learning and assessment; Development owns extracurricular, non-academic, and service achievements.

### Scope Boundaries & Explicit Deferrals

The following capabilities belong to companion modules, subsequent stages, or separate domains and are explicitly out of scope for this issue:

- **Graduation Eligibility Determination (`ACD-WF-14`, `UC-ACD-18`, `graduation_eligibilities`):** Formally determining cumulative graduation eligibility against multi-year program requirements is deferred to **Stage 3** as planned in `sub-modul-and-development-phases.md`.
- **Purpose-Specific Academic Clearance (`ACD-WF-16`, `ACD-WF-17`, `UC-ACD-14`, `UC-ACD-20`, `SCR-ACD-08`, `academic_clearances`):** Evaluating academic clearance for transfer-out is deferred to **Stage 3** as planned in `sub-modul-and-development-phases.md`.
- **Administrative Lifecycle Transition Execution:** Executing official grade promotion, retention, campus/program reassignment, or homeroom cohort rollover belongs exclusively to the `Student Placement & Status` module in the Students domain (`SCR-STU-03`, `STD-WF-08`). Academics only provides the recommendation.
- **Individualized Learning Plans (SLP):** Formulation, adjustments (accommodations, substitutions, exemptions), and formal approvals of individualized student learning plans belong to `Student Learning Plan` (`SCR-ACD-04`).
- **Foundational Curriculum & Learning Units:** Definition and maintenance of curriculum structures, syllabi, and requirement versioning belong to `Learning Structure & Requirements` (`IS008`).
- **Instructional Sessions & Classroom Attendance:** Conducting sessions, logging daily curriculum delivery, and recording instructional attendance belong to `Participation & Learning Activities` (`IS009`).
- **Assessment Scoring & Report Cards:** Assessment events, rubric scoring, result approvals, grade corrections, and report cards (*Buku Rapor*) belong to `Assessment & Results` (`IS010`).
- **Financial Merit Benefits:** Evaluating scholarships, tuition fee discounts, or benefit adjustments tied to academic honors belongs exclusively to `Finance Benefits` (Stage 2).
- **Non-Academic Support & Achievements:** Disciplinary actions, counselling cases, pastoral support, and extracurricular accolades belong exclusively to the Development domain (`Behaviour & Discipline`, `Counselling & Wellbeing`, `Extracurricular & Achievement`, Stage 3).

## Desired Outcome

1. Academic Coordinators can track student advancement across learning units, verify completion criteria, record completion outside period boundaries where applicable, and formally conclude learning unit participation in `SCR-ACD-07` (`UC-ACD-12`, `ACD-WF-11`).
2. Academic Coordinators and Teachers can identify students needing academic assistance or extension, create targeted intervention plans via system alerts or educator referrals, track remediation progress, and record outcomes in `SCR-ACD-06` (`Academic Intervention Workbench`, `UC-ACD-11`, `ACD-WF-10`).
3. Academic Authorities can evaluate cohort-wide academic progress, review approved assessment results, and formally determine periodic academic standing (`good_standing`, `conditional_standing`, `not_in_good_standing`) with embedded exception governance in `SCR-ACD-09` (`Cohort Academic Review`, `UC-ACD-17`, `ACD-WF-13`).
4. Academic Authorities can issue formal promotion and retention recommendations (`promotion_eligible`, `retention_recommended`, `conditional_promotion`) with supporting evidence and conditions in `SCR-ACD-09`, making these recommendations immediately available to the Students domain for administrative execution in `SCR-STU-03` (`UC-ACD-17`, `ACD-WF-13`).
5. Academic Authorities can formally confer and publish academic honors, subject distinctions, and academic achievements linked to approved results in `SCR-ACD-09` (`UC-ACD-21`, `ACD-WF-19`), surfacing them on student profiles and making them available for Finance merit benefit evaluation.
6. Fulfills the following SIS Implementation Roadmap completion criteria:
   - **Stage 2 Criterion 6:** "Academic standing has been determined for at least one student. A promotion or retention recommendation has been issued."
   - **Stage 2 Criterion 9 (Enablement):** Enables "A student has been promoted or retained based on an academic recommendation" by delivering authoritative progression recommendations to the Students domain.

## Current Situation

1. **Database Schema Status:**
   - The database currently provisions tables for Operations, Students, and Finance domains (Stage 1), as well as Academic Learning Structure (`learning_units`, `academic_requirements`) and Participation/Activities (`academic_participations`, `learning_activities`, `attendance_records`, `coursework_tasks`, `submissions`).
   - The tables for Submodule 4 specified in `docs/entities/academic-entities-tables.md` (lines 674–932) — `learning_progress`, `academic_standings`, `promotion_recommendations`, `academic_interventions`, and `academic_achievements` — are not yet provisioned in the database schema or EF migrations.
2. **Backend Status:**
   - In `src/backend`, there are no domain entities, entity type configurations, repository contracts/implementations, or MediatR commands/queries for learning progress completion, academic standing determination, promotion/retention recommendations, academic interventions, or academic achievement recognition.
3. **Frontend Status:**
   - In `src/frontend`, feature modules and UI components for `SCR-ACD-06` (`Academic Intervention Workbench`) and `SCR-ACD-09` (`Cohort Academic Review`) do not yet exist.
   - The participation conclusion and progress review tabs in `SCR-ACD-07` (`Academic Records Workbench`) are not yet implemented.
4. **Upstream Prerequisites Status:**
   - Operational Calendar & Periods (`IS001`, `IS003`) is active, providing operational academic period context.
   - Student Enrollment & Identity (`IS004`) is active, providing enrolled student records.
   - Learning Structure & Requirements (`IS008`) is implemented, providing approved learning units and academic requirements.
   - Participation & Learning Activities (`IS009`) is implemented, providing student participations, activities, and submissions.
   - Assessment & Results (`IS010`) provides approved academic results required as authoritative evidence for progress, standing, and recommendations.

## Evidence

- **Roadmap Specification:** `docs/sub-modul-and-development-phases.md` (Stage 2: Module Standing & Progression — core submodules; Section D Stage 2 Criteria 6 and 9; Section E Cross-Domain Dependencies).
- **Academic Domain Blueprint:** `docs/domains/academic-domain.md` (Section: Academic Intervention, lines 295–310; Section: Academic Achievements, lines 331–342; Section: Progression, lines 1668–1674; Section: Core Responsibilities, lines 36–45).
- **Academic Entities Specification:** `docs/entities/academic-entities.md` (Submodule 4 — Standing & Progression: `Learning Progress`, `Academic Standing`, `Promotion Recommendation`, `Academic Intervention`, `Academic Achievement`, lines 300–399).
- **Academic Database Tables Specification:** `docs/entities/academic-entities-tables.md` (Submodule 4: `learning_progress`, `academic_standings`, `promotion_recommendations`, `academic_interventions`, `academic_achievements`, lines 674–932).
- **Operational Workflow Discovery:** `docs/operational-workflow-discovery.md` (`ACD-WF-10` Review Progress and Initiate Academic Intervention, `ACD-WF-11` Complete Learning, `ACD-WF-13` Determine Academic Standing and Progression Recommendation, `ACD-WF-19` Recognize Academic Achievement; Section B.2; Section C Stage 2).
- **Use Cases Specification:** `docs/use-cases.md` (`UC-ACD-11` Initiate Academic Intervention, `UC-ACD-12` Conclude Learning Unit Participation, `UC-ACD-17` Determine Academic Standing and Progression, `UC-ACD-21` Confer Academic Achievement).
- **Screen Inventory:** `docs/screen-inventory.md` (`SCR-ACD-06` Academic Intervention Workbench, `SCR-ACD-07` Academic Records Workbench, `SCR-ACD-09` Cohort Academic Review, `SCR-STU-03` Student Lifecycle Workbench).
- **Existing Baseline Codebase:** `src/backend/Elsasa.Sis.sln`, `src/frontend/src/features/`.

## Notes

- This artifact represents the formal change intake for Stage 2 Standing & Progression. It defines WHAT capabilities are requested and the expected operational outcomes, maintaining solution neutrality without prescribing technical implementation details, API schemas, or software architecture patterns.
- Clear governance separation:
  - Routine learning completion and progress updates are coordinated by Academic Coordinators (`UC-ACD-12`, `SCR-ACD-07`).
  - Academic intervention planning and monitoring are managed by Academic Coordinators in consultation with Teachers (`UC-ACD-11`, `SCR-ACD-06`).
  - Academic standing determination, promotion/retention recommendations, and academic honors conferral strictly require Academic Authority authorization (`UC-ACD-17`, `UC-ACD-21`, `SCR-ACD-09`).
- Clear cross-domain authority boundaries:
  - Academics provides progression recommendations; the Students domain executes official promotion or retention administrative lifecycle changes (`SCR-STU-03`).
  - Academics owns academic learning interventions; the Development domain owns behavioural, wellbeing, counselling, and non-academic interventions.
- Technical feasibility assessment, domain modeling, and implementation planning will be conducted in subsequent SDLC stages.
