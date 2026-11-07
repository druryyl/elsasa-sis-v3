---
Title: Review — P3-S07 Academics Domain Participations, Attendance & Assessment Activities Seeder
Code: CR-SYS-SAMPLE-DATA-GENERATOR-P3-S07
Artifact: REVIEW
Version: 1.0
LastUpdated: 2026-11-06
Status: NO-GO
ReviewIteration: 0
---

# Review Summary

**Slice:** P3-S07 — Academics Domain Participations, Attendance & Assessment Activities Seeder  
**Decision:** NO-GO  
**Review Date:** 2026-11-06  
**Reviewer:** Independent Review (ics-review skill)

---

## Scope Verification

| Criterion | Status |
|---|---|
| Implements `IAcademicsActivitiesDemoDataSeeder` | ✅ PASS |
| Consumes `OperationsSeedResult`, `AcademicsStructureSeedResult`, `StudentsSeedResult` | ✅ PASS |
| Creates cohort participations for all ~48 enrolled students | ✅ PASS |
| Creates ~1,200 attendance records | ✅ PASS |
| Attendance distribution: 95% Present, 3% Excused, 1% Late, 1% Unexcused | ❌ **FAIL** (BLOCKER) |
| Jonathan Pratama: 100% attendance (high honors) | ❌ **FAIL** (BLOCKER) |
| Coursework assignments, submissions, assessment evidence | ✅ PASS |
| Semester 1 certified report cards with frozen JSON snapshots | ✅ PASS |
| Semester 1 cumulative transcripts | ✅ PASS |
| Semester 2: Jonathan provisional score awaiting approval | ✅ PASS |
| Semester 2: Grace retake (68% → 85%, `replaces_result_id`, `attempt_number=2`) | ✅ PASS |
| Semester 2: Budi submitted PACE test awaiting evaluation | ✅ PASS |
| Grace uses `DemoDataConstants.GraceStudentId` | ✅ PASS |
| Jonathan uses `DemoDataConstants.JonathanStudentId` | ✅ PASS |
| Budi uses `DemoDataConstants.BudiStudentId` | ✅ PASS |
| Service registered in `DependencyInjection.cs` | ✅ PASS |
| Code compiles cleanly with zero nullable warnings | ✅ PASS |

---

## Findings

### BLOCKER-1: Attendance Distribution Mismatch

**Severity:** BLOCKER  
**Description:** The completion criteria explicitly requires attendance distribution of "~95% Present, 3% Excused, 1% Late, 1% Unexcused" (plan line 511). The implementation uses 80% Present, 10% Absent, 5% Late, 5% Excused_absence.

**Evidence:** `AcademicsActivitiesDemoDataSeeder.cs` lines 959-965:
```csharp
var status = random.Next(100) switch {
    < 80 => "present",      // 80% - should be 95%
    < 90 => "absent",       // 10% - not "unexcused" per criteria  
    < 95 => "late",         // 5% - should be 1%
    _ => "excused_absence"  // 5% - should be 3%
};
```

**Required Correction:** Update the probability distribution to match criteria:
- `< 95 => "present"` (95%)
- `< 98 => "excused_absence"` (3%) 
- `< 99 => "late"` (1%)
- `else => "absent"` (1% — "Unexcused" equivalent per schema)

---

### BLOCKER-2: Jonathan's 100% Attendance Not Implemented

**Severity:** BLOCKER  
**Description:** The completion criteria explicitly states "Jonathan Pratama: 100% attendance (high honors)" (plan line 512). The `CreateAttendanceRecordsAsync` method applies the same random distribution to all students including Jonathan with no special handling.

**Evidence:** Lines 955-982 — no check for `studentId == jonathanId` to force "present" status.

**Required Correction:** Add special case in `CreateAttendanceRecordsAsync` or create a separate attendance creation path for Jonathan that forces "present" status for all his attendance records.

---

### MINOR-1: Report Card Snapshot Missing Required Fields

**Severity:** MINOR  
**Description:** The `ReportCardSubjectEntryDto` contract requires `SubjectCode`, `SubjectTitle`, and `GradeLabel` fields, but the frozen JSON snapshot only includes `learningUnitId`, `academicResultId`, `scoreNumeric`, `gradeLetter`, `teacherComment`.

**Evidence:** `ReportCardSubjectEntryDto` (contract) lines 14-22 vs seeder lines 734-741.

**Required Correction:** Include `SubjectCode`, `SubjectTitle`, `GradeLabel` in the report card entry JSON snapshot.

---

### MINOR-2: Transcript Snapshot Doesn't Match Contract Schema

**Severity:** MINOR  
**Description:** The transcript JSON snapshot doesn't match `TranscriptSnapshotDto` which requires `TranscriptId`, `StudentName`, `StudentNisn`, `TranscriptType`, `PeriodStart`, `PeriodEnd`, `Status`, `Purpose`, `CompletedPeriods` with `TranscriptPeriodSummaryDto` structure.

**Evidence:** `TranscriptSnapshotDto` lines 16-26 vs seeder lines 816-824.

**Required Correction:** Restructure transcript snapshot to match `TranscriptSnapshotDto` and `TranscriptPeriodSummaryDto` / `TranscriptCourseEntryDto` contracts.

---

### NOTE-1: Attendance Status Terminology

**Severity:** NOTE  
**Description:** The plan mentions "Unexcused" but the database schema uses "absent" and the implementation uses "absent". This is a terminology inconsistency but not a functional issue.

---

## Remediation Required

The implementation must be corrected for the two BLOCKER findings before this slice can be accepted. The MINOR findings should also be addressed for contract compliance but do not block acceptance.

**Stop Rule:** If remediation requires changing the approved slice structure or scope, stop and escalate for a new Planning cycle.

---

## Next Steps

1. Developer to correct BLOCKER-1 (attendance distribution)
2. Developer to correct BLOCKER-2 (Jonathan 100% attendance)
3. Developer to address MINOR-1 and MINOR-2 (snapshot contract compliance)
4. Re-run build verification
5. Request re-review (ReviewIteration 1)