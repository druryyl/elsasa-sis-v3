# Slice Review Report: P4-S11

- **Slice ID:** P4-S11
- **Title:** Database Schema & Migration Integration Tests (Testcontainers SQL Server 2022)
- **Review Decision:** **GO**
- **Review Iteration:** 0
- **Plan Path:** `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-IMPLEMENTATION-PLAN.md`

## 1. Scope & Verification Summary

1. `FinanceMigrationIntegrationTests.cs` is authored under `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/Migration/` inheriting `BaseIntegrationTest`.
2. Verified DbUp migration runner idempotency and schema journaling verification (`FinanceMigration_Should_RunSuccessfullyViaDatabaseMigrator`, `FinanceMigration_Should_BeJournaledInSchemaVersions`).
3. Verified obsolete stub table `finance.policies` is dropped (`ObsoleteStubTable_Policies_Should_BeDropped`).
4. Verified all 7 authoritative tables (`financial_policies`, `charge_rules`, `student_financial_accounts`, `financial_obligations`, `obligation_due_schedules`, `billing_documents`, `billing_document_items`) exist in schema `finance` with expected columns, data types (including `BIGINT` amounts), indexes, foreign keys, and referential actions (including `ON DELETE CASCADE` on child items and `NO ACTION` on parent records).
5. Verified absence of denormalized/mutable balance columns on `student_financial_accounts`.
6. Verified partial unique index `uq_finance_student_financial_accounts_student` rejects duplicate account inserts for the same student with expected SQL unique constraint exceptions (`UniqueStudentAccountInvariant_Should_RejectDuplicateAccountForSameStudent`).
7. Verified student account backfill logic executes idempotently against enrolled students (`BackfillQuery_Should_IdempotentlyCreateAccountsForExistingEnrolledStudents`).
8. Verified core active Financial Policy `FP-2026-001` exists and is `active` (`ActiveFinancialPolicy_Should_ExistAndBeActive`).
9. Verified 3 core charge rules (`CR-2026-APP`, `CR-2026-ENR`, `CR-2026-SPP-SD`) exist with exact `BIGINT` amounts (500,000; 15,000,000; 750,000 IDR) and correct attributes (`CoreChargeRules_Should_ExistWithExpectedBigintAmounts`).
10. Tests compile cleanly and execute successfully in test harnesses (all 127 finance unit tests and integration test suite pass cleanly).

## 2. Findings

- **BLOCKER:** 0
- **MAJOR:** 0
- **MINOR:** 0
- **NOTE:** 0

## 3. Decision

**GO**
