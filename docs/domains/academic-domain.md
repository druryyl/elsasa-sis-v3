# Academics Domain

## Purpose

The Academics domain manages each student's learning journey.

It is the authoritative source for:

* what a student is expected to learn
* what learning the student participates in
* the student's attendance in learning activities
* evidence of learning and assessment results
* the student's learning progress and academic history
* academic standing, progression recommendations, and graduation eligibility
* purpose-specific academic clearance

The domain begins when an active or otherwise eligible student is assigned or admitted to learning and continues as academic history after the student leaves the school.

The center of this domain is the student and the student's learning journey, not the classroom.

This domain does not manage student identity or administrative status, teacher resources or assignments, operational schedules, behaviour or counselling, or financial obligations.

---

## Domain Responsibility

The Academics domain owns:

* learning structures and academic requirements
* student learning plans
* academic participation
* learning activities and their academic purpose
* student attendance in learning activities
* assignments and submissions
* assessments and assessment evidence
* scores, grades, feedback, and approved academic results
* learning progress and completion
* academic interventions
* academic achievements
* report cards, transcripts, and academic history
* academic standing recommendations
* promotion, retention, and progression recommendations
* graduation eligibility
* purpose-specific academic clearance

Academics must support class-based, course-based, module-based, project-based, and individual-progress learning.

It must not assume that all students in one administrative placement learn the same things at the same pace.

Academics may recommend an administrative transition. Only Students may execute a change to the student's official status or administrative placement.

---

## Scope

### In Scope

#### Learning Structure

Describe the learning recognized by the school without requiring one universal curriculum model.

A learning structure may use concepts such as:

* subject
* course
* module
* unit
* PACE
* project
* competency
* learning outcome
* another school-defined learning unit

These names are curriculum expressions, not separate domains.

The minimum common abstraction is **Learning Unit**. A Learning Unit may represent a subject, course, module, project, PACE, or any class-based learning element. Do not introduce separate root concepts for each curriculum type. The preferred structure is:

```text
Learning Structure
    → Learning Unit
        → Learning Activity
```

Implementations must not force every learning model into a class hierarchy merely for technical convenience.

---

#### Academic Requirements

Manage the academic expectations that apply to a program, stage, period, or student.

Requirements may include:

* required and optional learning
* prerequisites and learning sequence
* expected outcomes
* credit or competency requirements
* completion criteria
* assessment and grading policy
* progression and graduation requirements
* substitutions and exemptions
* effective curriculum version

Requirements must be effective-dated or versioned when changes could affect historical interpretation.

---

#### Student Learning Plan

Manage what an individual student is expected to learn.

A Student Learning Plan is **optional**, not mandatory. Default academic operation must work without one. An SLP is intended for exceptional or individualized cases — acceleration, remediation, accommodation, transfer, or individualized pathways.

When a plan exists, the Phase-2 minimum scope is:

* Student
* Effective Period
* Learning Units
* Notes
* Status

A learning plan may also identify, where applicable:

* required sequence
* academic accommodations
* substitutions or exemptions
* plan approval
* revision history

Do not introduce goals, milestones, competencies, intervention plans, rubrics, or other advanced planning constructs in Phase 2.

**Governance:** An Academic Coordinator creates and revises a plan. An Academic Authority (Curriculum Lead, Academic Head, Vice Principal for Academics, or equivalent) approves the plan. Any revision requires re-approval by an Academic Authority.

**Concurrency:** A student may have at most one Active SLP per Academic Period. Multiple concurrent active plans are not permitted. Historical plans must be preserved.

**Academic Plan Adjustments:** Accommodations, substitutions, and exemptions are treated as a single category — Academic Plan Adjustment. All adjustments require explicit Academic Authority approval, must preserve rationale, authority, and effective dates, and do not modify administrative enrollment or student status. The authorization flow is: Academic Coordinator reviews → Academic Authority approves → Student Learning Plan updated.

A learning plan is not the student's administrative placement.

A student administratively placed in Grade 6 may learn selected material associated with other levels.

---

#### Academic Participation

Manage a student's participation in specific learning.

Participation may include:

* assignment or registration to learning
* start, pause, resume, withdrawal, and completion
* individual or learning-group participation
* recognition of prior or transferred learning
* participation history

Academic participation answers:

> What learning is this student undertaking?

Administrative enrollment in Students answers:

> Can this student officially study at this school?

**Boundary rule:** Administrative enrollment does not automatically create Academic Participation. Participation must be established through Academics. A student may be enrolled but have no active participation — valid cases include: newly enrolled students awaiting academic placement, transfer students awaiting learning assignment, students on leave, and students with a delayed participation start. Students owns administrative eligibility; Academics owns learning participation; neither domain may redefine the other's responsibility.

Do not use academic participation to independently activate, suspend, withdraw, promote, or graduate a student.

---

#### Learning Activities

Manage activities through which learning is undertaken or demonstrated.

Examples include:

* lesson
* tutorial
* laboratory activity
* independent work
* project activity
* discussion
* practice
* examination
* remediation activity

Academics owns the educational purpose, expected outcomes, student participation, and academic result of an activity.

Teaching owns teacher assignment. Operations owns operational scheduling and room allocation.

---

#### Student Attendance

Manage attendance in required learning activities.

Attendance may record:

* present
* absent
* late
* excused absence
* partial participation
* attendance evidence
* correction and approval history

Attendance must refer to a meaningful learning obligation. It must not assume that attendance always means presence in a classroom.

Academics does not own employee attendance or general event attendance.

---

#### Assignments and Student Work

Manage academic work expected from a student.

Assignments must always originate from a Learning Activity. The required relationship is:

```text
Learning Activity
    → Assignment
    → Submission
```

Assignments must not exist independently of a Learning Activity.

This may include:

* assignment requirements
* release and due dates
* student submission
* resubmission
* late or missing work
* evaluation
* feedback
* completion status

Missing submissions do not automatically become zero scores. Assignment status and academic evaluation are separate concerns. At minimum, the following submission statuses must be supported:

* Submitted
* Late
* Missing
* Excused
* Exempted

Any grading consequence is determined by assessment policy, not by submission status itself.

Academics may reference learning material, but full LMS content authoring, video delivery, and content publishing platforms are outside the current core scope.

---

#### Assessment

Manage the process of collecting and evaluating evidence of learning.

Assessment may include:

* assessment purpose and plan
* criteria or rubric
* assessment activity
* student evidence
* scoring or qualitative judgment
* teacher feedback
* moderation
* reassessment
* approval and publication
* correction history

The domain must permit different evaluation models, including numeric, letter-grade, pass/fail, competency, rubric, narrative, and completion-based models.

Raw evidence, provisional evaluation, approved result, and published result must not be treated as automatically equivalent.

---

#### Learning Progress and Completion

Track progress toward expected learning outcomes.

Phase-2 uses a minimal progress model. Required states:

* Not Started
* In Progress
* Completed

Optional states:

* Needs Remediation
* Exempted

These are business concepts, not mandatory database values. Do not introduce percentage-based progress tracking in Phase 2.

Progress must originate from learning work, assessment evidence, or an explicit authorized decision. It must not be entered only to satisfy reporting needs.

---

#### Academic Intervention

Manage academic action taken when a student needs additional learning support or challenge.

Examples include:

* remediation
* reassessment
* tutoring recommendation
* extension work
* recovery plan
* learning-plan adjustment
* academic warning

Academics owns intervention related to learning performance. Development owns behaviour, wellbeing, counselling, and non-academic intervention.

---

#### Academic Results and History

Preserve the student's academic record, including:

* completed learning
* assessment results
* approved grades
* credits or competencies earned
* academic-period results
* recognized prior or transferred learning
* result corrections
* curriculum and policy context
* report cards and transcripts

Approved history must not be silently overwritten. A correction must preserve what changed, why it changed, when it changed, and who authorized it.

---

#### Academic Achievements

Manage achievements derived from academic learning or assessment.

Examples may include:

* subject distinction
* academic honors
* mastery recognition
* academic certification
* curriculum-recognized academic competition achievement

Academics owns academic achievements. Development owns extracurricular and other non-academic achievements.

---

#### Academic Standing and Eligibility

Determine the student's academic condition and recommendations arising from it.

Academic Standing represents an academic determination, not grades. The minimum standing model is:

* Good Standing
* Conditional Standing
* Not In Good Standing

Academic Standing serves as input to progression and graduation decisions.

**Promotion Eligibility:** Academics owns promotion eligibility determination. Academics may issue:

* Promotion Eligible
* Retention Recommended
* Conditional Promotion Recommendation

Students is the sole domain that executes official promotion or retention.

**Graduation Eligibility:** Academics owns graduation eligibility determination. Academics may issue:

* Graduation Eligible
* Graduation Not Eligible
* Conditional Graduation Recommendation

Students is the sole domain that executes official graduation.

Academic standing does not change the student's administrative status.

Students consumes approved recommendations and executes any official promotion, retention placement, or graduation.

---

#### Academic Clearance

Determine whether defined academic prerequisites have been satisfied for a specific purpose.

Academic Clearance is distinct from Academic Standing and from promotion, retention, or graduation eligibility. Standing is an academic condition. Eligibility is a recommendation that Students may execute as an administrative transition. Clearance is a purpose-specific gate that another domain may consume. Academics must not infer clearance from standing, results, progress, or eligibility.

Phase-2 requires Academic Clearance for purpose Transfer Out. Additional clearance purposes may be configured later without changing the domain model. Graduation continues to use Graduation Eligibility; it does not consume Academic Clearance in Phase-2.

Clearance should identify:

* purpose
* subject
* applicable policy
* evaluated academic facts relevant to that purpose
* result
* determination date
* validity or expiry, when applicable
* authority

Permitted results are:

* Granted
* Denied
* Conditional
* Cannot Yet Be Determined
* Expired
* Revoked

Clearance is purpose-specific. A student may satisfy one prerequisite while remaining uncleared for another purpose.

**Clearance criteria:** Academic Clearance is blocked only by outstanding academic prerequisites that school policy treats as mandatory for the stated purpose. Evaluation must be transparent and explainable; blocking items must be identified. Schools may configure which academic categories are mandatory for each purpose. Agents must not invent those categories.

**Transfer Out execution:** Students may execute external transfer-out only when Academic Clearance for purpose Transfer Out is Granted. Denied, Conditional, Cannot Yet Be Determined, Expired, and Revoked do not permit transfer-out.

**Authority:** Academic Coordinator records the routine determination. Academic Authority records any exception determination, and the exception reason is mandatory. Academic Clearance must not change official student status. Students owns the administrative action that consumes the result.

Students requests Academic Clearance. Academics determines it. Students consumes the owned result.

---

## Out of Scope

The Academics domain does not own:

### Students

* student identity
* admission decision
* administrative enrollment
* student number
* administrative placement
* official student status
* leave approval
* transfer, withdrawal, promotion, or graduation execution
* alumni status

### Development

* behaviour and discipline
* counselling
* wellbeing
* extracurricular participation
* non-academic intervention
* non-academic achievements

### Finance

* billing
* payment
* scholarship
* discount
* receivable
* refund
* financial clearance

### Teaching

* teacher identity and profile
* teacher qualification
* teacher availability
* teacher assignment
* teacher workload
* teaching history

### Operations

* academic calendar ownership
* school events and announcements
* room allocation
* operational timetable or schedule
* cross-domain operational approvals

### Other Exclusions

* payroll
* accounting
* library
* transportation
* hostel
* cafeteria
* inventory and procurement
* LMS content authoring
* video learning platform
* BI or data warehouse

---

## Actors

The exact actor names and authority differ between schools.

### Student

Participates in learning and provides evidence of learning.

May:

* view assigned learning
* participate in learning activities
* submit work
* view permitted feedback and results
* request a correction or reassessment when policy permits

---

### Parent or Guardian

May view permitted information and participate in academic support workflows.

Access depends on school policy and the relationship information owned by Students.

A parent or guardian does not approve an academic result merely by acknowledging it.

---

### Teacher or Learning Facilitator

Conducts learning and assessment work after receiving an assignment from Teaching.

May:

* conduct learning activities
* record attendance
* issue assignments
* evaluate evidence
* provide feedback
* record provisional results
* recommend intervention

Academics references the teacher assignment but does not own it.

---

### Academic Coordinator

Coordinates learning plans, academic participation, assessment operations, and progress review.

May:

* review plan coverage
* coordinate result completion
* identify students needing attention
* initiate academic review
* prepare progression recommendations
* determine routine Academic Clearance

---

### Assessment Officer

Coordinates assessment administration, moderation, result completeness, and publication where this role exists.

---

### Academic Authority

Holds authority defined by school policy to approve academic plans, results, standing, eligibility, or Academic Clearance exceptions.

The Academic Authority may provide recommendations or exception clearance determinations to Students but does not directly change official student status.

---

### Student Administration Officer

Consumes approved academic recommendations and executes authorized administrative transitions in Students.

---

### School Leadership

May approve policy, exceptional decisions, or final recommendations where required.

Leadership involvement does not transfer ownership of academic records to Operations or Students.

---

## Core Business Concepts

### Learning Unit

The single curriculum-neutral abstraction for a defined body of learning that can be planned, undertaken, evaluated, or completed.

A Learning Unit may represent a subject, course, module, PACE, project, or class-based learning element. Subject, course, module, PACE, and project are not separate root concepts — they are expressions of a Learning Unit. The preferred structure is:

```text
Learning Structure
    → Learning Unit
        → Learning Activity
```

### Academic Requirement

A rule describing learning, evidence, completion, credit, or mastery expected for a student, program, or stage.

### Student Learning Plan

The approved set or sequence of learning expectations for a student.

### Academic Participation

The historical relationship between a student and a specific learning unit or activity.

### Learning Activity

An occurrence in which learning is undertaken, supported, or demonstrated.

### Attendance Record

The recorded participation of a student in a required learning activity.

### Assignment

Academic work requested from a student with defined expectations and, when applicable, a due date.

### Assessment Evidence

Student work, observation, response, performance, or other evidence used to evaluate learning.

### Academic Result

An authorized evaluation of learning evidence under an applicable assessment policy.

### Learning Progress

The student's current advancement toward defined learning expectations.

### Academic Standing

An academic conclusion or recommendation based on approved requirements and evidence.

### Academic Clearance

A purpose-specific Academics determination stating whether defined academic prerequisites have been satisfied.

### Academic Record Correction

An explicit authorized change to academic history that preserves the prior value and reason for change.

---

## Student Learning Journey

The following is conceptual and does not prescribe mandatory technical states.

```text
Eligible Student
      |
      v
Learning Planned
      |
      v
Learning Assigned or Selected
      |
      v
Learning In Progress
      |
      +------> Academic Support or Reassessment
      |                    |
      |<-------------------+
      v
Learning Evaluated
      |
      v
Result Approved
      |
      v
Learning Completed
      |
      v
Standing and Progression Reviewed
```

A student may undertake multiple learning units concurrently and progress through them at different speeds.

Administrative leave, transfer, withdrawal, or graduation comes from Students and may trigger an academic response. Academics must not infer those administrative transitions.

---

## Main Workflows

## 1. Define or Adopt Learning Structure

### Trigger

The school adopts or revises the learning it offers.

### Actor

* Academic Authority
* Academic Coordinator

### Input

* curriculum source
* learning outcomes
* academic requirements
* effective period or version
* approval

### Outcome

* an approved learning structure is available for planning
* previous versions remain interpretable

### Business Rules

* curriculum terminology must not redefine global domain boundaries
* effective changes must not silently alter historical results
* only the minimum structure required by actual workflows should be modeled
* class membership must not be required for all learning models

---

## 2. Establish Student Learning Plan

### Trigger

A student's exceptional or individualized learning must be established or revised.

### Actor

* Academic Coordinator (creates and revises)
* Academic Authority — Curriculum Lead, Academic Head, Vice Principal for Academics, or equivalent (approves; re-approves on revision)

### Input

* student identity reference
* administrative placement context
* applicable requirements
* prior progress
* substitutions, exemptions, or accommodations

### Outcome

* the student's expected learning is recorded
* approval and revision history are preserved
* at most one Active SLP per student per Academic Period

### Business Rules

* only an eligible student identity from Students may receive a plan
* administrative placement may inform but must not completely determine the plan
* revisions must not rewrite the plan that governed historical results
* any revision requires re-approval by an Academic Authority
* multiple concurrent active plans per student per Academic Period are not permitted; historical plans are preserved
* accommodations, substitutions, and exemptions are Academic Plan Adjustments: each requires Academic Authority approval and must record rationale, authority, and effective dates
* Academic Plan Adjustments do not modify administrative enrollment or student status

---

## 3. Start Academic Participation

### Trigger

A student is assigned, registered, or approved to undertake learning.

### Actor

* Academic Coordinator
* Student, when selection is permitted
* Academic Authority, when approval is required

### Input

* eligible student
* learning plan or approved selection
* learning unit
* applicable period
* prerequisite evidence

### Outcome

* academic participation is established
* learning may begin

### Business Rules

* participation must not change administrative enrollment or status
* prerequisites must follow approved policy
* repeated or concurrent participation must be explicit
* group membership is optional and must not define the student's identity

---

## 4. Conduct Learning Activity

### Trigger

Planned or authorized learning takes place.

### Actor

* Teacher or Learning Facilitator
* Student

### Input

* learning purpose
* expected participants
* teacher assignment reference
* operational schedule or context, when applicable

### Outcome

* the activity is recorded as conducted
* participation, work, or progress may be captured

### Business Rules

* the teacher must be authorized through Teaching when a teacher is required
* an activity may be individual, asynchronous, or group-based
* the academic activity must not become the owner of room or timetable information

---

## 5. Record or Correct Attendance

### Trigger

A required learning activity occurs or an attendance record requires correction.

### Actor

* Teacher or Learning Facilitator
* Authorized Academic Officer

### Input

* learning activity
* expected student participation
* observed attendance
* reason and evidence for correction, when applicable

### Outcome

* student attendance is recorded
* corrections preserve history and authority

### Business Rules

* attendance must not be inferred merely from administrative status
* absence must not independently place a student on leave or withdraw the student
* authorized attendance categories and correction periods are school policy
* bulk entry must retain an individual outcome for every student

---

## 6. Issue and Receive Assignment

### Trigger

Academic work is assigned and later submitted or marked missing.

### Actor

* Teacher or Learning Facilitator
* Student

### Input

* assignment expectations
* applicable students
* release and due information
* submission or completion evidence

### Outcome

* assignment participation and submission status are recorded
* work is available for evaluation

### Business Rules

* due dates must use operational calendar context where applicable
* late, missing, excused, and exempt work must remain distinguishable
* a missing submission is not automatically a zero unless policy explicitly says so

---

## 7. Plan and Conduct Assessment

### Trigger

Evidence is required to evaluate learning.

### Actor

* Teacher or Learning Facilitator
* Assessment Officer
* Academic Coordinator

### Input

* assessed learning outcomes
* assessment method
* criteria or rubric
* applicable policy
* participating students

### Outcome

* assessment evidence is collected or expected
* evaluation may proceed

### Business Rules

* the assessment method must be appropriate to the applicable policy
* assessment identity and criteria must remain linked to resulting evaluations
* accommodations must not be confused with unauthorized changes to requirements

---

## 8. Evaluate Student Learning

### Trigger

Assessment evidence is available for evaluation.

### Actor

* Teacher or Learning Facilitator
* Authorized Assessor

### Input

* assessment evidence
* applicable criteria
* grading or evaluation policy
* prior attempts, when relevant

### Outcome

* provisional evaluation and feedback are recorded
* the result is ready for any required moderation or approval

### Business Rules

* evaluation must identify the evidence and policy used
* absent, missing, incomplete, exempt, and failed are not automatically equivalent
* automated calculation may assist but must not invent policy
* reassessment and attempt-selection rules must be explicit

---

## 9. Approve and Publish Academic Result

### Trigger

A provisional result is complete and ready for authorization.

### Actor

* Academic Authority
* Assessment Officer
* Teacher, when policy grants authority

### Input

* provisional result
* moderation outcome, when required
* completeness checks
* approval authority

### Outcome

* an approved result becomes part of academic history
* the result may be published to authorized audiences

### Business Rules

* approval and publication are separate concepts when policy requires them
* publication must not expose information to an unauthorized actor
* approved results may change only through an explicit correction workflow
* bulk approval must preserve an individual outcome and failure reason

---

## 10. Review Progress and Initiate Academic Intervention

### Trigger

Progress evidence shows that attention or additional challenge may be needed.

### Actor

* Teacher or Learning Facilitator
* Academic Coordinator
* Academic Authority

### Input

* attendance
* assignments
* assessment results
* progress expectations
* previous academic intervention

### Outcome

* progress is reviewed
* an academic intervention may be established and monitored

### Business Rules

* intervention must be based on authorized academic information
* behaviour or wellbeing concerns must be referred to Development
* an intervention must not independently change official student status
* access to sensitive cross-domain information must remain limited to what is needed

---

## 11. Complete Learning

### Trigger

The applicable completion requirements have been evaluated.

### Actor

* Teacher or Learning Facilitator
* Academic Coordinator
* Academic Authority, when approval is required

### Input

* completion requirements
* approved results
* required participation or evidence
* exemption or recognition decisions

### Outcome

* learning is marked completed, mastered, incomplete, or otherwise concluded
* academic history is preserved

### Business Rules

* completion must follow the requirement version applicable to the student
* the end of a calendar period must not automatically imply completion
* completion of learning must not automatically promote or graduate the student

---

## 12. Produce Report Card or Transcript

### Trigger

Authorized academic information must be communicated or certified.

### Actor

* Academic Coordinator
* Assessment Officer
* Academic Authority

### Input

* approved academic results
* attendance and progress information permitted by policy
* applicable report definition
* reporting period

### Outcome

* an authorized report card, progress report, or transcript is produced
* the source academic record remains authoritative

### Business Rules

* reports must be generated from operational academic records
* a report layout must not become the owner of grades or progress
* published reports must remain reproducible under their historical rules
* curriculum-specific presentation should be configuration, not a new domain

---

## 13. Determine Academic Standing and Progression Recommendation

### Trigger

The student's academic position must be reviewed.

### Actor

* Academic Coordinator
* Academic Authority
* Authorized Academic Committee

### Input

* applicable academic requirements
* approved results
* learning completion
* approved exceptions
* prior academic standing

### Outcome

* academic standing is recorded
* promotion, retention, or progression recommendation may be issued to Students

### Business Rules

* standing must identify the policy and evidence used
* a recommendation is not an administrative transition
* conditional recommendations must state their conditions
* individual exceptions require explicit authority and reason

---

## 14. Determine Graduation Eligibility

### Trigger

A student's academic completion against graduation requirements must be evaluated.

### Actor

* Academic Authority
* Authorized Academic Committee

### Input

* applicable graduation requirements
* approved academic history
* recognized credits or competencies
* approved exceptions

### Outcome

* graduation eligibility or deficiency is recorded
* approved eligibility is provided to Students

### Business Rules

* Academics determines academic eligibility only
* Finance owns financial clearance
* Students owns graduation execution and official status
* eligibility must not be inferred solely from time spent at school

---

## 15. Correct Approved Academic Record

### Trigger

An approved academic record is found to be incorrect.

### Actor

* Academic Authority
* Assessment Officer
* Authorized Academic Officer

### Input

* original record
* requested correction
* reason and evidence
* authorization

### Outcome

* the corrected record becomes effective
* the original value, reason, actor, and time remain auditable
* affected reports or recommendations can be identified

### Business Rules

* approved records must never be silently overwritten
* correction authority must be explicit
* downstream standing and eligibility must be reviewed when affected
* a correction must not directly reverse an administrative action in Students

---

## 16. Determine Academic Clearance

### Trigger

An authorized Students workflow requests an academic prerequisite for a defined purpose.

### Actor

* Academic Coordinator
* Academic Authority

### Input

* student reference
* clearance purpose
* Students Academic Clearance request
* applicable academic policy
* academic facts relevant to that purpose, which may include participation, attendance, assignments, approved results, progress, standing, or eligibility when policy requires them
* effective date

### Outcome

* clearance is Granted, Denied, Conditional, Cannot Yet Be Determined, Expired, or Revoked
* Students receives the minimum necessary result and reference

### Business Rules

* Phase-2 determination is for purpose Transfer Out only
* clearance must not be inferred from standing, results, progress, or eligibility
* unrelated academic facts must not be included without policy basis
* routine determination is recorded by Academic Coordinator
* exception determination requires Academic Authority and a mandatory reason
* Students owns the action that consumes the clearance
* clearance must not change official student status
* later academic changes may require reevaluation under policy

---

## 17. Correct Academic Clearance

### Trigger

An Academic Clearance record is found to be inaccurate or incomplete.

### Actor

* Academic Coordinator
* Academic Authority

### Input

* original clearance record
* requested correction
* reason and evidence
* authorization

### Outcome

* the corrected clearance becomes effective
* the original value, reason, actor, and time remain auditable
* Students is identified to re-consume the owned result when the purpose still applies

### Business Rules

* correction is not a new clearance determination caused by changed academic circumstances
* a changed circumstance uses Determine Academic Clearance
* approved clearance records must never be silently overwritten
* a correction must not reverse a Students administrative action
* exception corrections require Academic Authority and a mandatory reason

---

## 18. Correct Phase-2 Academic Record

### Trigger

An Assignment, Submission, Submission Status, Submission Evaluation, Academic Standing, Promotion Recommendation, Retention Recommendation, or Graduation Eligibility record is found to be inaccurate or incomplete.

### Actor

* Teacher or Learning Facilitator
* Academic Coordinator
* Academic Authority

### Input

* original record
* record type
* requested correction
* reason and evidence
* authorization

### Outcome

* the corrected record becomes effective
* the original value, reason, actor, and time remain auditable
* affected progress, standing, eligibility, or Students consumers are identified for review

### Business Rules

* this workflow does not correct Student Learning Plan, Learning Progress, Academic Clearance, Phase-1 approved Academic Result, attendance, or participation
* correction is not a new operational decision caused by new evidence or changed circumstances
* a changed circumstance uses the owning operational procedure
* Teacher or Learning Facilitator may correct their own assignment or student-work records as a routine action
* Academic Coordinator records routine standing correction and may record routine assignment or student-work correction
* material correction, promotion or retention recommendation correction, and graduation-eligibility correction require Academic Authority and a mandatory reason
* submission-evaluation correction does not become an approved Academic Result
* a correction must not automatically Promote, Retain, or Graduate a student
* Students is identified for review when a consumed recommendation or eligibility result is corrected
* silent overwrite is prohibited

---

## Business Rules

### Student-Centric Learning

The student's learning journey is the primary organizing concept.

Class, cohort, room, timetable slot, teacher, and administrative grade may provide context but must not be required as the center of every workflow.

### Curriculum Independence

Differences in subjects, courses, modules, PACE, projects, competencies, grading, and reporting should be represented through academic policy and configuration where practical.

Do not create separate top-level domains for curriculum variants.

### Administrative Status Separation

Academics may recommend, request, or provide evidence for an administrative transition.

It may not activate, suspend, transfer, withdraw, promote, retain, or graduate a student administratively.

### Academic Participation Separation

Academic participation is not administrative enrollment.

A student may participate in multiple kinds of learning and may progress independently without changing official placement.

### Evidence Before Result

An academic result must be traceable to appropriate evidence, criteria, policy, or an explicit authorized decision.

Do not treat a manually entered summary as sufficient when the workflow requires underlying evidence.

### Provisional, Approved, and Published Results

These states have different meanings and must remain distinguishable where approval is required.

A provisional result must not be consumed as an approved result by progression or reporting workflows.

### Historical Integrity

Preserve the learning plan, requirements, evidence, results, standing, clearance, and policies that applied at the time.

Corrections must be explicit and auditable.

### Effective Dating and Versioning

The date a decision is recorded may differ from the period or date for which it applies.

Changes to requirements or grading policy must not reinterpret historical results without explicit authorization.

### Explicit Completion and Standing

Do not infer completion, mastery, promotion eligibility, graduation eligibility, or Academic Clearance merely from:

* the current date
* the end of an academic period
* administrative placement
* attendance alone
* one grade without the applicable policy
* absence of outstanding work

### Academic and Non-Academic Separation

Academics owns learning performance and academic intervention.

Development owns behaviour, counselling, wellbeing, extracurricular development, and non-academic intervention.

### Teacher Resource Separation

Academics records academic work performed by an authorized teacher or facilitator.

Teaching owns who the teacher is, whether the teacher is available, and what teaching assignment authorizes that work.

### Operational Context Separation

Operations owns calendars, rooms, events, and operational schedules.

Academics consumes that context but owns the educational meaning and result of learning activities.

### Minimum Required Information

Require only the academic information necessary for the current workflow.

Do not require a complete universal curriculum model before the school's initial learning workflows can operate.

---

## Inputs from Other Domains

### From Students

* student identity reference
* enrollment completed
* student activated
* administrative placement
* leave started or ended
* Academic Clearance request for Transfer Out
* internal transfer
* transfer out
* withdrawal
* promotion or retention execution
* graduation execution

Academics responds to these events but does not redefine them.

### From Development

* permitted context affecting learning support
* referral outcome relevant to an academic intervention
* non-academic achievement or participation context when authorized

Academics must not copy counselling notes, behaviour records, or sensitive wellbeing information merely for convenience.

### From Teaching

* teacher identity reference
* teaching assignment
* teacher availability context
* qualification context when required for an activity or assessment

### From Operations

* academic year and period
* academic calendar
* operational schedule
* room allocation
* school event context
* operational approval context

### From Finance

* only approved financial prerequisite or clearance information when a specific academic workflow requires it

Financial standing must not alter academic results.

---

## Outputs to Other Domains

### To Students

* academic standing recommendation
* promotion recommendation
* retention recommendation
* progression recommendation
* graduation eligibility
* academic completion or deficiency information needed for an authorized transition
* purpose-specific Academic Clearance result and reference

Students executes official administrative changes. Students consumes Academic Clearance and does not determine it.

### To Development

* academic concern referral
* permitted attendance or progress context
* request for non-academic support
* academic intervention status when relevant

### To Teaching

* learning demand requiring teacher assignment
* academic activity requirements
* assessment requirements
* completion of assigned academic work
* academic workload context without redefining teacher workload

### To Operations

* learning activities requiring scheduling
* room or resource requirements
* assessment-period needs
* operational exceptions or conflicts
* academic completion status needed for coordination

### To Finance

* academic participation or completion information only when required by an approved financial policy

Academics does not calculate charges, discounts, refunds, or financial eligibility.

---

## Domain Events

The following are conceptual business events, not mandatory technical event names.

* LearningStructureApproved
* AcademicRequirementVersionActivated
* StudentLearningPlanEstablished
* StudentLearningPlanRevised
* AcademicParticipationStarted
* AcademicParticipationPaused
* AcademicParticipationResumed
* AcademicParticipationWithdrawn
* LearningActivityConducted
* StudentAttendanceRecorded
* StudentAttendanceCorrected
* AssignmentIssued
* StudentWorkSubmitted
* AssessmentConducted
* LearningEvidenceEvaluated
* AcademicResultRecorded
* AcademicResultApproved
* AcademicResultPublished
* AcademicResultCorrected
* AcademicInterventionStarted
* AcademicInterventionReviewed
* LearningCompleted
* AcademicAchievementRecognized
* ReportCardPublished
* TranscriptIssued
* AcademicStandingDetermined
* PromotionRecommended
* RetentionRecommended
* ProgressionRecommended
* GraduationEligibilityDetermined
* AcademicClearanceRequested
* AcademicClearanceDetermined
* AcademicClearanceCorrected

Technical event design belongs in the architecture document.

---

## Ownership Matrix

| Business Concept | Owner | Other Domain Involvement |
| --- | --- | --- |
| Student Identity | Students | Referenced by Academics |
| Administrative Enrollment | Students | Establishes eligibility context |
| Administrative Placement | Students | Informs but does not determine learning |
| Student Learning Plan | Academics | May use Students placement context |
| Learning Structure | Academics | Curriculum-specific and policy-driven |
| Academic Participation | Academics | Distinct from administrative enrollment |
| Learning Activity | Academics | Operations schedules; Teaching assigns teacher |
| Student Learning Attendance | Academics | Development may consume permitted context |
| Assignment and Submission | Academics | Student and teacher participate |
| Assessment Evidence | Academics | Produced through authorized academic work |
| Assessment Result | Academics | Students may consume approved recommendation |
| Learning Progress | Academics | Development may support the student |
| Academic Intervention | Academics | Development owns non-academic intervention |
| Academic Achievement | Academics | Development owns non-academic achievement |
| Academic Standing | Academics | Students consumes recommendation |
| Promotion Eligibility | Academics | Students executes promotion or retention |
| Graduation Eligibility | Academics | Students executes graduation |
| Academic Clearance | Academics | Students requests and consumes the result |
| Student Status | Students | Academics must not change it |
| Teacher Assignment | Teaching | Authorizes academic work |
| Teacher Availability and Workload | Teaching | Academics communicates learning demand |
| Academic Calendar | Operations | Provides context to Academics |
| Room and Operational Schedule | Operations | Supports learning activities |
| Behaviour, Counselling, and Wellbeing | Development | May inform permitted learning support |
| Billing and Payment | Finance | Must not determine academic result |

---

## Reporting Responsibility

Academics may provide operational information such as:

* students with unstarted, active, overdue, or completed learning
* attendance by learning activity or period
* missing assignments and incomplete assessments
* provisional, pending-approval, and published results
* learning progress and mastery
* academic intervention workload and outcomes
* completion by learning requirement
* academic achievements
* standing and progression recommendations
* graduation eligibility and deficiencies
* Academic Clearance results by purpose
* report-card and transcript publication status

Reports must be produced from actual academic workflows.

Do not create separate data-entry processes solely for dashboards.

Cross-domain dashboards may combine academic information with Students, Development, Finance, Teaching, or Operations information, but ownership and access restrictions must remain explicit.

---

## Guiding Principles

### Learning Journey First

Design around what each student needs to learn, is doing, and has achieved.

### Evidence-Producing Workflows

Attendance, assignments, assessment, feedback, and approvals should naturally produce reliable progress information.

### Class Is Context, Not the Center

Support classes where they reflect real work, but do not require every learning journey to be class-based.

### Policy Is Explicit

Grading, reassessment, completion, standing, promotion, and graduation rules must be stated or configured. Agents must never invent them.

### Recommendation Is Not Execution

Academics determines academic standing and eligibility. Students executes official administrative transitions.

### Preserve Academic History

Keep historical requirements, plans, evidence, results, approvals, corrections, and decisions interpretable.

### Actor-Centric Workspaces

Show each actor what needs attention, what action is authorized, what evidence is needed, and what outcome is expected.

### Simple Before Flexible

Implement the smallest learning model and workflow that serves the current school. Do not introduce a speculative universal curriculum engine.

---

## Open Questions

These questions remain unresolved and must be clarified before detailed implementation. Questions resolved by approved policy decisions are recorded as closed below.

### Learning Structure

* ~~What is the minimum curriculum-neutral learning abstraction?~~ **Closed.** Learning Unit is the single abstraction. Subject, course, module, PACE, and project are expressions of a Learning Unit, not separate root concepts. Preferred structure: Learning Structure → Learning Unit → Learning Activity.
* How should competency and outcome relate to Learning Units?
* Which structures are required for the first school implementation?
* How are requirements and curriculum versions approved and activated?

### Student Learning Plan

* ~~Is an explicit learning plan required for every student?~~ **Closed.** Student Learning Plan is optional. Default academic operation must work without one. SLP is for exceptional or individualized cases only.
* ~~Who creates, approves, and revises a plan?~~ **Closed.** Created and revised by an Academic Coordinator. Approved (and re-approved on revision) by an Academic Authority (Curriculum Lead, Academic Head, Vice Principal for Academics, or equivalent).
* ~~Can a student have concurrent plans?~~ **Closed.** At most one Active SLP per student per Academic Period. Multiple concurrent active plans are not permitted. Historical plans are preserved.
* ~~How are accommodations, substitutions, and exemptions authorized?~~ **Closed.** Treated as a single category: Academic Plan Adjustment. Authorization: Academic Coordinator reviews; Academic Authority approves. All adjustments require explicit approval, preserve rationale, authority, and effective dates, and do not modify administrative enrollment or student status.

### Academic Participation

* ~~What is the exact boundary between administrative enrollment and academic participation?~~ **Closed.** Administrative Enrollment (Students): Can this student officially study at this school? Academic Participation (Academics): What learning is this student undertaking? Enrollment does not automatically create Academic Participation. Participation must be established through Academics. Valid cases where enrolled but no active participation: newly enrolled awaiting placement, transfer awaiting assignment, on leave, delayed participation start.
* Is yearly re-enrollment administrative or academic?
* Can students select learning, or is it always assigned?
* How are repeated, paused, transferred, and recognized learning represented?

### Learning Groups and Classes

* Is a class a learning group, an administrative group, a scheduled offering, or multiple concepts?
* Who creates learning groups?
* Can one student belong to multiple groups for different learning?
* Does homeroom belong to Students, Teaching, or Operations?

### Learning Activities and Attendance

* Which activities require attendance?
* What attendance categories and correction rules apply?
* How is asynchronous or individual-progress participation evidenced?
* Does general school-event attendance remain solely with Operations?

### Assignments and Assessment

* ~~Which assignment models and submission types are required initially?~~ **Closed.** Assignments must originate from a Learning Activity. Required relationship: Learning Activity → Assignment → Submission. Missing submissions do not automatically become zero scores. Required submission statuses: Submitted, Late, Missing, Excused, Exempted. Grading consequences are determined by assessment policy, not submission status.
* Which assessment models are required initially?
* Who may assess, moderate, approve, correct, and publish results?
* How are reassessment attempts combined or selected?
* Which evidence must be retained and for how long?

### Progress and Completion

* ~~What progress states and completion criteria apply for the first school?~~ **Closed.** Phase-2 uses a minimal progress model. Required: Not Started, In Progress, Completed. Optional: Needs Remediation, Exempted. No percentage-based progress tracking in Phase 2.
* Can completion occur outside academic-period boundaries?
* How is transferred or prior learning recognized?
* Who may override a calculated completion outcome?

### Academic Intervention

* What triggers an academic intervention?
* Who owns and reviews an intervention plan?
* When should Academics refer a concern to Development?
* What cross-domain information may be shared?

### Standing and Progression

* ~~What defines Academic Standing?~~ **Closed.** Academic Standing represents an academic determination, not grades. Minimum standing model: Good Standing, Conditional Standing, Not In Good Standing.
* ~~Does Academics issue a recommendation or a binding academic decision?~~ **Closed.** Academics issues a recommendation only; Students executes the administrative transition. Promotion eligibility outcomes: Promotion Eligible, Retention Recommended, Conditional Promotion Recommendation.
* ~~Who has final promotion and retention authority?~~ **Closed.** Academics owns promotion eligibility determination; Students is the sole domain that executes official promotion or retention.
* ~~Can progression be conditional or occur outside the standard year transition?~~ **Closed.** Conditional Promotion Recommendation is a valid eligibility outcome. Conditions must be explicitly recorded.
* How are exceptions and committee decisions recorded?

### Graduation

* ~~Does Academics issue a graduation recommendation or a binding academic decision?~~ **Closed.** Academics owns graduation eligibility determination and may issue: Graduation Eligible, Graduation Not Eligible, or Conditional Graduation Recommendation. Students is the sole domain that executes official graduation.
* ~~Who has final graduation eligibility approval authority?~~ **Closed.** Academic Authority or Authorized Academic Committee as defined by school policy.
* ~~Is Academic Clearance a dedicated Academics capability or an eligibility recommendation?~~ **Closed.** Academic Clearance is a dedicated purpose-specific Academics capability. It is not Academic Standing and not promotion, retention, or graduation eligibility. Phase-2 purpose is Transfer Out only. Students requests; Academics determines; Students consumes. Routine determination is Academic Coordinator; exception determination is Academic Authority with a mandatory reason. Transfer-out execution requires Granted only. Correction uses a dedicated Correct Academic Clearance procedure.
* ~~How are Phase-2 assignment, standing, and eligibility records corrected without broadening SOP-AC-25?~~ **Closed.** A dedicated Correct Phase-2 Academic Record procedure covers Assignment, Submission, Submission Status, Submission Evaluation, Academic Standing, Promotion Recommendation, Retention Recommendation, and Graduation Eligibility. Student Learning Plan and Learning Progress remain Correct Academic Plan or Progress Record. Academic Clearance remains Correct Academic Clearance. Phase-1 approved Academic Result remains Correct Approved Academic Record. Attendance and participation remain outside this procedure. Changed circumstances use the owning operational workflow. Correction does not become an Academic Result and does not execute Students transitions.
* What academic requirements determine graduation eligibility?
* How are non-academic and financial prerequisites represented without moving their ownership?
* What happens when an approved result is corrected after graduation?

### Reporting and Publication

* Which report cards and transcripts are required?
* How should curriculum-specific layouts be configured?
* Who may see provisional, approved, and published information?
* Must historical reports be reproducible exactly?

### Teaching and Operations Boundaries

* Does Academics request teacher capacity before or after learning is offered?
* What information constitutes an authorized teaching assignment?
* Who creates the operational timetable?
* How are schedule changes reflected without changing academic ownership?

---

## Implementation Guardrails

Before implementing an Academics feature:

1. Identify the student learning outcome being supported.
2. Identify the actor and their authority.
3. Identify the applicable learning structure, requirement, and policy version.
4. Identify the evidence or input required by the workflow.
5. Distinguish provisional, approved, and published information.
6. Identify information owned by Students, Development, Finance, Teaching, or Operations.
7. Identify the academic output and its consumers.
8. Preserve history, effective dates, approvals, and corrections.
9. Confirm that the design supports the school's actual learning model.
10. Implement the minimum workflow.

Do not begin from:

* database tables
* CRUD screens
* API endpoints
* a conventional class-gradebook hierarchy
* a report-card layout
* generic LMS features
* dashboard requirements

Begin from the student's learning workflow.

Do not invent:

* curriculum structure
* grading formulas
* attendance categories
* completion or mastery rules
* reassessment rules
* approval and publication authority
* promotion, retention, or graduation criteria
* academic intervention policy
* access to sensitive cross-domain information

When policy or workflow is unclear, record it as an open question and stop before encoding the assumption as a business rule.

For every proposed concept, ask:

```text
Does this describe learning, evidence, progress, or an academic decision?

If not, which domain owns it?

Does this concept work without assuming a classroom?

Is it required by a real workflow now?
```

---

## Summary

The Academics domain answers these primary business questions:

```text
What is the student expected to learn?

What is the student currently learning?

Which learning activities did the student participate in?

What evidence of learning has been collected?

How well is the student progressing?

What academic support is needed?

What has the student completed or achieved academically?

What academic progression is recommended?
```

It does not answer:

```text
Who is the student or what is the student's official status?

Has the student been officially promoted or graduated?

Who is available or assigned to teach?

Which room or timetable slot is allocated?

How is the student behaving or feeling?

What does the student owe?
```

Those questions belong to Students, Teaching, Operations, Development, and Finance respectively.
