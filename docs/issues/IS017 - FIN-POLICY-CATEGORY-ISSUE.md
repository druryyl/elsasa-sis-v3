---
Title: Financial Policy with non-matching category value does not appear under any Policy Category filter
Code: FIN-POLICY-CATEGORY-FILTER
Artifact: ISSUE
Version: 1.0
LastUpdated: 2026-11-11
Status: OPEN
Execution Approval: NOT-REQUIRED
---

# ISSUE

## Metadata

ID: IS017
Type: BUG
Status: OPEN
Title: Financial Policy with non-matching category value does not appear under any Policy Category filter

## Source

Reported By: Finance user (UI observation)
Reported Date: 2026-11-11

## Description

In the Financial Policy Workspace (`/finance/policies`), the "Policy Category" filter dropdown only offers a fixed set of values:
`tuition`, `admission`, `facilities`, `activities`, `general`.

Sample/demo data contains a financial policy `FP-2026-002: Hagios Institutional Fee Policy 2026/2027` whose stored `category` value is `GENERAL_FEE` (per the domain specification canonical values: `tuition`, `admission_fee`, `scholarship_guideline`, `refund_rule`, `clearance_rule`, `general_fee`).

Because the filter performs an exact, case-sensitive match against the hardcoded list, this policy does not match any of the offered category values and therefore does not appear under any specific category filter. It is only visible under "All Categories" (where no category filter is applied).

There is no master/reference data (table or lookup endpoint) for policy categories. The `category` column is free text, validated only by maximum length (64 characters) in both the database schema and the backend validator.

## Desired Outcome

A financial policy should be discoverable/filterable by its category. Policies stored with domain-canonical category values (e.g., `general_fee`) should appear when the corresponding category filter is selected. Category handling should be consistent between the data, the domain specification, and the user interface.

## Current Situation

- Sample data policy `FP-2026-002` (category `GENERAL_FEE` / `general_fee`) is visible only under "All Categories".
- Selecting any specific category filter (`Tuition`, `Admission`, `Facilities`, `Activities`, `General`) hides the policy.
- No master/reference data exists for policy categories; the frontend hardcodes its own category list, which diverges from the domain specification's canonical values.

## Evidence

- Frontend filter options: `src/frontend/src/features/finance/policies/views/FinancialPolicyWorkspaceView.vue` (`categoryFilterOptions`): `tuition`, `admission`, `facilities`, `activities`, `general`.
- Frontend creation categories: `src/frontend/src/features/finance/policies/schemas/policies.schema.ts` (`POLICY_CATEGORIES`): same set.
- Frontend type: `src/frontend/src/features/finance/policies/types/policies.types.ts` (`PolicyCategory`): same set.
- Domain specification: `docs/entities/finance-entities-tables.md` (`financial_policies.category`): `tuition`, `admission_fee`, `scholarship_guideline`, `refund_rule`, `clearance_rule`, `general_fee`.
- Backend filter: `src/backend/Elsasa.Sis.Finance/Features/Policies/GetFinancialPolicies/GetFinancialPoliciesHandler.cs` uses exact match `p.category = @Category` (case-sensitive).
- Backend validator: `src/backend/Elsasa.Sis.Finance/Features/Policies/CreateFinancialPolicy/CreateFinancialPolicyValidator.cs` validates only `NotEmpty` + `MaximumLength(64)`; no enumerated constraint.
- Database: no `policy_categories` (or equivalent) reference/master table in `src/backend/Elsasa.Sis.Database/Scripts`.

## Notes

This ISSUE is solution-neutral. Root-cause analysis, architecture, implementation, planning, and testing strategy are deferred to downstream artifacts (BUG-INVESTIGATION, then ARCHITECTURE UPDATE and PLANNING). Testing is one possible source of BUG issues; this ISSUE originates from a user/UI observation.