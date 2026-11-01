# SIS — Module Decomposition and Three-Stage Implementation Roadmap

---

## A. Executive Summary

This recommendation decomposes the SIS into **20 modules** across the six existing domains, then sequences them into **three implementation stages** driven by operational dependency and school-readiness priorities.

**Decomposition strategy.** Each domain is divided into modules that correspond to cohesive business capabilities — not individual entities or screens. Tightly related workflows remain together (e.g., admission and enrollment stay in one module because enrollment depends entirely on admission). Submodules represent meaningful functional areas within a module. The six domain boundaries documented in the source materials are preserved without modification; no cross-domain redistribution is proposed because the documents demonstrate well-defined ownership with explicit inputs/outputs.

**Roadmap logic.** The three stages follow operational dependency order, not an even distribution of domains:

| Stage | Theme | Rationale |
|---|---|---|
| **Stage 1** | **Student foundation + operational context + basic finance** | No domain can function without student identity, academic-period context, and basic financial obligations. This stage makes admission → enrollment → fee assessment operationally possible. |
| **Stage 2** | **Academic learning + teaching assignments + full finance** | Once students exist and the calendar is in place, the school must teach and track learning. Teaching assignment must precede learning activities. Full finance (scholarships, receivables, clearances) completes the financial picture. |
| **Stage 3** | **Student development + advanced lifecycle + operational maturity** | Behaviour, counselling, extracurriculars, and advanced cross-domain clearance workflows (transfer-out, graduation) depend on mature Student, Academic, and Finance capabilities. Operational timetabling and announcements become valuable once learning activities and teaching assignments exist. |

A module may appear in more than one stage only when a clear subset of its capabilities can be delivered independently (e.g., Operations Calendar & Periods in Stage 1; Operations Scheduling in Stage 2; Operations Events & Announcements in Stage 3).

---

## B. Module Decomposition

| Domain | Module | Responsibility | Submodules | Dependencies |
|---|---|---|---|---|
| **Student** | **Admission** | Manage applicant identity and the admission lifecycle from application to acceptance or rejection. Entrance assessment evidence is provided by Academics (Stage 2) and entered as external evidence in Stage 1. | • Application Registration<br>• Document & Identity Verification<br>• Admission Review & Decision<br>• Entrance Assessment Evidence Intake (Stage 1: manual/external input; Stage 2: integrated from Academics) | Operations (admission periods), Academics (entrance assessment result, when available) |
| **Student** | **Enrollment & Identity** | Establish official student identity, assign student number, complete enrollment, and maintain ongoing identity records. | • Enrollment Execution<br>• Student Number Assignment<br>• Student Identity Maintenance<br>• Parent & Guardian Management | Admission (accepted applicant), Finance (enrollment payment confirmation when policy requires) |
| **Student** | **Placement & Status** | Own the student's administrative placement (including homeroom cohort placement), official lifecycle status (active, on leave, leave expired, withdrawn, transferred, graduated, alumni), and annual re-enrollment. | • Administrative Placement (incl. homeroom cohort)<br>• Student Status Management<br>• Re-enrollment<br>• Leave of Absence<br>• Promotion & Retention Execution<br>• Internal Transfer<br>• External Transfer (Out & In)<br>• Withdrawal<br>• Graduation Execution<br>• Alumni<br>• Administrative History | Enrollment & Identity (student must exist), Academics (promotion/graduation eligibility, academic clearance), Finance (enrollment payment confirmation when policy requires, financial clearance for transfer-out and graduation), Operations (re-enrollment period) |
| **Academic** | **Learning Structure & Requirements** | Define and maintain the curriculum-neutral learning structure, academic requirements, and versioned policies that govern learning. | • Learning Structure Management<br>• Academic Requirements & Policy Versioning<br>• Curriculum Configuration | Operations (academic year and period) |
| **Academic** | **Participation & Learning Activities** | Manage student academic participation, learning activities, attendance, assignments, and submissions. | • Academic Participation<br>• Learning Activity Conduct<br>• Student Attendance<br>• Assignment & Submission Management | Learning Structure & Requirements (what to teach), Student Enrollment & Identity (eligible students), Teaching Assignment (authorized teacher), Operations (schedule/room context) |
| **Academic** | **Assessment & Results** | Manage assessment planning, evidence collection, evaluation, result approval, publication, report cards, and transcripts. | • Assessment Planning & Conduct<br>• Evaluation & Scoring<br>• Result Approval & Publication<br>• Report Card & Transcript Production<br>• Academic Record Correction | Participation & Learning Activities (evidence from learning) |
| **Academic** | **Standing & Progression** | Determine academic standing, progression recommendations, graduation eligibility, academic clearance, and academic intervention. | • Learning Progress & Completion<br>• Academic Standing Determination<br>• Promotion & Retention Recommendation<br>• Graduation Eligibility Determination<br>• Academic Clearance<br>• Academic Intervention<br>• Academic Achievement Recognition | Assessment & Results (approved results), Student Placement & Status (consumes recommendations) |
| **Academic** | **Student Learning Plan** | Manage individualized or exceptional learning plans, accommodations, substitutions, and exemptions. | • SLP Creation & Revision<br>• Academic Plan Adjustment<br>• SLP Approval & Governance | Learning Structure & Requirements, Student Enrollment & Identity |
| **Teaching** | **Teacher Profile & Authorization** | Manage the teaching profile, capabilities, qualification evidence, verification, and teaching authorization lifecycle. | • Teaching Profile Management<br>• Teaching Capability<br>• Qualification Evidence & Verification<br>• Teaching Authorization Lifecycle | (None within SIS — may consume future HR/employee identity) |
| **Teaching** | **Assignment & Workload** | Receive teaching demand, evaluate eligibility, approve teaching assignments (including homeroom advisory assignments), manage workload, and handle substitution and reassignment. | • Teaching Demand Intake<br>• Teacher Eligibility Evaluation<br>• Teaching Assignment Approval (incl. homeroom advisory)<br>• Workload & Capacity Management<br>• Substitution & Reassignment<br>• Assignment Completion<br>• Teaching History | Teacher Profile & Authorization (eligible teachers), Academic Participation & Learning Activities (demand source), Operations (schedule conflicts) |
| **Finance** | **Obligations & Billing** | Manage financial policies, charge rules, obligation assessment, billing, and statements. | • Financial Policy & Charge Rules<br>• Student Financial Account<br>• Obligation Assessment & Issuance<br>• Billing & Statement Communication | Student Enrollment & Identity (student + guardian context), Operations (academic periods) |
| **Finance** | **Benefits** | Manage scholarships, discounts, waivers — application, approval, benefit stacking, renewal, and revocation. | • Scholarship Processing<br>• Discount & Waiver Processing<br>• Benefit Stacking & Priority<br>• Renewal & Revocation | Obligations & Billing (applicable obligations), Academics/Development (recommendation/evidence only) |
| **Finance** | **Payments & Receivables** | Manage payment receipt, confirmation, allocation, reallocation, receivables, overdue status, reminders, and payment arrangements. | • Payment Receipt & Confirmation<br>• Payment Allocation & Reallocation<br>• Receivables & Overdue Management<br>• Payment Arrangement<br>• Credit & Overpayment Management | Obligations & Billing (obligations to settle) |
| **Finance** | **Refunds & Adjustments** | Manage refund requests, approval, execution, financial adjustments, reversals, disputes, and record correction. | • Refund Processing<br>• Financial Adjustment & Reversal<br>• Dispute Management<br>• Financial Record Correction | Payments & Receivables (refundable value) |
| **Finance** | **Financial Clearance** | Determine purpose-specific financial clearance for transfer-out, graduation, and configurable purposes. | • Clearance Request & Evaluation<br>• Clearance Determination & Exception<br>• Clearance Correction | Payments & Receivables (outstanding obligations), Student Placement & Status (requests clearance) |
| **Development** | **Behaviour & Discipline** | Manage behaviour observations, positive recognition, incidents, and formal discipline cases. | • Behaviour Observation & Recognition<br>• Incident Reporting & Triage<br>• Discipline Case Management<br>• Cross-Domain Recommendation | Student Enrollment & Identity (student reference) |
| **Development** | **Counselling & Wellbeing** | Manage counselling referrals, wellbeing concerns, restricted case records, and external referrals. | • Development Referral<br>• Counselling Case Management<br>• Wellbeing Support<br>• Non-Academic Intervention | Student Enrollment & Identity (student reference) |
| **Development** | **Extracurricular & Achievement** | Manage extracurricular programs, participation, and non-academic achievements. | • Extracurricular Program Management<br>• Extracurricular Participation<br>• Non-Academic Achievement Recognition | Student Enrollment & Identity, Operations (event/room context) |
| **Operational** | **Calendar & Periods** | Manage academic year, operational periods, school calendar, holidays, closures, and shared date context. | • Academic Year Management<br>• Operational Period Management<br>• School Calendar Management<br>• Closure Coordination | (None — foundational) |
| **Operational** | **Scheduling & Rooms** | Manage operational activity requests, feasibility review, timetable, scheduled occurrences, and room context/allocation (including homeroom room assignment). | • Room & Location Context (incl. homeroom room assignment)<br>• Room Availability & Allocation<br>• Operational Activity Request<br>• Operational Feasibility Review<br>• Timetable & Scheduled Occurrences<br>• Conflict & Disruption Management | Calendar & Periods, Teaching Assignment & Workload (teacher availability), Academic Participation & Learning Activities (source activities) |
| **Operational** | **Events & Announcements** | Manage school event operational lifecycle, operational announcements, and operational approval/exception workflows. | • School Event Coordination<br>• Announcement Publication & Withdrawal<br>• Operational Approval & Exception<br>• Operations History & Correction | Calendar & Periods, Scheduling & Rooms |

---

## C. Three-Stage Roadmap

### Stage 1 — Student Foundation, Operational Context, and Basic Finance

**Stage Objective:** Enable the school to admit applicants, enroll students, assign administrative placements, define the academic calendar, and assess basic financial obligations. At the end of this stage, the school can operate its intake process and know who its students are, when the school year runs, and what each student owes.

| Stage | Domain | Module / Submodule | Prerequisites | Operational Deliverable |
|---|---|---|---|---|
| 1 | **Operational** | Calendar & Periods — full scope | None | Academic year, terms, holidays, and school calendar are defined and published. All domains have shared date context. |
| 1 | **Student** | Admission — full scope (entrance assessment evidence entered as manual/external input; full Academics integration deferred to Stage 2) | Calendar & Periods (admission periods) | Applicants can apply, submit documents, and receive admission decisions. Entrance assessment scores are recorded as external evidence attached to the application. |
| 1 | **Student** | Enrollment & Identity — full scope | Admission (accepted applicant) | Accepted applicants are enrolled, assigned student numbers, and officially become students. Parent/guardian records are established. |
| 1 | **Student** | Placement & Status — core submodules only: Administrative Placement, Student Status Management, Administrative History | Enrollment & Identity | Students receive initial placement (campus, level, program, cohort). Active status is managed. Historical records begin. |
| 1 | **Finance** | Obligations & Billing — full scope | Enrollment & Identity, Calendar & Periods | Financial policies are defined. Charges are assessed (application fee, enrollment fee, tuition). Bills/statements are communicated. |
| 1 | **Finance** | Payments & Receivables — core submodules: Payment Receipt & Confirmation, Payment Allocation & Reallocation | Obligations & Billing | Payments are received, confirmed, and allocated against obligations. Enrollment payment confirmation is available. |

---

### Stage 2 — Academic Learning, Teaching, and Expanded Finance

**Stage Objective:** Enable the school to define curriculum, assign teachers, conduct learning activities, record attendance and assignments, produce assessment results, report cards, and manage full financial operations including scholarships, receivables, refunds, and payment arrangements. At the end of this stage, the school can run its complete teaching-and-learning cycle and financial management.

| Stage | Domain | Module / Submodule | Prerequisites | Operational Deliverable |
|---|---|---|---|---|
| 2 | **Academic** | Learning Structure & Requirements — full scope | Calendar & Periods | Curriculum and learning units are defined with academic requirements and effective versions. |
| 2 | **Academic** | Participation & Learning Activities — full scope | Learning Structure & Requirements, Enrollment & Identity, Teaching Assignment & Workload (concurrent development; may use provisional teacher context initially) | Students are assigned to learning. Learning activities occur. Attendance is recorded. Assignments are issued and submitted. |
| 2 | **Academic** | Assessment & Results — full scope | Participation & Learning Activities | Assessments are planned and conducted. Results are evaluated, approved, published. Report cards and transcripts are produced. |
| 2 | **Academic** | Standing & Progression — core submodules: Learning Progress & Completion, Academic Standing Determination, Promotion & Retention Recommendation, Academic Intervention, Academic Achievement Recognition | Assessment & Results | Academic standing is determined. Promotion/retention recommendations are issued to Students. Academic achievements are recognized. |
| 2 | **Academic** | Student Learning Plan — full scope | Learning Structure & Requirements, Enrollment & Identity | Individualized learning plans are created, approved, and revised for exceptional cases. |
| 2 | **Teaching** | Teacher Profile & Authorization — full scope | None | Teachers are profiled, qualifications verified, and teaching authorizations granted. |
| 2 | **Teaching** | Assignment & Workload — full scope | Teacher Profile & Authorization, Participation & Learning Activities (demand source) | Teaching demand is received from Academics. Eligible teachers are assigned. Workload is tracked. Substitutions are handled. |
| 2 | **Student** | Placement & Status — additional submodules: Leave of Absence, Promotion & Retention Execution, Internal Transfer, **Re-enrollment** | Stage 1 Placement & Status, Academic Standing & Progression (for promotion), Finance (re-enrollment payment confirmation when policy requires) | Students can be placed on leave, returned from leave, promoted, retained, or internally transferred based on academic recommendations. Re-enrollment for the next academic period is confirmed administratively. |
| 2 | **Student** | Admission — entrance assessment integration: Academics-provided assessment results are now consumed directly (replacing Stage 1 manual input) | Academic Assessment & Results (concurrent Stage 2) | Entrance assessment for new applicants is conducted as an Academics workflow; result is provided to admission review as an authorized input. |
| 2 | **Finance** | Benefits — full scope | Obligations & Billing | Scholarships, discounts, and waivers are processed and applied to obligations. |
| 2 | **Finance** | Payments & Receivables — remaining submodules: Receivables & Overdue Management, Payment Arrangement, Credit & Overpayment Management | Stage 1 Payments & Receivables | Full receivables management, overdue tracking, reminders, payment arrangements, and credit management are operational. |
| 2 | **Finance** | Refunds & Adjustments — full scope | Payments & Receivables | Refunds are requested, approved, executed. Financial adjustments and disputes are managed. |
| 2 | **Operational** | Scheduling & Rooms — full scope | Calendar & Periods, Teaching Assignment & Workload, Academic Participation & Learning Activities | Operational timetable is created. Rooms are allocated. Learning activities are scheduled. Conflicts are managed. |

---

### Stage 3 — Student Development, Advanced Lifecycle, and Operational Maturity

**Stage Objective:** Enable the school to manage behaviour, discipline, counselling, wellbeing, extracurricular programs, non-academic achievements, and the advanced lifecycle transitions of transfer-out, withdrawal, and graduation (including cross-domain clearances). Complete operational coordination with events and announcements. At the end of this stage, the SIS supports the full breadth of school operations.

| Stage | Domain | Module / Submodule | Prerequisites | Operational Deliverable |
|---|---|---|---|---|
| 3 | **Development** | Behaviour & Discipline — full scope | Enrollment & Identity | Behaviour is observed and recognized. Incidents are reported and triaged. Discipline cases are managed with fair process. Cross-domain recommendations are issued. |
| 3 | **Development** | Counselling & Wellbeing — full scope | Enrollment & Identity | Referrals are routed. Counselling and wellbeing cases are managed with confidentiality controls. Interventions are planned and reviewed. |
| 3 | **Development** | Extracurricular & Achievement — full scope | Enrollment & Identity, Scheduling & Rooms (event/room context) | Extracurricular programs are established. Students participate. Non-academic achievements are recognized. |
| 3 | **Academic** | Standing & Progression — remaining submodules: Graduation Eligibility Determination, Academic Clearance | Assessment & Results, Stage 2 Standing & Progression | Graduation eligibility is determined. Academic clearance for transfer-out is provided. |
| 3 | **Finance** | Financial Clearance — full scope | Payments & Receivables, Student Placement & Status | Purpose-specific financial clearance for transfer-out and graduation is determined. |
| 3 | **Student** | Placement & Status — remaining submodules: External Transfer (Out & In), Withdrawal, Graduation Execution, Alumni | Stage 2 Placement & Status, Academic Clearance, Financial Clearance | External transfers execute with academic and financial clearance. Withdrawal is formalized. Graduation is executed with eligibility and clearance. Alumni status is activated. |
| 3 | **Operational** | Events & Announcements — full scope | Calendar & Periods, Scheduling & Rooms | School events are coordinated end-to-end. Announcements are published, corrected, and withdrawn. Operational approvals and exceptions are managed. |

---

## D. Stage Completion Criteria

### Stage 1 — Student Foundation, Operational Context, and Basic Finance

| # | Criterion |
|---|---|
| 1 | An academic year with at least one term/semester and a school calendar (including holidays) is approved and published. |
| 2 | An applicant can register an admission application, undergo review, and receive an acceptance or rejection decision. |
| 3 | An accepted applicant can be enrolled, assigned a unique student number, and placed in an administrative location (campus, level, program). |
| 4 | Parent/guardian relationships are recorded for enrolled students. |
| 5 | Student status is set to Active and administrative history records the complete intake sequence. |
| 6 | At least one financial policy and charge rule (e.g., tuition) is defined and an obligation is assessed against an enrolled student. |
| 7 | A bill or statement communicating obligations is produced for an authorized recipient. |
| 8 | A payment is received, confirmed, and allocated against an outstanding obligation. |
| 9 | Enrollment payment confirmation is available to Students when policy requires it. |
| 10 | All domain events for the above are recorded and historically traceable. |

### Stage 2 — Academic Learning, Teaching, and Expanded Finance

| # | Criterion |
|---|---|
| 1 | A learning structure with at least one learning unit and academic requirements is approved and effective. |
| 2 | An active student has academic participation in at least one learning unit. |
| 3 | A learning activity has been conducted with attendance recorded. |
| 4 | An assignment has been issued, a submission received, and an evaluation recorded. |
| 5 | An academic result has been approved and published. A report card or progress report is produced. |
| 6 | Academic standing has been determined for at least one student. A promotion or retention recommendation has been issued. |
| 7 | A teacher has a verified profile, granted authorization, and an approved teaching assignment linking them to a learning unit. |
| 8 | Teaching workload is visible and substitution for an absent teacher is recorded. |
| 9 | A student has been promoted or retained based on an academic recommendation. |
| 10 | A scholarship or discount is approved and applied to an obligation; an overdue receivable triggers a reminder; a payment arrangement is approved; a refund is processed. |
| 11 | An operational timetable with at least one scheduled occurrence and room allocation is published. |
| 12 | A student learning plan is created, approved, and governs an exceptional student's learning. |
| 13 | Leave of absence is placed and returned successfully. Internal transfer is executed. |
| 14 | A student re-enrollment is confirmed for the next academic period; re-enrollment history is preserved and a financial trigger (if policy requires) is available to Finance. |
| 15 | An entrance assessment is conducted through an Academics workflow and its result is consumed by the Admission module as an authorized admission input. |

### Stage 3 — Student Development, Advanced Lifecycle, and Operational Maturity

| # | Criterion |
|---|---|
| 1 | A behaviour observation (positive and concerning) is recorded. An incident is triaged and a discipline case is investigated and determined. |
| 2 | A development referral is accepted, routed, and results in a counselling or wellbeing case with appropriate confidentiality controls. |
| 3 | A non-academic intervention plan is established, reviewed, and closed. |
| 4 | An extracurricular program is established with student participation recorded. A non-academic achievement is recognized. |
| 5 | Graduation eligibility is determined by Academics and provided to Students. |
| 6 | Academic clearance for purpose Transfer Out is requested, determined, and consumed. |
| 7 | Financial clearance for purposes Transfer Out and Graduation is requested, determined, and consumed. |
| 8 | An external transfer-out is executed with both academic and financial clearance Granted. |
| 9 | A withdrawal is formally processed with proper authorization and history. |
| 10 | Graduation is executed with academic eligibility and financial clearance (when required). Alumni status is activated automatically. |
| 11 | A school event is proposed, approved, published, and completed through its operational lifecycle. |
| 12 | An operational announcement is published to a defined audience, corrected, and withdrawn. |
| 13 | A cross-domain Development recommendation (e.g., discipline outcome recommending withdrawal) is issued and consumed by Students. |

---

## E. Dependencies and Risks

### Critical Cross-Domain Dependencies

| Dependency | Domains Involved | Impact | Must Resolve Before |
|---|---|---|---|
| **Student identity is the universal reference.** Every other domain references student identity owned by Students. | All → Student | If student identity module is delayed, no other domain can operate. | Stage 1 |
| **Academic year and calendar context is required by all domains.** Admission periods, financial policy effective dates, and academic requirements all depend on Operations calendar. | All → Operational (Calendar & Periods) | Without calendar context, no domain can associate its work with a time period. | Stage 1 |
| **Enrollment payment confirmation** may be required before enrollment completes. | Student ↔ Finance | If Finance basic payments are not available at enrollment time, an interim manual workaround is needed or policy must allow enrollment without payment confirmation. | Stage 1 |
| **Teaching Assignment must precede learning activities** in the documented model. | Academic → Teaching | The documents state that a teacher must be authorized through Teaching when required. Concurrent development of Teaching and Academic modules in Stage 2 requires coordination so that learning activities can reference at least a provisional teaching assignment. | Stage 2 |
| **Promotion/retention execution requires academic recommendation.** | Student ← Academic | Promotion execution in Students depends on Academics issuing a recommendation. Both must be available by end of Stage 2. | Stage 2 |
| **Transfer-out and graduation require clearances from both Academics and Finance.** | Student ← Academic (clearance) + Finance (clearance) | These advanced lifecycle transitions cannot be implemented until the clearance capabilities in Academics and Finance are complete. | Stage 3 |

### Domain Boundary Issues

| Issue | Affected Domains | Documented Status | Impact |
|---|---|---|---|
| **Homeroom ownership** — **Resolved.** Decoupled into: (a) Student cohort/homeroom placement → Students (Placement & Status), (b) Teacher advisory/homeroom assignment → Teaching (Assignment & Workload), (c) Physical homeroom room allocation → Operations (Scheduling & Rooms). Each domain owns only its slice. | Student, Teaching, Operational | **Resolved** | No impact — each capability sits naturally within its owning module. Cohort placement is Stage 1; advisory assignment is Stage 2; room allocation is Stage 2. |
| **Yearly re-enrollment** — **Resolved.** Re-enrollment is administrative and belongs to Students (Placement & Status). It answers "Is this student continuing next year?" — the same question Students owns at initial enrollment. Course/learning-unit selection for the new period remains Academic Participation (Academics). Finance may be triggered by the re-enrollment event when policy requires a re-enrollment fee. | Student, Academic | **Resolved** | Re-enrollment submodule added to Students Placement & Status, implemented in Stage 2. |
| **General daily school attendance** ownership is unresolved. Academics owns learning-activity attendance. Development asks whether it owns general daily attendance. | Academic, Development | Open question in Development | Low impact — can be deferred. Learning-activity attendance (Academics) is sufficient for Stage 2. |
| **Employee / HR boundary.** Teaching acknowledges that teacher identity may need a future employee domain. Teaching currently holds minimum identity information. | Teaching | Documented as provisional | No impact on the three-stage plan. Teaching module includes teacher profile with minimum identity. |

### Unresolved Decisions

| Decision | Impact | Status |
|---|---|---|
| ~~Is entrance assessment part of Students or provided by Academics?~~ **Resolved.** Academics provides, Students consumes (Model B). Stage 1 uses manual/external evidence entry; Stage 2 integrates directly from Academics assessment workflow. | Admission module in Stage 1 operates without Academics dependency. Full integration available in Stage 2. | **Resolved** |
| Is payment confirmation required before enrollment? | Determines whether Finance Payments & Receivables must be synchronously available during enrollment. | Resolve before Stage 1. If required, it is already sequenced correctly in the plan. |
| Can an accepted applicant defer enrollment? | Affects enrollment workflow and admitted-but-not-enrolled state management. | Nice-to-have for Stage 1; can be added later. |
| Which charge types are required first? | Determines initial financial policy configuration. | Resolve during Stage 1 implementation — this is configuration, not a structural decision. |
| Are suspension and expulsion Development decisions or recommendations to Students? | Affects the Development → Students recommendation interface in Stage 3. | Resolve before Stage 3 Development implementation. |
| Safeguarding and child-protection scope. | Development documents explicitly defer this pending legal/policy clarification. | Does not block any stage but must be resolved before any safeguarding capability is implemented. |

---

## F. Final Recommendation

### Recommended Implementation Sequence

**Stage 1 (Foundation)** → **Stage 2 (Learning & Teaching)** → **Stage 3 (Development & Completion)**

This sequence is the most pragmatic option for the following reasons:

1. **Dependency-driven.** Student identity and academic calendar are prerequisites for every other domain. Starting with them eliminates the most upstream blockers. Basic finance (obligations + payments) is included in Stage 1 because enrollment payment confirmation is a common school requirement.

2. **Operational value at each stage.**
   - Stage 1 delivers a working intake process: applicants become students with financial accounts.
   - Stage 2 delivers a working academic cycle: students learn, are assessed, receive report cards, and can be promoted.
   - Stage 3 delivers the complete system: behaviour/support, advanced transitions, and full operational coordination.

3. **Risk-managed.** The most complex cross-domain workflows (transfer-out with dual clearance, graduation with eligibility + clearance) are deferred to Stage 3 when all prerequisite capabilities are mature. This avoids implementing clearance consumers before clearance providers exist.

4. **No premature capability.** Development (behaviour, counselling, extracurriculars) is deferred to Stage 3 not because it lacks importance, but because it has no upstream dependencies on other incomplete domains — it depends only on student identity (Stage 1). Placing it in Stage 3 keeps Stage 2 focused on the core teaching-and-learning cycle, which is the most dependency-intensive and technically complex stage.

### Key Trade-off

The primary alternative considered was promoting Development Behaviour & Discipline to Stage 2, since it depends only on student identity. This was evaluated and **confirmed as Stage 3** for the following reasons:

- Stage 2 already carries the highest implementation complexity (concurrent Academic + Teaching + Finance expansion + Scheduling). Adding Development increases scope and risk without unblocking any other module.
- Behaviour and discipline are operationally useful but do not unblock any other domain's work.
- Stage 3 allows Development to be implemented against a fully functional student and academic base, enabling more realistic implementation and testing of cross-domain recommendations (e.g., discipline outcome leading to withdrawal or transfer).
- The most complex Development capabilities (discipline cases, counselling, wellbeing) carry significant unresolved policy questions (confidentiality, consent, safeguarding scope) that are better resolved while Stages 1–2 are in progress.

> [!IMPORTANT]
> Remaining open questions in Section E should be reviewed with school stakeholders before implementation begins. Structural decisions (payment-before-enrollment, suspension/expulsion handling, safeguarding scope) must be resolved before the relevant stage begins. Policy-configuration questions (charge types, attendance categories, workload units) can be resolved concurrently with implementation.
