# Elsasa SIS V3 — Use-Case Inventory

---

## Executive Summary

This document establishes the authoritative **Use-Case Inventory** for the Elsasa Student Information System (SIS) V3. It translates the 86 operational business workflows identified across all six core domains into actionable, actor-centric use cases.

Each use case defines the **actual work** that an authorized human or institutional actor needs to accomplish to achieve a business goal. In accordance with architectural principles:
- **Business-goal oriented:** Every use case is defined by what the actor needs to accomplish and the expected business outcome, not by database entities, data structures, or CRUD operations (Create, Read, Update, Delete).
- **Actor-centric and boundary-respecting:** Use cases strictly enforce actor responsibilities, decision authority hierarchies, and domain boundaries established in `actors.md` and primary domain specifications.
- **Pragmatic and minimalist:** Includes only the use cases required to operate the school across its three implementation stages. Speculative features, future capabilities, and undocumented approval workflows are deliberately excluded.
- **Foundation for next phases:** This inventory provides the direct input for subsequent **Activity Decomposition** and **Screen Inventory** specifications.

---

## Inventory Structure and Principles

### Principles Enforced
1. **Pragmatic:** Focused exclusively on what school operations require to function.
2. **Minimalist:** No duplicate use cases representing the same core business goal.
3. **Explicit:** Every use case specifies an ID, action-oriented Name, Primary Actor, Supporting Actors (where meaningful), Business Goal, Expected Outcome, and Source Workflow Reference.
4. **Incremental:** Sequenced according to the approved three-stage roadmap (`Stage 1: Foundation`, `Stage 2: Teaching & Learning`, `Stage 3: Development & Maturity`).

### Attribute Specification

| Attribute | Description |
|---|---|
| **ID** | Unique identifier prefixed by domain code (`UC-STD-xx`, `UC-ACD-xx`, `UC-FIN-xx`, `UC-OPS-xx`, `UC-TCH-xx`, `UC-DEV-xx`) |
| **Name** | Clear, action-oriented business verb phrase |
| **Primary Actor** | The actor responsible for driving the interaction to achieve the business goal |
| **Supporting Actor(s)** | Actors providing mandatory inputs, approvals, or collaborative participation |
| **Goal** | What the primary actor needs to accomplish in business terms |
| **Expected Outcome** | The measurable business state or artifact produced upon success |
| **Workflow Reference** | Authoritative workflow code from `operational-workflow-discovery.md` |

---

## 1. Students Domain Use Cases

The Students Domain owns student identity, admission intake, enrollment, administrative placement, lifecycle status transitions, and administrative history.

### Summary by Actor

| Actor | Use Case ID | Use Case Name | Workflow Reference | Stage |
|---|---|---|---|---|
| **Applicant / Parent / Guardian** | `UC-STD-01` | Register Admission Application | `STD-WF-01` | Stage 1 |
| | `UC-STD-02` | Submit Re-enrollment Confirmation | `STD-WF-16` | Stage 2 |
| **Admission Officer** | `UC-STD-03` | Review Admission Application | `STD-WF-02` | Stage 1 / Stage 2 |
| **Student Administration Officer** | `UC-STD-04` | Enroll Accepted Applicant | `STD-WF-03` | Stage 1 |
| | `UC-STD-05` | Maintain Student Identity | `STD-WF-04` | Stage 1 |
| | `UC-STD-06` | Change Administrative Placement | `STD-WF-05` | Stage 1 |
| | `UC-STD-07` | Place Student on Leave | `STD-WF-06` | Stage 2 |
| | `UC-STD-08` | Return Student from Leave | `STD-WF-07` | Stage 2 |
| | `UC-STD-09` | Execute Promotion or Retention | `STD-WF-08` | Stage 2 |
| | `UC-STD-10` | Process Internal Transfer | `STD-WF-09` | Stage 2 |
| | `UC-STD-11` | Process External Transfer Out | `STD-WF-10` | Stage 3 |
| | `UC-STD-12` | Process Withdrawal | `STD-WF-11` | Stage 3 |
| | `UC-STD-13` | Execute Graduation | `STD-WF-12` | Stage 3 |
| | `UC-STD-14` | Activate Alumni Status | `STD-WF-13` | Stage 3 |
| | `UC-STD-15` | Request Academic Clearance for Transfer Out | `STD-WF-14` | Stage 3 |
| | `UC-STD-16` | Request Purpose-Specific Financial Clearance | `STD-WF-15` | Stage 3 |
| | `UC-STD-17` | Process Student Re-enrollment | `STD-WF-16` | Stage 2 |
| | `UC-STD-18` | Process External Transfer In | `STD-WF-17` | Stage 3 |

---

### 1.1 Applicant / Parent / Guardian

#### `UC-STD-01`. Register Admission Application
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-01` |
| **Name** | Register Admission Application |
| **Primary Actor** | Applicant / Parent / Guardian |
| **Supporting Actor(s)** | Admission Officer (for assisted/in-person registration) |
| **Goal** | Submit prospective learner personal identity information, intended entry grade/program, contact details, and required documentary credentials for an open admission period. |
| **Expected Outcome** | An admission application is created with a unique application number, initial status `Submitted`, and linked applicant credentials. |
| **Workflow Reference** | `STD-WF-01` |

#### `UC-STD-02`. Submit Re-enrollment Confirmation
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-02` |
| **Name** | Submit Re-enrollment Confirmation |
| **Primary Actor** | Parent / Guardian |
| **Supporting Actor(s)** | Student (when permitted by school policy) |
| **Goal** | Formally declare the intent for an active student to continue enrollment for the upcoming academic period and provide updated contact or emergency details. |
| **Expected Outcome** | Re-enrollment submission is lodged for the upcoming academic period and queued for administrative confirmation and fee check. |
| **Workflow Reference** | `STD-WF-16` |

---

### 1.2 Admission Officer

#### `UC-STD-03`. Review Admission Application
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-03` |
| **Name** | Review Admission Application |
| **Primary Actor** | Admission Officer |
| **Supporting Actor(s)** | School Leadership (contested/quota exceptions), Academic Authority (entrance exam evaluation in Stage 2) |
| **Goal** | Evaluate applicant credentials and entrance assessment results against institutional admission criteria and record an official admission decision. |
| **Expected Outcome** | Official admission decision (`Accepted`, `Rejected`, `Waitlisted`, or `Returned for Correction`) is recorded and formal decision notice is issued to the applicant. |
| **Workflow Reference** | `STD-WF-02` |

---

### 1.3 Student Administration Officer

#### `UC-STD-04`. Enroll Accepted Applicant
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-04` |
| **Name** | Enroll Accepted Applicant |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | Finance Officer (payment confirmation gate), School Leadership (payment override GAP-01) |
| **Goal** | Establish official student identity for an accepted applicant upon satisfaction of the mandatory enrollment fee payment gate (or authorized executive override). |
| **Expected Outcome** | Student identity is activated, unique Student Number is permanently assigned, initial administrative placement is assigned, student status becomes `Active`, and student financial account is initialized. |
| **Workflow Reference** | `STD-WF-03` |

#### `UC-STD-05`. Maintain Student Identity
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-05` |
| **Name** | Maintain Student Identity |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | Parent / Guardian, Student |
| **Goal** | Update and verify official student legal identity, biographical information, citizenship, residential address, and parent/guardian relationship records. |
| **Expected Outcome** | Certified biographical and guardian data are updated, maintaining an auditable change history without altering student identity numbers. |
| **Workflow Reference** | `STD-WF-04` |

#### `UC-STD-06`. Change Administrative Placement
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-06` |
| **Name** | Change Administrative Placement |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | School Leadership (exceptional reassignments) |
| **Goal** | Reassign an active student's administrative placement (campus, level, program, stream, cohort, or homeroom cohort) with effective dating. |
| **Expected Outcome** | Student administrative placement is updated with an effective date, placement history is preserved, and linked operational/teaching contexts are updated. |
| **Workflow Reference** | `STD-WF-05` |

#### `UC-STD-07`. Place Student on Leave
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-07` |
| **Name** | Place Student on Leave |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | Parent / Guardian, School Leadership (approves extended leaves) |
| **Goal** | Temporarily suspend a student's active enrollment for an authorized leave of absence up to one academic year based on approved justification. |
| **Expected Outcome** | Student status transitions to `On Leave` with start and expected return dates; active academic participations are suspended and fee billing adjustments are triggered. |
| **Workflow Reference** | `STD-WF-06` |

#### `UC-STD-08`. Return Student from Leave
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-08` |
| **Name** | Return Student from Leave |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | School Leadership |
| **Goal** | Reinstate a student from `On Leave` or `Leave Expired` status back to `Active` status upon approved resumption of studies. |
| **Expected Outcome** | Student status is restored to `Active`, administrative placement is re-established, and resumption of academic participation and billing is initiated. |
| **Workflow Reference** | `STD-WF-07` |

#### `UC-STD-09`. Execute Promotion or Retention
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-09` |
| **Name** | Execute Promotion or Retention |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | Academic Authority (issues authoritative progression recommendation) |
| **Goal** | Apply an approved academic progression recommendation (promotion or retention) by administratively updating the student's level and cohort placement. |
| **Expected Outcome** | Student administrative level and cohort are updated, academic progression history is recorded, and enrollment continuity is maintained. |
| **Workflow Reference** | `STD-WF-08` |

#### `UC-STD-10`. Process Internal Transfer
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-10` |
| **Name** | Process Internal Transfer |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | School Leadership |
| **Goal** | Administratively transfer an active student between campuses, educational programs, or tracks within the school organization. |
| **Expected Outcome** | Student administrative placement and campus/program affiliation are updated with an effective date, preserving historical identity and financial ledger continuity. |
| **Workflow Reference** | `STD-WF-09` |

#### `UC-STD-11`. Process External Transfer Out
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-11` |
| **Name** | Process External Transfer Out |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | Parent / Guardian, School Leadership |
| **Goal** | Formally conclude enrollment and release a student transferring to another educational institution upon verifying that both Academic and Financial Clearances are Granted. |
| **Expected Outcome** | Student status transitions to `Transferred`, official transfer release certificate is issued, clearance records are archived, and active participations are closed. |
| **Workflow Reference** | `STD-WF-10` |

#### `UC-STD-12`. Process Withdrawal
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-12` |
| **Name** | Process Withdrawal |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | Parent / Guardian, School Leadership, Discipline Authority (for disciplinary expulsion GAP-02) |
| **Goal** | Conclude a student's enrollment prior to graduation due to voluntary exit, family relocation, non-return, or an institutional disciplinary decision. |
| **Expected Outcome** | Student status transitions to `Withdrawn`, withdrawal category and reason are permanently recorded, active participations are closed, and financial account settlement is initiated. |
| **Workflow Reference** | `STD-WF-11` |

#### `UC-STD-13`. Execute Graduation
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-13` |
| **Name** | Execute Graduation |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | Academic Authority (certifies Graduation Eligibility), School Leadership, Finance Authority (confirms Financial Clearance) |
| **Goal** | Formally graduate an eligible student upon verifying certified Academic Graduation Eligibility and required Financial Clearance. |
| **Expected Outcome** | Student status transitions to `Graduated`, graduation conferral is recorded in administrative history, and automated alumni status activation is triggered. |
| **Workflow Reference** | `STD-WF-12` |

#### `UC-STD-14`. Activate Alumni Status
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-14` |
| **Name** | Activate Alumni Status |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | System Automated |
| **Goal** | Establish and verify the formal alumni record following graduation to preserve lifelong institutional affiliation and verified identity continuity. |
| **Expected Outcome** | An official alumni record is established and linked to historical student credentials and academic records. |
| **Workflow Reference** | `STD-WF-13` |

#### `UC-STD-15`. Request Academic Clearance for Transfer Out
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-15` |
| **Name** | Request Academic Clearance for Transfer Out |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | Academic Coordinator |
| **Goal** | Formally request the Academics domain to evaluate outstanding academic obligations (library books, equipment, coursework) for a pending transfer-out. |
| **Expected Outcome** | An official Academic Clearance Request is lodged in Academics and linked to the student's transfer dossier. |
| **Workflow Reference** | `STD-WF-14` |

#### `UC-STD-16`. Request Purpose-Specific Financial Clearance
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-16` |
| **Name** | Request Purpose-Specific Financial Clearance |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | Finance Officer |
| **Goal** | Formally request the Finance domain to evaluate outstanding monetary obligations and issue financial clearance for transfer-out or graduation. |
| **Expected Outcome** | An official Financial Clearance Request is lodged in Finance and tracked within the student's transition dossier. |
| **Workflow Reference** | `STD-WF-15` |

#### `UC-STD-17`. Process Student Re-enrollment
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-17` |
| **Name** | Process Student Re-enrollment |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | Finance Officer (verifies re-enrollment payment if policy requires), Parent / Guardian |
| **Goal** | Verify re-enrollment continuation requirements (active status and fee settlement if mandated) and officially record continuing enrollment into the upcoming academic period. |
| **Expected Outcome** | Re-enrollment record is created for the target period, active status is confirmed, and student is cleared for upcoming academic participation. |
| **Workflow Reference** | `STD-WF-16` |

#### `UC-STD-18`. Process External Transfer In
| Attribute | Value |
|---|---|
| **ID** | `UC-STD-18` |
| **Name** | Process External Transfer In |
| **Primary Actor** | Student Administration Officer |
| **Supporting Actor(s)** | Parent / Guardian, School Leadership, Academic Authority (prior credit evaluation GAP-08) |
| **Goal** | Formally admit and enroll a mid-program transfer student from another accredited institution, recording origin school history and initial administrative placement. |
| **Expected Outcome** | Official student identity is created, unique Student Number assigned, external transfer-in record established with origin school details, initial placement assigned, and financial account initialized. |
| **Workflow Reference** | `STD-WF-17` |

---

## 2. Academics Domain Use Cases

The Academics Domain owns curriculum structure, learning units, student learning plans, academic participation, learning activities, attendance, assignments, assessments, grading, report cards, academic standing, and academic clearance.

### Summary by Actor

| Actor | Use Case ID | Use Case Name | Workflow Reference | Stage |
|---|---|---|---|---|
| **Student** | `UC-ACD-01` | Submit Coursework Assignment | `ACD-WF-06` | Stage 2 |
| **Teacher** | `UC-ACD-02` | Record Learning Activity Delivery | `ACD-WF-04` | Stage 2 |
| | `UC-ACD-03` | Record and Correct Student Attendance | `ACD-WF-05` | Stage 2 |
| | `UC-ACD-04` | Issue Coursework Assignment | `ACD-WF-06` | Stage 2 |
| | `UC-ACD-05` | Plan and Administer Assessment | `ACD-WF-07` | Stage 2 |
| | `UC-ACD-06` | Evaluate Student Learning | `ACD-WF-08` | Stage 2 |
| | `UC-ACD-07` | Conduct Applicant Entrance Assessment | `ACD-WF-20` | Stage 2 |
| **Academic Coordinator** | `UC-ACD-08` | Define Learning Structure | `ACD-WF-01` | Stage 2 |
| | `UC-ACD-09` | Establish Student Learning Plan | `ACD-WF-02` | Stage 2 |
| | `UC-ACD-10` | Register Academic Participation | `ACD-WF-03` | Stage 2 |
| | `UC-ACD-11` | Initiate Academic Intervention | `ACD-WF-10` | Stage 2 |
| | `UC-ACD-12` | Conclude Learning Unit Participation | `ACD-WF-11` | Stage 2 |
| | `UC-ACD-13` | Produce Report Card and Transcript | `ACD-WF-12` | Stage 2 |
| | `UC-ACD-14` | Determine Academic Clearance | `ACD-WF-16` | Stage 3 |
| | `UC-ACD-15` | Correct Non-Grade Academic Record | `ACD-WF-18` | Stage 2 / Stage 3 |
| **Academic Authority** | `UC-ACD-16` | Approve and Publish Academic Results | `ACD-WF-09` | Stage 2 |
| | `UC-ACD-17` | Determine Academic Standing and Progression | `ACD-WF-13` | Stage 2 |
| | `UC-ACD-18` | Certify Graduation Academic Eligibility | `ACD-WF-14` | Stage 3 |
| | `UC-ACD-19` | Correct Approved Academic Result | `ACD-WF-15` | Stage 2 |
| | `UC-ACD-20` | Adjudicate Academic Clearance Correction | `ACD-WF-17` | Stage 3 |
| | `UC-ACD-21` | Confer Academic Achievement | `ACD-WF-19` | Stage 2 |

---

### 2.1 Student

#### `UC-ACD-01`. Submit Coursework Assignment
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-01` |
| **Name** | Submit Coursework Assignment |
| **Primary Actor** | Student |
| **Supporting Actor(s)** | Teacher |
| **Goal** | Submit completed coursework files, project materials, or responses for an assigned learning task before or on the established due date. |
| **Expected Outcome** | Submission is timestamped, recorded with submission status (`On Time`, `Late`), and made available to the teacher for evaluation. |
| **Workflow Reference** | `ACD-WF-06` |

---

### 2.2 Teacher

#### `UC-ACD-02`. Record Learning Activity Delivery
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-02` |
| **Name** | Record Learning Activity Delivery |
| **Primary Actor** | Teacher |
| **Supporting Actor(s)** | Teaching Coordinator |
| **Goal** | Record instructional conduct, curriculum content delivered, and pedagogical notes for a planned class session tied to an authorized teaching assignment. |
| **Expected Outcome** | Learning activity is marked as delivered with curriculum milestones logged against the syllabus. |
| **Workflow Reference** | `ACD-WF-04` |

#### `UC-ACD-03`. Record and Correct Student Attendance
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-03` |
| **Name** | Record and Correct Student Attendance |
| **Primary Actor** | Teacher |
| **Supporting Actor(s)** | Academic Coordinator |
| **Goal** | Capture observed student attendance (Present, Absent, Late, Excused) for a learning activity (including statutory homeroom roll-call per GAP-03) and submit auditable attendance corrections. |
| **Expected Outcome** | Session attendance is permanently recorded, aggregated into student attendance summaries, and corrections are logged with reason. |
| **Workflow Reference** | `ACD-WF-05` |

#### `UC-ACD-04`. Issue Coursework Assignment
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-04` |
| **Name** | Issue Coursework Assignment |
| **Primary Actor** | Teacher |
| **Supporting Actor(s)** | Academic Coordinator |
| **Goal** | Create and publish coursework tasks, instructions, evaluation criteria/rubrics, and submission deadlines for enrolled students in a learning unit. |
| **Expected Outcome** | Assignment is published to the student cohort with due date linked to operational calendar context. |
| **Workflow Reference** | `ACD-WF-06` |

#### `UC-ACD-05`. Plan and Administer Assessment
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-05` |
| **Name** | Plan and Administer Assessment |
| **Primary Actor** | Teacher |
| **Supporting Actor(s)** | Academic Coordinator |
| **Goal** | Schedule and administer formative or summative assessments according to the approved syllabus, rubric standards, and exam timetable context. |
| **Expected Outcome** | Assessment is scheduled, administered, and assessment evidence is captured for evaluation. |
| **Workflow Reference** | `ACD-WF-07` |

#### `UC-ACD-06`. Evaluate Student Learning
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-06` |
| **Name** | Evaluate Student Learning |
| **Primary Actor** | Teacher |
| **Supporting Actor(s)** | Academic Coordinator |
| **Goal** | Score and grade submitted assessment evidence and assignments against established rubrics, recording provisional marks and qualitative feedback. |
| **Expected Outcome** | Provisional scores, rubric breakdowns, and evaluative feedback are recorded on student evaluations. |
| **Workflow Reference** | `ACD-WF-08` |

#### `UC-ACD-07`. Conduct Applicant Entrance Assessment
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-07` |
| **Name** | Conduct Applicant Entrance Assessment |
| **Primary Actor** | Teacher (or Assessment Officer) |
| **Supporting Actor(s)** | Academic Authority (approves result), Admission Officer (requests assessment) |
| **Goal** | Administer an academic diagnostic entrance test or admissions interview for a prospective applicant, evaluate evidence, and compile an approved diagnostic score report. |
| **Expected Outcome** | Scored entrance assessment report is approved and transmitted to the Students domain as an authorized input to admission review. |
| **Workflow Reference** | `ACD-WF-20` |

---

### 2.3 Academic Coordinator

#### `UC-ACD-08`. Define Learning Structure
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-08` |
| **Name** | Define Learning Structure |
| **Primary Actor** | Academic Coordinator |
| **Supporting Actor(s)** | Academic Authority (approves framework) |
| **Goal** | Establish and maintain curriculum frameworks, learning units, course syllabi, academic prerequisites, and policy versions for an academic year/period. |
| **Expected Outcome** | Learning structure and syllabus versions are configured, validated against institutional standards, and queued for authority approval. |
| **Workflow Reference** | `ACD-WF-01` |

#### `UC-ACD-09`. Establish Student Learning Plan
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-09` |
| **Name** | Establish Student Learning Plan |
| **Primary Actor** | Academic Coordinator |
| **Supporting Actor(s)** | Academic Authority (approves accommodations), Student Development Officer, Parent / Guardian |
| **Goal** | Formulate, monitor, and adjust an individualized Student Learning Plan (SLP) specifying academic accommodations, course substitutions, or exemptions for an exceptional learner. |
| **Expected Outcome** | An SLP is formulated, approved by Academic Authority, and recorded to govern the student's academic requirements and evaluations. |
| **Workflow Reference** | `ACD-WF-02` |

#### `UC-ACD-10`. Register Academic Participation
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-10` |
| **Name** | Register Academic Participation |
| **Primary Actor** | Academic Coordinator |
| **Supporting Actor(s)** | Student (when elective selection permitted), Academic Authority (prerequisite waivers) |
| **Goal** | Register an active student into specific learning units for an academic period, verifying prerequisite requirements, learning plan alignment, and class capacity. |
| **Expected Outcome** | Active academic participation record is established, linking the student to the learning unit roster. |
| **Workflow Reference** | `ACD-WF-03` |

#### `UC-ACD-11`. Initiate Academic Intervention
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-11` |
| **Name** | Initiate Academic Intervention |
| **Primary Actor** | Academic Coordinator |
| **Supporting Actor(s)** | Teacher, Student Development Officer (pastoral referral) |
| **Goal** | Identify students at academic risk based on progress reviews and establish a structured academic support or remediation plan with measurable targets. |
| **Expected Outcome** | Academic intervention plan is established, support strategies are assigned, and review checkpoints are scheduled. |
| **Workflow Reference** | `ACD-WF-10` |

#### `UC-ACD-12`. Conclude Learning Unit Participation
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-12` |
| **Name** | Conclude Learning Unit Participation |
| **Primary Actor** | Academic Coordinator |
| **Supporting Actor(s)** | Teacher |
| **Goal** | Verify completion criteria (attendance, coursework submissions, and passing scores) and formally conclude student participation in a learning unit. |
| **Expected Outcome** | Learning participation is marked as `Completed` or `Incomplete`, credit units tallied, and recorded in student academic history. |
| **Workflow Reference** | `ACD-WF-11` |

#### `UC-ACD-13`. Produce Report Card and Transcript
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-13` |
| **Name** | Produce Report Card and Transcript |
| **Primary Actor** | Academic Coordinator |
| **Supporting Actor(s)** | Academic Authority |
| **Goal** | Compile, format, and generate certified report cards, progress summaries, and official academic transcripts reflecting approved results and attendance. |
| **Expected Outcome** | Certified report card or official transcript is generated and issued for authorized communication. |
| **Workflow Reference** | `ACD-WF-12` |

#### `UC-ACD-14`. Determine Academic Clearance
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-14` |
| **Name** | Determine Academic Clearance |
| **Primary Actor** | Academic Coordinator |
| **Supporting Actor(s)** | Academic Authority |
| **Goal** | Evaluate an outgoing student's academic obligations (textbooks, library returns, lab equipment, course completion) and issue an Academic Clearance determination for transfer-out. |
| **Expected Outcome** | Formal Academic Clearance determination (`Granted` or `Denied`) is recorded and transmitted to the Students domain. |
| **Workflow Reference** | `ACD-WF-16` |

#### `UC-ACD-15`. Correct Non-Grade Academic Record
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-15` |
| **Name** | Correct Non-Grade Academic Record |
| **Primary Actor** | Academic Coordinator |
| **Supporting Actor(s)** | Teacher, Academic Authority |
| **Goal** | Correct non-grade operational academic records (assignment metadata, attendance history, standing records, or progression recommendations) with documented justification. |
| **Expected Outcome** | Operational academic record is corrected with logged reason, preserving full audit history. |
| **Workflow Reference** | `ACD-WF-18` |

---

### 2.4 Academic Authority

#### `UC-ACD-16`. Approve and Publish Academic Results
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-16` |
| **Name** | Approve and Publish Academic Results |
| **Primary Actor** | Academic Authority |
| **Supporting Actor(s)** | Academic Coordinator, Teacher |
| **Goal** | Moderate provisional grades across cohorts, grant formal institutional approval, and publish official academic results to students and parents. |
| **Expected Outcome** | Academic results are officially approved, finalized, locked against unapproved alteration, and published. |
| **Workflow Reference** | `ACD-WF-09` |

#### `UC-ACD-17`. Determine Academic Standing and Progression
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-17` |
| **Name** | Determine Academic Standing and Progression |
| **Primary Actor** | Academic Authority |
| **Supporting Actor(s)** | Academic Coordinator |
| **Goal** | Evaluate cumulative cohort performance and approve formal academic standing determinations and progression recommendations (`Promote`, `Retain`, `Conditional Promotion`) for Students domain execution. |
| **Expected Outcome** | Academic standing is recorded and progression recommendations are officially transmitted to the Students domain. |
| **Workflow Reference** | `ACD-WF-13` |

#### `UC-ACD-18`. Certify Graduation Academic Eligibility
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-18` |
| **Name** | Certify Graduation Academic Eligibility |
| **Primary Actor** | Academic Authority |
| **Supporting Actor(s)** | Academic Coordinator |
| **Goal** | Audit cumulative academic completion records against institutional graduation standards and certify official Graduation Eligibility. |
| **Expected Outcome** | Graduation eligibility is certified (`Eligible` or `Ineligible`) and transmitted to the Students domain for graduation execution. |
| **Workflow Reference** | `ACD-WF-14` |

#### `UC-ACD-19`. Correct Approved Academic Result
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-19` |
| **Name** | Correct Approved Academic Result |
| **Primary Actor** | Academic Authority |
| **Supporting Actor(s)** | Academic Coordinator, Teacher |
| **Goal** | Authorize an auditable, justified correction to a finalized, approved academic result or published grade following an appeal or clerical error discovery. |
| **Expected Outcome** | Approved grade is corrected with mandatory audit rationale, previous value preserved in history, and downstream impacts evaluated. |
| **Workflow Reference** | `ACD-WF-15` |

#### `UC-ACD-20`. Adjudicate Academic Clearance Correction
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-20` |
| **Name** | Adjudicate Academic Clearance Correction |
| **Primary Actor** | Academic Authority |
| **Supporting Actor(s)** | Academic Coordinator |
| **Goal** | Authorize a formal correction to a previously issued Academic Clearance determination with documented rationale. |
| **Expected Outcome** | Academic Clearance status is corrected, audit trail updated, and Students domain notified to re-evaluate pending transfer-out. |
| **Workflow Reference** | `ACD-WF-17` |

#### `UC-ACD-21`. Confer Academic Achievement
| Attribute | Value |
|---|---|
| **ID** | `UC-ACD-21` |
| **Name** | Confer Academic Achievement |
| **Primary Actor** | Academic Authority |
| **Supporting Actor(s)** | Academic Coordinator |
| **Goal** | Formally approve and confer academic honors, subject distinctions, honor roll placements, or competition recognitions based on approved academic results. |
| **Expected Outcome** | An Academic Achievement record is established, published to the student profile, and transmitted to Finance when tied to merit benefits. |
| **Workflow Reference** | `ACD-WF-19` |

---

## 3. Finance Domain Use Cases

The Finance Domain owns financial policies, fee structures, charge rules, obligation assessments, billing statements, payment receipt, payment allocation, receivables management, financial adjustments, refunds, and financial clearance.

### Summary by Actor

| Actor | Use Case ID | Use Case Name | Workflow Reference | Stage |
|---|---|---|---|---|
| **Payer / Parent / Guardian** | `UC-FIN-01` | Submit Fee Payment | `FIN-WF-05` | Stage 1 |
| **Finance Officer** | `UC-FIN-02` | Assess Financial Obligations | `FIN-WF-02` | Stage 1 |
| | `UC-FIN-03` | Issue Billing Statement | `FIN-WF-04` | Stage 1 |
| | `UC-FIN-04` | Confirm Payment Receipt | `FIN-WF-05` | Stage 1 |
| | `UC-FIN-05` | Allocate Payment to Obligations | `FIN-WF-06` | Stage 1 |
| | `UC-FIN-06` | Monitor Overdue Receivables | `FIN-WF-07` | Stage 2 |
| | `UC-FIN-07` | Manage Overpayment and Account Credit | `FIN-WF-09` | Stage 2 |
| | `UC-FIN-08` | Evaluate Financial Clearance | `FIN-WF-11` | Stage 3 |
| | `UC-FIN-09` | Confirm Enrollment Payment Gate | `FIN-WF-13` | Stage 1 |
| **Finance Authority** | `UC-FIN-10` | Establish Financial Policy | `FIN-WF-01` | Stage 1 |
| | `UC-FIN-11` | Approve Financial Benefit | `FIN-WF-03` | Stage 2 |
| | `UC-FIN-12` | Establish Installment Payment Arrangement | `FIN-WF-07` | Stage 2 |
| | `UC-FIN-13` | Adjudicate Financial Dispute or Adjustment | `FIN-WF-08` | Stage 2 |
| | `UC-FIN-14` | Approve and Execute Refund | `FIN-WF-10` | Stage 2 |
| | `UC-FIN-15` | Correct Approved Finance Record | `FIN-WF-12` | Stage 2 |

---

### 3.1 Payer / Parent / Guardian

#### `UC-FIN-01`. Submit Fee Payment
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-01` |
| **Name** | Submit Fee Payment |
| **Primary Actor** | Payer (Parent / Guardian / Student) |
| **Supporting Actor(s)** | External Payment Provider, Finance Officer |
| **Goal** | Transfer monetary funds (via online gateway or bank transfer) and submit verifiable payment evidence toward student fee obligations. |
| **Expected Outcome** | Payment transaction is submitted with payment evidence and queued for verification and confirmation. |
| **Workflow Reference** | `FIN-WF-05` |

---

### 3.2 Finance Officer

#### `UC-FIN-02`. Assess Financial Obligations
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-02` |
| **Name** | Assess Financial Obligations |
| **Primary Actor** | Finance Officer |
| **Supporting Actor(s)** | Finance Authority |
| **Goal** | Calculate and generate monetary charges (application, enrollment, tuition, facility fees) against student financial accounts based on approved fee schedules and student placements. |
| **Expected Outcome** | Obligations are assessed, assigned unique obligation IDs, and posted to student financial accounts with due dates. |
| **Workflow Reference** | `FIN-WF-02` |

#### `UC-FIN-03`. Issue Billing Statement
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-03` |
| **Name** | Issue Billing Statement |
| **Primary Actor** | Finance Officer |
| **Supporting Actor(s)** | Parent / Guardian |
| **Goal** | Compile, format, and distribute official fee invoices and account statements detailing assessed obligations, payments, and outstanding balances to payers. |
| **Expected Outcome** | Official billing statement is generated and communicated to authorized parents/payers. |
| **Workflow Reference** | `FIN-WF-04` |

#### `UC-FIN-04`. Confirm Payment Receipt
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-04` |
| **Name** | Confirm Payment Receipt |
| **Primary Actor** | Finance Officer |
| **Supporting Actor(s)** | Payer, External Payment Provider |
| **Goal** | Verify incoming payment transactions against bank deposits or gateway settlement reports, confirm receipt, and issue payment receipts. |
| **Expected Outcome** | Payment is confirmed, official receipt is issued, and unallocated payment value is credited to the student account. |
| **Workflow Reference** | `FIN-WF-05` |

#### `UC-FIN-05`. Allocate Payment to Obligations
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-05` |
| **Name** | Allocate Payment to Obligations |
| **Primary Actor** | Finance Officer |
| **Supporting Actor(s)** | Finance Authority |
| **Goal** | Apply confirmed payments or available account credits to settle specific outstanding obligations according to priority allocation rules (or execute authorized reallocations). |
| **Expected Outcome** | Targeted obligations are marked partially or fully settled, remaining balances updated, and account ledger updated. |
| **Workflow Reference** | `FIN-WF-06` |

#### `UC-FIN-06`. Monitor Overdue Receivables
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-06` |
| **Name** | Monitor Overdue Receivables |
| **Primary Actor** | Finance Officer |
| **Supporting Actor(s)** | Parent / Guardian, Finance Authority |
| **Goal** | Monitor accounts receivable aging ledgers, dispatch automated or manual payment reminders, and track overdue collection status. |
| **Expected Outcome** | Overdue accounts are identified, reminder notices dispatched to payers, and overdue aging ledgers updated. |
| **Workflow Reference** | `FIN-WF-07` |

#### `UC-FIN-07`. Manage Overpayment and Account Credit
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-07` |
| **Name** | Manage Overpayment and Account Credit |
| **Primary Actor** | Finance Officer |
| **Supporting Actor(s)** | Finance Authority |
| **Goal** | Track excess payments and unallocated credits on student accounts, apply credits to upcoming period charges, or process authorized credit transfers. |
| **Expected Outcome** | Account credits are tracked, applied to new obligations, or transferred with logged justification. |
| **Workflow Reference** | `FIN-WF-09` |

#### `UC-FIN-08`. Evaluate Financial Clearance
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-08` |
| **Name** | Evaluate Financial Clearance |
| **Primary Actor** | Finance Officer |
| **Supporting Actor(s)** | Finance Authority |
| **Goal** | Evaluate an outgoing or graduating student's outstanding financial balance against mandatory clearance rules and determine Financial Clearance for Transfer Out or Graduation. |
| **Expected Outcome** | Formal Financial Clearance determination (`Granted` or `Denied`) is issued and transmitted to the Students domain. |
| **Workflow Reference** | `FIN-WF-11` |

#### `UC-FIN-09`. Confirm Enrollment Payment Gate
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-09` |
| **Name** | Confirm Enrollment Payment Gate |
| **Primary Actor** | Finance Officer |
| **Supporting Actor(s)** | Student Administration Officer |
| **Goal** | Verify real-time settlement of mandatory admission and enrollment fees for an accepted applicant to satisfy the enrollment intake gate. |
| **Expected Outcome** | Real-time confirmation of enrollment payment status is provided to the Students domain, unblocking official student enrollment. |
| **Workflow Reference** | `FIN-WF-13` |

---

### 3.3 Finance Authority

#### `UC-FIN-10`. Establish Financial Policy
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-10` |
| **Name** | Establish Financial Policy |
| **Primary Actor** | Finance Authority |
| **Supporting Actor(s)** | School Leadership, Finance Officer |
| **Goal** | Define and approve institutional tuition policies, fee schedules, charge rules, payment due schedules, and currency rounding rules for an operational period. |
| **Expected Outcome** | Financial policy version and charge rules are approved, effective, and published. |
| **Workflow Reference** | `FIN-WF-01` |

#### `UC-FIN-11`. Approve Financial Benefit
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-11` |
| **Name** | Approve Financial Benefit |
| **Primary Actor** | Finance Authority |
| **Supporting Actor(s)** | Finance Officer, School Leadership |
| **Goal** | Review benefit eligibility, approve scholarships, sibling discounts, staff concessions, or discretionary fee waivers, configure benefit stacking, and apply deductions to obligations. |
| **Expected Outcome** | Approved financial benefit is applied as an obligation deduction or account credit, reducing payable balance. |
| **Workflow Reference** | `FIN-WF-03` |

#### `UC-FIN-12`. Establish Installment Payment Arrangement
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-12` |
| **Name** | Establish Installment Payment Arrangement |
| **Primary Actor** | Finance Authority |
| **Supporting Actor(s)** | Finance Officer, Parent / Guardian |
| **Goal** | Review payer hardship requests, approve formal installment payment plans, and set temporary holds on overdue sanctions. |
| **Expected Outcome** | A formal installment payment agreement is established with customized installment due dates and amounts. |
| **Workflow Reference** | `FIN-WF-07` |

#### `UC-FIN-13`. Adjudicate Financial Dispute or Adjustment
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-13` |
| **Name** | Adjudicate Financial Dispute or Adjustment |
| **Primary Actor** | Finance Authority |
| **Supporting Actor(s)** | Finance Officer, Parent / Guardian |
| **Goal** | Adjudicate contested billing challenges raised by payers and authorize binding financial adjustments, fee reductions, or obligation reversals. |
| **Expected Outcome** | Binding dispute resolution is recorded; obligation balance is adjusted, reversed, or reaffirmed. |
| **Workflow Reference** | `FIN-WF-08` |

#### `UC-FIN-14`. Approve and Execute Refund
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-14` |
| **Name** | Approve and Execute Refund |
| **Primary Actor** | Finance Authority |
| **Supporting Actor(s)** | Finance Officer, Payer |
| **Goal** | Authorize and execute monetary refund disbursements of credit balances back to verified payers (due to overpayment or withdrawal). |
| **Expected Outcome** | Refund is approved, payment disbursement is recorded, student account credit is reduced, and refund transaction is finalized. |
| **Workflow Reference** | `FIN-WF-10` |

#### `UC-FIN-15`. Correct Approved Finance Record
| Attribute | Value |
|---|---|
| **ID** | `UC-FIN-15` |
| **Name** | Correct Approved Finance Record |
| **Primary Actor** | Finance Authority |
| **Supporting Actor(s)** | Finance Officer |
| **Goal** | Execute an auditable correction to finalized financial accounting records (misallocated payment, ledger error, or clearance determination) with required justification. |
| **Expected Outcome** | Erroneous financial record is corrected, audit trail logged, and affected account balances are recalculated. |
| **Workflow Reference** | `FIN-WF-12` |

---

## 4. Operations Domain Use Cases

The Operations Domain owns the school calendar, academic periods, physical room contexts, scheduling feasibility, timetable occurrences, room allocations, school events, operational announcements, and closure coordination.

### Summary by Actor

| Actor | Use Case ID | Use Case Name | Workflow Reference | Stage |
|---|---|---|---|---|
| **Requesting Actors** | `UC-OPS-01` | Submit Operational Activity Request | `OPS-WF-02` | Stage 2 |
| **Operations Coordinator** | `UC-OPS-02` | Review Operational Feasibility | `OPS-WF-03` | Stage 2 |
| | `UC-OPS-03` | Schedule Activity and Allocate Room | `OPS-WF-04` | Stage 2 |
| | `UC-OPS-04` | Resolve Operational Resource Conflict | `OPS-WF-05` | Stage 2 |
| | `UC-OPS-05` | Coordinate School Event Logistics | `OPS-WF-06` | Stage 3 |
| | `UC-OPS-06` | Publish Operational Announcement | `OPS-WF-07` | Stage 3 |
| | `UC-OPS-07` | Record Room Restriction | `OPS-WF-08` | Stage 2 |
| | `UC-OPS-08` | Modify or Cancel Scheduled Occurrence | `OPS-WF-09` | Stage 2 |
| | `UC-OPS-09` | Close Operational Arrangement | `OPS-WF-11` | Stage 2 |
| **Operations Authority** | `UC-OPS-10` | Establish School Calendar and Periods | `OPS-WF-01` | Stage 1 |
| | `UC-OPS-11` | Declare Campus Closure | `OPS-WF-10` | Stage 1 / Stage 2 |
| | `UC-OPS-12` | Correct Approved Operations Record | `OPS-WF-12` | Stage 2 / Stage 3 |

---

### 4.1 Requesting Actors (Teacher / Academic Coordinator / Student Development Officer)

#### `UC-OPS-01`. Submit Operational Activity Request
| Attribute | Value |
|---|---|
| **ID** | `UC-OPS-01` |
| **Name** | Submit Operational Activity Request |
| **Primary Actor** | Teacher / Academic Coordinator / Student Development Officer |
| **Supporting Actor(s)** | Operations Coordinator |
| **Goal** | Submit requests for physical room allocation, timetable slots, and facility equipment for instructional sessions, examinations, meetings, or student activities. |
| **Expected Outcome** | Operational activity request is logged with attendance, facility requirements, and scheduling preferences, queued for feasibility review. |
| **Workflow Reference** | `OPS-WF-02` |

---

### 4.2 Operations Coordinator

#### `UC-OPS-02`. Review Operational Feasibility
| Attribute | Value |
|---|---|
| **ID** | `UC-OPS-02` |
| **Name** | Review Operational Feasibility |
| **Primary Actor** | Operations Coordinator |
| **Supporting Actor(s)** | Teaching Coordinator, Requesting Actors |
| **Goal** | Evaluate physical room capacity, facility features, teacher availability, and calendar constraints against an operational activity request. |
| **Expected Outcome** | Request is evaluated as feasible, conflicted, or infeasible, with documented constraints. |
| **Workflow Reference** | `OPS-WF-03` |

#### `UC-OPS-03`. Schedule Activity and Allocate Room
| Attribute | Value |
|---|---|
| **ID** | `UC-OPS-03` |
| **Name** | Schedule Activity and Allocate Room |
| **Primary Actor** | Operations Coordinator |
| **Supporting Actor(s)** | Teacher, Academic Coordinator |
| **Goal** | Commit a scheduled occurrence to the operational timetable, reserve a specific physical room, and link authorized teaching assignments (including physical homerooms). |
| **Expected Outcome** | Timetable occurrence is created and published; room allocation is committed; participants are notified. |
| **Workflow Reference** | `OPS-WF-04` |

#### `UC-OPS-04`. Resolve Operational Resource Conflict
| Attribute | Value |
|---|---|
| **ID** | `UC-OPS-04` |
| **Name** | Resolve Operational Resource Conflict |
| **Primary Actor** | Operations Coordinator |
| **Supporting Actor(s)** | Operations Authority, School Leadership, Affected Requestors |
| **Goal** | Resolve room double-bookings, timetable collisions, or facility capacity overruns through negotiation, rescheduling, or executive escalation. |
| **Expected Outcome** | Conflict is resolved via alternative room/time allocation or authoritative executive adjudication. |
| **Workflow Reference** | `OPS-WF-05` |

#### `UC-OPS-05`. Coordinate School Event Logistics
| Attribute | Value |
|---|---|
| **ID** | `UC-OPS-05` |
| **Name** | Coordinate School Event Logistics |
| **Primary Actor** | Operations Coordinator |
| **Supporting Actor(s)** | Operations Authority, School Leadership, Event Organizer |
| **Goal** | Coordinate operational logistics, facility reservations, security, and institutional approvals for major school events, ceremonies, or extracurricular fixtures. |
| **Expected Outcome** | School event is approved, facility reservations locked, logistical arrangements committed, and event published on the school calendar. |
| **Workflow Reference** | `OPS-WF-06` |

#### `UC-OPS-06`. Publish Operational Announcement
| Attribute | Value |
|---|---|
| **ID** | `UC-OPS-06` |
| **Name** | Publish Operational Announcement |
| **Primary Actor** | Operations Coordinator |
| **Supporting Actor(s)** | Operations Authority |
| **Goal** | Format, schedule, broadcast, correct, or withdraw official school and operational announcements across defined communication channels and audience segments. |
| **Expected Outcome** | Announcement is published to target audiences (parents, students, staff) or formally withdrawn. |
| **Workflow Reference** | `OPS-WF-07` |

#### `UC-OPS-07`. Record Room Restriction
| Attribute | Value |
|---|---|
| **ID** | `UC-OPS-07` |
| **Name** | Record Room Restriction |
| **Primary Actor** | Operations Coordinator |
| **Supporting Actor(s)** | Operations Authority, Facilities Staff |
| **Goal** | Log physical room maintenance closures, repairs, or facility restrictions and flag affected scheduled occurrences for relocation. |
| **Expected Outcome** | Room unavailability window is recorded, room blocked from allocation, and impacted schedules identified for rescheduling. |
| **Workflow Reference** | `OPS-WF-08` |

#### `UC-OPS-08`. Modify or Cancel Scheduled Occurrence
| Attribute | Value |
|---|---|
| **ID** | `UC-OPS-08` |
| **Name** | Modify or Cancel Scheduled Occurrence |
| **Primary Actor** | Operations Coordinator |
| **Supporting Actor(s)** | Teacher, Academic Coordinator, Student |
| **Goal** | Adjust active scheduled occurrences by changing times, moving to alternative rooms, postponing, or cancelling sessions, and notify all affected parties. |
| **Expected Outcome** | Occurrence is updated or cancelled, room reservations released/reassigned, and immediate notifications dispatched. |
| **Workflow Reference** | `OPS-WF-09` |

#### `UC-OPS-09`. Close Operational Arrangement
| Attribute | Value |
|---|---|
| **ID** | `UC-OPS-09` |
| **Name** | Close Operational Arrangement |
| **Primary Actor** | Operations Coordinator |
| **Supporting Actor(s)** | None |
| **Goal** | Conclude finished operational occurrences, release facility holds, and archive operational execution logs. |
| **Expected Outcome** | Scheduled arrangement is closed, facility freed for future allocation, and operational logs archived. |
| **Workflow Reference** | `OPS-WF-11` |

---

### 4.3 Operations Authority

#### `UC-OPS-10`. Establish School Calendar and Periods
| Attribute | Value |
|---|---|
| **ID** | `UC-OPS-10` |
| **Name** | Establish School Calendar and Periods |
| **Primary Actor** | Operations Authority |
| **Supporting Actor(s)** | Operations Coordinator |
| **Goal** | Define, approve, and amend academic year dates, term boundaries, operational periods, official school calendar, and statutory holidays. |
| **Expected Outcome** | Official institutional calendar and operational periods are published and made effective for all school domains. |
| **Workflow Reference** | `OPS-WF-01` |

#### `UC-OPS-11`. Declare Campus Closure
| Attribute | Value |
|---|---|
| **ID** | `UC-OPS-11` |
| **Name** | Declare Campus Closure |
| **Primary Actor** | Operations Authority |
| **Supporting Actor(s)** | School Leadership, Operations Coordinator |
| **Goal** | Declare emergency or unscheduled school/campus closures, amend calendar operational dates, and coordinate mass cancellation or rescheduling of all affected occurrences. |
| **Expected Outcome** | Closure is formally declared, calendar amended, affected timetable occurrences suspended/cancelled, and school-wide emergency notice dispatched. |
| **Workflow Reference** | `OPS-WF-10` |

#### `UC-OPS-12`. Correct Approved Operations Record
| Attribute | Value |
|---|---|
| **ID** | `UC-OPS-12` |
| **Name** | Correct Approved Operations Record |
| **Primary Actor** | Operations Authority |
| **Supporting Actor(s)** | Operations Coordinator |
| **Goal** | Execute an auditable correction to historical operational records (calendar milestones, past room allocations, or timetable logs). |
| **Expected Outcome** | Historical operations record is corrected with mandatory justification and audit trail. |
| **Workflow Reference** | `OPS-WF-12` |

---

## 5. Teaching Domain Use Cases

The Teaching Domain owns teacher professional profiles, qualification evidence, teaching authorizations, teacher availability, teaching demand intake, eligibility evaluation, teaching assignments (including homeroom advisory), workload management, and substitutions.

### Summary by Actor

| Actor | Use Case ID | Use Case Name | Workflow Reference | Stage |
|---|---|---|---|---|
| **Teacher** | `UC-TCH-01` | Declare Teaching Availability | `TCH-WF-04` | Stage 2 |
| **Teaching Coordinator** | `UC-TCH-02` | Maintain Teaching Profile | `TCH-WF-01` | Stage 2 |
| | `UC-TCH-03` | Verify Teacher Qualifications | `TCH-WF-02` | Stage 2 |
| | `UC-TCH-04` | Process Teaching Demand | `TCH-WF-05` | Stage 2 |
| | `UC-TCH-05` | Evaluate Teacher Eligibility and Capacity | `TCH-WF-06` | Stage 2 |
| | `UC-TCH-06` | Arrange Substitute Teacher | `TCH-WF-09` | Stage 2 |
| | `UC-TCH-07` | Monitor Faculty Workload | `TCH-WF-10` | Stage 2 |
| | `UC-TCH-08` | Conclude Teaching Assignment | `TCH-WF-11` | Stage 2 |
| **Teaching Authority** | `UC-TCH-09` | Determine Teaching Authorization | `TCH-WF-03` | Stage 2 |
| | `UC-TCH-10` | Approve Teaching Assignment | `TCH-WF-07` | Stage 2 |
| | `UC-TCH-11` | Modify or Cancel Teaching Assignment | `TCH-WF-08` | Stage 2 |
| | `UC-TCH-12` | Correct Approved Teaching Record | `TCH-WF-12` | Stage 2 |

---

### 5.1 Teacher

#### `UC-TCH-01`. Declare Teaching Availability
| Attribute | Value |
|---|---|
| **ID** | `UC-TCH-01` |
| **Name** | Declare Teaching Availability |
| **Primary Actor** | Teacher |
| **Supporting Actor(s)** | Teaching Coordinator |
| **Goal** | Declare weekly working availability, preferred teaching blocks, and unavailable time windows (leaves, personal constraints) for timetable scheduling. |
| **Expected Outcome** | Teacher availability profile is recorded and made available to Operations for timetable feasibility review. |
| **Workflow Reference** | `TCH-WF-04` |

---

### 5.2 Teaching Coordinator

#### `UC-TCH-02`. Maintain Teaching Profile
| Attribute | Value |
|---|---|
| **ID** | `UC-TCH-02` |
| **Name** | Maintain Teaching Profile |
| **Primary Actor** | Teaching Coordinator |
| **Supporting Actor(s)** | Teacher, Teaching Authority |
| **Goal** | Create and maintain a teacher's professional profile, verified identity link, contact information, subject competencies, and institutional affiliation. |
| **Expected Outcome** | Teaching profile is established or updated and placed into active status. |
| **Workflow Reference** | `TCH-WF-01` |

#### `UC-TCH-03`. Verify Teacher Qualifications
| Attribute | Value |
|---|---|
| **ID** | `UC-TCH-03` |
| **Name** | Verify Teacher Qualifications |
| **Primary Actor** | Teaching Coordinator |
| **Supporting Actor(s)** | Teacher |
| **Goal** | Collect, inspect, and verify documentary evidence of teacher qualifications (academic degrees, teaching certificates, government licenses, background clearances). |
| **Expected Outcome** | Qualification credentials are verified, validity dates recorded, and verification status logged. |
| **Workflow Reference** | `TCH-WF-02` |

#### `UC-TCH-04`. Process Teaching Demand
| Attribute | Value |
|---|---|
| **ID** | `UC-TCH-04` |
| **Name** | Process Teaching Demand |
| **Primary Actor** | Teaching Coordinator |
| **Supporting Actor(s)** | Academic Coordinator, Student Administration Officer |
| **Goal** | Receive, validate, and clarify teaching staffing requirements from Academics (learning unit sections) and Students (homeroom advisory roles) for an upcoming term. |
| **Expected Outcome** | Teaching demand requirements are documented, validated, and logged as actionable demand entries. |
| **Workflow Reference** | `TCH-WF-05` |

#### `UC-TCH-05`. Evaluate Teacher Eligibility and Capacity
| Attribute | Value |
|---|---|
| **ID** | `UC-TCH-05` |
| **Name** | Evaluate Teacher Eligibility and Capacity |
| **Primary Actor** | Teaching Coordinator |
| **Supporting Actor(s)** | Teaching Authority |
| **Goal** | Match candidate teachers against teaching demand by checking verified qualifications, authorization levels, and remaining workload capacity. |
| **Expected Outcome** | Eligible and available candidate teachers are identified and recommended for assignment. |
| **Workflow Reference** | `TCH-WF-06` |

#### `UC-TCH-06`. Arrange Substitute Teacher
| Attribute | Value |
|---|---|
| **ID** | `UC-TCH-06` |
| **Name** | Arrange Substitute Teacher |
| **Primary Actor** | Teaching Coordinator |
| **Supporting Actor(s)** | Teaching Authority, Teacher, Operations Coordinator |
| **Goal** | Appoint a qualified, authorized temporary substitute teacher to cover instructional sessions for an absent faculty member without altering historical assignment records. |
| **Expected Outcome** | A temporary substitution record is established, substitute authorized to conduct the activity, and timetable updated. |
| **Workflow Reference** | `TCH-WF-09` |

#### `UC-TCH-07`. Monitor Faculty Workload
| Attribute | Value |
|---|---|
| **ID** | `UC-TCH-07` |
| **Name** | Monitor Faculty Workload |
| **Primary Actor** | Teaching Coordinator |
| **Supporting Actor(s)** | Teaching Authority, School Leadership |
| **Goal** | Monitor aggregate faculty teaching hours against maximum institutional workload thresholds, identify overloading, and process workload policy exceptions. |
| **Expected Outcome** | Workload reports are compiled, capacity rebalanced, or formal overload exceptions approved. |
| **Workflow Reference** | `TCH-WF-10` |

#### `UC-TCH-08`. Conclude Teaching Assignment
| Attribute | Value |
|---|---|
| **ID** | `UC-TCH-08` |
| **Name** | Conclude Teaching Assignment |
| **Primary Actor** | Teaching Coordinator |
| **Supporting Actor(s)** | Teacher, Academic Coordinator |
| **Goal** | Formally conclude an active teaching assignment at the end of the academic period upon verification of grade submissions and handover completion. |
| **Expected Outcome** | Teaching assignment is marked `Completed`, teaching history updated, and teacher workload capacity released for subsequent periods. |
| **Workflow Reference** | `TCH-WF-11` |

---

### 5.3 Teaching Authority

#### `UC-TCH-09`. Determine Teaching Authorization
| Attribute | Value |
|---|---|
| **ID** | `UC-TCH-09` |
| **Name** | Determine Teaching Authorization |
| **Primary Actor** | Teaching Authority |
| **Supporting Actor(s)** | Teaching Coordinator, School Leadership |
| **Goal** | Formally determine, grant, condition, suspend, or revoke a teacher's institutional teaching authorization level and subject competencies based on verified credentials. |
| **Expected Outcome** | Formal Teaching Authorization record is created or updated, defining authorized subjects, grade levels, and supervision conditions. |
| **Workflow Reference** | `TCH-WF-03` |

#### `UC-TCH-10`. Approve Teaching Assignment
| Attribute | Value |
|---|---|
| **ID** | `UC-TCH-10` |
| **Name** | Approve Teaching Assignment |
| **Primary Actor** | Teaching Authority |
| **Supporting Actor(s)** | Teaching Coordinator, Teacher |
| **Goal** | Review proposed teaching assignments matching authorized teachers to learning units or homeroom advisory roles, grant binding approval, and commit workload units. |
| **Expected Outcome** | Approved Teaching Assignment is committed, teacher assigned to learning unit/cohort, workload capacity deducted, and Academics notified. |
| **Workflow Reference** | `TCH-WF-07` |

#### `UC-TCH-11`. Modify or Cancel Teaching Assignment
| Attribute | Value |
|---|---|
| **ID** | `UC-TCH-11` |
| **Name** | Modify or Cancel Teaching Assignment |
| **Primary Actor** | Teaching Authority |
| **Supporting Actor(s)** | Teaching Coordinator, Teacher |
| **Goal** | Authorize modifications, temporary suspensions, or cancellations of active teaching assignments due to curriculum changes, faculty leaves, or departures. |
| **Expected Outcome** | Teaching assignment status is updated (`Changed`, `Suspended`, or `Cancelled`), workload capacity restored, and Academics notified to arrange replacement. |
| **Workflow Reference** | `TCH-WF-08` |

#### `UC-TCH-12`. Correct Approved Teaching Record
| Attribute | Value |
|---|---|
| **ID** | `UC-TCH-12` |
| **Name** | Correct Approved Teaching Record |
| **Primary Actor** | Teaching Authority |
| **Supporting Actor(s)** | Teaching Coordinator |
| **Goal** | Execute an auditable correction to finalized teaching profiles, credential records, authorizations, or assignment histories. |
| **Expected Outcome** | Teaching record is corrected with mandatory justification and audit trail. |
| **Workflow Reference** | `TCH-WF-12` |

---

## 6. Development Domain Use Cases

The Development Domain owns non-academic student growth, behaviour observations, positive recognitions, incident triage, formal discipline cases, pastoral referrals, confidential counselling cases, wellbeing support plans, non-academic interventions, extracurricular programs, non-academic achievements, and cross-domain support recommendations.

### Summary by Actor

| Actor | Use Case ID | Use Case Name | Workflow Reference | Stage |
|---|---|---|---|---|
| **Student / Parent / Guardian** | `UC-DEV-01` | Submit Development Referral | `DEV-WF-04` | Stage 3 |
| | `UC-DEV-02` | Register for Extracurricular Program | `DEV-WF-09` | Stage 3 |
| **Teacher / Authorized Reporter** | `UC-DEV-03` | Record Behaviour Observation | `DEV-WF-01` | Stage 3 |
| | `UC-DEV-04` | Report Behaviour Incident | `DEV-WF-02` | Stage 3 |
| **Student Development Officer** | `UC-DEV-05` | Triage Behaviour Incident | `DEV-WF-02` | Stage 3 |
| | `UC-DEV-06` | Route Development Referral | `DEV-WF-04` | Stage 3 |
| | `UC-DEV-07` | Manage Wellbeing Support Plan | `DEV-WF-06` | Stage 3 |
| | `UC-DEV-08` | Establish Non-Academic Intervention | `DEV-WF-07` | Stage 3 |
| | `UC-DEV-09` | Establish Extracurricular Program | `DEV-WF-08` | Stage 3 |
| | `UC-DEV-10` | Manage Extracurricular Participation and Attendance | `DEV-WF-09` | Stage 3 |
| | `UC-DEV-11` | Recognize Non-Academic Achievement | `DEV-WF-10` | Stage 3 |
| | `UC-DEV-12` | Issue Pastoral Cross-Domain Recommendation | `DEV-WF-11` | Stage 3 |
| **Counselor** | `UC-DEV-13` | Manage Confidential Counselling Case | `DEV-WF-05` | Stage 3 |
| **Discipline Authority** | `UC-DEV-14` | Investigate and Determine Discipline Case | `DEV-WF-03` | Stage 3 |
| | `UC-DEV-15` | Recommend Disciplinary Administrative Sanction | `DEV-WF-11` | Stage 3 |
| **Development Authority / Multi-Role** | `UC-DEV-16` | Correct Approved Development Record | `DEV-WF-12` | Stage 3 |

---

### 6.1 Student / Parent / Guardian

#### `UC-DEV-01`. Submit Development Referral
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-01` |
| **Name** | Submit Development Referral |
| **Primary Actor** | Student / Parent / Guardian |
| **Supporting Actor(s)** | Student Development Officer, Counselor |
| **Goal** | Request non-academic pastoral support, mental wellbeing assistance, or developmental guidance for a student (self-referral or parental request). |
| **Expected Outcome** | Support referral is logged, timestamped, and submitted for triage while maintaining requested privacy. |
| **Workflow Reference** | `DEV-WF-04` |

#### `UC-DEV-02`. Register for Extracurricular Program
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-02` |
| **Name** | Register for Extracurricular Program |
| **Primary Actor** | Student |
| **Supporting Actor(s)** | Parent / Guardian (authorizes consent), Student Development Officer |
| **Goal** | Register for an approved extracurricular club, sports team, or performing arts group during the active registration window. |
| **Expected Outcome** | Student registration is submitted and pending parental consent or facilitator confirmation. |
| **Workflow Reference** | `DEV-WF-09` |

---

### 6.2 Teacher / Authorized Reporter

#### `UC-DEV-03`. Record Behaviour Observation
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-03` |
| **Name** | Record Behaviour Observation |
| **Primary Actor** | Teacher / Authorized Staff |
| **Supporting Actor(s)** | Student Development Officer |
| **Goal** | Record factual, objective observations of commendable student conduct (positive recognition) or emerging behavioral concerns in the non-academic domain. |
| **Expected Outcome** | Behaviour observation or recognition is logged on the student's developmental record with factual context. |
| **Workflow Reference** | `DEV-WF-01` |

#### `UC-DEV-04`. Report Behaviour Incident
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-04` |
| **Name** | Report Behaviour Incident |
| **Primary Actor** | Teacher / Authorized Reporter |
| **Supporting Actor(s)** | Student Development Officer |
| **Goal** | Submit a factual, objective report documenting an observed behavioural infraction, safety event, or student misconduct. |
| **Expected Outcome** | Behaviour incident report is logged with event date, location, involved individuals, and initial evidence, queued for triage. |
| **Workflow Reference** | `DEV-WF-02` |

---

### 6.3 Student Development Officer

#### `UC-DEV-05`. Triage Behaviour Incident
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-05` |
| **Name** | Triage Behaviour Incident |
| **Primary Actor** | Student Development Officer |
| **Supporting Actor(s)** | Discipline Authority, Parent / Guardian |
| **Goal** | Review an incoming behavioural incident report, determine severity and risk, and route it to pastoral support or escalate to formal disciplinary review. |
| **Expected Outcome** | Incident is triaged, assigned handling category (Pastoral Care vs Disciplinary Investigation), and routed to the appropriate authority. |
| **Workflow Reference** | `DEV-WF-02` |

#### `UC-DEV-06`. Route Development Referral
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-06` |
| **Name** | Route Development Referral |
| **Primary Actor** | Student Development Officer |
| **Supporting Actor(s)** | Counselor, Requesting Actor |
| **Goal** | Evaluate an incoming student support referral, determine clinical urgency and support type, and assign the referral to confidential counselling or pastoral staff. |
| **Expected Outcome** | Referral is assigned, urgency classified, and intake session initiated. |
| **Workflow Reference** | `DEV-WF-04` |

#### `UC-DEV-07`. Manage Wellbeing Support Plan
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-07` |
| **Name** | Manage Wellbeing Support Plan |
| **Primary Actor** | Student Development Officer |
| **Supporting Actor(s)** | Counselor, Student, Parent / Guardian |
| **Goal** | Formulate, coordinate, and review holistic pastoral care and support plans addressing non-clinical student emotional, physical, social, or attendance wellbeing concerns. |
| **Expected Outcome** | Wellbeing support plan is established, monitoring checkpoints scheduled, and pastoral accommodations tracked. |
| **Workflow Reference** | `DEV-WF-06` |

#### `UC-DEV-08`. Establish Non-Academic Intervention
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-08` |
| **Name** | Establish Non-Academic Intervention |
| **Primary Actor** | Student Development Officer |
| **Supporting Actor(s)** | Teacher, Student, Parent / Guardian |
| **Goal** | Design, monitor, adjust, and conclude formal structured non-academic behavioral or developmental intervention plans for students with repeated infractions or developmental challenges. |
| **Expected Outcome** | Formal intervention plan is created, scheduled review milestones evaluated, and intervention concluded upon target achievement. |
| **Workflow Reference** | `DEV-WF-07` |

#### `UC-DEV-09`. Establish Extracurricular Program
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-09` |
| **Name** | Establish Extracurricular Program |
| **Primary Actor** | Student Development Officer |
| **Supporting Actor(s)** | School Leadership, Operations Coordinator |
| **Goal** | Create and approve school extracurricular offerings (clubs, athletic teams, cultural ensembles, student leadership bodies), establish program charters, and set eligibility criteria. |
| **Expected Outcome** | Extracurricular program charter is approved and published for student registration. |
| **Workflow Reference** | `DEV-WF-08` |

#### `UC-DEV-10`. Manage Extracurricular Participation and Attendance
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-10` |
| **Name** | Manage Extracurricular Participation and Attendance |
| **Primary Actor** | Student Development Officer |
| **Supporting Actor(s)** | Teacher / Facilitator, Student |
| **Goal** | Oversee student roster approvals, verify parental consent, monitor regular attendance and participation, and record program completion for extracurricular activities. |
| **Expected Outcome** | Student participation record is maintained, attendance logged, and official completion certified. |
| **Workflow Reference** | `DEV-WF-09` |

#### `UC-DEV-11`. Recognize Non-Academic Achievement
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-11` |
| **Name** | Recognize Non-Academic Achievement |
| **Primary Actor** | Student Development Officer |
| **Supporting Actor(s)** | Teacher / Facilitator, Student |
| **Goal** | Officially certify and record student achievements, awards, and distinctions in athletics, arts, leadership, and community service. |
| **Expected Outcome** | Non-academic achievement record is created and published to the student's holistic profile. |
| **Workflow Reference** | `DEV-WF-10` |

#### `UC-DEV-12`. Issue Pastoral Cross-Domain Recommendation
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-12` |
| **Name** | Issue Pastoral Cross-Domain Recommendation |
| **Primary Actor** | Student Development Officer |
| **Supporting Actor(s)** | Academic Authority, Student Administration Officer |
| **Goal** | Formally issue and transmit authorized pastoral support recommendations (e.g. learning plan accommodations to Academics or administrative support to Students). |
| **Expected Outcome** | Recommendation is formally transmitted, tracked, and linked to receiving domain decision processes. |
| **Workflow Reference** | `DEV-WF-11` |

---

### 6.4 Counselor

#### `UC-DEV-13`. Manage Confidential Counselling Case
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-13` |
| **Name** | Manage Confidential Counselling Case |
| **Primary Actor** | Counselor |
| **Supporting Actor(s)** | Student, Parent / Guardian |
| **Goal** | Deliver confidential psychological and emotional counselling support, maintain restricted clinical case notes (GAP-04), set support goals, and coordinate external mental health referrals. |
| **Expected Outcome** | Confidential counselling case is managed with strict privacy controls, session logs recorded, and sanitized non-confidential recommendations issued when needed. |
| **Workflow Reference** | `DEV-WF-05` |

---

### 6.5 Discipline Authority

#### `UC-DEV-14`. Investigate and Determine Discipline Case
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-14` |
| **Name** | Investigate and Determine Discipline Case |
| **Primary Actor** | Discipline Authority |
| **Supporting Actor(s)** | School Leadership, Student, Parent / Guardian |
| **Goal** | Conduct a formal, fair process investigation into alleged student misconduct, evaluate evidence and hearing statements, establish factual findings, and determine appropriate disciplinary sanctions. |
| **Expected Outcome** | Formal discipline determination is recorded, findings documented, sanctions imposed within policy limits, and notifications dispatched. |
| **Workflow Reference** | `DEV-WF-03` |

#### `UC-DEV-15`. Recommend Disciplinary Administrative Sanction
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-15` |
| **Name** | Recommend Disciplinary Administrative Sanction |
| **Primary Actor** | Discipline Authority |
| **Supporting Actor(s)** | School Leadership, Student Administration Officer |
| **Goal** | Formally recommend severe administrative lifecycle sanctions (suspension or disciplinary expulsion / withdrawal per GAP-02) to School Leadership and Students domain. |
| **Expected Outcome** | Disciplinary recommendation is officially transmitted to School Leadership and Students domain for executive authorization and execution. |
| **Workflow Reference** | `DEV-WF-11` |

---

### 6.6 Development Authority / Multi-Role

#### `UC-DEV-16`. Correct Approved Development Record
| Attribute | Value |
|---|---|
| **ID** | `UC-DEV-16` |
| **Name** | Correct Approved Development Record |
| **Primary Actor** | Student Development Officer / Counselor / Discipline Authority |
| **Supporting Actor(s)** | None |
| **Goal** | Execute an auditable correction to finalized incident reports, observation entries, counselling case files, or discipline records under strict confidentiality and privacy safeguards. |
| **Expected Outcome** | Erroneous development record is corrected with documented justification, preserving confidentiality and audit history. |
| **Workflow Reference** | `DEV-WF-12` |

---

## 7. Cross-Domain Traceability Matrix

The table below traces every source workflow from `operational-workflow-discovery.md` to its implementing use case(s) and development stage:

| Workflow Code | Workflow Name | Domain | Stage | Implementing Use Case(s) | Primary Actor(s) |
|---|---|---|---|---|---|
| `STD-WF-01` | Register Admission Application | Students | Stage 1 | `UC-STD-01` | Applicant / Parent / Guardian |
| `STD-WF-02` | Review Admission Application | Students | Stage 1 / Stage 2 | `UC-STD-03` | Admission Officer |
| `STD-WF-03` | Enroll Accepted Applicant | Students | Stage 1 | `UC-STD-04` | Student Administration Officer |
| `STD-WF-04` | Maintain Student Identity | Students | Stage 1 | `UC-STD-05` | Student Administration Officer |
| `STD-WF-05` | Change Administrative Placement | Students | Stage 1 | `UC-STD-06` | Student Administration Officer |
| `STD-WF-06` | Place Student on Leave | Students | Stage 2 | `UC-STD-07` | Student Administration Officer |
| `STD-WF-07` | Return Student from Leave | Students | Stage 2 | `UC-STD-08` | Student Administration Officer |
| `STD-WF-08` | Execute Promotion or Retention | Students | Stage 2 | `UC-STD-09` | Student Administration Officer |
| `STD-WF-09` | Process Internal Transfer | Students | Stage 2 | `UC-STD-10` | Student Administration Officer |
| `STD-WF-10` | Process External Transfer Out | Students | Stage 3 | `UC-STD-11` | Student Administration Officer |
| `STD-WF-11` | Process Withdrawal | Students | Stage 3 | `UC-STD-12` | Student Administration Officer |
| `STD-WF-12` | Execute Graduation | Students | Stage 3 | `UC-STD-13` | Student Administration Officer |
| `STD-WF-13` | Activate Alumni Status | Students | Stage 3 | `UC-STD-14` | Student Administration Officer |
| `STD-WF-14` | Request Academic Clearance for Transfer Out | Students | Stage 3 | `UC-STD-15` | Student Administration Officer |
| `STD-WF-15` | Request Purpose-Specific Financial Clearance | Students | Stage 3 | `UC-STD-16` | Student Administration Officer |
| `STD-WF-16` | Re-enroll Student | Students | Stage 2 | `UC-STD-02`, `UC-STD-17` | Parent / Guardian; Student Administration Officer |
| `STD-WF-17` | Process External Transfer In | Students | Stage 3 | `UC-STD-18` | Student Administration Officer |
| `ACD-WF-01` | Define or Adopt Learning Structure | Academics | Stage 2 | `UC-ACD-08` | Academic Coordinator |
| `ACD-WF-02` | Establish Student Learning Plan | Academics | Stage 2 | `UC-ACD-09` | Academic Coordinator |
| `ACD-WF-03` | Start Academic Participation | Academics | Stage 2 | `UC-ACD-10` | Academic Coordinator |
| `ACD-WF-04` | Conduct Learning Activity | Academics | Stage 2 | `UC-ACD-02` | Teacher |
| `ACD-WF-05` | Record or Correct Attendance | Academics | Stage 2 | `UC-ACD-03` | Teacher |
| `ACD-WF-06` | Issue and Receive Assignment | Academics | Stage 2 | `UC-ACD-01`, `UC-ACD-04` | Student; Teacher |
| `ACD-WF-07` | Plan and Conduct Assessment | Academics | Stage 2 | `UC-ACD-05` | Teacher |
| `ACD-WF-08` | Evaluate Student Learning | Academics | Stage 2 | `UC-ACD-06` | Teacher |
| `ACD-WF-09` | Approve and Publish Academic Result | Academics | Stage 2 | `UC-ACD-16` | Academic Authority |
| `ACD-WF-10` | Review Progress and Initiate Academic Intervention | Academics | Stage 2 | `UC-ACD-11` | Academic Coordinator |
| `ACD-WF-11` | Complete Learning | Academics | Stage 2 | `UC-ACD-12` | Academic Coordinator |
| `ACD-WF-12` | Produce Report Card or Transcript | Academics | Stage 2 | `UC-ACD-13` | Academic Coordinator |
| `ACD-WF-13` | Determine Academic Standing and Progression Recommendation | Academics | Stage 2 | `UC-ACD-17` | Academic Authority |
| `ACD-WF-14` | Determine Graduation Eligibility | Academics | Stage 3 | `UC-ACD-18` | Academic Authority |
| `ACD-WF-15` | Correct Approved Academic Record | Academics | Stage 2 | `UC-ACD-19` | Academic Authority |
| `ACD-WF-16` | Determine Academic Clearance | Academics | Stage 3 | `UC-ACD-14` | Academic Coordinator |
| `ACD-WF-17` | Correct Academic Clearance | Academics | Stage 3 | `UC-ACD-20` | Academic Authority |
| `ACD-WF-18` | Correct Phase-2 Academic Record | Academics | Stage 2 / Stage 3 | `UC-ACD-15` | Academic Coordinator |
| `ACD-WF-19` | Recognize Academic Achievement | Academics | Stage 2 | `UC-ACD-21` | Academic Authority |
| `ACD-WF-20` | Conduct Entrance Assessment for Applicant | Academics | Stage 2 | `UC-ACD-07` | Teacher (Assessment Officer) |
| `FIN-WF-01` | Define or Adopt Financial Policy | Finance | Stage 1 | `UC-FIN-10` | Finance Authority |
| `FIN-WF-02` | Assess and Issue Financial Obligation | Finance | Stage 1 | `UC-FIN-02` | Finance Officer |
| `FIN-WF-03` | Approve Scholarship, Discount, or Waiver | Finance | Stage 2 | `UC-FIN-11` | Finance Authority |
| `FIN-WF-04` | Issue Bill or Financial Statement | Finance | Stage 1 | `UC-FIN-03` | Finance Officer |
| `FIN-WF-05` | Receive and Confirm Payment | Finance | Stage 1 | `UC-FIN-01`, `UC-FIN-04` | Payer; Finance Officer |
| `FIN-WF-06` | Allocate or Reallocate Payment | Finance | Stage 1 | `UC-FIN-05` | Finance Officer |
| `FIN-WF-07` | Manage Due and Overdue Receivable | Finance | Stage 2 | `UC-FIN-06`, `UC-FIN-12` | Finance Officer; Finance Authority |
| `FIN-WF-08` | Manage Financial Dispute or Adjustment | Finance | Stage 2 | `UC-FIN-13` | Finance Authority |
| `FIN-WF-09` | Manage Credit or Overpayment | Finance | Stage 2 | `UC-FIN-07` | Finance Officer |
| `FIN-WF-10` | Request, Approve, and Execute Refund | Finance | Stage 2 | `UC-FIN-14` | Finance Authority |
| `FIN-WF-11` | Determine Financial Clearance | Finance | Stage 3 | `UC-FIN-08` | Finance Officer |
| `FIN-WF-12` | Correct Approved Finance Record | Finance | Stage 2 | `UC-FIN-15` | Finance Authority |
| `FIN-WF-13` | Confirm Enrollment Payment Status | Finance | Stage 1 | `UC-FIN-09` | Finance Officer |
| `OPS-WF-01` | Establish or Amend Academic Year and School Calendar | Operations | Stage 1 | `UC-OPS-10` | Operations Authority |
| `OPS-WF-02` | Submit and Clarify Operational Activity Request | Operations | Stage 2 | `UC-OPS-01` | Requesting Actors (Teacher/Coord) |
| `OPS-WF-03` | Review Operational Feasibility | Operations | Stage 2 | `UC-OPS-02` | Operations Coordinator |
| `OPS-WF-04` | Schedule an Activity and Allocate a Room | Operations | Stage 2 | `UC-OPS-03` | Operations Coordinator |
| `OPS-WF-05` | Resolve Operational Conflict or Exception | Operations | Stage 2 | `UC-OPS-04` | Operations Coordinator |
| `OPS-WF-06` | Plan and Approve School Event | Operations | Stage 3 | `UC-OPS-05` | Operations Coordinator |
| `OPS-WF-07` | Publish or Withdraw Operational Announcement | Operations | Stage 3 | `UC-OPS-06` | Operations Coordinator |
| `OPS-WF-08` | Record Room Restriction or Unavailability | Operations | Stage 2 | `UC-OPS-07` | Operations Coordinator |
| `OPS-WF-09` | Reschedule, Relocate, Postpone, or Cancel | Operations | Stage 2 | `UC-OPS-08` | Operations Coordinator |
| `OPS-WF-10` | Coordinate School or Campus Closure | Operations | Stage 1 / Stage 2 | `UC-OPS-11` | Operations Authority |
| `OPS-WF-11` | Close Completed Operational Arrangement | Operations | Stage 2 | `UC-OPS-09` | Operations Coordinator |
| `OPS-WF-12` | Correct Approved Operations Record | Operations | Stage 2 / Stage 3 | `UC-OPS-12` | Operations Authority |
| `TCH-WF-01` | Establish or Change Teaching Profile | Teaching | Stage 2 | `UC-TCH-02` | Teaching Coordinator |
| `TCH-WF-02` | Submit and Verify Qualification Evidence | Teaching | Stage 2 | `UC-TCH-03` | Teaching Coordinator |
| `TCH-WF-03` | Grant, Restrict, Suspend, or Revoke Teaching Authorization | Teaching | Stage 2 | `UC-TCH-09` | Teaching Authority |
| `TCH-WF-04` | Establish or Change Teacher Availability | Teaching | Stage 2 | `UC-TCH-01` | Teacher |
| `TCH-WF-05` | Receive and Clarify Teaching Demand | Teaching | Stage 2 | `UC-TCH-04` | Teaching Coordinator |
| `TCH-WF-06` | Evaluate Teacher Eligibility and Capacity | Teaching | Stage 2 | `UC-TCH-05` | Teaching Coordinator |
| `TCH-WF-07` | Propose and Approve Teaching Assignment | Teaching | Stage 2 | `UC-TCH-10` | Teaching Authority |
| `TCH-WF-08` | Change, Suspend, or Cancel Teaching Assignment | Teaching | Stage 2 | `UC-TCH-11` | Teaching Authority |
| `TCH-WF-09` | Arrange Substitution or Reassignment | Teaching | Stage 2 | `UC-TCH-06` | Teaching Coordinator |
| `TCH-WF-10` | Review Workload and Resolve Capacity Conflict | Teaching | Stage 2 | `UC-TCH-07` | Teaching Coordinator |
| `TCH-WF-11` | Complete Teaching Assignment | Teaching | Stage 2 | `UC-TCH-08` | Teaching Coordinator |
| `TCH-WF-12` | Correct Approved Teaching Record | Teaching | Stage 2 | `UC-TCH-12` | Teaching Authority |
| `DEV-WF-01` | Record Behaviour Observation or Recognition | Development | Stage 3 | `UC-DEV-03` | Teacher / Authorized Staff |
| `DEV-WF-02` | Report and Triage Behaviour Incident | Development | Stage 3 | `UC-DEV-04`, `UC-DEV-05` | Teacher / Reporter; Student Development Officer |
| `DEV-WF-03` | Investigate and Determine Discipline Case | Development | Stage 3 | `UC-DEV-14` | Discipline Authority |
| `DEV-WF-04` | Receive and Route Development Referral | Development | Stage 3 | `UC-DEV-01`, `UC-DEV-06` | Student / Parent; Student Development Officer |
| `DEV-WF-05` | Manage Counselling Support | Development | Stage 3 | `UC-DEV-13` | Counselor |
| `DEV-WF-06` | Assess and Support Wellbeing Concern | Development | Stage 3 | `UC-DEV-07` | Student Development Officer |
| `DEV-WF-07` | Establish and Review Non-Academic Intervention | Development | Stage 3 | `UC-DEV-08` | Student Development Officer |
| `DEV-WF-08` | Establish Extracurricular Program | Development | Stage 3 | `UC-DEV-09` | Student Development Officer |
| `DEV-WF-09` | Manage Extracurricular Participation | Development | Stage 3 | `UC-DEV-02`, `UC-DEV-10` | Student; Student Development Officer |
| `DEV-WF-10` | Recognize Non-Academic Achievement | Development | Stage 3 | `UC-DEV-11` | Student Development Officer |
| `DEV-WF-11` | Issue Cross-Domain Recommendation | Development | Stage 3 | `UC-DEV-12`, `UC-DEV-15` | Student Development Officer; Discipline Authority |
| `DEV-WF-12` | Correct Approved Development Record | Development | Stage 3 | `UC-DEV-16` | SDO / Counselor / Discipline Authority |

---

## 8. Open Questions and Operational Policy Dependencies

The following open policy questions from `actors.md` directly impact the implementation details and permission boundaries of specific use cases. They are formally recorded below to prevent speculative assumptions:

1. **Segregation of Duties in Operational Finance (`UC-FIN-02`, `UC-FIN-04`, `UC-FIN-05`):**
   - *Question:* Does institutional financial policy require strict segregation of duties between billing assessment and cashiering/payment confirmation? If required by audit standards, the consolidated `Finance Officer` role must be partitioned into mutually exclusive `Billing Officer` and `Cashier` roles.
2. **Discretionary Monetary Thresholds for Adjustments and Refunds (`UC-FIN-13`, `UC-FIN-14`):**
   - *Question:* What specific monetary thresholds govern whether a `Finance Officer` can independently execute routine fee adjustments or small refunds versus requiring mandatory prior sign-off from the `Finance Authority`?
3. **Student Self-Service Decision Authority vs. Parental Governance (`UC-STD-07`, `UC-STD-10`, `UC-STD-12`):**
   - *Question:* Under what legal or program age thresholds (e.g. legal age of majority) can an enrolled `Student` independently request leaves of absence, internal transfers, or voluntary withdrawals without parental signature?
4. **Multi-Guardian Authority and Conflicting Directives (`UC-STD-05`, `UC-STD-11`, `UC-STD-12`):**
   - *Question:* When multiple legal guardians issue conflicting administrative directives (e.g., one requests transfer-out while another objects), what legal documentation rules govern which directive is binding?
5. **Structural Independence of Disciplinary Appeals (`UC-DEV-14`):**
   - *Question:* Who holds authoritative jurisdiction to decide formal disciplinary appeals, and how is that role structurally separated from the original investigating `Discipline Authority`?
6. **Parental Consent and Mandatory Reporting in Counselling (`UC-DEV-13`):**
   - *Question:* Under what student age thresholds and clinical risk criteria is explicit parental consent mandatory before a `Counselor` may maintain restricted counseling cases, versus cases where student confidentiality legally overrides parental disclosure?
7. **Teacher Grade Publication Autonomy vs. Central Moderation (`UC-ACD-06`, `UC-ACD-16`):**
   - *Question:* Can a `Teacher` publish routine formative classroom assignment marks directly to students/parents, or does all evaluative feedback require moderation and release approval by the `Academic Authority`?
8. **Emergency Substitution Authorization Threshold (`UC-TCH-06`):**
   - *Question:* Can a `Teaching Coordinator` unilaterally assign an emergency substitute teacher for short absences, or does institutional policy require formal `Teaching Authority` sign-off for all substitutions?
9. **Operational Activity Rescheduling Autonomy (`UC-OPS-01`, `UC-OPS-08`):**
   - *Question:* Can an activity initiator independently reschedule or cancel an approved room reservation, or does all operational modification require formal processing by the `Operations Coordinator`?

---

## 9. Final Validation Verification

Prior to finalizing this document, the inventory was verified against all validation criteria:

- [x] **Supported by identified workflows:** Every use case traces directly to an identified business workflow from `operational-workflow-discovery.md`.
- [x] **No violation of actor responsibility or authority:** Primary and supporting actors strictly reflect the definitions and decision authorities established in `actors.md`.
- [x] **No CRUD operations:** All use cases represent purposeful business workflows with distinct goals and outcomes (e.g., `Enroll Accepted Applicant`, `Change Administrative Placement`, `Issue Coursework Assignment`, `Evaluate Financial Clearance`), not generic database manipulation screens.
- [x] **No speculative requirements:** No unverified features, future automation, or speculative integrations were introduced.
- [x] **No unnecessary use cases:** Workflows representing the same cohesive business goal were unified under single use cases, splitting only where distinct actors perform independent work (e.g., issuing an assignment vs. submitting an assignment).
