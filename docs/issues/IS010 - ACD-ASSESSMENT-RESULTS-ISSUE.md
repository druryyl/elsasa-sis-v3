# ISSUE

## Metadata

ID: CR-ACD-ASSESSMENT-RESULTS
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage-2 Academic Assessment & Results Full Scope

## Source

Reported By: Drury
Reported Date: 2026-11-05

## Description

Implement the full scope of Stage 2 "Assessment & Results" under the Academic domain as planned in the SIS implementation roadmap (`docs/sub-modul-and-development-phases.md`).

The scope encompasses five cohesive submodules:
1. **Assessment Planning & Conduct (`ACD-WF-07`, `ACD-WF-20`, `UC-ACD-05`, `UC-ACD-07`, `SCR-ACD-02`, `SCR-STU-01`):**
   - Manage planned and conducted evaluation events (`assessments`) linked to approved learning units (`learning_unit_id`) and optionally to specific instructional learning activities (`learning_activity_id`).
   - Support standard assessment types (`formative`, `summative`, `diagnostic`, `portfolio`, `project`, `other`) and assessment methods (`written_test`, `oral`, `practical`, `project`, `portfolio`, `observation`, `other`).
   - Define scoring specifications: maximum score (`max_score`), minimum passing / mastery threshold (`passing_score` / KKM), evaluation criteria or rubric descriptors (`criteria`), and scheduled conduct date (`assessment_date`).
   - Configure clear reassessment policies (`reassessment_policy`: `capped_at_passing`, `highest_score`, `latest_attempt`, `average`, `none`) to govern how subsequent retake attempts are adjudicated.
   - Track assessment lifecycle statuses: `planned`, `conducted`, `cancelled`.
   - Support applicant entrance assessment administration (`ACD-WF-20`, `UC-ACD-07`), linking applicant evaluation evidence directly to Academics scoring workflows and transmitting authorized scored results to the Student Admission review process (`STD-WF-02`, `SCR-STU-01`), fulfilling the Stage 2 integration milestone.
2. **Evidence Collection & Evaluation / Scoring (`ACD-WF-08`, `UC-ACD-06`, `SCR-ACD-02`):**
   - Collect and track raw learning evidence (`assessment_evidence`) linked to assessments and students, originating from coursework submissions (`submissions`), examination papers, classroom observations, oral responses, or practical demonstrations.
   - Maintain strict conceptual and operational separation between raw evidence, provisional evaluations, approved results, and published results.
   - Support multiple evaluation models (`evaluation_model`): `numeric`, `letter_grade`, `pass_fail`, `competency`, `rubric`, `narrative`, and `completion`.
   - Preserve complete remediation history by recording distinct immutable result records for retakes (`attempt_number = 1, 2, 3...`, referencing `replaces_result_id`), flagging the operative outcome via `is_final = true` in strict accordance with the assessment's configured `reassessment_policy`.
   - Authorize classroom teachers to record provisional evaluations and exercise autonomous authority to release routine formative scores, rubric feedback, and qualitative developmental guidance directly to students and parents (`UC-ACD-06`).
3. **Result Moderation, Approval & Publication (`ACD-WF-09`, `UC-ACD-16`, `SCR-ACD-07`):**
   - Enable Academic Authorities and delegated Coordinators to review and moderate provisional summative results across classes and cohorts.
   - Enforce the formal governance lifecycle: Assigned Teacher records provisional evaluations (`provisional`) → Academic Authority formally approves summative results (`approved`) → authorized release to stakeholders (`published`).
   - Enforce the foundational rule that only approved results can be consumed by downstream academic progression, standing determination, promotion recommendations, report cards, or transcripts.
   - Publish approved results to student and parent self-service workspaces (`SCR-SELF-03`, `SCR-SELF-02`).
4. **Report Card & Transcript Production (`ACD-WF-12`, `UC-ACD-13`, `SCR-ACD-07`, `SCR-SELF-03`, `SCR-SELF-02`):**
   - Compile periodic academic reports (`report_cards` and `report_card_entries`) for active reporting periods (supporting Mid-Term Progress Reports `mid_term` and End-of-Semester Report Cards `end_of_term` — e.g., *Buku Rapor Merdeka*).
   - Enforce the boundary rule that report cards do not own grades or attendance: report cards compile and present approved academic results and attendance summaries sourced directly from authoritative operational records.
   - Compile and issue interim academic transcripts (`transcripts`: `unofficial` advising transcripts and interim `official` transcripts).
   - Capture immutable frozen document snapshots (`content_snapshot` serialized JSON/archive) upon approval and publication, guaranteeing 100% exact historical reproducibility under historical rules.
5. **Academic Record Correction Protocol (`ACD-WF-15`, `UC-ACD-19`, `SCR-ACD-07`):**
   - Enforce a formal, auditable correction protocol for approved academic results (`academic_record_corrections`).
   - Strictly prohibit silent or in-place overwriting of approved academic records.
   - Capture complete correction audit metadata: original value (`original_value`), new corrected value (`corrected_value`), mandatory justification reason (`reason`), supporting evidence (`evidence`), authorizing actor (`corrected_by`), timestamp (`corrected_date`), and materiality flag (`is_material`).
   - Restrict material grade alterations to Academic Authority authorization.
   - Trigger downstream alerts for re-evaluating academic standing or progression recommendations when underlying result records are modified.

Scope Boundaries & Deferrals:
The following capabilities belong to companion or subsequent modules and are explicitly out of scope for this issue:
- Curriculum frameworks, learning units, and foundational academic policy versions are owned by `Learning Structure & Requirements` (`IS008`).
- Student academic participation registration, daily instructional conduct, session attendance recording, coursework assignment issuance, and submission tracking are owned by `Participation & Learning Activities` (`IS009`).
- Individualized Student Learning Plans (exceptional accommodations, exemptions, SLP approval) belong to `Student Learning Plan`.
- Academic standing determination (good standing, probation), progression recommendations (promotion, retention), and academic honors conferral belong to `Standing & Progression` (`SCR-ACD-09`).
- Purpose-specific Academic Clearance (transfer-out academic clearance) is deferred to Stage 3 `Standing & Progression` (`SCR-ACD-08`, `ACD-WF-16`).
- Final official graduation transcript certification, graduation honors conferral notation, and accredited external transfer-in credit integration (`UC-STD-18`) are deferred to Stage 3 (`SCR-ACD-07` Stage 3 extension).
- Teacher profiling, credential verification, and official teacher assignment approvals belong to the `Teaching` domain (`Teacher Profile & Authorization`, `Assignment & Workload`).
- Operational examination timetables and room allocations belong to `Scheduling & Rooms` (`Operational` domain).

## Desired Outcome

1. Teachers can plan and schedule formative and summative assessments, specify rubrics/criteria and reassessment policies, score student evidence against rubrics, record provisional marks, and publish formative feedback directly to students and parents in `SCR-ACD-02` (`Coursework & Assessment Workbench`, `UC-ACD-05`, `UC-ACD-06`, `ACD-WF-07`, `ACD-WF-08`).
2. Academic Authorities and Coordinators can moderate provisional summative grades across class cohorts, grant formal approval, publish official results, and execute auditable score corrections in `SCR-ACD-07` (`Academic Records Workbench`, `UC-ACD-16`, `UC-ACD-19`, `ACD-WF-09`, `ACD-WF-15`).
3. Academic Coordinators can compile, format, certify, and issue periodic report cards (Mid-Term and End-of-Semester Buku Rapor) and interim transcripts with immutable snapshot archiving in `SCR-ACD-07` (`UC-ACD-13`, `ACD-WF-12`).
4. Students and parents/guardians can view approved and published academic results, teacher feedback, and certified report cards via self-service portals (`SCR-SELF-03` Student Learning Workspace, `SCR-SELF-02` Family Tasks Portal).
5. Academics can administer entrance assessments for registered applicants and transmit scored evaluation results to Student Admission (`ACD-WF-20`, `UC-ACD-07`), retiring Stage 1 manual score entry in favor of direct cross-domain assessment integration with `STD-WF-02` (`SCR-STU-01`).
6. Downstream Stage 2 and Stage 3 modules can consume approved academic results:
   - `Standing & Progression` can evaluate approved results against academic requirements to determine academic standing, honors, and promotion/retention recommendations.
   - `Student Placement & Status` can consume entrance assessment scores for admission review and academic standing for promotion execution.
   - `Finance Benefits` can verify academic eligibility/standing for scholarship renewal and tuition discounts.
7. Meets Stage 2 Completion Criteria:
   - **Criterion 4:** "An assignment has been issued, a submission received, and an evaluation recorded."
   - **Criterion 5:** "An academic result has been approved and published. A report card or progress report is produced."
   - **Criterion 15:** "An entrance assessment is conducted through an Academics workflow and its result is consumed by the Admission module as an authorized admission input."

## Current Situation

1. Schema status: The database currently provisions tables for Operations, Students, and Finance domains (Stage 1), and `IS008`/`IS009` define learning structure and participation/learning activity tables. The tables for Submodule 3 (`assessments`, `assessment_evidence`, `academic_results`, `academic_record_corrections`, `report_cards`, `report_card_entries`, `transcripts`) specified in `docs/entities/academic-entities-tables.md` (lines 390–660) are not yet provisioned.
2. Backend status: In `src/backend`, there are no domain models, entity configurations, repositories, or MediatR commands/queries for assessment planning, evidence collection, evaluation/scoring, result moderation/approval/publication, report card generation, transcript generation, or academic record corrections.
3. Frontend status: In `src/frontend`, `src/features/` does not yet contain feature implementations or workbench views for `SCR-ACD-02` (assessment planning & evaluation views), `SCR-ACD-07` (Academic Records Workbench), or result/report card viewing tabs in `SCR-SELF-03` and `SCR-SELF-02`.
4. Upstream prerequisites:
   - Operational Calendar & Periods (`IS001`, `IS003`) provides active academic period context and calendar milestones.
   - Student Enrollment & Identity (`IS004`) provides active enrolled student records.
   - Learning Structure & Requirements (`IS008`) provides approved learning units, passing criteria, and grading policies.
   - Participation & Learning Activities (`IS009`) provides active student participations, learning activity contexts, coursework assignments, and student submissions.

## Evidence

- Roadmap Specification: `docs/sub-modul-and-development-phases.md` (Stage 2, Module: Assessment & Results — full scope; Section D Stage 2 Criteria 4, 5, 15; Section E Cross-Domain Dependencies).
- Academic Domain Blueprint: `docs/domains/academic-domain.md` (Section: Assessment; Section: Academic Results and History; Section: Academic Record Correction).
- Academic Entities Specification: `docs/entities/academic-entities.md` (Submodule 3 — Assessment & Results: `Assessment`, `Assessment Evidence`, `Academic Result`, `Academic Record Correction`, `Report Card`, `Transcript`).
- Academic Database Tables Specification: `docs/entities/academic-entities-tables.md` (Submodule 3: `assessments`, `assessment_evidence`, `academic_results`, `academic_record_corrections`, `report_cards`, `report_card_entries`, `transcripts`, lines 390–660).
- Operational Workflow Discovery: `docs/operational-workflow-discovery.md` (`ACD-WF-07`, `ACD-WF-08`, `ACD-WF-09`, `ACD-WF-12`, `ACD-WF-15`, `ACD-WF-20`; Section B.2; Section C Stage 2).
- Use Cases Specification: `docs/use-cases.md` (`UC-ACD-05`, `UC-ACD-06`, `UC-ACD-07`, `UC-ACD-13`, `UC-ACD-16`, `UC-ACD-19`).
- Screen Inventory: `docs/screen-inventory.md` (`SCR-ACD-02`, `SCR-ACD-07`, `SCR-SELF-03`, `SCR-SELF-02`, `SCR-STU-01`).
- Existing Baseline Code: `src/backend/Elsasa.Sis.sln`, `src/frontend/src/features/`.

## Notes

- This artifact serves as the formal change intake. It defines WHAT capability is requested and the operational outcomes expected, maintaining solution neutrality without prescribing technical architecture, database migration designs, API schemas, or UI component implementations.
- Governance boundary discipline: Teacher autonomy applies to routine classroom scoring, rubric breakdowns, and formative developmental feedback (`UC-ACD-06`). Formal summative approval, result publication, and official certification belong strictly to Academic Authority / Academic Coordinator review (`UC-ACD-16`, `UC-ACD-13`, `SCR-ACD-07`).
- Immutability and audit discipline: Approved academic results and published report snapshots cannot be silently overwritten. Reassessments generate new immutable attempt records, and post-approval adjustments require explicit, authorized entries in `academic_record_corrections`.
- Technical feasibility assessment, domain modeling, feature decomposition, and architecture updates will be performed in downstream SDLC stages.
