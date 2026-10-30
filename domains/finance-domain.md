# Finance Domain

## Purpose

The Finance domain manages financial obligations between students and the school.

It is the authoritative source for:

* what a student owes the school
* why an amount is owed
* when an obligation is due
* which approved scholarship, discount, or waiver changes the obligation
* which payment has been received and from whom
* how a payment is allocated
* what amount remains receivable
* whether credit or a refund is due
* whether a purpose-specific financial prerequisite has been satisfied
* the history of financial decisions and corrections

The domain begins when an approved policy or authorized decision may create a financial consequence for an applicant or student. It continues until the resulting obligations, payments, credits, refunds, and required history have been resolved or preserved.

This domain does not perform general accounting and does not determine a student's administrative or academic status.

---

## Domain Responsibility

The Finance domain owns:

* fee and charge policy as applied to student-school obligations
* student financial accounts
* charges and financial obligations
* billing and statements
* due schedules and approved payment arrangements
* scholarship financial decisions
* discounts and waivers
* payment receipt and confirmation
* payment allocation and reallocation
* receivables and overdue status
* credits and overpayments
* refunds
* financial adjustments and reversals
* purpose-specific financial clearance
* student financial history

Finance is the authoritative source of a student's financial position with the school.

Other domains may provide identity, status, placement, learning, development, calendar, or approval context. They must not independently create, change, settle, or cancel a financial obligation.

Finance may provide a prerequisite, confirmation, or recommendation to another domain. It must not directly change student status, academic results, learning participation, behaviour outcomes, teacher assignments, or operational schedules.

---

## Scope

### In Scope

#### Financial Policy and Charge Rules

Manage approved rules used to determine financial obligations between students and the school.

Examples may include:

* application fee
* enrollment fee
* tuition
* activity fee
* examination fee
* program, course, module, or service fee
* approved deposit
* approved financial penalty
* one-time charge
* recurring charge
* installment schedule

A financial policy or charge rule should identify:

* business purpose
* applicability criteria
* amount or approved method of determination
* currency
* effective period
* due-date rule
* approving authority
* policy version

The school determines actual amounts, categories, applicability, approval authority, and effective dates.

Agents must never invent them.

---

#### Student Financial Account

Maintain the student's financial relationship with the school.

The financial account provides an interpretable view of:

* obligations
* benefits
* payments
* allocations
* credits
* adjustments
* refunds
* remaining receivables

This is not a general-ledger account.

The student is the subject of the obligation, but the student is not necessarily the payer. A parent, guardian, sponsor, organization, or other authorized party may pay on the student's behalf.

The Students domain owns the identity of the student and the official relationship of parents, guardians, and financial contacts. Finance owns payer information required for a financial transaction and the transaction itself.

---

#### Financial Obligation and Charge

Manage an amount the student is required to pay to the school under an approved policy or authorized decision.

An obligation should preserve:

* student reference
* reason and source
* original amount
* currency
* issue date
* due date or schedule
* applicable period
* policy or decision reference
* current financial position
* adjustments and settlement history

A charge must not be created merely because data exists in another domain.

For example, enrollment, placement, academic participation, an activity, or a Development outcome may provide context. An approved Finance rule or authorized Finance decision determines the financial consequence.

---

#### Billing and Statements

Communicate financial obligations and their current position to an authorized recipient.

Billing may include:

* issuing a bill or notice
* producing a statement of obligations and payments
* communicating due dates
* showing approved benefits and credits
* correcting or replacing an inaccurate communication

A bill or statement represents financial information. It must not become the only source of the underlying obligations, payments, or allocations.

Reissuing a statement must not silently rewrite financial history.

---

#### Due Schedules and Payment Arrangements

Manage when an obligation is expected to be settled.

This may include:

* a single due date
* scheduled installments
* an approved payment arrangement
* revised due dates
* temporary deferment

An approved payment arrangement changes the expected settlement schedule. It does not erase the original obligation, does not create a new obligation, and does not reduce the original obligation amount.

**Phase-2 arrangement model:** Phase-2 supports a single arrangement concept — Payment Arrangement with Installment Schedule. Multiple arrangement categories (deferred payment, restructuring, hardship plan, payment holiday, or other specialized finance products) are not required and must not be introduced.

**Approval model:** Payment Arrangements require explicit Finance approval (Requester → Finance Officer → Finance Authority → Approved Arrangement). Arrangements are never auto-approved. School leadership involvement is only required when explicitly defined by school policy.

**Eligibility:** Eligibility requires: Outstanding Mandatory Obligation Exists and Finance Approval. Eligibility rules remain configurable school policy.

**Arrangement states:** Active, Delinquent, Completed, Cancelled.

**Missed installment policy:** A missed installment moves the arrangement to Delinquent status. Delinquent arrangements remain valid unless explicitly cancelled by Finance action. Cancellation requires an explicit Finance decision. Historical installment obligations and arrangement history must be preserved.

Revision rules must come from explicit school policy.

---

#### Scholarship

Manage the financial decision through which approved assistance is applied to a student's obligations.

Scholarship processing may include:

* application, nomination, or recommendation
* evidence receipt
* eligibility review
* approval or rejection
* amount or percentage
* applicable obligations
* effective period
* renewal or review
* suspension, revocation, or correction

**Benefit model:** Benefit Type is configurable by school policy. Phase-2 must not hard-code scholarship categories into the domain model.

**Renewal:** Renewable and Non-Renewable are both supported. Renewal terms come from school policy.

**Revocation:** Revocation is supported; benefits may be withdrawn according to school policy.

**Formal appeals:** Formal appeal workflows are out of scope for Phase-2.

**Ownership model:** Academics or Development provides recommendation or evidence only. Finance owns the scholarship financial decision and application. Academics does not grant financial benefits. Finance does not alter academic outcomes. Recommendations and evidence remain owned by their originating domains.

A scholarship must not change an academic result or non-academic achievement.

**Benefit stacking with Discounts and Waivers** is policy-driven (Allow Stacking = Yes / No per policy). Priority rules require explicit policy.

External scholarship funding, sponsor settlement, and accounting treatment remain outside Phase-2 core scope unless required by an approved workflow.

---

#### Discount and Waiver

Manage an approved reduction or release of an obligation.

A discount normally reduces an obligation according to an approved policy. A waiver releases some or all of an obligation under explicit authority.

Each discount or waiver should preserve:

* reason
* authority
* applicable obligation
* amount or method
* effective date
* supporting policy or decision
* reversal or correction history

A discount or waiver is not a payment.

**Benefit model:** Benefit Type is configurable by school policy. Phase-2 must not hard-code discount or waiver categories into the domain model.

**Renewal:** Renewable and Non-Renewable are both supported. Renewal terms come from school policy.

**Revocation:** Revocation is supported; benefits may be withdrawn according to school policy.

**Formal appeals:** Formal appeal workflows are out of scope for Phase-2.

**Benefit stacking:** Discounts, Waivers, and Scholarships may be combined when permitted by school policy. Stacking is policy-driven; eligibility and stacking rules must be explicit. The system must not assume that benefits are mutually exclusive, and must not assume that all benefits may always be combined. Stacking policy is expressed as: Allow Stacking = Yes / No per benefit type or policy.

Agents must not infer a benefit from family relationship, achievement, financial difficulty, staff status, timing, or any other characteristic without an explicit policy.

---

#### Payment Receipt and Confirmation

Manage value received by the school for a student's financial obligations.

Payment information may include:

* student reference
* payer
* amount and currency
* payment date
* payment method
* external or internal reference
* confirmation status
* receipt or acknowledgement
* reversal or correction

A submitted or externally reported payment is not necessarily a confirmed payment.

Confirmation rules depend on the payment method and approved school procedure.

Payment-channel implementation, gateway mechanics, and bank settlement are technical or accounting concerns unless required to express a business confirmation workflow.

---

#### Payment Allocation

Manage how confirmed value is applied to obligations.

Allocation may include:

* applying one payment to one obligation
* partially settling an obligation
* splitting a payment across multiple obligations
* using multiple payments for one obligation
* retaining an unallocated amount
* reallocating an incorrect allocation
* reversing an allocation

These concepts must remain distinct:

```text
Payment received
        │
        ▼
Payment confirmed
        │
        ▼
Payment allocated
        │
        ▼
Obligation partially or fully settled
```

A payment must not be assumed to settle the oldest, newest, or most important obligation unless an approved allocation rule says so.

Allocation and reallocation history must be preserved.

---

#### Receivables and Overdue Obligations

Manage amounts still owed to the school.

This may include:

* outstanding amount
* partial settlement
* due and overdue status
* aging
* reminder status
* disputed amount
* approved payment arrangement
* authorized write-off, if policy permits

An overdue obligation is financial information only. Finance records overdue status, aging, reminders, arrangements, disputes, and escalation. Finance does not automatically: suspend students, block participation, withhold academic results, prevent promotion, prevent graduation, or execute withdrawal. If school policy requires any such consequence, the owning domain must perform the action under its own authority and policy.

**Reminder model:** Reminder timing is configurable school policy; hard-coded day counts are not part of the domain contract. Phase-2 must support: Reminder Stage, Reminder Timing, and Escalation Stage without hard-coded dates. Conceptual model: Reminder Before Due Date → Reminder Shortly After Due Date → Escalated Overdue Reminder. Specific day counts remain school-policy configuration.

**Boundary principle:** Finance records financial status. Other domains execute their own decisions.

Another domain may consume an authorized financial prerequisite and perform its own decision under its own policy.

Collection agency activity, legal recovery, and accounting provisions are outside the current core scope unless explicitly added.

---

#### Credit and Overpayment

Manage confirmed value that is not currently allocated to an obligation or that exceeds the amount owed.

A credit may arise from:

* overpayment
* reversed charge
* reduced obligation after payment
* approved transfer of value
* other authorized correction

**Default flow:** Overpayments become Credit by default. Refund is not automatic. Default flow: Overpayment → Credit Balance → Future Allocation. Credit remains traceable to its source. Credit may later be allocated to eligible obligations. Refund requires a separate explicit refund process.

Policy determines whether credit may further be:

* allocated to another eligible obligation
* transferred between students or accounts (requires explicit authority)
* refunded (requires separate refund process)

Agents must not assume transferability or refundability.

---

#### Refund

Manage value the school returns to an authorized recipient.

Refund processing may include:

* refund request
* validation of available refundable value
* identification of the authorized recipient
* approval or rejection
* execution
* failure, cancellation, or reversal
* communication of outcome

A refund should reference the payment, credit, reversed charge, or other financial source that made the value refundable.

**Approval model:** Refunds require explicit Finance approval. Refunds are never automatically generated. Approval model: Finance Officer reviews → Finance Authority approves → Refund Executed. Refund authority belongs to Finance. Refund execution and approval history must be auditable.

**Refund ownership:** Refundable value belongs to the original payer by default. Student identity alone does not determine refund ownership; Financial Contact identity alone does not determine refund ownership. Exceptions require explicit Finance approval and documented justification. Example: if Payer = Company C, refund defaults to Company C regardless of whether Financial Contact = Parent B.

A charge reversal is not itself a refund. A refund is not complete merely because it was approved.

Refund eligibility, deductions, authority, destination, and timing must follow explicit policy.

---

#### Financial Adjustment, Reversal, and Dispute

Manage correction or authorized reconsideration of financial information.

This may include:

* charge adjustment
* charge cancellation or reversal
* payment reversal
* allocation reversal or reallocation
* benefit correction
* disputed obligation
* dispute determination

An adjustment must identify its reason, authority, effective date, and effect on the student's financial position.

**Historical integrity principle:** Financial history must never be rewritten. Corrections occur through adjustment, reversal, dispute resolution, or other approved financial events. Original records remain historically visible and traceable. Principle: Adjust History, Never Rewrite History.

**Adjustment authority:** Authority is determined by financial impact, not transaction type names. Billing Officers may perform: administrative corrections, reference corrections, note corrections, and non-financial metadata corrections. Finance Authority approval is required for any adjustment affecting financial value — including amount changes, waivers, refunds, reversals, write-offs, credits, and financial reallocations. Principle: Administrative Impact = Billing Officer; Financial Impact = Finance Authority. Historical traceability remains mandatory regardless of adjustment type.

Incorrect records must be corrected through an auditable action. They must not be silently edited to appear as if the original action never occurred.

---

#### Financial Clearance

Determine whether an explicit financial prerequisite has been satisfied for a defined purpose.

Examples may include:

* enrollment payment confirmation
* eligibility to complete a defined administrative step
* transfer financial clearance
* withdrawal financial settlement status
* graduation financial clearance

Clearance should identify:

* purpose
* subject
* applicable policy
* evaluated obligations
* result
* determination date
* validity or expiry, when applicable
* authority

Clearance is purpose-specific. A student may satisfy one prerequisite while retaining unrelated or future obligations.

**Clearance criteria:** Financial Clearance is blocked only by outstanding mandatory obligations. The following do not block clearance: pending refunds, future installments, credit balances, optional charges, non-mandatory fees. Clearance evaluation must be transparent and explainable; blocking obligations must be identified; schools may configure which obligation categories are considered mandatory.

**Mandatory clearance purposes:** Financial Clearance is mandatory for Transfer Out and Graduation. The following remain configurable school policy: Withdrawal, Re-enrollment, and other administrative processes. Additional clearance purposes may be configured without changing the domain model.

Enrollment-purpose clearance remains the Phase-0 procedure. Enrollment clearance is not Transfer Out clearance and is not Graduation clearance.

Phase-2 permitted results are:

* Granted
* Denied
* Conditional
* Cannot Yet Be Determined
* Expired
* Revoked

**Transfer Out execution:** Students may execute external transfer-out only when Financial Clearance for purpose Transfer Out is Granted. Denied, Conditional, Cannot Yet Be Determined, Expired, and Revoked do not permit transfer-out.

**Graduation execution:** When school policy requires graduation financial clearance (default: required), Students may execute graduation only when Financial Clearance for purpose Graduation is Granted. Denied, Conditional, Cannot Yet Be Determined, Expired, and Revoked do not permit graduation. If school policy does not require graduation financial clearance, Students does not wait for a Finance Graduation result. Graduation clearance evaluates only graduation-related mandatory obligations.

**Authority:** Finance Officer records the routine determination. Finance Authority records any exception determination, and the exception reason is mandatory. Financial Clearance must not change official student status or academic outcomes.

Students requests Phase-2 Financial Clearance for Transfer Out or Graduation. Finance determines it. Students consumes the owned result. Correction of an inaccurate clearance record uses Correct Approved Finance Record and is distinct from a new determination caused by changed financial circumstances.

Finance supplies the financial determination. Students, Academics, Development, or Operations performs any decision or transition it owns.

---

#### Financial History and Correction

Preserve an interpretable history of:

* policies used
* obligations issued
* benefits granted or rejected
* bills and statements communicated
* payments submitted, confirmed, rejected, or reversed
* allocations and reallocations
* overdue and arrangement decisions
* credits
* refunds
* clearance determinations
* disputes, adjustments, and corrections

The event date, transaction date, decision date, due date, effective date, settlement date, and recording date may be different. Their meanings must remain distinguishable.

Corrections must preserve what changed, why it changed, when it changed, and who authorized it.

---

## Out of Scope

The Finance domain does not own:

### Students

* applicant or student identity
* parent or guardian identity and relationship
* official financial-contact relationship
* admission decision
* enrollment execution
* administrative placement
* leave
* transfer
* withdrawal
* graduation
* official student status

Finance may consume lifecycle information and determine financial consequences. It must not execute the lifecycle transition.

### Academics

* learning structures or plans
* academic participation
* attendance
* assignments
* assessment
* grades and academic results
* academic progress
* academic standing
* academic achievement
* promotion or graduation eligibility

Financial standing must not alter academic evidence or results.

### Development

* behaviour
* discipline
* counselling
* wellbeing
* non-academic intervention
* extracurricular participation
* non-academic achievement

Development may provide permitted scholarship evidence or support context. Finance owns the financial result.

### Teaching

* teacher identity
* teacher qualification
* teacher assignment
* teacher availability
* teacher workload
* teaching history
* payroll

### Operations

* academic calendar
* school events
* announcements
* room allocation
* operational schedule
* operational approval workflow

Operations provides context. Finance owns the resulting financial action only when an approved financial rule applies.

### Other Exclusions

* general ledger
* chart of accounts
* journal entries
* accounts payable
* vendor management
* procurement
* budgeting
* payroll
* employee reimbursement
* inventory valuation
* bank reconciliation
* statutory financial statements
* tax accounting
* treasury management
* fundraising and donor management
* collection agency or legal recovery management
* payment-gateway implementation
* business intelligence or data warehouse

Finance may later provide approved information to an accounting system. It must not absorb accounting merely because some financial transactions are useful to accounting.

---

## Actors

The exact actor names and separation of duties may differ between schools.

### Student

The person whose relationship with the school gives rise to the financial obligation.

May, according to age and policy:

* view authorized financial information
* receive a bill or statement
* submit payment information
* request a payment arrangement
* request a review or refund

The student is not necessarily the payer or authorized financial decision-maker.

### Parent, Guardian, or Financial Contact

An authorized person associated with the student.

May:

* receive financial communication
* make payment
* provide scholarship or arrangement information
* request review or refund
* receive authorized financial outcomes

The Students domain owns the person's official relationship and authority context. Finance must verify the authority required for each financial action or disclosure.

### Payer

A person or organization that provides payment for a student.

The payer may be the student, parent, guardian, sponsor, employer, external organization, or another authorized party.

Making a payment does not automatically grant access to the student's complete financial or school record.

### Billing Officer

Responsible for operational billing work.

May:

* assess approved charges
* issue bills or statements
* review charge exceptions
* propose or perform authorized adjustments
* monitor billing completeness

Must not invent fee policy or approval authority.

### Cashier or Payment Officer

Responsible for receiving, confirming, and acknowledging payments according to approved procedure.

May:

* record payment
* verify required payment evidence
* confirm or reject payment
* issue receipt or acknowledgement
* initiate an authorized correction

Authority to receive payment does not automatically include authority to discount, waive, refund, or write off an obligation.

### Receivables Officer

Responsible for outstanding obligations and approved payment arrangements.

May:

* monitor due and overdue obligations
* communicate authorized reminders
* review arrangement requests
* record approved arrangements
* escalate defined exceptions

Must not cause a student-status or academic action directly.

### Scholarship or Financial Assistance Officer

Responsible for processing scholarship, discount, waiver, or financial-assistance workflows.

May:

* receive applications or recommendations
* validate required evidence
* evaluate explicit criteria
* record decisions within authority
* arrange review or renewal

Must not independently redefine academic or Development evidence.

### Finance Authority

An authorized person or committee responsible for financial decisions beyond routine processing.

May approve, according to policy:

* financial policy
* exceptions
* scholarship or waiver
* payment arrangement
* material adjustment
* refund
* write-off
* dispute determination

### School Leadership

Provides governance and approval where school policy requires it.

Leadership involvement does not remove the need to preserve the applicable policy, authority, reason, and decision history.

### External Payment Provider

An external service that may report payment initiation, success, failure, reversal, or settlement information.

It is an integration participant, not the owner of the school's student financial decision.

Finance determines how provider information becomes an accepted business payment record under approved procedure.

---

## Core Business Concepts

### Financial Policy

An approved statement governing a financial decision, obligation, benefit, due schedule, allocation, refund, clearance, or exception.

### Charge Rule

An approved rule that determines when and how a student obligation may be assessed.

### Student Financial Account

The coherent financial position and history of a student in relation to the school.

### Financial Obligation

An amount the student is required to pay under an approved policy or authorized decision.

### Bill or Statement

An authorized communication presenting obligations, payments, benefits, credits, and balances for a defined scope or date.

### Due Schedule

The approved dates and amounts by which an obligation is expected to be settled.

### Scholarship Award

An approved financial benefit applied to eligible obligations for a defined period and purpose.

### Discount

An approved reduction calculated under a financial policy.

### Waiver

An authorized release of some or all of an obligation.

### Payment

Value submitted and, when verified, received by the school for a student financial purpose.

### Payment Allocation

The application of confirmed payment or credit to a particular obligation.

### Receivable

The portion of a valid obligation that remains unsettled and owed to the school.

### Credit Balance

Confirmed value available to the student's financial account that is not currently consumed by an obligation.

### Payment Arrangement

An approved agreement changing the expected timing of settlement without erasing the obligation.

### Refund

The authorized return of refundable value to an eligible recipient.

### Financial Clearance

A purpose-specific Finance determination stating whether defined financial prerequisites have been satisfied.

### Financial Adjustment

An authorized action that changes the financial effect of an earlier record while preserving its history.

### Financial Record Correction

An auditable correction to inaccurate or incomplete financial information.

---

## Student Financial Journey

A conceptual student financial journey may be represented as:

```text
Approved financial policy or decision
                 │
                 ▼
      Applicability is evaluated
                 │
                 ▼
       Obligation is established
                 │
       ┌─────────┴─────────┐
       ▼                   ▼
Benefit may apply     Bill or statement
       │                   │
       └─────────┬─────────┘
                 ▼
          Payment submitted
                 │
                 ▼
          Payment confirmed
                 │
                 ▼
          Payment allocated
                 │
       ┌─────────┼─────────┐
       ▼         ▼         ▼
   Settled   Receivable   Credit
                 │         │
                 ▼         ▼
          Arrangement    Refund or
          or follow-up   future use

At defined points, Finance may determine a purpose-specific clearance.
Every material decision, reversal, and correction remains historical.
```

This is a conceptual journey, not a mandatory status machine.

A student may have multiple obligations, payments, benefits, arrangements, and clearances at the same time.

---

## Main Workflows

## 1. Define or Adopt Financial Policy

### Trigger

The school approves or revises a rule governing student financial obligations or decisions.

### Actor

* Finance Authority
* School Leadership, when required

### Input

* business purpose
* applicability criteria
* amount or approved determination method
* currency and rounding rule
* effective dates
* due-date or installment rule
* exception and approval authority

### Outcome

* a policy is approved, rejected, superseded, or retired
* its effective period and authority are preserved
* affected future workflows can identify the applicable version

### Business Rules

* policy must be approved before use
* changes must not silently rewrite historical obligations
* overlapping or conflicting rules require explicit resolution
* Finance must consume identity, placement, academic, Development, or calendar criteria from their owning domains
* agents must not invent amounts, categories, eligibility, or authority

---

## 2. Assess and Issue Financial Obligation

### Trigger

An event or authorized request requires Finance to evaluate whether a charge applies.

### Actor

* Billing Officer
* Finance Authority, for exceptions

### Input

* student identity reference
* relevant lifecycle, placement, participation, or operational context
* applicable financial policy
* effective period
* approved exception, when any

### Outcome

* an obligation is issued, not applicable, deferred for review, or rejected
* reason, amount, due schedule, policy, and authority are preserved

### Business Rules

* another-domain event provides context, not an automatic financial amount
* the policy effective for the relevant event must be identifiable
* duplicate obligations must be prevented or explicitly resolved
* original amount and later adjustments must remain distinguishable
* Finance must not change the source domain's information

---

## 3. Approve Scholarship, Discount, or Waiver

### Trigger

An application, nomination, recommendation, policy condition, or authorized request proposes a financial benefit.

### Actor

* Scholarship or Financial Assistance Officer
* Finance Authority
* School Leadership, when required

### Input

* student reference
* benefit type and purpose
* applicable policy and criteria
* minimum required evidence
* academic or Development recommendation, when applicable
* requested effective period and obligations

### Outcome

* benefit is approved, rejected, waitlisted, returned for information, revised, renewed, revoked, or corrected
* its effect on eligible obligations is explicit

### Business Rules

* evidence and recommendation remain owned by their source domains; Academics does not grant financial benefits; Finance does not alter academic outcomes
* Finance owns the financial eligibility decision and application
* scholarship, discount, waiver, and payment must remain distinct
* benefit must not be applied outside its approved scope or period
* adverse decisions must not be inferred from missing or unrelated information
* benefit stacking is policy-driven (Allow Stacking = Yes / No per benefit type); the system must not assume benefits are mutually exclusive, and must not assume all benefits may always be combined
* renewal, priority, and revocation require explicit policy

---

## 4. Issue Bill or Financial Statement

### Trigger

An authorized recipient requires communication of obligations or financial position.

### Actor

* Billing Officer
* Finance Officer

### Input

* authorized recipient and communication purpose
* obligations and due schedules
* applicable benefits
* payments, allocations, credits, and refunds within scope
* statement date or billing period

### Outcome

* bill or statement is issued, reissued, corrected, or delivery failure is recorded
* communicated content remains traceable to authoritative financial records

### Business Rules

* recipient authority and minimum necessary disclosure must be checked
* a communication must not create an otherwise unauthorized obligation
* statement date and current balance must not be confused
* correction must preserve the earlier communication when policy requires

---

## 5. Receive and Confirm Payment

### Trigger

A payer submits value or an external provider reports a payment outcome.

### Actor

* Payer
* Cashier or Payment Officer
* External Payment Provider

### Input

* student or intended account reference
* payer information required by policy
* amount and currency
* payment date and method
* payment reference and evidence
* external status, when applicable

### Outcome

* payment is submitted, confirmed, rejected, unidentified, duplicated, failed, or reversed
* an authorized acknowledgement or receipt may be issued

### Business Rules

* submission and confirmation are distinct
* duplicate and unidentified payments require explicit handling
* payer identity does not grant unrestricted access
* confirmation must follow the applicable payment procedure
* payment history must survive rejection, failure, or reversal when required
* payment confirmation alone does not identify allocation

---

## 6. Allocate or Reallocate Payment

### Trigger

A confirmed payment or available credit must be applied to one or more obligations, or an existing allocation is found to be incorrect.

### Actor

* Payment Officer
* Receivables Officer
* Finance Authority, for exceptions

### Input

* confirmed payment or credit
* eligible obligations
* payer instruction, when recognized by policy
* approved allocation priority
* reason and authority for reallocation

### Outcome

* value is allocated, partially allocated, left unallocated, reallocated, or returned for review
* affected obligation balances are determined

### Business Rules

* allocated value must not exceed available value
* an obligation must not receive more value than policy permits
* automatic priority requires an approved rule
* partial settlement must remain explicit
* reallocation must reverse or supersede the prior allocation auditably
* allocation must not alter the original payment facts

---

## 7. Manage Due and Overdue Receivable

### Trigger

An obligation approaches or passes its due date, or an authorized actor requests a payment arrangement.

### Actor

* Receivables Officer
* Finance Authority
* Student or authorized financial contact

### Input

* valid outstanding obligations
* due schedule
* payment and allocation position
* reminder or arrangement policy
* arrangement request and permitted evidence

### Outcome

* receivable remains due, becomes overdue, is placed under an approved arrangement, is disputed, is escalated, or is otherwise resolved under policy

### Business Rules

* overdue status must derive from valid obligations, dates, and allocations
* reminders must follow communication authority and policy
* an arrangement changes expected timing, not historical obligation; arrangement approval changes settlement timing only; it does not create a new obligation and does not reduce the original obligation amount
* arrangements require explicit Finance Authority approval; they are never auto-approved
* a missed installment moves the arrangement to Delinquent; delinquent arrangements remain valid unless explicitly cancelled by Finance action
* Finance does not directly apply administrative, academic, or operational sanctions
* penalties, write-offs, and escalation require explicit policy and authority

---

## 8. Manage Financial Dispute or Adjustment

### Trigger

A student, payer, Finance actor, or authorized source challenges or identifies an error in a financial record.

### Actor

* Student or authorized financial contact
* Billing Officer
* Payment Officer
* Finance Authority

### Input

* disputed or incorrect record
* reason and supporting information
* applicable policy
* requested outcome
* decision authority

### Outcome

* dispute is accepted, rejected, partially accepted, withdrawn, or escalated
* an authorized adjustment, reversal, or correction may be recorded

### Business Rules

* disputed does not mean invalid
* original record and decision history must remain traceable
* adjustment must identify financial effect and effective date
* payment reversal, allocation reversal, charge reversal, and refund are distinct
* appeal or review rights must come from policy

---

## 9. Manage Credit or Overpayment

### Trigger

Confirmed value remains after eligible allocations or becomes available following an authorized adjustment.

### Actor

* Payment Officer
* Receivables Officer
* Finance Authority

### Input

* source payment or adjustment
* available amount
* current eligible obligations
* credit-use and refund policy
* payer instruction, when permitted

### Outcome

* credit is retained, allocated, transferred when authorized, marked refundable, or returned for review

### Business Rules

* source and ownership of value must remain traceable
* credit must not be transferred between students without explicit authority
* retention, expiration, and refundability require policy
* available credit must not be consumed more than once

---

## 10. Request, Approve, and Execute Refund

### Trigger

An authorized party requests return of refundable value, or Finance identifies that a refund may be due.

### Actor

* Student or authorized financial contact
* Payer
* Payment Officer
* Finance Authority

### Input

* refundable payment or credit source
* requested amount
* intended recipient and destination
* reason and supporting information
* applicable refund policy

### Outcome

* refund is requested, approved, rejected, executed, failed, cancelled, reversed, or completed
* the financial account reflects the authorized outcome

### Business Rules

* refundable value and recipient authority must be verified
* approval and execution are distinct
* completed refund must not exceed available refundable value
* failure or reversal must restore the correct financial position
* charge reversal alone must not be recorded as executed refund
* deductions and destination rules must come from policy

---

## 11. Determine Financial Clearance

### Trigger

An authorized workflow requests a financial prerequisite for a defined purpose.

### Actor

* Finance Officer
* Finance Authority
* Authorized requesting domain actor

### Input

* student reference
* clearance purpose
* applicable financial policy
* obligations, benefits, payments, allocations, disputes, and arrangements relevant to that purpose
* effective date

### Outcome

* clearance is granted, denied, conditional, expired, revoked, or cannot yet be determined
* the requesting domain receives the minimum necessary result and reference

### Business Rules

* clearance must be purpose-specific
* enrollment clearance must not be reused as Transfer Out or Graduation clearance
* Phase-2 determination procedures cover Transfer Out and Graduation only
* unrelated or future obligations must not be included without policy basis
* Graduation purpose evaluates only graduation-related mandatory obligations
* pending refunds, future installments, credit balances, optional charges, and non-mandatory fees do not block clearance
* blocking items must be identifiable and explainable
* Students may execute Transfer Out or required Graduation only when the result is Granted
* Conditional is a valid recorded result and is not executable for Transfer Out or Graduation
* routine determination is recorded by Finance Officer
* exception determination requires Finance Authority and a mandatory reason
* another domain owns the action that consumes the clearance
* clearance must not change academic results or student status
* later financial changes may require reevaluation under policy
* inaccurate records are corrected through Correct Approved Finance Record; changed circumstances use a new determination

---

## 12. Correct Approved Finance Record

### Trigger

An approved Finance record is found to be inaccurate or incomplete.

### Actor

* Authorized Finance Officer
* Finance Authority

### Input

* original record
* requested correction
* reason and supporting evidence
* authorization
* affected downstream records or clearances

### Outcome

* corrected information becomes effective
* original information and correction history remain auditable
* affected balances, communications, and cross-domain outputs can be identified and reviewed

### Business Rules

* approved records must never be silently overwritten
* correction must use the appropriate financial action
* correction is distinct from policy change, dispute, refund, or changed judgment
* affected allocations, refunds, receivables, and clearances must be reevaluated

---

## Business Rules

### Obligation First

Design Finance around explicit obligations and their settlement, not around invoices, payment screens, or accounting tables.

Every amount owed must have an approved reason, policy or decision, subject, amount, currency, and effective context.

### Policy Before Calculation

Fee amounts, applicability, due dates, discounts, scholarships, penalties, allocation priority, refunds, write-offs, and clearance criteria must come from approved policy.

Agents must never infer them from common school practice.

### Payer and Student Are Distinct

The student is the subject of the financial relationship. The payer is the source of a particular payment.

Do not assume the student pays, that one payer funds every obligation, or that a payer may see all student information.

### Charge, Benefit, Payment, Allocation, and Refund Are Distinct

* a charge establishes an obligation
* a scholarship, discount, or waiver reduces eligible obligation under authority
* a payment records value received
* an allocation applies value to an obligation
* a refund returns eligible value

Do not collapse these concepts into a single balance update.

### Balance Is a Result

A balance is derived from valid obligations, benefits, allocations, adjustments, credits, and refunds for a defined scope and time.

It must not be maintained as an unexplained number that can be edited directly.

### Purpose-Specific Clearance

Financial clearance answers a defined business question under a defined policy. It is not a universal label for the student.

### Administrative Status Separation

Finance may issue payment confirmation, outstanding-obligation information, or clearance. Students owns enrollment, leave, transfer, withdrawal, graduation, and official status.

Finance must not directly execute an administrative transition.

### Academic Separation

Financial standing must not change attendance, assessment evidence, grades, learning progress, academic standing, or graduation eligibility.

Academics may consume a permitted prerequisite only for an explicitly authorized workflow.

### Scholarship Evidence and Decision Separation

Students owns identity and status evidence. Academics owns academic evidence and achievement. Development owns non-academic evidence and achievement. Finance owns the financial eligibility decision and application.

### External Provider Information Is Not Automatically Final

An external payment message is evidence for the Finance workflow. Confirmation, rejection, duplication handling, reversal, and business acceptance follow school procedure.

### Effective Dating

Policy date, event date, issue date, due date, payment date, confirmation date, allocation date, settlement date, refund date, decision date, effective date, and recording date may differ.

Their meanings must remain distinguishable.

### Currency and Rounding Are Explicit

Every amount must have a currency. Calculation and rounding rules must be approved and consistently applied.

Multi-currency conversion must not be introduced without an explicit business requirement and policy.

### Historical Integrity

Preserve original obligations, payments, decisions, allocations, adjustments, refunds, clearances, and corrections.

Do not recalculate historical outcomes under a newly approved policy unless an authorized retroactive decision explicitly requires it.

### Separation of Duties

Authority to issue a charge, receive payment, allocate value, approve a benefit, approve a refund, write off an obligation, or correct a record may be separated by policy.

Do not assume one Finance role may perform every action.

### No Automatic Adverse Decision

Do not infer suspension, exclusion, withholding of results, withdrawal, transfer, graduation denial, scholarship loss, or another adverse outcome solely from:

* an unpaid or overdue amount
* an unconfirmed payment
* a disputed obligation
* a payment arrangement
* a historical financial difficulty
* missing financial data
* a payer's behaviour

Any cross-domain consequence requires explicit policy and action by the owning domain.

### No Silent Deletion

Cancellation, reversal, correction, refund, write-off, or lawful deletion must not silently erase required financial history.

Retention and lawful deletion rules remain policy questions.

### Minimum Necessary Information

Collect and disclose only information required for the current authorized financial workflow.

Do not copy complete academic, Development, identity, or family records merely because they may be useful in a financial decision.

---

## Inputs from Other Domains

### From Students

* student identity reference
* applicant accepted
* enrollment initiated or completed
* student activated
* administrative placement
* parent, guardian, and financial-contact relationship and authority
* leave started or ended
* placement changed
* transfer
* withdrawal
* graduation

Finance references this information but does not redefine it.

Lifecycle events may trigger financial evaluation. Finance decides the financial consequence under approved policy.

### From Academics

* academic participation or completion information required by an approved financial policy
* approved academic achievement or scholarship recommendation
* correction to previously shared academic context

Finance must request only the minimum evidence required. It must not calculate grades, academic standing, progression, or graduation eligibility.

### From Development

* approved non-academic achievement or scholarship recommendation
* permitted support context for a defined financial-assistance workflow
* correction to previously shared Development context

Finance must not request unrestricted counselling, discipline, wellbeing, or case records.

### From Teaching

Normally no direct input is required for the minimum Finance scope.

If an approved student charge depends on teaching context, Finance should consume the minimum authorized business information without redefining teacher identity, assignment, availability, workload, or payroll.

### From Operations

* academic year and period
* calendar and effective-date context
* school event context
* campus context
* operational approval reference
* approved schedule context when required by a financial rule

Operations provides context. Finance does not own the calendar, event, room, or operational schedule.

---

## Outputs to Other Domains

### To Students

* enrollment payment confirmation, when policy requires it
* outstanding-obligation information required for an authorized transition
* transfer, withdrawal, or graduation financial clearance
* financial settlement or refund status required for an authorized workflow

Students owns and executes the administrative decision or transition.

### To Academics

* only an approved purpose-specific financial prerequisite or clearance result

Finance must not instruct Academics to change evidence, results, progress, standing, or eligibility.

### To Development

* scholarship or financial-assistance decision status
* minimum financial eligibility or support outcome required by an authorized Development workflow

Detailed balances, payer information, or payment history must not be shared merely for convenience.

### To Teaching

Normally no direct output is required for the minimum Finance scope.

Finance must not provide payroll instructions or redefine teacher assignment and workload.

### To Operations

* purpose-specific payment confirmation or financial clearance required for an approved event or operational workflow
* Finance action requiring authorized operational coordination

Operations performs the operational decision it owns.

### To External Accounting or Payment Systems

Finance may provide or receive approved business information through a defined integration.

Integration must preserve:

* ownership
* purpose
* authority
* identifiers
* status meaning
* timing
* correction and reconciliation responsibility

The detailed accounting and technical integration models remain outside this domain document.

---

## Domain Events

The following are conceptual business events, not mandatory technical event names.

* FinancialPolicyApproved
* FinancialPolicySuperseded
* FinancialObligationIssued
* FinancialObligationAdjusted
* FinancialObligationReversed
* BillIssued
* FinancialStatementIssued
* ScholarshipRequested
* ScholarshipApproved
* ScholarshipRejected
* ScholarshipRevised
* ScholarshipRevoked
* DiscountApplied
* WaiverApplied
* BenefitReversed
* PaymentSubmitted
* PaymentConfirmed
* PaymentRejected
* PaymentUnidentified
* PaymentReversed
* PaymentAllocated
* PaymentPartiallyAllocated
* PaymentReallocated
* AllocationReversed
* ObligationPartiallySettled
* ObligationSettled
* ObligationOverdue
* PaymentArrangementRequested
* PaymentArrangementApproved
* PaymentArrangementRevised
* FinancialDisputeRaised
* FinancialDisputeDetermined
* CreditEstablished
* CreditApplied
* RefundRequested
* RefundApproved
* RefundRejected
* RefundExecuted
* RefundFailed
* RefundReversed
* FinancialClearanceRequested
* FinancialClearanceGranted
* FinancialClearanceDenied
* FinancialClearanceConditioned
* FinancialClearanceRevoked
* FinanceRecordCorrected

Technical event design belongs in the architecture document.

---

## Ownership Matrix

| Business Concept | Owner | Other Domain Involvement |
| --- | --- | --- |
| Student Identity | Students | Referenced by Finance |
| Parent, Guardian, and Financial-Contact Relationship | Students | Finance consumes authority context |
| Student Administrative Status | Students | May trigger financial evaluation |
| Administrative Placement | Students | May be an approved charge criterion |
| Academic Participation and Completion | Academics | May provide limited charging context |
| Academic Achievement | Academics | May support scholarship recommendation |
| Non-Academic Achievement and Support Context | Development | May support financial-assistance recommendation |
| Teacher Assignment and Workload | Teaching | Not owned by Finance |
| Calendar, Period, Event, and Operational Schedule | Operations | Provides financial policy context |
| Financial Policy for Student Obligations | Finance | School authority approves according to policy |
| Student Financial Account | Finance | References student identity |
| Financial Obligation and Charge | Finance | Other domains may provide trigger context |
| Bill and Financial Statement | Finance | Delivered to authorized recipients |
| Scholarship Financial Decision | Finance | Academics or Development may provide evidence |
| Discount and Waiver | Finance | Requires approved policy and authority |
| Payment | Finance | May originate through an external provider |
| Payment Allocation | Finance | Payer instruction may be considered under policy |
| Receivable and Overdue Status | Finance | Other domains may consume minimum result |
| Credit and Overpayment | Finance | Refundability follows policy |
| Refund | Finance | Accounting or provider may execute external movement |
| Financial Clearance | Finance | Requesting domain owns resulting action |
| General Ledger and Accounting | Out of current core scope | May consume Finance information later |
| Payroll | Out of current core scope | Teaching does not own it either |

---

## Reporting Responsibility

Finance may provide operational information such as:

* obligations awaiting issue or review
* bills or statements awaiting communication
* scholarship, discount, or waiver requests awaiting action
* payments awaiting confirmation or identification
* unallocated or partially allocated payments
* due and overdue receivables
* aging by approved reporting dimension
* payment arrangements requiring review
* disputes and adjustments awaiting determination
* credit and overpayment position
* refunds awaiting approval or execution
* clearance requests awaiting determination
* corrections and reversals requiring follow-up

Reports must be produced from actual Finance workflows.

Do not create separate data-entry processes solely for dashboards.

Financial reports must respect disclosure authority. A payer, student, parent, teacher, Development actor, or school leader does not automatically have access to every financial detail.

Cross-domain dashboards may combine Finance information with Students, Academics, Development, Teaching, or Operations information, but ownership, purpose, time meaning, and access authority must remain explicit.

Management summaries from this domain are operational receivables information, not statutory accounting statements.

---

## Guiding Principles

### Student Financial Journey First

Design around the student's obligations, benefits, payments, allocations, balances, and authorized outcomes.

### Explain Every Amount

Every amount should be traceable to an approved obligation, benefit, payment, allocation, credit, adjustment, or refund.

### Separate Business Meanings

Do not replace distinct financial actions with unexplained positive and negative numbers.

### Policy Is Explicit

Financial consequences require approved policy or authority. Configuration may implement policy but must not silently become policy.

### Recommendation Is Not Execution

Other domains provide their authorized context. Finance makes its financial decision. Finance provides financial outcomes; other domains execute the decisions they own.

### Preserve Financial History

Keep historical policies, obligations, decisions, payments, allocations, adjustments, refunds, and clearances interpretable over time.

### Privacy by Purpose

Financial access depends on actor, authority, relationship, purpose, and policy. Share the minimum necessary information.

### Actor-Centric Workspaces

Show each actor what needs attention, what action is authorized, what evidence is required, and what outcome is expected.

### Accounting Boundary

Finance in the SIS manages student-school obligations. General accounting remains outside the current core domains.

### Simple Before Flexible

Implement the smallest fee, benefit, payment, allocation, receivable, refund, and clearance workflows required by the current school.

Do not create a speculative universal billing, accounting, banking, or scholarship platform.

---

## Open Questions

### MVP Scope

* Which charge types are required first?
* Are recurring charges required in the MVP?
* Are bills, statements, or both required?
* Are payment arrangements required initially?
* Is overdue follow-up in scope for the first release?
* Which financial clearances are operationally required?

### Financial Policy and Charges

* Who approves fee policies and exceptions?
* Which student, placement, period, or participation facts may determine applicability?
* When does a policy change apply prospectively or retroactively?
* How are duplicate, prorated, cancelled, or replacement charges handled?
* Are deposits part of student Finance?
* Are financial penalties permitted, and who authorizes them?

### Billing

* What is the business distinction between charge, bill, invoice, and statement in this school?
* Who receives each communication?
* Which delivery and acknowledgement rules apply?
* May obligations from multiple students or family members appear on one communication?

### Scholarship, Discount, and Waiver

* ~~Which benefit types exist?~~ **Closed.** Benefit Type is configurable by school policy. Phase-2 must not hard-code scholarship, discount, waiver, or assistance categories. Specific types remain school-policy definition.
* Who may apply, nominate, evaluate, approve, renew, revoke, or appeal?
* Which academic or Development evidence is permitted?
* ~~May benefits be combined, and in which priority?~~ **Closed.** Benefit stacking (Discounts, Waivers, Scholarships) is policy-driven. System must not assume mutual exclusivity and must not assume all benefits may always be combined. Stacking eligibility is explicit per benefit type or policy (Allow Stacking = Yes / No). Priority rules must come from school policy.
* ~~Does a scholarship reduce the student's obligation or introduce an external sponsor obligation?~~ **Closed.** Finance owns the scholarship financial decision and application. Academics or Development provides recommendation or evidence only. External sponsor obligation is outside Phase-2 core scope unless an approved workflow explicitly requires it.
* ~~Are renewal and revocation supported?~~ **Closed.** Renewal (Renewable / Non-Renewable) is supported; renewal terms come from school policy. Revocation is supported; benefits may be withdrawn per school policy. Formal appeal workflows are out of scope for Phase-2.
* How are benefit funding limits handled?

### Payment

* Which payment methods are supported?
* What evidence confirms each method?
* When is a provider-reported payment considered final?
* How are cash, transfer, cheque, failed, reversed, duplicate, and unidentified payments handled?
* Who may issue or cancel a receipt?
* Are partial payments permitted?

### Payment Allocation

* Does the payer direct allocation?
* Is there an automatic allocation priority?
* May value be allocated to future obligations?
* How are partial allocations and reallocation approvals handled?
* May scholarship, discount, credit, and payment follow different allocation rules?

### Receivables and Payment Arrangements

* When does an obligation become overdue?
* ~~Which reminder stages and communication rules apply?~~ **Closed.** Reminder timing is configurable school policy; hard-coded day counts are not part of the domain contract. Phase-2 supports: Reminder Stage, Reminder Timing, and Escalation Stage without hard-coded dates. Conceptual model: Reminder Before Due Date → Reminder Shortly After Due Date → Escalated Overdue Reminder. Specific day counts and communication rules remain school-policy configuration.
* ~~Who approves arrangements and revised due dates?~~ **Closed.** Payment Arrangements require explicit Finance approval: Requester → Finance Officer → Finance Authority → Approved Arrangement. Never auto-approved. School leadership only when explicitly required by policy.
* ~~What types of arrangements are supported?~~ **Closed.** Phase-2 supports a single arrangement concept: Payment Arrangement with Installment Schedule. Multiple arrangement categories (deferred payment, restructuring, hardship plan, payment holiday) are not introduced. Eligibility requires: Outstanding Mandatory Obligation Exists and Finance Approval.
* ~~What happens when an arrangement is missed?~~ **Closed.** A missed installment moves the arrangement to Delinquent status. Delinquent arrangements remain valid unless explicitly cancelled by Finance action. Cancellation requires an explicit Finance decision. Arrangement states: Active, Delinquent, Completed, Cancelled. Historical installment obligations and arrangement history are preserved.
* Are penalties or write-offs permitted?
* ~~Which cross-domain consequences, if any, are explicitly authorized?~~ **Closed (boundary).** Overdue status is financial information only. Finance does not automatically suspend students, block participation, withhold academic results, prevent promotion, prevent graduation, or execute withdrawal. Any cross-domain consequence requires explicit action by the owning domain under its own policy.

### Credit and Refund

* ~~Who owns refundable value when the payer differs from the student or financial contact?~~ **Closed.** Refundable value belongs to the original payer by default. Student identity alone does not determine refund ownership; Financial Contact identity alone does not determine refund ownership. Exceptions require explicit Finance approval and documented justification. Example: Payer = Company C → refund defaults to Company C regardless of Financial Contact = Parent B.
* ~~May credit be retained for future obligations?~~ **Closed.** Yes — credit may be retained for future eligible obligation allocation. Transfer between students requires explicit authority.
* May credit transfer between siblings or students?
* Which deductions, limits, approvals, and destination rules apply?
* ~~When is a refund approved versus completed?~~ **Closed.** Approval and execution are always distinct. Refunds require explicit Finance Authority approval (Finance Officer reviews → Finance Authority approves → Refund Executed). A refund is not complete merely because it was approved.
* How are failed or reversed refunds handled?

### Financial Clearance

* ~~Which workflows require clearance?~~ **Closed.** Financial Clearance is mandatory for Transfer Out and Graduation. The following remain configurable school policy: Withdrawal, Re-enrollment, and other administrative processes. Additional clearance purposes may be configured without changing the domain model.
* ~~What obligations are relevant to each purpose?~~ **Closed (principle).** Financial Clearance is blocked only by outstanding mandatory obligations. The following do not block clearance: pending refunds, future installments, credit balances, optional charges, non-mandatory fees. Schools may configure which obligation categories are mandatory. Blocking obligations must be identifiable and explainable.
* ~~Are disputed obligations or payment arrangements blocking?~~ **Closed.** Disputed obligations and active payment arrangements (that are not in Cancelled state and cover outstanding mandatory obligations) follow the mandatory-obligation principle above. Specific configuration is school-policy.
* ~~Can clearance be conditional, expire, or be revoked?~~ **Closed.** Permitted results include Granted, Denied, Conditional, Cannot Yet Be Determined, Expired, and Revoked. Transfer Out execution and required Graduation execution consume Granted only. Conditional is not executable.
* ~~Who may grant an exception?~~ **Closed.** Finance Officer records the routine determination. Finance Authority records exception determinations, and the exception reason is mandatory.
* ~~What minimum result may be shared with the requesting domain?~~ **Closed.** The requesting domain receives purpose, subject reference, result, determination date, reason, identified blockers when the result is not Granted, conditions when Conditional, and the clearance result reference. Finance does not share unrelated financial internals.

### Accounting and External Integration

* What information must be exported to an accounting system?
* Which system owns reconciliation and accounting classification?
* How are corrections synchronized without transferring ownership?
* Which payment providers are used?
* What is the source of truth when an external status conflicts with Finance records?

### Currency and Organization

* Is only one currency supported?
* Which precision and rounding rules apply?
* Are campuses financially independent or only reporting dimensions?
* May one student hold obligations across campuses or school entities?
* Are family-level billing or sponsor accounts required?

### Record Lifecycle and Access

* Which actors may view each part of the financial account?
* What may a payer see when the payer is not a guardian?
* Which records may be corrected, restricted, anonymized, or lawfully deleted?
* What retention periods apply?
* How are late payments, refunds, disputes, and corrections handled after withdrawal or graduation?

---

## Implementation Guardrails

Before implementing a Finance feature:

1. Identify the real financial workflow and actor.
2. Identify the obligation, benefit, payment, allocation, credit, refund, or clearance outcome.
3. Identify the approved policy version and decision authority.
4. Identify the student and distinguish the payer and authorized financial contact.
5. Identify the amount, currency, relevant dates, and rounding rule.
6. Identify information owned by Students, Academics, Development, Teaching, or Operations.
7. Identify what Finance owns and the minimum output another domain may consume.
8. Preserve original records, decisions, reversals, allocations, and corrections.
9. Determine whether purpose-specific access, approval, or separation of duties is required.
10. Implement the minimum workflow.

Do not begin from:

* database tables
* a balance column
* CRUD screens
* REST endpoints
* an invoice template
* a payment gateway payload
* accounting journal structures
* a dashboard requirement
* a generic billing product

Begin from the authorized student financial workflow.

Do not invent:

* fee types or amounts
* charge applicability
* due dates or installment rules
* currency conversion or rounding rules
* scholarship eligibility or award formula
* discount, waiver, or benefit priority
* payment-confirmation criteria
* payment allocation priority
* overdue penalties or reminder stages
* payment-arrangement eligibility
* write-off authority
* refund eligibility or recipient
* financial-clearance criteria
* cross-domain sanctions or consequences
* retention periods or access rights
* accounting classifications

When policy or workflow is unclear, record it as an open question and stop before encoding the assumption as a business rule.

For every proposed concept, ask:

```text
Does this describe a financial obligation between a student and the school,
an approved financial benefit, payment, allocation, receivable, credit,
refund, adjustment, or purpose-specific financial clearance?

If not, which domain owns it?

What approved policy or decision explains the amount and its effect?

Is the person the student, financial contact, payer, or decision authority?

Is this a charge, benefit, payment, allocation, credit, refund, or correction?

Which date and currency meaning applies?

Is the information required by a real workflow now?
```

---

## Summary

The Finance domain answers these primary business questions:

```text
What does the student owe the school, and why?

When is the obligation due?

Which approved scholarship, discount, or waiver applies?

What payment was submitted and confirmed, and by whom?

How was payment or credit allocated?

What amount remains receivable or available as credit?

Is a refund due, approved, and completed?

Has the student satisfied a defined financial prerequisite?

What decision, adjustment, or correction produced the current position?
```

It does not answer:

```text
Who is the student or what is the student's official status?

What is the student learning or what academic result was achieved?

What behaviour, wellbeing, counselling, or development outcome applies?

Who is assigned or available to teach?

Which calendar, event, room, or operational schedule applies?

How should the school maintain its general ledger or statutory accounts?
```

Those questions belong to Students, Academics, Development, Teaching, Operations, or a future accounting domain respectively.
