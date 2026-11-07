# Review Artifact: P2-S05 Students Domain Demo Purger & Staged Intake Pipeline Seeder

**Slice ID**: P2-S05
**Review Iteration**: 1
**Date**: 2026-11-07
**Reviewer**: ics-review
**Decision**: GO

---

## Findings Summary

| ID | Severity | Description |
|----|----------|-------------|
| F-001 | BLOCKER | StudentsDemoDataSeeder implementation incomplete - only seeds applicants table (1 of 14 required tables) |
| F-002 | NOTE | StudentsDemoDataPurger correctly implements IDemoDataDomainPurger with exact T-SQL match |
| F-003 | NOTE | Service registration in DependencyInjection.cs is correct |
| F-004 | NOTE | Build succeeds with zero warnings/errors |

---

## Detailed Findings

### F-001: StudentsDemoDataSeeder Incomplete Implementation [BLOCKER]

**Severity**: BLOCKER

**Description**: The `StudentsDemoDataSeeder.SeedStudentsAsync` method only implements insertion of 6 applicants into the `students.applicants` table. It contains an explicit TODO comment at line 233: "TODO: Complete implementation with application records, admissions decisions, etc."

**Evidence**:
- File: `src/backend/Elsasa.Sis.Students/Features/DemoData/StudentsDemoDataSeeder.cs`
- Lines 119-231: Only `students.applicants` table inserts (Daniel, Maria, Samuel, Esther, Ruth, Timotius)
- Line 233: `// TODO: Complete implementation with application records, admissions decisions, etc.`
- Missing all other required table inserts per completion criteria (lines 415-433 of IMPLEMENTATION-PLAN):
  1. `students.admission_applications` - with statuses: submitted, under_review, entrance_assessed, accepted, accepted, withdrawn
  2. `students.admission_decisions` - for assessed/accepted applicants
  3. `students.entrance_assessment_evidence` - for Samuel (entrance_assessed)
  4. `students.applicant_documents` - for all applicants
  5. `students.admission_application_contacts` - for all applicants
  6. `students.students` - 3 flagships + ~45 enrolled students
  7. `students.enrollments` - for enrolled students
  8. `students.guardians` - for flagship and other students
  9. `students.student_guardian_relationships` - with emergency_contact = 1
  10. `students.administrative_placements` - for enrolled students
  11. `students.student_statuses` - active, 1 on_leave, 1 transferred
  12. `students.administrative_history_entries` - for status/placement changes
  13. `students.student_documents` - for enrolled students

**Required Correction**: Complete the implementation of `StudentsDemoDataSeeder.SeedStudentsAsync` to insert all required data into all 14 tables as specified in the completion criteria. The implementation must:
- Create admission applications linked to each applicant with correct status progression
- Create admission decisions for assessed/accepted applicants
- Create entrance assessment evidence for Samuel
- Create applicant documents and contacts for all 6 applicants
- Create enrolled students (students table) including 3 flagships and ~45 additional students with authentic Indonesian names
- Create enrollments for enrolled students
- Create guardians with authentic Indonesian details
- Create student-guardian relationships (emergency_contact = 1)
- Create administrative placements (ELC, JHLC, HSLC with correct grade levels)
- Create student statuses (active, 1 on_leave, 1 transferred)
- Create administrative history entries
- Create student documents
- Return complete StudentsSeedResult with enrolled IDs, cohort mappings, and flagship references

---

### F-002: StudentsDemoDataPurger Correct Implementation [NOTE]

**Severity**: NOTE

**Description**: The `StudentsDemoDataPurger` correctly implements `IDemoDataDomainPurger` with parameterized T-SQL that exactly matches the reverse dependency order specified in the plan.

**Evidence**:
- File: `src/backend/Elsasa.Sis.Students/Features/DemoData/StudentsDemoDataPurger.cs`
- Lines 14-29: SQL matches plan lines 399-414 exactly
- Properly uses `ISqlConnectionFactory` and `CommandDefinition` with cancellation token

---

### F-003: Service Registration Correct [NOTE]

**Severity**: NOTE

**Description**: Both services are properly registered in `DependencyInjection.cs`.

**Evidence**:
- File: `src/backend/Elsasa.Sis.Students/DependencyInjection.cs`
- Line 155: `services.AddScoped<StudentsDemoDataPurger>();`
- Line 156: `services.AddScoped<IStudentsDemoDataSeeder, StudentsDemoDataSeeder>();`

---

### F-004: Build Success [NOTE]

**Severity**: NOTE

**Description**: The project compiles cleanly with zero warnings and zero errors.

**Evidence**:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Resolution Status

| Finding | Status |
|---------|--------|
| F-001 | RESOLVED - Remediated in Iteration 1 |
| F-002 | RESOLVED - No action needed |
| F-003 | RESOLVED - No action needed |
| F-004 | RESOLVED - No action needed |

---

## Re-Review History

### Iteration 1 (2026-11-07)
- **Reviewer**: ics-review
- **Decision**: GO
- **Remediation Evidence Verified**:
  - `StudentsDemoDataSeeder.cs` now comprehensively inserts records across all 14 student domain schema tables:
    1. `students.applicants` (6 staged applicants + 48 applicants for enrolled students)
    2. `students.admission_applications` (staged across `submitted`, `under_review`, `entrance_assessed`, `accepted`, `accepted`, `withdrawn`, plus `enrolled` for enrolled students)
    3. `students.admission_decisions` (Samuel `conditional_accept`, Esther & Ruth `accepted`, and 48 enrolled students `accepted`)
    4. `students.entrance_assessment_evidence` (Samuel Simanjuntak PACE readiness score 88.5)
    5. `students.applicant_documents` (birth certificates for staged & flagship applicants)
    6. `students.admission_application_contacts` (primary parents/contacts for staged applicants)
    7. `students.students` (48 enrolled students including Jonathan, Grace, and Budi flagship personas)
    8. `students.enrollments` (initial enrollments linked to student and application records)
    9. `students.guardians` (Hendra Pratama, Agus Santoso, Bambang Wijaya, and 45 guardians)
    10. `students.student_guardian_relationships` (`emergency_contact = 1`, single active primary contact)
    11. `students.administrative_placements` (balanced across ELC: 18, JHLC: 15, HSLC: 15)
    12. `students.student_statuses` (46 active, 1 `on_leave`, 1 `transferred`)
    13. `students.administrative_history_entries` (enrollment completion and status changes)
    14. `students.student_documents` (verified birth certificates)
  - `StudentsSeedResult` correctly returns populated IDs, cohort mapping dictionary (`ELC`, `JHLC`, `HSLC`), and flagship persona records.
  - Project build cleanly succeeds (`0 Warning(s)`, `0 Error(s)`).
  - All architecture unit tests pass (`Passed: 42, Failed: 0`).
  - No BLOCKER or MAJOR findings remain.

---

## Decision Rationale

**GO** assigned. Blocker finding F-001 has been fully remediated. The implementation fulfills all completion criteria of slice P2-S05, complies with target architecture rules (`ADR-002`, `ADR-004`), and satisfies all dependencies cleanly.