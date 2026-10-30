# Students Domain

## Purpose

The Students domain manages the administrative identity and lifecycle of every student.

It is the authoritative source for:

* who the student is
* whether the person has been accepted as a student
* the student's official administrative status
* the student's administrative placement
* the student's lifecycle history within the school

The Students domain begins when a person applies to join the school and continues after the person graduates or leaves the school.

This domain does not manage the student's learning activities, academic results, behaviour, counselling, financial obligations, or attendance.

---

## Domain Responsibility

The Students domain owns:

* applicant identity
* student identity
* parent and guardian information
* admission
* acceptance
* enrollment
* student number assignment
* administrative placement
* active student status
* leave of absence
* promotion execution
* retention execution
* internal transfer
* external transfer
* withdrawal
* graduation execution
* alumni status
* administrative lifecycle history

The Students domain is the authoritative source of student identity and administrative status.

Other domains may reference student information but must not redefine or independently change it.

---

## Scope

### In Scope

The Students domain includes the following business capabilities.

#### Student Identity

Manage the official identity of an applicant or student.

Examples include:

* full name
* preferred name
* date and place of birth
* gender
* nationality
* government-issued identification
* student number
* contact information
* home address
* previous school
* supporting documents

The exact identity information required may differ between schools and jurisdictions.

---

#### Parent and Guardian Information

Manage the people who are officially associated with the student.

Examples include:

* parent
* legal guardian
* emergency contact
* authorized pickup person
* financial contact

A person may hold more than one responsibility.

The Students domain records the relationship between the person and the student.

It does not manage financial transactions performed by the person.

---

#### Admission

Manage the process through which a prospective student applies to join the school.

Admission may include:

* application registration
* applicant identity
* admission period
* requested entry level
* requested campus or program
* document submission
* application review
* admission decision
* acceptance
* rejection
* applicant withdrawal

Admission determines whether an applicant may proceed to enrollment.

Admission does not make the applicant an active student.

---

#### Enrollment

Manage the formal administrative registration of an accepted applicant as a student.

Enrollment may include:

* acceptance confirmation
* required document completion
* student number assignment
* enrollment date
* initial administrative placement
* initial campus
* initial level or grade
* initial program
* initial cohort
* activation of student status

Enrollment establishes the person as an official student of the school.

Admission answers:

> May this applicant join the school?

Enrollment answers:

> Has this accepted applicant officially joined the school?

---

#### Administrative Placement

Manage where the student is administratively registered.

Placement may include:

* campus
* school level
* grade or year level
* program
* stream
* cohort
* homeroom or administrative group

Administrative placement is not the same as a student's learning plan.

The Academics domain may organize learning differently from the student's administrative placement.

For example, a student may be administratively registered in Grade 6 while taking selected learning modules from another level.

---

#### Student Status

Manage the student's official administrative relationship with the school.

The minimum conceptual statuses are:

* Applicant
* Accepted
* Enrolled
* Active
* On Leave
* Leave Expired
* Transferred
* Withdrawn
* Graduated
* Alumni

These are business concepts, not mandatory database values.

A more detailed status model may be defined during workflow design.

Only the Students domain may change the official administrative status of a student.

---

#### Leave of Absence

Manage temporary interruption of the student's active administrative participation.

A leave record should identify:

* effective date
* expected return date, when known
* reason
* approval
* supporting information
* actual return date

Leave does not end the student's relationship with the school.

Leave expiry does not automatically reactivate, withdraw, or otherwise change student status. When the expected return date passes without an explicit return action, the leave transitions to a distinct administrative state: Leave Expired. Return from leave requires an explicit administrative action to transition the student back to Active, and must be recorded historically.

The student's learning and financial consequences are handled by their respective domains.

---

#### Promotion and Retention Execution

Manage the official administrative movement of a student from one level or period to another.

The Academics domain may produce a recommendation such as:

* promote
* retain
* conditionally promote
* eligible for progression

The Students domain applies the approved administrative transition.

Students owns:

* effective promotion date
* previous placement
* new placement
* approval reference
* administrative history

Students does not calculate academic eligibility.

---

#### Transfer

Manage changes in the student's administrative placement or school relationship.

Transfer may include:

##### Internal Transfer

Movement within the same school organization, such as:

* campus transfer
* program transfer
* stream transfer
* administrative group transfer

##### External Transfer In

Enrollment of a student who previously attended another school.

##### External Transfer Out

Administrative release of a student who moves to another school.

Transfer records should preserve:

* previous placement
* destination placement or institution
* effective date
* reason
* approval
* supporting documents

A transfer must not erase the student's previous administrative history.

---

#### Withdrawal

Manage the ending of a student's enrollment before graduation.

Withdrawal may occur because of:

* voluntary withdrawal
* relocation
* prolonged absence
* disciplinary decision
* financial decision
* medical or personal circumstances
* other approved reasons

The Students domain records the official withdrawal.

Other domains may provide information or prerequisite decisions, but they do not directly change the student status.

---

#### Graduation

Manage the official completion of the student's enrollment.

The Academics domain determines or recommends academic eligibility for graduation.

The Students domain executes graduation after all required approvals and administrative conditions are satisfied.

Graduation may record:

* graduation date
* completion level
* certificate or diploma information
* approval reference
* final administrative placement
* graduation cohort

Graduation ends active enrollment and allows the student to become alumni.

---

#### Alumni

Manage the student's administrative identity after graduation.

Alumni information may include:

* alumni status
* graduation cohort
* current contact information
* communication consent

The Students domain maintains continuity between the former student identity and alumni identity.

Alumni scope is intentionally minimal: alumni CRM, fundraising, events, donations, networking, and engagement programs are outside the current core scope unless explicitly added later.

---

#### Administrative History

Preserve the student's administrative lifecycle as a historical record.

Examples include:

* application submitted
* application accepted
* enrollment completed
* student activated
* placement changed
* leave started
* leave completed
* student promoted
* student retained
* student transferred
* student withdrawn
* student graduated
* student marked as alumni

Historical records must not be silently overwritten.

Corrections should preserve what changed, when it changed, and who authorized the change.

---

## Out of Scope

The Students domain does not own:

### Academics

* learning activities
* courses
* subjects
* modules
* assignments
* attendance
* assessments
* grades
* report cards
* learning progress
* academic achievements
* academic standing calculation
* promotion eligibility calculation
* graduation eligibility calculation

### Development

* behaviour
* discipline cases
* counselling
* student wellbeing
* extracurricular participation
* non-academic intervention
* non-academic achievements

### Finance

* billing
* tuition
* payment
* discount
* scholarship
* refund
* receivable
* payment allocation

### Teaching

* teacher identity
* teacher availability
* teacher workload
* teaching assignment
* teacher qualification

### Operations

* academic calendar
* school events
* announcements
* room allocation
* operational scheduling
* cross-domain approvals

### Other Exclusions

* payroll
* library
* transportation
* cafeteria
* hostel
* learning content
* accounting
* inventory
* procurement
* CRM
* general alumni engagement

---

## Actors

The exact actor names may differ between schools.

The domain commonly involves the following actors.

### Applicant

A person applying to join the school.

May:

* submit an application
* provide identity information
* provide required documents
* accept or decline an offer
* withdraw an application

---

### Parent or Guardian

A person responsible for an applicant or student.

May:

* provide student information
* provide supporting documents
* confirm enrollment
* request administrative changes
* receive official communication

A parent or guardian does not automatically have permission to perform every action.

Authority depends on school policy and the person's relationship to the student.

---

### Admission Officer

Responsible for operational admission work.

May:

* register applications
* verify documents
* request missing information
* prepare applications for review
* record admission decisions
* prepare accepted applicants for enrollment

---

### Student Administration Officer

Responsible for the student's administrative records.

May:

* complete enrollment
* assign student numbers
* maintain student identity
* update administrative placement
* process leave
* process transfers
* execute promotion
* execute withdrawal
* execute graduation

---

### School Leadership

Provides authority for decisions requiring approval.

Depending on school policy, this may include:

* principal
* head of school
* registrar
* academic director
* foundation representative
* campus director

---

### Academic Authority

Provides academic recommendations consumed by the Students domain.

May recommend:

* promotion
* retention
* progression
* graduation eligibility

The Academic Authority does not directly change the student's administrative status.

---

### System Administrator

Configures access and system operation.

The System Administrator does not automatically have authority to make business decisions.

Technical access must not be treated as business authorization.

---

## Core Business Concepts

### Applicant

A person who has submitted or is being prepared to submit an admission application.

An Applicant is not yet a Student.

---

### Admission Application

A request by an Applicant to join the school for a specific admission period and intended placement.

A person may have more than one application over time.

Each application should remain historically identifiable.

---

### Admission Decision

The official decision regarding an Admission Application.

Typical outcomes include:

* accepted
* rejected
* waitlisted
* withdrawn
* expired

The exact decision model remains configurable according to school policy.

---

### Student

A person who has completed enrollment and has an official administrative relationship with the school.

---

### Student Number

The school's official identifier for a Student.

The format and assignment policy may vary by school.

A student number must not be reused for a different person.

---

### Enrollment

The administrative act that establishes an accepted Applicant as a Student.

Enrollment is distinct from yearly academic participation.

Whether annual re-enrollment belongs in this domain remains an open question.

---

### Administrative Placement

The student's official organizational placement within the school.

Placement may change over time and must preserve history.

---

### Student Status

The student's current official administrative relationship with the school.

Student Status is owned exclusively by the Students domain.

---

### Lifecycle Transition

An approved change from one administrative state or placement to another.

Examples include:

* accepted applicant to enrolled student
* enrolled student to active student
* active student to on leave
* on leave to active
* active student to transferred
* active student to withdrawn
* active student to graduated
* graduated student to alumni

---

### Administrative Decision

A recorded decision authorizing a lifecycle transition.

A decision should identify:

* decision type
* decision date
* decision maker
* reason
* supporting reference
* effective date

---

## Student Lifecycle

The general lifecycle is:

```text
Prospective Person
        |
        v
Applicant
        |
        v
Admission Review
        |
        +----> Rejected
        |
        +----> Withdrawn
        |
        v
Accepted
        |
        v
Enrollment
        |
        v
Active Student
        |
        +----> On Leave ----> Active Student
        |
        +----> Internal Transfer
        |
        +----> Promoted or Retained
        |
        +----> External Transfer
        |
        +----> Withdrawn
        |
        v
Graduated
        |
        v
Alumni
```

This lifecycle is conceptual.

Not every school must use every state or transition.

Implementation should begin with the minimum lifecycle required by the first participating school.

---

## Main Workflows

## 1. Register Admission Application

### Trigger

A prospective student begins an application.

### Actor

* Applicant
* Parent or Guardian
* Admission Officer

### Input

* applicant identity
* intended admission period
* intended entry level
* contact information
* required initial documents

### Outcome

* admission application is registered
* applicant identity is established
* application receives a traceable reference

### Business Rules

* an application must belong to one applicant
* an application must target one admission period
* duplicate applicant identities should be detected, not silently created
* incomplete applications may be saved according to school policy
* application history must be preserved

---

## 2. Review Admission Application

### Trigger

An application is ready for review.

### Actor

* Admission Officer
* Admission Committee
* Authorized School Leader

### Input

* application
* required documents
* admission criteria
* assessment information, when applicable

### Outcome

One admission decision is recorded.

Possible outcomes may include:

* accepted
* rejected
* waitlisted
* returned for completion
* withdrawn

### Business Rules

* only authorized actors may record the final decision
* the decision must identify its effective date
* changing a final decision requires an explicit correction or reconsideration process
* academic or entrance assessment details may be produced by another capability, but the admission decision belongs to Students

---

## 3. Enroll Accepted Applicant

### Trigger

An accepted applicant confirms enrollment and satisfies required administrative conditions.

### Actor

* Student Administration Officer
* Parent or Guardian
* Authorized School Leader

### Input

* accepted application
* required identity information
* required enrollment documents
* initial administrative placement
* enrollment approval

### Outcome

* official student identity is created
* student number is assigned
* initial placement is recorded
* student administrative status becomes enrolled or active
* enrollment history is preserved

### Business Rules

* only an accepted applicant may be enrolled
* one applicant identity must not create duplicate student identities
* student number must be unique within its defined school scope
* enrollment must preserve its source admission application
* enrollment must not create academic results or financial transactions directly

---

## 4. Maintain Student Identity

### Trigger

Official student information must be added, corrected, or updated.

### Actor

* Student Administration Officer
* Parent or Guardian
* Student, when permitted

### Input

* requested change
* supporting evidence
* authorization, when required

### Outcome

* current identity information is updated
* significant changes are recorded historically

### Business Rules

* official identity changes may require supporting documents
* corrections must not hide previous official information
* identity changes do not create a new student
* duplicate students must not be merged automatically

---

## 5. Change Administrative Placement

### Trigger

The student's official placement changes.

### Actor

* Student Administration Officer
* Authorized School Leader

### Input

* current placement
* new placement
* effective date
* reason
* approval

### Outcome

* new placement becomes effective
* previous placement remains in history

### Business Rules

* placement changes must have an effective date
* previous placement must remain traceable
* placement changes must not silently modify academic history
* administrative placement must not redefine the student's learning structure

---

## 6. Place Student on Leave

### Trigger

An approved temporary leave is requested.

### Actor

* Parent or Guardian
* Student Administration Officer
* Principal or delegated School Leader

### Input

* leave request
* leave period
* reason
* approval
* supporting documents, when required

### Outcome

* student status becomes On Leave
* leave history is recorded

### Business Rules

* leave requires an effective start date
* expected return date may be optional
* maximum leave duration is 1 Academic Year
* returning from leave requires an explicit action (including returning from Leave Expired)
* leave expiry does not automatically reactivate, withdraw, or otherwise change student status; expired leave transitions to a distinct administrative state: Leave Expired
* financial and academic consequences are handled by Finance and Academics

---

## 7. Return Student from Leave

### Trigger

The student requests return from leave (while the student is in On Leave or Leave Expired) after an approved leave period.

### Actor

* Student Administration Officer
* Principal or delegated School Leader

### Input

* active leave record
* return confirmation
* effective return date

### Outcome

* leave is closed
* student status returns to Active
* return is recorded in administrative history

---

## 8. Execute Promotion or Retention

### Trigger

An approved progression decision is available.

### Actor

* Academic Authority
* Student Administration Officer
* Authorized School Leader

### Input

* academic recommendation
* administrative approval
* current placement
* target placement
* effective date

### Outcome

* student is promoted, retained, or otherwise placed
* official administrative placement is updated
* transition history is preserved

### Business Rules

* Academics owns eligibility evaluation and issues recommendation (Promote, Retain, or Conditional Promote with explicitly recorded conditions); Academics recommendation does not directly change student administrative status
* Students executes the administrative transition by applying the approved recommendation outcome (including the recorded conditions for Conditional Promote)
* promotion must not be inferred solely from grade records
* retention must be explicitly recorded
* bulk promotion may process some students successfully while failing others; each student outcome must be individually preserved
* failed records in a bulk process must be identifiable and recoverable without rerunning successful records

---

## 9. Process Internal Transfer

### Trigger

The student moves to another campus, program, stream, or administrative unit within the same school organization.

### Actor

* Student Administration Officer
* Authorized School Leader

### Input

* current placement
* destination placement
* effective date
* reason
* approval

### Outcome

* destination placement becomes active
* previous placement is retained historically

### Business Rules

* transfer must not create a duplicate student
* transfer must preserve the same student identity
* effective dates must not create unexplained placement gaps or overlaps
* academic consequences are handled by Academics
* financial consequences are handled by Finance

---

## 10. Process External Transfer Out

### Trigger

The student officially leaves to attend another school.

### Actor

* Parent or Guardian
* Student Administration Officer
* Authorized School Leader

### Input

* transfer request
* destination school, when known
* effective date
* reason
* required Academic Clearance and Finance Clearance (additional clearances may be added by policy without changing ownership)

### Outcome

* student status becomes Transferred
* transfer documents may be issued
* administrative history is preserved

### Business Rules

* transfer must be explicitly approved
* Students requests Academic Clearance; Academics determines it; Students consumes the owned result and does not determine it
* Students requests Financial Clearance for purpose Transfer Out; Finance determines it; Students consumes the owned result and does not determine it
* external transfer-out requires Academic Clearance for purpose Transfer Out with result Granted, and Financial Clearance for purpose Transfer Out with result Granted (additional clearances may be added by policy without changing ownership)
* Denied, Conditional, Cannot Yet Be Determined, Expired, and Revoked Academic Clearance or Financial Clearance do not permit transfer-out
* outstanding academic or financial matters may be reported as prerequisites but do not independently change the status; transfer release occurs only after required clearances are satisfied
* transfer must not delete the student record
* the effective transfer date must be recorded

---

## 11. Process Withdrawal

### Trigger

The student's enrollment is ended without transfer or graduation.

### Actor

* Parent or Guardian
* Student Administration Officer
* Authorized School Leader

### Input

* withdrawal request or decision
* reason
* effective date
* approval
* supporting information

### Outcome

* student status becomes Withdrawn
* administrative history is preserved

### Business Rules

* withdrawal requires an explicit reason
* withdrawal must identify the authority approving it
* disciplinary, financial, academic, or wellbeing information may support the decision but remains owned by the originating domain
* withdrawal must not delete the student record

---

## 12. Execute Graduation

### Trigger

The student has been declared eligible and all required administrative approvals are complete, and the configured graduation financial clearance prerequisite is granted by Finance per school policy (default: required; limited to graduation-related obligations).

### Actor

* Academic Authority
* Student Administration Officer
* Authorized School Leader
* Finance (graduation financial clearance source)

### Input

* graduation eligibility
* administrative approval
* graduation date
* certificate or diploma information, when applicable

### Outcome

* student status becomes Graduated
* graduation record is created
* alumni status is established automatically when graduation execution succeeds (no separate alumni activation workflow)

### Business Rules

* Academics provides graduation eligibility
* Students executes the official graduation
* Finance provides configured graduation financial clearance; default is required before graduation; when required, execution consumes Granted only; only graduation-related obligations participate in clearance evaluation
* Denied, Conditional, Cannot Yet Be Determined, Expired, and Revoked Financial Clearance do not permit graduation when clearance is required
* Students requests Financial Clearance for purpose Graduation when policy requires that gate; Finance determines it; Students consumes the owned result
* graduation must not be inferred directly from the end of an academic period
* graduation records must remain immutable except through an explicit correction process

---

## 13. Activate Alumni Status

### Trigger
Graduation is successfully executed; alumni status is established automatically (no separate activation workflow is required).

### Actor

* Automatic within Execute Graduation (no separate manual activation workflow)

### Input

* graduation record
* alumni activation date (set as part of the graduation execution)

### Outcome

* alumni status is established
* identity continuity with the former student is preserved

---

## 14. Request Academic Clearance for Transfer Out

### Trigger

An Active student is preparing for external transfer-out and Students needs an Academics-owned Academic Clearance result.

### Actor

* Parent or Guardian
* Student Administration Officer

### Input

* student identity
* clearance purpose Transfer Out
* effective date or needed-by date when school procedure requires it

### Outcome

* Students records an Academic Clearance request for purpose Transfer Out
* Academics receives the request
* Students does not grant, deny, or otherwise determine Academic Clearance

### Business Rules

* Students owns the request and the later transfer-out action
* Academics owns the clearance determination
* this workflow does not set Transferred status

---

## 15. Request Purpose-Specific Financial Clearance

### Trigger

An Active student is preparing for external transfer-out, or for graduation when school policy requires graduation financial clearance, and Students needs a Finance-owned Financial Clearance result.

### Actor

* Parent or Guardian
* Student Administration Officer

### Input

* student identity
* clearance purpose Transfer Out or Graduation
* effective date or needed-by date when school procedure requires it

### Outcome

* Students records a Financial Clearance request for the stated purpose
* Finance receives the request
* Students does not grant, deny, or otherwise determine Financial Clearance

### Business Rules

* Phase-2 requests are for Transfer Out or Graduation only
* Enrollment financial clearance remains the Phase-0 request procedure
* Students owns the request and the later administrative action
* Finance owns the clearance determination
* this workflow does not set Transferred or Graduated status

---

## Business Rules

### Identity Ownership

Students is the only authoritative source for student identity.

Other domains may cache or reference identity information but must not independently redefine it.

---

### Administrative Status Ownership

Only Students may change the student's official administrative status.

Other domains may:

* recommend
* request
* approve
* provide prerequisites
* provide supporting information

They may not directly perform the status transition.

---

### One Person, One Student Identity

A person should have one student identity within the defined school scope.

Multiple admission applications must not automatically produce multiple student records.

Potential duplicates must be reviewed explicitly.

---

### Historical Integrity

Administrative history must be preserved.

Do not overwrite:

* previous placements
* previous status periods
* previous identity facts that have legal or operational importance
* previous decisions
* previous approvals

---

### Effective Dating

Lifecycle transitions should identify when they become effective.

The date on which a decision is recorded may differ from the date on which it takes effect.

---

### Explicit Transitions

Important lifecycle changes must occur through explicit business actions.

Do not derive official status merely from:

* current date
* absence of activity
* academic grade
* unpaid bill
* end of school year
* class placement
* missing schedule

---

### Cross-Domain Recommendations

A recommendation from another domain is not the same as an administrative transition.

Examples:

```text
Academics:
Student is eligible for promotion.

Students:
Student is officially promoted.
```

```text
Academics:
Student is eligible for graduation.

Students:
Student is officially graduated.
```

```text
Finance:
Student has unpaid obligations.

Students:
Student remains active unless an authorized withdrawal or suspension decision is executed.
```

---

### No Automatic Deletion

Student and applicant records must not be physically removed merely because an application was rejected, withdrawn, duplicated, or cancelled.

Invalid or duplicate records should be resolved through an explicit administrative process.

---

### Minimum Required Information

The system should require only information necessary for the current workflow.

Do not require every possible student field during initial application or enrollment.

Information may be completed progressively.

---

### Separation of Identity and Participation

Student identity should remain stable even when:

* status changes
* placement changes
* campus changes
* the student takes leave
* the student graduates
* the student becomes alumni

---

## Inputs from Other Domains

### From Academics

* promotion recommendation
* retention recommendation
* progression recommendation
* graduation eligibility
* academic standing recommendation
* Academic Clearance result and reference

Students consumes these as supporting decisions.

Students does not recalculate them.

---

### From Development

* approved administrative recommendation related to student wellbeing or behaviour
* supporting decision reference for withdrawal or other administrative action

Students does not own counselling, discipline, or behaviour records.

---

### From Finance

* financial clearance information
* outstanding obligation information
* enrollment payment confirmation, when school policy requires it
* Transfer Out financial clearance result and reference
* graduation clearance information

Finance information may be a prerequisite.

Finance does not directly change student status.

---

### From Operations

* admission period
* academic year
* school calendar context
* campus information
* operational approval context
* school event context

Operations provides context but does not own student lifecycle decisions.

---

## Outputs to Other Domains

### To Academics

* student identity reference
* enrollment completed
* student activated
* administrative placement changed
* leave started
* leave ended
* student transferred
* student withdrawn
* student graduated

---

### To Development

* student identity reference
* enrollment completed
* student activated
* placement changed
* student transferred
* student withdrawn
* student graduated

---

### To Finance

* applicant accepted
* enrollment initiated
* enrollment completed
* student activated
* placement changed
* leave started
* student transferred
* student withdrawn
* student graduated

Finance decides the financial consequences.

---

### To Teaching

* student identity reference
* student activated
* administrative placement changed
* student transferred
* student withdrawn
* student graduated

Teaching may use this information for teaching and homeroom context.

---

### To Operations

* applicant and student counts
* enrollment completion
* student placement
* student status changes
* transfer
* withdrawal
* graduation

Operations consumes this information for coordination and planning.

---

## Domain Events

The following are conceptual business events.

They are not mandatory technical event names.

* AdmissionApplicationRegistered
* AdmissionApplicationCompleted
* AdmissionApplicationAccepted
* AdmissionApplicationRejected
* AdmissionApplicationWithdrawn
* StudentEnrolled
* StudentNumberAssigned
* StudentActivated
* StudentIdentityCorrected
* StudentPlacementChanged
* StudentLeaveStarted
* StudentReturnedFromLeave
* StudentPromoted
* StudentRetained
* StudentInternallyTransferred
* StudentTransferredOut
* StudentWithdrawn
* StudentGraduated
* AlumniStatusActivated

Technical event design belongs in the architecture document.

---

## Ownership Matrix

| Business Concept                 | Owner       | Other Domain Involvement                       |
| -------------------------------- | ----------- | ---------------------------------------------- |
| Applicant Identity               | Students    | Operations may provide admission context       |
| Student Identity                 | Students    | Referenced by all domains                      |
| Parent and Guardian Relationship | Students    | Finance may reference financial contacts       |
| Admission Application            | Students    | Academics may provide assessment information   |
| Admission Decision               | Students    | Authorized committees may participate          |
| Enrollment                       | Students    | Finance may provide payment confirmation       |
| Student Number                   | Students    | Referenced by all domains                      |
| Administrative Placement         | Students    | Academics and Teaching consume it              |
| Student Status                   | Students    | Other domains may recommend changes            |
| Leave of Absence                 | Students    | Academics and Finance determine consequences   |
| Academic Clearance               | Academics   | Students requests and consumes the result      |
| Promotion Eligibility            | Academics   | Students consumes recommendation; is notified for review if a consumed recommendation is later corrected |
| Promotion Execution              | Students    | Academics provides recommendation              |
| Graduation Eligibility           | Academics   | Students consumes recommendation; is notified for review if a consumed eligibility result is later corrected |
| Graduation Execution             | Students    | Finance may provide clearance                  |
| Transfer                         | Students    | Academics provides Academic Clearance; Finance provides Finance Clearance |
| Withdrawal                       | Students    | Other domains may provide supporting decisions |
| Alumni Status                    | Students    | Operations may use alumni information          |
| Attendance                       | Academics   | Students does not own it                       |
| Academic Results                 | Academics   | Students does not own them                     |
| Behaviour and Counselling        | Development | Students does not own them                     |
| Billing and Payment              | Finance     | Students does not own them                     |

---

## Reporting Responsibility

Students may provide operational information such as:

* applications by admission period
* admission decisions
* accepted applicants
* completed enrollments
* active students
* students on leave
* students by administrative placement
* promotions and retentions
* internal transfers
* external transfers
* withdrawals
* graduations
* alumni counts

These reports must be produced from actual lifecycle workflows.

Do not create separate data-entry processes solely for reporting.

Cross-domain management dashboards may combine Students information with Academics, Development, Finance, Teaching, or Operations information, but ownership must remain explicit.

---

## Guiding Principles

### Administrative Lifecycle First

Design features around real student administration work.

Do not begin from a student master-data screen.

---

### One Authoritative Student Identity

Every domain must refer to the student identity owned by Students.

---

### Explicit State Changes

Administrative transitions must be visible, intentional, and authorized.

---

### Preserve History

Do not replace historical placement or status information with only the latest value.

---

### Recommendation Is Not Execution

Other domains may determine eligibility or provide recommendations.

Students owns the official administrative transition.

---

### Minimum Information First

Collect information when it becomes operationally necessary.

Avoid large mandatory forms at the beginning of the lifecycle.

---

### School Policy Is Configuration

Differences between schools should primarily be handled through policy and configuration.

Do not create a new top-level domain because one school uses different admission, promotion, or graduation rules.

---

### Simple Before Flexible

Implement the minimum lifecycle required by the current school.

Do not introduce speculative workflows, generalized rule engines, or unnecessary abstractions.

---

## Open Questions

The following questions remain unresolved and must be clarified before detailed implementation.

### Admission

* Is admission required for every student, including transferred students?
* Can one applicant apply to multiple campuses, programs, or levels simultaneously?
* Is waitlisting required in the first implementation?
* Who has final authority to accept or reject an application?
* Are entrance assessments part of Students or provided by Academics?

### Enrollment

* What conditions must be satisfied before enrollment is complete?
* Is payment confirmation required before enrollment?
* When is the student number assigned?
* Can an accepted applicant defer enrollment?
* Is yearly re-enrollment part of Students or Academics?

### Student Identity

* What identity fields are legally required?
* Can one student have multiple guardians?
* Which guardian is the primary contact?
* How are duplicate student identities resolved?
* Which identity changes require approval or supporting documents?

### Administrative Placement

* What is the minimum administrative placement model?
* Is homeroom placement owned by Students, Teaching, or Operations?
* Can students belong to multiple administrative groups?
* How should mid-period placement changes be handled?

### Promotion and Retention

* Can students be promoted outside the standard academic-year transition?

### Leave


### Transfer

* ~~Who determines Academic Clearance for external transfer-out?~~ **Closed.** Academics owns Academic Clearance as a dedicated capability. Students requests for purpose Transfer Out; Academics determines; Students consumes. Transfer-out execution requires result Granted only.
* ~~Who determines Financial Clearance for external transfer-out and graduation?~~ **Closed.** Finance owns Financial Clearance. Students requests purpose Transfer Out or Graduation; Finance determines; Students consumes. Enrollment clearance remains Phase-0 and is not reused. Transfer-out and required graduation execution consume Granted only. Conditional is not executable.
* How is multi-campus transfer handled?
* Does a campus transfer preserve the same student number?
* Is transfer between school levels considered internal transfer or new enrollment?

### Withdrawal

* Who may authorize withdrawal?
* Which withdrawal reasons must be standardized?
* Does expulsion use the withdrawal workflow or a separate administrative process?
* Can a withdrawn student later re-enroll using the same identity?

### Graduation and Alumni

* Who provides final graduation approval?

### Multi-Campus

* Is student identity global across all campuses or local to one campus?
* Is the student number unique across the school organization or per campus?
* Can a student be active in more than one campus?
* Which decisions are controlled centrally and which are controlled by campus?

---

## Implementation Guardrails

Before implementing a Students feature:

1. Identify the actor.
2. Identify the current lifecycle state.
3. Identify the intended administrative outcome.
4. Identify the required authority.
5. Identify information consumed from another domain.
6. Identify information produced for another domain.
7. Preserve the previous state and history.
8. Implement the minimum workflow.

Do not begin from:

* database tables
* CRUD screens
* API endpoints
* existing student master data
* generic education software patterns

Begin from the administrative workflow.

Do not invent:

* admission rules
* required documents
* promotion authority
* graduation authority
* status transitions
* approval requirements
* multi-campus rules

When these are unclear, record them as open questions before implementation.

---

## Summary

The Students domain answers five primary business questions:

```text
Who is this student?

How did this person enter the school?

What is the student's official administrative status?

Where is the student administratively placed?

How did the student's administrative relationship with the school change over time?
```

It does not answer:

```text
What is the student learning?

How well is the student performing?

How is the student behaving or developing?

What does the student owe?

Who is teaching the student?
```

Those questions belong to other domains.
