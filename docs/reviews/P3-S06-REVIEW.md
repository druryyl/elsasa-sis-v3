---
Title: Review — P3-S06 Finance Domain Transactions, Obligations & Live Cashier Demo Seeder
Code: CR-SYS-SAMPLE-DATA-GENERATOR-P3-S06
Artifact: REVIEW
Version: 1.0
LastUpdated: 2026-11-07
Status: NO-GO
ReviewIteration: 0
---

# Review Summary

**Slice:** P3-S06 — Finance Domain Transactions, Obligations & Live Cashier Demo Seeder  
**Decision:** NO-GO  
**Review Date:** 2026-11-07  
**Reviewer:** Independent Review (ics-review skill)  

---

## Scope Verification

| Criterion | Status |
|---|---|
| Implements `IFinanceTransactionsDemoDataSeeder` | ✅ PASS |
| Consumes `OperationsSeedResult`, `FinancePolicySeedResult`, `StudentsSeedResult` | ✅ PASS |
| Creates financial accounts (`finance.student_financial_accounts`) for ~48 enrolled students | ✅ PASS |
| Creates financial obligations and due schedules (`financial_obligations`, `obligation_due_schedules`) | ❌ **FAIL** (BLOCKER-001, MAJOR-002) |
| Semester 1 (historical): 100% billed and 100% paid in full | ❌ **FAIL** (BLOCKER-001) |
| Semester 2: 60% paid, 20% partial, 10% overdue, 10% unpaid | ✅ PASS (Distribution logic present) |
| Jonathan Pratama live cashier demo invoice (`INV-SEM2-0042`, balance due IDR 1,500,000, `status = 'unpaid'`) | ❌ **FAIL** (MAJOR-001) |
| Grace Santoso overdue SPP receivable | ⚠️ PARTIAL (Bundled in lump invoice) |
| Pastoral scholarship fee waiver record | ✅ PASS |
| Returns `FinanceTransactionsSeedResult` with account, invoice, payment counts, and `JonathanUnpaidInvoiceId` | ✅ PASS |
| Service registered in `Elsasa.Sis.Finance/DependencyInjection.cs` | ✅ PASS |
| Code compiles cleanly with zero nullable warnings | ✅ PASS |

---

## Findings

### BLOCKER-001: Runtime ArgumentOutOfRangeException in DateOnly Month Calculation

**Severity:** BLOCKER  
**Description:** `SeedFinanceTransactionsAsync` crashes unconditionally on execution due to calculating `6 + month` where `month` loops from `1` to `12`. For `month >= 7`, `6 + month >= 13`, which causes `new DateOnly(2026, 6 + month, 10)` to throw `System.ArgumentOutOfRangeException: Year, Month, and Day parameters describe an un-representable DateTime.`

**Evidence:**
`src/backend/Elsasa.Sis.Finance/Features/DemoData/FinanceTransactionsDemoDataSeeder.cs` lines 253-255:
```csharp
// ── Semester 1 obligations (historical, fully settled) ──
for (int month = 1; month <= 12; month++)
{
    var dueDate = new DateOnly(2026, 6 + month, 10); // Jul–Dec 2026
    var oblId = Guid.NewGuid();
    ...
```
July through December is 6 months (`month` from 1 to 6). Running with `month <= 12` attempts to construct `DateOnly(2026, 13, 10)`, `DateOnly(2026, 14, 10)`, etc., immediately aborting the database transaction during seeder execution.

**Required Correction:**
Correct the loop condition for Semester 1 SPP obligations to `month <= 6` (or iterate months properly from July to December) so that `DateOnly` values are valid calendar dates.

---

### MAJOR-001: Jonathan Pratama Unpaid Invoice Balance Due Mismatch

**Severity:** MAJOR  
**Description:** The completion criteria and Architecture `TD-06` explicitly mandate that Jonathan Pratama's live demonstration invoice (`INV-SEM2-0042`) have a balance due of exactly IDR 1,500,000 in `unpaid` status for the Cashier workbench live demo hook (`SCR-FIN-02`). In the implementation, `INV-SEM2-0042` sums all Semester 2 obligations, yielding an invoice amount of IDR 18,250,000.

**Evidence:**
`src/backend/Elsasa.Sis.Finance/Features/DemoData/FinanceTransactionsDemoDataSeeder.cs` lines 606-614:
```csharp
foreach (var (sid, s2Obss) in s2ObligationsByStudent.OrderBy(kv => kv.Key))
{
    var accountId = accountById[sid];
    var isJonathan = sid == jonathanId;
    var docId = isJonathan ? jonathanUnpaidInvoiceId : Guid.NewGuid();
    var docNumber = isJonathan ? "INV-SEM2-0042" : $"INV-SEM2-{s2InvoiceSeq:D4}";
    s2InvoiceSeq++;
    invoiceCount++;
    var totalBilled = s2Obss.Sum(o => o.Amount);
```
Jonathan's S2 obligations include:
- 5 months of SPP @ IDR 1,500,000 = IDR 7,500,000
- Facility fee = IDR 10,000,000
- PACE material fee = IDR 750,000
Total billed amount: IDR 18,250,000. Because no payment is applied to Jonathan, the balance due is IDR 18,250,000 instead of IDR 1,500,000.

**Required Correction:**
Ensure Jonathan's `INV-SEM2-0042` invoice billed amount and outstanding balance due is IDR 1,500,000 (e.g., billing a single SPP installment for Jonathan's live demo invoice, or adjusting the line items so the invoice matches the acceptance criteria).

---

### MAJOR-002: Duplicate Annual Facility Fee (*Uang Gedung*) Billed in Semester 2

**Severity:** MAJOR  
**Description:** Annual Facility Fee (*Uang Gedung Tahunan*) is an annual charge per academic year. The seeder assesses the full IDR 10,000,000 facility fee in Semester 1 (`historicalPeriodId`, lines 277-298) and assesses another IDR 10,000,000 in Semester 2 (`activePeriodId`, lines 346-367).

**Evidence:**
- Line 280: `allObligations.Add((facilityOblS1, sid, accountId, "Uang Gedung Tahunan - TP 2026/2027", facilityAnnualAmount, facilityDueDateS1, historicalPeriodId, sem1Label, "settled"));`
- Line 349: `allObligations.Add((facilityOblS2, sid, accountId, "Uang Gedung Tahunan - TP 2026/2027", facilityAnnualAmount, facilityDueDateS2, activePeriodId, sem2Label, "assessed"));`

**Required Correction:**
Do not re-assess the annual facility fee in Semester 2 if it was already assessed and settled in Semester 1 for the same academic year (TP 2026/2027).

---

### MINOR-001: Disregard of Dynamic Date Anchoring from OperationsSeedResult

**Severity:** MINOR  
**Description:** Per `ADR-002` and `TD-03`, temporal milestones should dynamically anchor around `operationsContext.BaseDate` (`Today = DateTime.UtcNow.Date`). `FinanceTransactionsDemoDataSeeder.cs` hardcodes static dates (`new DateOnly(2026, 7, 1)`, `new DateOnly(2027, 1, 1)`, `new DateOnly(2027, 5, 15)`) instead of deriving them dynamically from `operationsContext.BaseDate` or operational periods.

**Required Correction:**
Optionally calculate dates relative to `operationsContext.BaseDate` or operational period start/end dates so the timeline remains consistent if `BaseDate` shifts.

---

## Resolution Status

| Finding ID | Severity | Status |
|---|---|---|
| BLOCKER-001 | BLOCKER | Open |
| MAJOR-001 | MAJOR | Open |
| MAJOR-002 | MAJOR | Open |
| MINOR-001 | MINOR | Open |

---

## Re-Review History

None (initial review, ReviewIteration 0).
