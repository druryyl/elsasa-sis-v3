---
Title: Feasibility Assessment - Financial Policy Category Master Reference Table and Dynamic Lookup
Code: FIN-POLICY-CATEGORY
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-11
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Feature being assessed: Transitioning Financial Policy Category from hardcoded frontend/backend strings to an authoritative master reference table (`finance.policy_categories`), an authenticated lookup endpoint (`GET /api/finance/policy-categories`), database referential integrity, and dynamic frontend consumption across workspace filters and creation modals, resolving issue `IS017` (`docs/issues/IS017 - FIN-POLICY-CATEGORY-ISSUE.md`).

Referenced artifacts:
- **ISSUE:** `docs/issues/IS017 - FIN-POLICY-CATEGORY-ISSUE.md`
- **DOMAIN:** `docs/domains/finance-domain.md` (Financial Policy and Charge Rules, lines 58–91)
- **DATA SPECIFICATIONS:** `docs/entities/finance-entities-tables.md` (Submodule 1, Table 1.1 `financial_policies.category`, lines 36–58)
- **WORKFLOWS & USE CASES:** `docs/operational-workflow-discovery.md` (`FIN-WF-01: Define or Adopt Financial Policy`), `docs/use-cases.md` (`UC-FIN-10: Establish Financial Policy`), `docs/screen-inventory.md` (`SCR-FIN-04: Financial Policy Workspace`)
- **ARCHITECTURE CONSTRAINTS:** `docs/architecture/adr.md` (ADR-001 Modular Monolith, ADR-002 Database Schema Isolation, ADR-004 CQRS Query Slices, ADR-005 Vertical Slice Architecture), `docs/architecture/frontend-architecture.md` (Section 8.1 TanStack Query Caching Tiers)

## Objective

Assess the technical, architectural, and operational feasibility, database schema requirements, data migration and integrity impact, API contracts, caching strategy, and testing requirements to replace hardcoded financial policy categories with an authoritative database master table and dynamic lookup.

This assessment embodies a **Pragmatic**, **Minimalist**, and **Explicit** approach to eliminate discrepancy between sample/demo data, domain specifications, and the user interface, ensuring all financial policies are accurately discoverable and validated.

---

# 2. Current State

## Existing Behavior

1. **Database Schema & Persistence (`src/backend/Elsasa.Sis.Database`):**
   - Migration `20261104_0201_Finance_ObligationsBilling_Tables.sql` created `finance.financial_policies` with column `[category] NVARCHAR(64) NOT NULL` and index `ix_finance_financial_policies_category_status` on `([category], [status], [effective_date] DESC)`.
   - There is no master or reference table for policy categories in the `finance` schema or anywhere in the database.
   - The column is free text without database-level domain constraint, check constraint, or foreign key.
   - Seed data in migration `20261104_0201` created policy `FP-2026-001` with `category = 'tuition'`.
   - Autonomous demo seeder `FinancePolicyDemoDataSeeder.cs` persists policy `FP-2026-002` with `Category = "general_fee"`.

2. **Backend API & Validation (`Elsasa.Sis.Finance`):**
   - Query endpoint `GET /api/finance/policies` (`GetFinancialPoliciesEndpoint.cs` / `GetFinancialPoliciesHandler.cs`) filters policies with exact string matching: `AND (@Category IS NULL OR p.category = @Category)`.
   - Creation endpoint `POST /api/finance/policies` (`CreateFinancialPolicyEndpoint.cs` / `CreateFinancialPolicyHandler.cs`):
     - `CreateFinancialPolicyValidator.cs` validates only `NotEmpty()` and `MaximumLength(64)`.
     - `CreateFinancialPolicyHandler.cs` directly inserts `request.Category.Trim()` without verifying if the category is recognized.
   - There is no lookup endpoint to query available policy categories.

3. **Frontend Implementation (`src/frontend`):**
   - In `policies.schema.ts`, categories are hardcoded as a static array `POLICY_CATEGORIES = ['tuition', 'admission', 'facilities', 'activities', 'general'] as const`.
   - In `policies.types.ts`, `PolicyCategory` is defined as a static union type matching those 5 literals.
   - In `CreatePolicyModal.vue`, the dropdown options are mapped directly from `POLICY_CATEGORIES`, defaulting to `'tuition'`.
   - In `FinancialPolicyWorkspaceView.vue`, filter dropdown options are hardcoded as `categoryFilterOptions = [{ label: 'All Categories', value: '' }, { label: 'Tuition (SPP)', value: 'tuition' }, { label: 'Admission (Pendaftaran)', value: 'admission' }, { label: 'Facilities (Sarana)', value: 'facilities' }, { label: 'Activities (Kegiatan)', value: 'activities' }, { label: 'General (Umum)', value: 'general' }]`.

4. **Observed Defect (`IS017`):**
   - Policy `FP-2026-002: Hagios Institutional Fee Policy 2026/2027` seeded with category `general_fee` does not match the frontend's hardcoded filter value `general`.
   - Selecting any category in the filter hides `FP-2026-002`. It only displays when the filter is reset to "All Categories".
   - The domain specification `docs/entities/finance-entities-tables.md` (Table 1.1) specifies canonical values: `tuition`, `admission_fee`, `scholarship_guideline`, `refund_rule`, `clearance_rule`, `general_fee`, which diverged from the frontend's initial set.

## Existing Constraints

1. **Modular Monolith & Schema Isolation (ADR-001, ADR-002):** The `finance.policy_categories` table and any foreign key relationship must reside strictly within schema `finance`. Cross-schema references are prohibited.
2. **Backward Compatibility:** Existing client contracts (`policy.category` string code in DTOs, query param `?category=...`, and existing test fixtures) should be preserved to avoid cascade breaking changes across consumers.
3. **CQRS & Data Access (ADR-004, ADR-005):** Read operations use Dapper queries with explicit SQL literals. Mutations are isolated in vertical slice handlers returning `Result<T>`.
4. **Caching Strategy (Frontend Architecture Section 8.1):** Reference and master data must utilize `CACHE_TIERS.STATIC` (30m stale, 60m gc, no window focus refetch) to minimize unnecessary network roundtrips.

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| GAP-001 | CRITICAL | **Missing Reference Persistence Schema:** No master table exists for policy categories in the database. Categories in `finance.financial_policies` lack referential integrity. |
| GAP-002 | CRITICAL | **Taxonomy Divergence & Data Inconsistency:** Three conflicting sets of categories exist across domain specifications (`general_fee`, `admission_fee`), demo seed data (`general_fee`), and frontend code (`general`, `admission`). |
| GAP-003 | MAJOR | **Missing Category Lookup Endpoint:** The backend lacks a lookup endpoint (`GET /api/finance/policy-categories`) to supply active category definitions to clients dynamically. |
| GAP-004 | MAJOR | **Hardcoded Frontend Schemas and Workspaces:** `policies.schema.ts`, `policies.types.ts`, `FinancialPolicyWorkspaceView.vue`, and `CreatePolicyModal.vue` contain hardcoded category arrays and unions. |
| GAP-005 | MINOR | **Lax Backend Validation on Policy Creation:** `CreateFinancialPolicyHandler` and `CreateFinancialPolicyValidator` do not validate category existence against authoritative master data. |

---

# 4. Open Questions

| ID | Question | Impact | Status |
|---|---|---|---|
| OQ-001 | How should the master data table be structured and linked to `finance.financial_policies`? | Governs schema design, primary key type, foreign key constraints, and query joins. | CLOSED |
| OQ-002 | Which set of categories should be seeded and how should code discrepancies (`general_fee` vs `general`, `admission_fee` vs `admission`) be reconciled? | Governs migration seed scripts, data normalization, and canonical category definitions. | CLOSED |
| OQ-003 | What is the scope of policy category master data management (lookup-only vs full CRUD)? | Governs endpoint scope, authorization permissions, and UI management components. | CLOSED |
| OQ-004 | How should category validation on policy creation and category display name projection be handled? | Governs handler pre-checks, error codes, and DTO backward compatibility. | CLOSED |
| OQ-005 | How should the frontend initialize form fields and cache lookup data? | Governs Vue composable caching tier, default values, and Zod validation rules. | CLOSED |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | Natural code strings (`VARCHAR(64)`) such as `'tuition'` and `'general_fee'` remain the primary identifier for categories across the API and persistence layers. |
| ASM-002 | Master categories are relatively static operational configuration; managing them via versioned SQL migrations is sufficient for current requirements without an administrative CRUD UI. |
| ASM-003 | Existing policies in dev/test databases with category `'general'` or `'admission'` can be cleanly updated in place to `'general_fee'` and `'admission_fee'` during migration. |
| ASM-004 | Policy category codes are lowercase, snake_case strings. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | Foreign key constraint violation during migration on existing environments | DbUp migration script fails if arbitrary unmapped category strings exist in `finance.financial_policies`. | Include pre-migration data cleansing step in the SQL script mapping legacy values before applying `ALTER TABLE ... ADD CONSTRAINT FK_...`. |
| RISK-002 | Frontend regression during category loading latency | Filter dropdown or modal renders empty or unselectable while lookup query is pending. | Provide graceful loading/disabled states and empty-state placeholders in PrimeVue Select components. |
| RISK-003 | Test suite failures asserting legacy categories or hardcoded arrays | Existing backend integration/unit tests and frontend Vitest specs may fail if they rely on `'general'` or hardcoded schema lists. | Update unit/integration tests to use canonical categories (`'general_fee'`, `'admission_fee'`) and mock the dynamic lookup endpoint in frontend specs. |

---

# 7. Recommendations

## Option A (Recommended - Natural Code Key + Lookup Slice + Static Cache)
- Create `finance.policy_categories` with natural code primary key `code VARCHAR(64)`.
- Enforce intra-schema foreign key on `finance.financial_policies.category`.
- Seed unified domain taxonomy (8 canonical categories) and migrate legacy rows.
- Provide vertical slice endpoint `GET /api/finance/policy-categories`.
- Validate category in `CreateFinancialPolicyHandler`.
- Replace hardcoded arrays in frontend with dynamic query using `CACHE_TIERS.STATIC`.

### Advantages
- Full referential integrity enforced at database level.
- 100% backward compatibility with existing DTOs, query parameters, and endpoints.
- No table rewrites or complex UUID foreign key migrations.
- Resolves IS017 completely without over-engineering CRUD UI.

### Disadvantages
- Category codes cannot be renamed without cascading foreign key updates (acceptable for standard reference taxonomies).

## Option B (Surrogate UUID Key + CategoryId Column Migration)
- Create `finance.policy_categories` with `id UNIQUEIDENTIFIER PRIMARY KEY` and `code VARCHAR(64) UNIQUE`.
- Alter `financial_policies` to drop column `category` and add `category_id UNIQUEIDENTIFIER NOT NULL`.
- Rewrite all Dapper queries, DTOs, request payloads, and frontend models to use `categoryId`.

### Advantages
- Strictly adheres to surrogate key style.
- Allows category codes to change without updating referencing records.

### Disadvantages
- High blast radius: breaks existing API contracts, DTOs, queries, seed scripts, and frontend components.
- Unnecessary complexity for a simple reference lookup.

---

# 8. Gap Closure

## GAP-001: Missing Reference Persistence Schema

### Decision
Provision `finance.policy_categories` in a new DbUp migration (`20261111_0201_Finance_PolicyCategories_Table.sql`) with natural key `code VARCHAR(64) NOT NULL PRIMARY KEY`:
```sql
CREATE TABLE [finance].[policy_categories] (
    [code]          VARCHAR(64)       NOT NULL,
    [name]          NVARCHAR(100)     NOT NULL,
    [description]   NVARCHAR(255)     NULL,
    [display_order] INT               NOT NULL CONSTRAINT [df_policy_categories_display_order] DEFAULT (0),
    [is_active]     BIT               NOT NULL CONSTRAINT [df_policy_categories_active] DEFAULT (1),
    [created_at]    DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_policy_categories_created_at] DEFAULT (SYSUTCDATETIME()),
    [created_by]    NVARCHAR(100)     NULL,
    [updated_at]    DATETIMEOFFSET(7) NULL,
    [updated_by]    NVARCHAR(100)     NULL,
    CONSTRAINT [pk_finance_policy_categories] PRIMARY KEY CLUSTERED ([code])
);

ALTER TABLE [finance].[financial_policies]
    ADD CONSTRAINT [fk_finance_financial_policies_category]
    FOREIGN KEY ([category]) REFERENCES [finance].[policy_categories] ([code])
    ON DELETE NO ACTION;
```

### Rationale
Guarantees database-level referential integrity without mutating column types in `finance.financial_policies`. Preserves existing indexing (`ix_finance_financial_policies_category_status`) and Dapper query performance.

### Impact
Protects the database from invalid or orphaned category entries.

### Architecture Impact
Persistence schema update in schema `finance`.

### Resolved By
Drury & Antigravity (Collaborative /grill-me session)

### Resolved Date
2026-11-11

---

## GAP-002: Taxonomy Divergence & Data Inconsistency

### Decision
Seed the authoritative unified taxonomy in `20261111_0201_Finance_PolicyCategories_Table.sql` with 8 categories:
1. `tuition`: Name = "Tuition (SPP)", Description = "Regular academic and instructional fees", DisplayOrder = 1
2. `admission_fee`: Name = "Admission Fee (Uang Pangkal)", Description = "One-time enrollment and registration fees", DisplayOrder = 2
3. `facilities`: Name = "Facilities (Sarana & Prasarana)", Description = "Campus development, building, and facility maintenance fees", DisplayOrder = 3
4. `activities`: Name = "Activities (Kegiatan Siswa)", Description = "Co-curricular activities, field trips, and student events", DisplayOrder = 4
5. `general_fee`: Name = "General Institutional Fee (Biaya Umum)", Description = "Institutional and operational charges including materials and books", DisplayOrder = 5
6. `scholarship_guideline`: Name = "Scholarship Guideline (Panduan Beasiswa)", Description = "Scholarship and institutional financial aid policies", DisplayOrder = 6
7. `refund_rule`: Name = "Refund Rule (Ketentuan Pengembalian Dana)", Description = "Policies governing withdrawal, overpayment, and fee refund calculations", DisplayOrder = 7
8. `clearance_rule`: Name = "Clearance Rule (Ketentuan Bebas Keuangan)", Description = "Financial clearance requirements for examination and graduation eligibility", DisplayOrder = 8

Prior to applying the foreign key, execute data cleanup:
```sql
UPDATE [finance].[financial_policies] SET [category] = 'general_fee' WHERE [category] IN ('general', 'GENERAL_FEE');
UPDATE [finance].[financial_policies] SET [category] = 'admission_fee' WHERE [category] = 'admission';
```

### Rationale
Reconciles the domain specification canonical values with actual frontend operational requirements, while immediately fixing the `FP-2026-002` visibility bug (`IS017`).

### Impact
Ensures all seeded and existing financial policies match valid master data.

### Architecture Impact
Seed and migration script in `Elsasa.Sis.Database`.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-11

---

## GAP-003: Missing Category Lookup Endpoint

### Decision
Implement vertical slice feature `GetPolicyCategories` under `src/backend/Elsasa.Sis.Finance/Features/Policies/GetPolicyCategories/`:
- Endpoint: `GET /api/finance/policy-categories`
- Permissions: Authenticated user / `Permissions.Finance.Policies.View`
- DTO: `PolicyCategoryDto(string Code, string Name, string? Description, int DisplayOrder, bool IsActive)`
- Handler: Fast read-only Dapper query returning all active records:
  ```sql
  SELECT
      code AS Code,
      name AS Name,
      description AS Description,
      display_order AS DisplayOrder,
      is_active AS IsActive
  FROM finance.policy_categories
  WHERE is_active = 1
  ORDER BY display_order ASC, name ASC;
  ```

### Rationale
Follows ADR-004 and ADR-005 vertical slice conventions, providing a dedicated lookup endpoint for client components.

### Impact
Enables client applications to dynamically consume category definitions without compile-time hardcoding.

### Architecture Impact
New endpoint and query handler in `Elsasa.Sis.Finance`.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-11

---

## GAP-004: Hardcoded Frontend Schemas and Workspaces

### Decision
1. In `policies.types.ts`: Deprecate/remove static union `PolicyCategory` in favor of `string`, and export `PolicyCategoryDto`.
2. In `policies.schema.ts`: Remove static `POLICY_CATEGORIES` array. Update `createFinancialPolicySchema` to validate `category: z.string({ required_error: 'Category is required.' }).trim().min(1, 'Category is required.').max(64)`.
3. In `policies.api.ts`: Add `getPolicyCategories(): Promise<PolicyCategoryDto[]>`.
4. In `queryKeys.ts`: Add `financeKeys.policies.categories: ['finance', 'policies', 'categories'] as const`.
5. In `useFinancePolicies.ts`: Add `useFinancePolicyCategoriesQuery()` using `CACHE_TIERS.STATIC`.
6. In `FinancialPolicyWorkspaceView.vue`:
   - Compute `categoryFilterOptions` from `useFinancePolicyCategoriesQuery()` data:
     `[{ label: 'All Categories', value: '' }, ...(data.value ?? []).map(c => ({ label: c.name, value: c.code }))]`.
   - Resolve table cell and detail view display labels via a computed lookup map `categoryNameByCode[policy.category] || policy.category`.
7. In `CreatePolicyModal.vue`:
   - Populate dropdown options dynamically from `useFinancePolicyCategoriesQuery()`.
   - Initialize `category` as empty string `""` with placeholder "Select Category", enforcing explicit user selection.

### Rationale
Eliminates code duplication and prevents UI-backend drift. Ensures any new categories added to master data appear automatically in workspace filters and creation dialogs.

### Impact
Resolves defect `IS017` in the frontend; `FP-2026-002` (category `general_fee`) will display properly with filter label "General Institutional Fee (Biaya Umum)".

### Architecture Impact
Frontend components, composables, schema, and API client updates.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-11

---

## GAP-005: Lax Backend Validation on Policy Creation

### Decision
In `CreateFinancialPolicyHandler.cs`, query the master table before persisting:
```sql
SELECT 1 FROM finance.policy_categories WHERE code = @Category AND is_active = 1;
```
If the category is not found or inactive, return:
```csharp
Result.Failure<CreateFinancialPolicyResponse>(
    DomainError.Validation(
        "Finance.Policy.InvalidCategory",
        $"Policy category '{request.Category}' is invalid or inactive."));
```
Keep DTOs returning `Category` (code) to preserve full backward compatibility.

### Rationale
Enforces domain validation with clear error messages at the handler layer before hitting the database foreign key constraint.

### Impact
Rejects invalid policy creation attempts cleanly with appropriate domain error contracts.

### Architecture Impact
Handler enhancement in `Elsasa.Sis.Finance`.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-11

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-NOT-REQUIRED**

## Rationale

1. **No Cross-Domain Boundaries:** The change is strictly confined to the `Finance` bounded context (`finance` schema, `Elsasa.Sis.Finance` assembly, and `features/finance` frontend module). No cross-domain contracts, integration events, or inter-assembly interfaces are modified.
2. **Established Architectural Patterns:** The implementation strictly follows established patterns:
   - DDL DbUp migration with intra-schema foreign key (ADR-002).
   - Minimal API vertical slice endpoint with Dapper query (ADR-004, ADR-005).
   - TanStack Query with standard `CACHE_TIERS.STATIC` (Frontend Architecture Section 8.1).
   - PrimeVue Select components with VeeValidate/Zod forms.
3. **Exhaustive Feasibility Specification:** All schema definitions, seed data, validation rules, endpoint signatures, DTO structures, and component updates have been explicitly defined and agreed upon during this feasibility assessment.
4. **Direct Progression to Planning:** No unresolved architectural ambiguities remain. The team can proceed directly to creating the Implementation Plan (`IS017 - FIN-POLICY-CATEGORY-IMPLEMENTATION-PLAN.md`).

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps (GAP-001 through GAP-005) have been identified and analyzed.
- [x] All blocking open questions (OQ-001 through OQ-005) have been resolved with explicit decisions and rationales.
- [x] Natural key structure and database foreign key constraint agreed (`finance.policy_categories(code)`).
- [x] Authoritative seed taxonomy (8 canonical categories) and data migration mapping agreed.
- [x] Lookup endpoint contract (`GET /api/finance/policy-categories`) and handler validation defined.
- [x] Frontend dynamic consumption, static cache tier, and empty-form initialization specified.
- [x] Architecture Applicability established as `ARCHITECTURE-NOT-REQUIRED`.

## Status

**READY-FOR-PLANNING**

## Notes

Feasibility assessment is fully resolved and validated. The next stage in the Knowledge-Centric SDLC is to author the executable implementation plan (`docs/issues/IS017 - FIN-POLICY-CATEGORY-IMPLEMENTATION-PLAN.md`) detailing the migration, backend slice, frontend updates, and test coverage.

---

# 11. References

Referenced artifacts:
- `docs/issues/IS017 - FIN-POLICY-CATEGORY-ISSUE.md`
- `docs/domains/finance-domain.md`
- `docs/entities/finance-entities-tables.md`
- `docs/architecture/adr.md`
- `docs/architecture/frontend-architecture.md`

Referenced codebase locations:
- `src/backend/Elsasa.Sis.Database/Scripts/20261104_0201_Finance_ObligationsBilling_Tables.sql`
- `src/backend/Elsasa.Sis.Finance/Features/Policies/GetFinancialPolicies/GetFinancialPoliciesHandler.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Policies/CreateFinancialPolicy/CreateFinancialPolicyHandler.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Policies/CreateFinancialPolicy/CreateFinancialPolicyValidator.cs`
- `src/backend/Elsasa.Sis.Finance/Features/DemoData/FinancePolicyDemoDataSeeder.cs`
- `src/frontend/src/features/finance/policies/schemas/policies.schema.ts`
- `src/frontend/src/features/finance/policies/types/policies.types.ts`
- `src/frontend/src/features/finance/policies/views/FinancialPolicyWorkspaceView.vue`
- `src/frontend/src/features/finance/policies/components/CreatePolicyModal.vue`
- `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/CreateFinancialPolicyValidatorTests.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/FinancialPolicyWorkflowIntegrationTests.cs`
