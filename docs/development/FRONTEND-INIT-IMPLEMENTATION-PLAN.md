---
Title: Incremental Frontend Initialization Plan — Elsasa SIS V3
Code: FRONTEND-INIT
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-02
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Establish an enterprise-grade, feature-oriented modular frontend foundation for Elsasa SIS V3 without prematurely implementing the entire SIS business surface. This plan bootstraps the Vue 3.5+ / TypeScript 5.6+ / Vite 6+ Single Page Application under `src/frontend/`, configures Tailwind CSS v4 and PrimeVue v4 with institutional design tokens, establishes bifurcated state management (TanStack Query for server state and Pinia for client session context), implements the centralized Axios HTTP client with RFC 7807 `ProblemDetails` error transformation and `X-Correlation-ID` tracing, configures Vue Router with permission-driven navigation guards, delivers shared UI primitives and persistent layout shells, and validates live frontend-backend integration with an end-to-end verification vertical slice communicating with the running ASP.NET Core Minimal API.

Planning Mode: GREENFIELD-PLANNING

Referenced artifacts:

- ARCHITECTURE:
  - `docs/architecture/frontend-architecture.md` (Feature-Oriented Modular Frontend & Design System specification)
  - `docs/architecture/backend-architecture.md` (Modular Monolith & Minimal API backend specification)
  - `docs/architecture/adr.md` (ADR-001 through ADR-010, ADR-FE-001 through ADR-FE-009)
  - `docs/development/frontend-initialization-plan.md` (Source initialization roadmap)

Architecture Applicability: GREENFIELD-ARCHITECTURE

Greenfield planning mode based on complete target architecture. Scope encompasses the entire target system architecture foundation.

---

# 2. Planning Scope

The planning scope establishes the runnable frontend application foundation ready for subsequent Stage 1 feature development:

- **Workspace Scaffolding & Tooling Baseline:** Scaffolding the frontend workspace under `src/frontend/`, configuring `package.json` with locked dependency versions, TypeScript composite project configuration (`tsconfig.json`, `tsconfig.app.json`, `tsconfig.node.json`), Vite 6 build pipeline with manual chunk splitting and dev server proxy forwarding `/api` to `http://localhost:5000`, ESLint flat configuration (`eslint.config.js`), Prettier formatting (`.prettierrc`), and `.editorconfig`.
- **Styling Engine & Design Tokens:** Configuring Tailwind CSS v4 via `@tailwindcss/vite`, CSS layer precedence (`@layer primevue, custom;`), institutional color palette (Navy `#0f172a`, Teal `#0d9488`, Slate neutrals, Emerald/Amber/Rose/Sky status tokens), and PrimeVue v4 Aura preset configuration with global PassThrough defaults.
- **Shared Kernel & Outcome Modeling:** Shared TypeScript definitions (`ProblemDetails`, `ApiError`, `DomainErrorType`, `Result<T>`, `PaginatedResponse<T>`, `NavigationItem`), `date-fns` formatting utilities (`formatDisplayDate`, `formatDisplayDateTime`, `formatDateOnly`, `formatTimeOnly`), Indonesian Rupiah currency formatter (`formatIDR`), type-safe namespaced `localStorage` service, and structured client logger.
- **Centralized HTTP Client & Backend Contract Alignment:** Centralized Axios instance with base URL environment resolution (`.env.development`, `.env.staging`, `.env.production`), request interceptor for Bearer token and `X-Correlation-ID` UUID tracing header injection matching backend `CorrelationIdMiddleware`, and response interceptor parsing backend RFC 7807 `ProblemDetails` into strongly-typed `ApiError` instances with 401 session expiration handling.
- **State Management & Query Infrastructure:** `@tanstack/vue-query` QueryClient configuration with cache staleness tiers (static 30m, reference 5m, active queue 30s, transactional 0s), and Pinia stores strictly restricted to client-only session state (`auth.store.ts` with in-memory JWT and permission checks, `appContext.store.ts` for active campus/period context, `ui.store.ts` for sidebar toggle and theme).
- **Client Routing & Authorization Security:** Vue Router configuration with hierarchical route definitions, HTML5 web history, lazy route chunk loading, and global navigation guards enforcing `requiresAuth` and `permission` meta against the active user's permissions, redirecting to `/auth/login` or `/403`.
- **Shared UI Primitives & Layout Shells:** Domain-agnostic reusable components (`FormField.vue`, `StatusPill.vue`, `EmptyState.vue`, `LoadingSpinner.vue`, `PageHeader.vue`, `ContentCard.vue`), persistent shell layouts (`AppLayout.vue` with collapsible sidebar, institutional topbar, breadcrumb header, and router-view; `AuthLayout.vue` for centered auth screens), and root `App.vue` with PrimeVue `Toast` and `ConfirmDialog`.
- **Minimal Application Shell & Verification Vertical Slice:** Root application bootstrap in `main.ts`, base views (`DashboardView.vue`, `LoginView.vue`, `ForbiddenView.vue`, `NotFoundView.vue`), and an end-to-end integration verification vertical slice (`CalendarWorkspaceView.vue`) probing live backend `/health` status and exercising `POST /api/operations/academic-years` with form validation (testing 201 Created and 422 ProblemDetails mapping).

---

# 3. Dependencies

### External System & Tooling Dependencies

- Node.js runtime (v22.x or LTS)
- pnpm package manager (v9.x or v11.x)
- ASP.NET Core Backend Host (`Elsasa.Sis.Api` running at `http://localhost:5000`)
- Microsoft SQL Server 2022 instance (for live backend integration execution)

### Internal Slice Dependencies

- `Depends On` declares implementation prerequisites between slices.
- Dependencies reference Slice IDs only.
- Dependency satisfaction requires the referenced slice to have implementation status `IMPLEMENTED`.
- Dependency satisfaction does not require review status `GO`.
- Slices with `Depends On: None` or whose dependencies are satisfied may be executed in parallel.

### Dependency Graph

```mermaid
flowchart TD
    P1_S01["P1-S01: Frontend Workspace & Package Setup"] --> P1_S02["P1-S02: TypeScript & Vite Build Pipeline"]
    P1_S01 --> P1_S03["P1-S03: Tailwind CSS v4 & PrimeVue Design Tokens"]
    P1_S01 --> P1_S04["P1-S04: Code Quality, Linting & Formatting"]
    P1_S02 --> P2_S05["P2-S05: Outcome Types, RFC 7807 & Utilities"]
    P2_S05 --> P2_S06["P2-S06: HTTP Client, Tracing & Environment Setup"]
    P1_S02 --> P3_S07["P3-S07: TanStack Query Provider & Cache Engine"]
    P2_S05 --> P3_S08["P3-S08: Pinia Stores for Session & App Context"]
    P3_S08 --> P3_S09["P3-S09: Vue Router & Permission Navigation Guards"]
    P1_S03 --> P4_S10["P4-S10: Shared UI Primitives & Form Feedback"]
    P2_S05 --> P4_S10
    P4_S10 --> P4_S11["P4-S11: Layout Shells & Root Application Shell"]
    P3_S08 --> P4_S11
    P3_S09 --> P4_S11
    P4_S11 --> P5_S12["P5-S12: Application Bootstrap & Minimal Route Views"]
    P3_S07 --> P5_S12
    P5_S12 --> P5_S13["P5-S13: Backend Integration Verification Vertical Slice"]
    P2_S06 --> P5_S13
```

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Workspace Scaffolding & Tooling Foundation | IMPLEMENTED | GO | 4/4 |
| P2 - Core Infrastructure & HTTP Client Integration | IMPLEMENTED | GO | 2/2 |
| P3 - State Management & Application Infrastructure | IMPLEMENTED | GO | 3/3 |
| P4 - Shared UI Primitives & Application Layout Shells | IMPLEMENTED | GO | 2/2 |
| P5 - Minimal Application Shell & Integration Verification | IMPLEMENTED | GO | 2/2 |

---

# 5. Phases

## P1 - Workspace Scaffolding & Tooling Foundation

Implementation Status: IMPLEMENTED  
Review Status: GO

### P1-S01

Title: Frontend Workspace Scaffolding, Package Manifest, and Dependency Installation

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Establish the standalone frontend project under `src/frontend/`, configure `package.json` with all required production and development dependencies conforming to the technology stack baseline in `docs/architecture/frontend-architecture.md`, generate `pnpm-lock.yaml`, and provide index HTML and public asset entry points.

Depends On: None

Repository: elsasa-sis-v3

Completion Criteria:
- `src/frontend/package.json` exists declaring:
  - Runtime dependencies: `vue` (`^3.5.0`), `vue-router` (`^4.4.0`), `pinia` (`^2.2.0`), `@tanstack/vue-query` (`^5.60.0`), `axios` (`^1.7.0`), `primevue` (`^4.2.0`), `@primevue/themes` (`^4.2.0`), `vee-validate` (`^4.14.0`), `@vee-validate/zod` (`^4.14.0`), `zod` (`^3.23.0`), `lucide-vue-next` (`^0.460.0`), `date-fns` (`^4.1.0`), `@vueuse/core` (`^11.3.0`), `uuid` (`^10.0.0`).
  - Development dependencies: `typescript` (`^5.6.0`), `vite` (`^6.0.0`), `@vitejs/plugin-vue` (`^5.2.0`), `@tailwindcss/vite` (`^4.0.0`), `tailwindcss` (`^4.0.0`), `vue-tsc` (`^2.1.0`), `vitest` (`^2.1.0`), `@vue/test-utils` (`^2.4.0`), `msw` (`^2.6.0`), `openapi-typescript` (`^7.4.0`), `eslint` (`^9.15.0`), `prettier` (`^3.3.0`), `@types/node` (`^22.0.0`), `@types/uuid` (`^10.0.0`), `jsdom` (`^25.0.0`).
  - Scripts: `dev`, `build`, `preview`, `typecheck`, `lint`, `format:check`, `format`, `test:unit`, `api:generate`.
- `pnpm install` succeeds inside `src/frontend/`, generating `src/frontend/pnpm-lock.yaml` with zero dependency peer resolution errors.
- `src/frontend/index.html` exists with responsive viewport meta, favicon link, and `<script type="module" src="/src/main.ts"></script>`.
- `src/frontend/public/favicon.ico` or SVG placeholder exists.

Notes:
- Establishes the foundational Node.js / pnpm package workspace for all frontend code.
- Package manifest configured with required runtime and dev dependencies; deterministic pnpm-lock.yaml generated via frozen-compatible pnpm install; responsive index.html and public/favicon.svg created.

---

### P1-S02

Title: TypeScript Composite Configuration and Vite 6 Build Pipeline Setup

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Configure TypeScript composite projects (`tsconfig.json`, `tsconfig.app.json`, `tsconfig.node.json`) with strict compiler enforcement and path aliases, configure `vite.config.ts` with Vue SFC, Tailwind CSS v4, manual chunk splitting, path aliases (`@` resolving to `src/`), and dev server API proxy forwarding `/api` to `http://localhost:5000`.

Depends On: P1-S01

Repository: elsasa-sis-v3

Completion Criteria:
- `src/frontend/tsconfig.json` exists configuring project references to `tsconfig.app.json` and `tsconfig.node.json`.
- `src/frontend/tsconfig.app.json` configures:
  - `"target": "ES2022"`, `"module": "ESNext"`, `"moduleResolution": "bundler"`
  - `"strict": true`, `"noImplicitAny": true`, `"exactOptionalPropertyTypes": true`
  - `"paths": { "@/*": ["./src/*"] }`
  - `"jsx": "preserve"`
- `src/frontend/tsconfig.node.json` configures Vite tooling compilation.
- `src/frontend/vite.config.ts` configures:
  - Plugins: `vue()`, `tailwindcss()`
  - Resolve alias: `@` -> `path.resolve(__dirname, './src')`
  - Dev server proxy: `server.proxy['/api']` pointing to `http://localhost:5000` with `changeOrigin: true`
  - Rollup manual chunk vendor splitting: `vendor-core` (`vue`, `vue-router`, `pinia`), `vendor-query` (`@tanstack/vue-query`, `axios`), `vendor-primevue` (`primevue`), `vendor-utils` (`date-fns`, `zod`, `vee-validate`, `lucide-vue-next`).
- `pnpm --dir src/frontend typecheck` executes without configuration errors.

Notes:
- Dev server proxy eliminates CORS constraints during local frontend development without modifying the ASP.NET Core backend.
- TypeScript composite configuration established (`tsconfig.json` referencing `tsconfig.app.json` and `tsconfig.node.json`) with strict compiler enforcement and `@/*` alias resolution.
- Vite 6 build pipeline configured in `vite.config.ts` with `@vitejs/plugin-vue`, `@tailwindcss/vite`, `@` path alias, `/api` proxy targeting `http://localhost:5000`, and vendor chunk splitting (`vendor-core`, `vendor-query`, `vendor-primevue`, `vendor-utils`).
- Type checking (`vue-tsc --noEmit`) and project build (`vue-tsc -b && vite build`) validated with zero configuration errors.

---

### P1-S03

Title: Tailwind CSS v4 Styling Engine and PrimeVue Aura Design System Setup

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Configure the Tailwind CSS v4 modern engine with institutional design tokens and configure PrimeVue v4 with the Aura preset and CSS layer ordering (`@layer primevue, custom;`).

Depends On: P1-S01

Repository: elsasa-sis-v3

Completion Criteria:
- `src/frontend/src/shared/styles/main.css` exists containing:
  - `@import "tailwindcss";`
  - `@layer primevue, custom;`
  - `@theme` block defining:
    - Primary Navy: `--color-elsasa-navy-900: #0f172a;`, `--color-elsasa-navy-800: #1e293b;`
    - Brand Teal: `--color-elsasa-teal-600: #0d9488;`, `--color-elsasa-teal-500: #14b8a6;`, `--color-elsasa-teal-50: #f0fdfa;`
    - Surface & Border neutrals: Slate tones (`#f8fafc`, `#ffffff`, `#e2e8f0`)
    - Status colors: Emerald (`#059669`), Amber (`#d97706`), Rose (`#e11d48`), Sky (`#0284c7`)
    - Font sans: `'Inter', system-ui, -apple-system, sans-serif;`
- `src/frontend/src/app/config/primevue.ts` exists configuring PrimeVue plugin options:
  - Aura preset integration from `@primevue/themes/aura`
  - CSS layer ordering option `cssLayer: { name: 'primevue', order: 'tailwind-base, primevue, tailwind-utilities' }` or modern PassThrough unstyled base.
- Styles compile cleanly with zero CSS parse errors during Vite dev and build.

Notes:
- Conforms to Section 10 of `docs/architecture/frontend-architecture.md`.
- Implemented Tailwind CSS v4 base stylesheet at `src/frontend/src/shared/styles/main.css` defining CSS layer precedence (`@layer primevue, custom;`) and institutional `@theme` tokens (Primary Navy, Brand Teal, Surface & Border neutrals, Status colors, and font-sans).
- Configured PrimeVue v4 Aura preset and CSS layer ordering in `src/frontend/src/app/config/primevue.ts`.
- Verified Vite build and dev compilation with zero CSS parse errors.

---

### P1-S04

Title: Code Quality, Linting, Formatting, and Architectural Boundary Rules

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Establish deterministic code formatting and static analysis rules using ESLint (Flat Config), Prettier, and `.editorconfig`, enforcing strict TypeScript checks, Vue 3 SFC best practices, and architectural layer boundaries (preventing direct cross-feature coupling).

Depends On: P1-S01

Repository: elsasa-sis-v3

Completion Criteria:
- `src/frontend/eslint.config.js` exists configured with:
  - JavaScript, TypeScript (`typescript-eslint`), and Vue (`eslint-plugin-vue`) recommended rulesets.
  - SFC `<script setup lang="ts">` component rules.
  - Architectural restriction rules (`no-restricted-imports` preventing `shared/` from importing upwards into `features/` or `app/`, and preventing cross-imports between distinct `features/` submodules).
- `src/frontend/.prettierrc` exists configuring:
  - `singleQuote: true`, `semi: true`, `trailingComma: "all"`, `printWidth: 100`, `tabWidth: 2`.
- `src/frontend/.editorconfig` aligns with repository conventions (2 spaces, UTF-8, LF).
- `src/frontend/.gitignore` ignores `node_modules`, `dist`, `.env.local`, coverage reports.
- `pnpm --dir src/frontend lint` and `pnpm --dir src/frontend format:check` execute cleanly with exit code 0.

Notes:
- Enforces ADR-FE-001 compile-time architectural integrity across all developer contributions.
- Configured ESLint v9 Flat Config in `src/frontend/eslint.config.js` with `@eslint/js`, `typescript-eslint`, and `eslint-plugin-vue` recommended configs.
- Configured Vue SFC `<script setup lang="ts">` enforcement rules (`vue/block-order`, `vue/component-api-style`, `vue/define-macros-order`, `vue/component-name-in-template-casing`).
- Implemented `no-restricted-imports` architectural boundary rules covering 6 domains and 20 functional modules, preventing `shared/` from importing upwards into `features/` or `app/`, preventing `features/` from importing upwards into `app/`, and preventing cross-imports across distinct feature submodules and domains.
- Configured deterministic Prettier settings in `src/frontend/.prettierrc` (`singleQuote: true`, `semi: true`, `trailingComma: "all"`, `printWidth: 100`, `tabWidth: 2`) with `.prettierignore`.
- Configured `.editorconfig` aligned with repository conventions (2 spaces, UTF-8, LF) and `.gitignore` ignoring `node_modules`, `dist`, `.env.local`, coverage reports.
- Verified both `pnpm --dir src/frontend lint` and `pnpm --dir src/frontend format:check` execute with exit code 0.

---

## P2 - Core Infrastructure & HTTP Client Integration

Implementation Status: IMPLEMENTED  
Review Status: GO

### P2-S05

Title: Domain Outcome Types, RFC 7807 ProblemDetails Modeling, and Shared Utilities

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the shared domain outcome types (`Result<T>`, `ProblemDetails`, `ApiError`), navigation/breadcrumb models, date formatting utilities matching backend `DateOnly`/`TimeOnly` representations, and currency formatters under `src/frontend/src/shared/`.

Depends On: P1-S02

Repository: elsasa-sis-v3

Completion Criteria:
- `src/frontend/src/shared/types/common.ts` exists and exports:
  - `DomainErrorType`: `'NotFound' | 'Conflict' | 'Validation' | 'Unauthorized' | 'Forbidden' | 'Failure'`
  - `ProblemDetails` interface: `type?: string; title?: string; status?: number; detail?: string; instance?: string; errorCode?: string; errors?: Record<string, string[]>;`
  - `ApiError` class: implements `Error`, encapsulates `statusCode`, `title`, `detail`, `errorCode`, `errors`, and helper `hasValidationErrors()`.
  - `Result<T>` interface: `isSuccess: boolean; isFailure: boolean; value?: T; error?: ApiError;`
  - `PaginatedResponse<T>` interface: `items: T[]; pageNumber: number; pageSize: number; totalCount: number; totalPages: number; hasPreviousPage: boolean; hasNextPage: boolean;`
- `src/frontend/src/shared/types/navigation.ts` exports `NavigationItem`, `BreadcrumbItem`, and `AppRouteMeta`.
- `src/frontend/src/shared/utils/dateUtils.ts` wraps `date-fns` and exports:
  - `formatDisplayDate(date: Date | string): string` -> e.g. "12 Nov 2026"
  - `formatDisplayDateTime(date: Date | string): string` -> e.g. "12 Nov 2026, 14:30"
  - `formatDateOnly(date: Date | string): string` -> "YYYY-MM-DD"
  - `formatTimeOnly(date: Date | string): string` -> "HH:mm:ss"
  - `parseIsoDate(dateString: string): Date`
- `src/frontend/src/shared/utils/currencyUtils.ts` exports `formatIDR(amount: number): string` -> e.g. "Rp 1.500.000".
- `src/frontend/src/shared/services/storage.ts` provides type-safe JSON `localStorage` get/set/remove with namespace prefix `elsasa:v3:`.
- `src/frontend/src/shared/services/logger.ts` provides structured logging respecting log level.
- Unit tests in `src/frontend/src/shared/utils/__tests__/dateUtils.spec.ts` and `currencyUtils.spec.ts` pass via `vitest`.

Notes:
- Completely aligns frontend error and outcome modeling with backend `ResultsExtensions.ToProblemDetails()` and C# Dapper `DateOnly`/`TimeOnly` handlers.
- Created `src/frontend/src/shared/types/common.ts` exporting `DomainErrorType`, `ProblemDetails`, `ApiError`, `Result<T>`, and `PaginatedResponse<T>`.
- Created `src/frontend/src/shared/types/navigation.ts` exporting `BreadcrumbItem`, `NavigationItem`, and `AppRouteMeta` with Vue Router module augmentation.
- Created `src/frontend/src/shared/utils/dateUtils.ts` exporting `formatDisplayDate`, `formatDisplayDateTime`, `formatDateOnly`, `formatTimeOnly`, and `parseIsoDate`.
- Created `src/frontend/src/shared/utils/currencyUtils.ts` exporting `formatIDR`.
- Created `src/frontend/src/shared/services/storage.ts` providing namespaced JSON storage (`elsasa:v3:`).
- Created `src/frontend/src/shared/services/logger.ts` providing structured logging respecting environment log levels.
- Added comprehensive unit test suites in `dateUtils.spec.ts`, `currencyUtils.spec.ts`, `common.spec.ts`, `storage.spec.ts`, and `logger.spec.ts`, passing all tests with Vitest. Fully verified with `typecheck`, `lint`, `format:check`, and production `build`.

---

### P2-S06

Title: Centralized HTTP Client, Request Tracing Interceptors, and Environment Configuration

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the centralized Axios client `http.ts` with request interceptors injecting JWT Bearer tokens and `X-Correlation-ID` tracing headers, and response interceptors parsing backend RFC 7807 `ProblemDetails` into typed `ApiError` instances with 401 session expiration handling, supported by environment configuration files.

Depends On: P2-S05

Repository: elsasa-sis-v3

Completion Criteria:
- Environment files exist:
  - `src/frontend/.env.development`: `VITE_API_BASE_URL=/api`, `VITE_APP_TITLE=Elsasa SIS (Dev)`, `VITE_ENABLE_MSW=false`, `VITE_LOG_LEVEL=debug`
  - `src/frontend/.env.staging`: `VITE_API_BASE_URL=https://staging-api.elsasa.sch.id/api`, `VITE_APP_TITLE=Elsasa SIS (Staging)`, `VITE_ENABLE_MSW=false`, `VITE_LOG_LEVEL=warn`
  - `src/frontend/.env.production`: `VITE_API_BASE_URL=https://api.elsasa.sch.id/api`, `VITE_APP_TITLE=Elsasa SIS`, `VITE_ENABLE_MSW=false`, `VITE_LOG_LEVEL=error`
- `src/frontend/src/shared/services/http.ts` exports `apiClient`:
  - Created with `baseURL: import.meta.env.VITE_API_BASE_URL || '/api'`, `timeout: 15000`.
  - Request interceptor:
    - Attaches `Authorization: Bearer <token>` from active auth state if token exists.
    - Attaches `X-Correlation-ID: <uuid>` using `v4 as uuidv4` if not already present.
  - Response interceptor:
    - Unwraps `response.data` on HTTP 2xx.
    - Intercepts 401 Unauthorized, notifies auth store to expire session, and rejects with `ApiError(401, 'Unauthorized', ...)`.
    - Intercepts RFC 7807 error responses (400, 403, 404, 409, 422, 500), extracts `title`, `detail`, `errorCode`, and validation `errors`, and rejects with `ApiError`.
    - Handles offline / network drop (status 0) rejecting with `ApiError(0, 'Network Error', ...)`.
- Unit tests in `src/frontend/src/shared/services/__tests__/http.spec.ts` verify header injection and RFC 7807 error transformation.

Notes:
- Establishes the authoritative communication backbone between Vue composables and ASP.NET Core endpoints.
- Created environment configuration files: `.env.development`, `.env.staging`, and `.env.production` with appropriate `VITE_API_BASE_URL`, `VITE_APP_TITLE`, `VITE_ENABLE_MSW`, and `VITE_LOG_LEVEL` variables.
- Extended `src/frontend/src/vite-env.d.ts` with strongly-typed `ImportMetaEnv` and `ImportMeta` declarations.
- Implemented `src/frontend/src/shared/services/http.ts` exporting centralized `apiClient` instance and `createHttpClient` factory configuring default headers (`Content-Type: application/json`, `Accept: application/json`), 15000ms timeout, and base URL fallback.
- Added request interceptor injecting `Authorization: Bearer <token>` (resolving dynamically via registered token provider or namespaced `storage.ts` fallback) and generating unique `X-Correlation-ID` (UUID v4) when missing.
- Added response interceptor unwrapping HTTP 2xx `response.data`, invoking session expiration handler and dispatching `elsasa:auth:session-expired` event on 401 Unauthorized, transforming RFC 7807 `ProblemDetails` payloads (400, 403, 404, 409, 422, 500) into typed `ApiError` instances, and capturing network drop/offline failures as status 0 `ApiError`.
- Authored 26 comprehensive unit tests in `src/frontend/src/shared/services/__tests__/http.spec.ts` covering header injection, token resolution, correlation ID generation, 2xx response unwrapping, 401 session expiration, RFC 7807 validation error mapping, and offline handling. All 26 unit tests pass with 100% success.
- Verified TypeScript type checking (`vue-tsc --noEmit`), ESLint linting (0 errors), and Prettier formatting compliance across the workspace.

---

## P3 - State Management & Application Infrastructure

Implementation Status: IMPLEMENTED  
Review Status: GO

### P3-S07

Title: TanStack Query Provider and Cache Infrastructure Setup

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Configure `@tanstack/vue-query` QueryClient with deterministic caching, garbage collection, and stale time policies categorized according to Section 8.1 of `docs/architecture/frontend-architecture.md`, and establish root Vue plugin composition providers.

Depends On: P1-S02

Repository: elsasa-sis-v3

Completion Criteria:
- `src/frontend/src/app/providers/queryClient.ts` exists configuring `QueryClient` with:
  - `defaultOptions.queries.staleTime`: 30000 (30s default)
  - `defaultOptions.queries.gcTime`: 300000 (5 minutes default)
  - `defaultOptions.queries.retry`: 1
  - `defaultOptions.queries.refetchOnWindowFocus`: true
  - `defaultOptions.mutations.retry`: 0
- `src/frontend/src/app/providers/index.ts` exists exporting `registerAppProviders(app: App)` registering:
  - Pinia
  - VueQueryPlugin with configured QueryClient
  - PrimeVue with Aura preset configuration
  - PrimeVue `ToastService` and `ConfirmationService`
- Unit tests verify QueryClient default options and query behavior.

Notes:
- ADR-FE-002: Server state belongs exclusively in TanStack Query. Pinia is never used for server cache.
- Configured `QueryClient` with default query options (staleTime 30s, gcTime 5m, retry 1, refetchOnWindowFocus true) and mutation options (retry 0) in `src/frontend/src/app/providers/queryClient.ts`.
- Encoded architecture cache staleness tiers (`CACHE_TIERS.STATIC`, `OPERATIONAL`, `ACTIVE_QUEUE`, `TRANSACTIONAL`) and provided `createQueryClient` factory for isolated instances.
- Implemented `registerAppProviders(app: App)` in `src/frontend/src/app/providers/index.ts` registering Pinia, VueQueryPlugin, PrimeVue with Aura preset (`primevueConfig`), PrimeVue `ToastService`, and PrimeVue `ConfirmationService`.
- Verified QueryClient default options, caching, invalidation, cache clearance, and provider registrations via comprehensive unit test suites in `src/frontend/src/app/providers/__tests__/queryClient.spec.ts` and `providers.spec.ts` (11 tests passing, 0 lint warnings, 0 type errors).

---

### P3-S08

Title: Pinia Stores for Client Session and Application Context

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement Pinia stores strictly scoped to browser-only and authenticated user context: `useAuthStore` for session identity and permissions, `useAppContextStore` for active campus and academic period, and `useUIStore` for sidebar collapse and theme preferences.

Depends On: P2-S05

Repository: elsasa-sis-v3

Completion Criteria:
- `src/frontend/src/app/stores/auth.store.ts` exports `useAuthStore`:
  - State: `user: UserSession | null`, `token: string | null`, `permissions: string[]`, `roles: string[]`.
  - Getters: `isAuthenticated: boolean`, `hasPermission(permission: string): boolean`, `hasAnyRole(roles: string[]): boolean`.
  - Actions: `setSession(token: string, user: UserSession)`, `logout()`, `handleSessionExpired()`.
  - Token is maintained in-memory with optional local dev persistence fallback.
- `src/frontend/src/app/stores/appContext.store.ts` exports `useAppContextStore`:
  - State: `activeCampusId: string | null`, `activeAcademicPeriodId: string | null`.
  - Actions: `setActiveCampus(id: string | null)`, `setActiveAcademicPeriod(id: string | null)`.
- `src/frontend/src/app/stores/ui.store.ts` exports `useUIStore`:
  - State: `isSidebarCollapsed: boolean`, `activeTheme: 'light' | 'dark'`, `breadcrumbs: BreadcrumbItem[]`.
  - Actions: `toggleSidebar()`, `setSidebarCollapsed(collapsed: boolean)`, `setBreadcrumbs(items: BreadcrumbItem[])`.
- Unit tests in `src/frontend/src/app/stores/__tests__/auth.store.spec.ts` verify permission checking and session lifecycle.

Notes:
- Enforces strict state ownership: No backend database entities or lists are stored in Pinia.
- Created `src/frontend/src/app/stores/auth.store.ts` exporting `useAuthStore` managing session identity (`UserSession`), token, permissions, and roles, with getters (`isAuthenticated`, `hasPermission` supporting wildcards and SuperAdmin bypass, `hasAnyRole`, `hasRole`), actions (`setSession`, `logout`, `handleSessionExpired`, `restoreSession`, `persistSession`), and configurable persistence via `storage`.
- Created `src/frontend/src/app/stores/appContext.store.ts` exporting `useAppContextStore` managing `activeCampusId` and `activeAcademicPeriodId` with persistence and getters (`hasActiveCampus`, `hasActiveAcademicPeriod`, `isContextComplete`).
- Created `src/frontend/src/app/stores/ui.store.ts` exporting `useUIStore` managing `isSidebarCollapsed`, `activeTheme`, and `breadcrumbs` with storage synchronization.
- Created barrel export in `src/frontend/src/app/stores/index.ts`.
- Implemented comprehensive unit test suites in `auth.store.spec.ts`, `appContext.store.spec.ts`, and `ui.store.spec.ts` (25 passing tests in Vitest). Verified with `vue-tsc --noEmit` and ESLint.

---

### P3-S09

Title: Vue Router Configuration, Hierarchical Routes, and Permission Navigation Guards

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Configure Vue Router with HTML5 web history, lazy-loaded route records organized hierarchically by stage and domain context, and global navigation guards (`beforeEach`) enforcing authentication and actor permissions.

Depends On: P3-S08

Repository: elsasa-sis-v3

Completion Criteria:
- `src/frontend/src/app/router/routes.ts` exists declaring:
  - Root redirect `/` -> `/dashboard`
  - Auth route `/auth/login` (`auth.login`) using `AuthLayout`, `meta: { requiresAuth: false, title: 'Sign In — Elsasa SIS' }`
  - Authenticated shell wrapped in `AppLayout` with `meta: { requiresAuth: true }`:
    - `/dashboard` (`dashboard`) -> `DashboardView.vue`
    - `/operations/calendar` (`operations.calendar.workspace`) -> `CalendarWorkspaceView.vue`, `meta: { permission: 'Permissions.Operations.Calendar.Manage', title: 'Calendar & Periods Workspace' }`
    - Placeholder route groups for Students (`/students/admission`, `/students/enrollment`) and Finance (`/finance/obligations`, `/finance/payments`)
    - `/403` (`forbidden`) -> `ForbiddenView.vue`
    - `/:pathMatch(.*)*` (`not-found`) -> `NotFoundView.vue`
- `src/frontend/src/app/router/guards.ts` exports `setupNavigationGuards(router)`:
  - If `to.meta.requiresAuth !== false` and `!authStore.isAuthenticated`, redirects to `/auth/login?redirect=${encodeURIComponent(to.fullPath)}`.
  - If `to.meta.permission` is specified and `!authStore.hasPermission(to.meta.permission)`, redirects to `/403`.
  - Sets browser document title to `${to.meta.title} — Elsasa SIS`.
- `src/frontend/src/app/router/index.ts` creates and exports the configured router instance.
- Unit tests in `src/frontend/src/app/router/__tests__/guards.spec.ts` verify unauthenticated redirection and permission enforcement.

Notes:
- Enforces ADR-FE-007 UX defense while backend Minimal APIs remain the authoritative security boundary.
- Configured hierarchical route records in `src/frontend/src/app/router/routes.ts` covering root redirect (`/` -> `/dashboard`), unauthenticated auth layout (`/auth/login`), authenticated shell (`AppLayout` with `requiresAuth: true`), dashboard, operations calendar workspace with permission `Permissions.Operations.Calendar.Manage`, domain route groups for Students (`/students/admission`, `/students/enrollment`) and Finance (`/finance/obligations`, `/finance/payments`), and standard error handlers (`/403` and `/:pathMatch(.*)*`).
- Created placeholder layout shells (`AppLayout.vue`, `AuthLayout.vue`) and minimal view stubs across all routed contexts to support lazy loading with clean TypeScript and Vite bundling.
- Implemented global navigation guards in `src/frontend/src/app/router/guards.ts` enforcing unauthenticated redirection to `/auth/login?redirect=${encodeURIComponent(to.fullPath)}`, permission enforcement redirecting to `/403`, and dynamic document title formatting (`${to.meta.title} — Elsasa SIS`).
- Created and exported the router instance in `src/frontend/src/app/router/index.ts` with HTML5 web history, scroll behavior restoration, and global guards applied.
- Implemented 19 unit test cases in `src/frontend/src/app/router/__tests__/guards.spec.ts` covering unauthenticated redirection, query param preservation, permission checking, SuperAdmin/wildcard bypass, hierarchical route resolution, catch-all routing, and document title formatting. Passing all tests cleanly with Vitest.

---

## P4 - Shared UI Primitives & Application Layout Shells

Implementation Status: IMPLEMENTED  
Review Status: GO

### P4-S10

Title: Shared UI Primitives and Form Feedback Components

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement reusable, domain-agnostic UI presentation primitives under `src/frontend/src/shared/components/` utilizing PrimeVue primitives styled with Tailwind CSS v4 design tokens.

Depends On: P1-S03, P2-S05

Repository: elsasa-sis-v3

Completion Criteria:
- The following reusable components exist under `src/frontend/src/shared/components/`:
  - `forms/FormField.vue`: Accessible form field wrapper providing label, optional/required indicator, help text, and red validation error message text.
  - `feedback/LoadingSpinner.vue`: Accessible animated spinner with `sm`, `md`, `lg` size props.
  - `feedback/EmptyState.vue`: Generic empty state container supporting custom icon, title, description, and action button slot.
  - `layout/PageHeader.vue`: Consistent page header displaying view title, descriptive subtitle, breadcrumb navigation, and header action button slots.
  - `layout/ContentCard.vue`: Card container styling with slate borders, white background, and optional header/footer slots.
  - `StatusPill.vue`: Status badge component mapping domain status strings (`active`, `pending`, `accepted`, `rejected`, `closed`) to Emerald, Amber, Rose, or Slate badge styles.
- Components use `<script setup lang="ts">` with typed props and zero domain business dependencies.
- Component tests in `src/frontend/src/shared/components/__tests__/FormField.spec.ts` pass via `@vue/test-utils` and Vitest.

Notes:
- Provides the uniform UI building blocks required for consistent administrative screens across all SIS domains.
- Implemented accessible form feedback wrapper `FormField.vue` supporting label, required indicator (`*`), optional indicator (`(Optional)`), help text, and red validation error messages with WAI-ARIA alert and describedby attributes.
- Implemented `LoadingSpinner.vue` with `sm`, `md`, and `lg` sizing, brand color variants (`teal`, `primary`, `white`, `slate`), and screen-reader accessible attributes.
- Implemented `EmptyState.vue` supporting custom icon component, title, description, and actions slot with optional dashed border styling.
- Implemented `PageHeader.vue` providing view title, subtitle, semantic breadcrumb navigation hierarchy, and header action button slot.
- Implemented `ContentCard.vue` providing container styling with slate borders, white background, and optional header, title, header-actions, and footer slots.
- Implemented `StatusPill.vue` mapping domain status strings (`active`, `pending`, `accepted`, `rejected`, `closed`) to Emerald, Amber, Rose, and Slate institutional badge styles with optional dot indicators and casing normalization.
- Created centralized export index in `src/frontend/src/shared/components/index.ts`.
- Created comprehensive Vitest and Vue Test Utils unit test suites (`FormField.spec.ts`, `StatusPill.spec.ts`, `LoadingSpinner.spec.ts`, `EmptyState.spec.ts`, `PageHeader.spec.ts`, `ContentCard.spec.ts`) passing all 38 component unit tests (and 134 total frontend tests). Verified with `vue-tsc -b`, `vite build`, `lint`, and `prettier`.

---

### P4-S11

Title: Persistent Layout Shells and Root Application Shell Component

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the primary authenticated shell `AppLayout.vue`, distraction-free `AuthLayout.vue`, and the root application shell `App.vue` hosting global PrimeVue `Toast` and `ConfirmDialog` providers.

Depends On: P4-S10, P3-S08, P3-S09

Repository: elsasa-sis-v3

Completion Criteria:
- `src/frontend/src/app/layouts/AppLayout.vue` exists containing:
  - Top Navigation Bar: Institution branding ("Elsasa SIS"), active Academic Period indicator, campus selector placeholder, user profile menu with logout trigger, and sidebar toggle button.
  - Collapsible Sidebar: Hierarchical navigation links (Dashboard, Operations, Students, Finance) filtered dynamically via `authStore.hasPermission()`. Supports expanded and collapsed mini-sidebar modes.
  - Dynamic Breadcrumbs & Page Header region.
  - Main Routed Content: `<router-view>` wrapped in transition.
- `src/frontend/src/app/layouts/AuthLayout.vue` exists providing a centered, distraction-free container for authentication workflows.
- `src/frontend/src/App.vue` exists containing:
  - `<Toast position="top-right" />`
  - `<ConfirmDialog />`
  - `<router-view />`
- Component tests verify `AppLayout.vue` sidebar toggle and menu rendering.

Notes:
- Implemented root application shell `src/frontend/src/App.vue` hosting PrimeVue `<Toast position="top-right" />`, `<ConfirmDialog />`, and routed view `<router-view />`.
- Implemented centered, distraction-free authentication layout `src/frontend/src/app/layouts/AuthLayout.vue` with institutional branding, card container, and footer.
- Implemented primary administrative layout `src/frontend/src/app/layouts/AppLayout.vue` with:
  - Sticky Top Navigation Bar: institutional branding, academic period indicator badge, campus selector dropdown, user profile menu with logout action, and responsive sidebar toggle.
  - Collapsible Sidebar: full-height navigation with expanded (`w-64`) and collapsed mini-sidebar (`w-20`) modes, accordion grouping for Operations, Students, and Finance modules, and dynamic permission filtering driven by `authStore.hasPermission()`.
  - Dynamic Breadcrumbs & Page Header region: driven by `uiStore.breadcrumbs` or route metadata hierarchy.
  - Main content region: `<router-view>` wrapped in `<transition name="page" mode="out-in">` with subtle fade/slide animation.
- Created comprehensive component test suite `src/frontend/src/app/layouts/__tests__/AppLayout.spec.ts` (17 unit tests) covering sidebar toggling, topbar controls, permission-based link filtering, accordion group toggle, router view rendering, `AuthLayout.vue`, and `App.vue`.
- Verified all quality gates pass: `vitest` (170 tests passing across 19 files), `vue-tsc` (clean), `eslint` (0 errors), `prettier` (clean), and `vite build` (production assets generated cleanly).

---

## P5 - Minimal Application Shell & Integration Verification

Implementation Status: IMPLEMENTED  
Review Status: GO

### P5-S12

Title: Application Bootstrap Entry Point and Minimal Route Views

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Wire the Single Page Application entry point `main.ts`, register all providers, stores, and router, and implement minimal route views (`DashboardView.vue`, `LoginView.vue`, `ForbiddenView.vue`, `NotFoundView.vue`) establishing the runnable application shell.

Depends On: P4-S11, P3-S07

Repository: elsasa-sis-v3

Completion Criteria:
- `src/frontend/src/main.ts` exists and implements the complete bootstrap lifecycle:
  - Calls `createApp(App)`.
  - Registers `registerAppProviders(app)`.
  - Registers `router`.
  - Mounts root application `#app`.
- `src/frontend/src/features/dashboard/views/DashboardView.vue` exists:
  - Displays administrative dashboard with current session greeting, active campus/period indicator, system status tile, and navigation shortcuts to Operations.
- `src/frontend/src/features/auth/views/LoginView.vue` exists:
  - Provides login form (Username, Password) using VeeValidate and Zod validation.
  - Handles authentication submit (authenticating local session context or development mock) and redirects to target route.
- `src/frontend/src/shared/views/ForbiddenView.vue` exists (HTTP 403 access forbidden page with return action).
- `src/frontend/src/shared/views/NotFoundView.vue` exists (HTTP 404 page not found).
- Development server `pnpm --dir src/frontend dev` boots cleanly and serves the interactive application shell at `http://localhost:5173`.

Notes:
- Wired the Single Page Application bootstrap entry point in `src/frontend/src/main.ts`:
  - Implements complete lifecycle: `createApp(App)`, registers composition providers (Pinia, VueQuery, PrimeVue, ToastService, ConfirmationService), hydrates client authentication session (`authStore.restoreSession(true)`), configures and registers Vue Router, binds global error handling boundary to `logger.error`, and mounts root application component to `#app`.
- Implemented `src/features/dashboard/views/DashboardView.vue`:
  - Displays administrative dashboard with session greeting (user name, roles, user ID), active operational scope indicator (campus and academic period status), system health card (Operational, Minimal APIs & Dapper connected, version v0.1.0), and navigation shortcut cards to Operations (`/operations/calendar`) and Stage 1 core domain workspaces (Admission, Enrollment, Obligations, Payments).
- Implemented `src/features/auth/views/LoginView.vue` and `src/features/auth/schemas/login.schema.ts`:
  - Implemented schema-first form using Zod validation and VeeValidate `useForm` / `defineField` bindings.
  - Handles authentication submit for local session context / dev mock, stores session in `useAuthStore`, and redirects to requested target path (`route.query.redirect`) or defaults to `/dashboard`.
- Implemented `src/shared/views/ForbiddenView.vue`:
  - HTTP 403 authorization barrier page with `StatusPill`, clear access policy guidance, and Return to Dashboard / Go Back actions.
- Implemented `src/shared/views/NotFoundView.vue`:
  - HTTP 404 page not found view with `StatusPill`, resource missing explanation, and Return to Dashboard / Go Back navigation actions.
- Created comprehensive unit and component test suites:
  - `src/__tests__/main.spec.ts` (bootstrap lifecycle, plugin registration, session hydration, error telemetry)
  - `src/features/auth/schemas/__tests__/login.schema.spec.ts` (Zod schema credentials validation)
  - `src/features/auth/views/__tests__/LoginView.spec.ts` (rendering, VeeValidate form validation, session creation, redirection)
  - `src/features/dashboard/views/__tests__/DashboardView.spec.ts` (greeting, active context, system status, operations shortcuts)
  - `src/shared/views/__tests__/ForbiddenView.spec.ts` (HTTP 403 view and return navigation)
  - `src/shared/views/__tests__/NotFoundView.spec.ts` (HTTP 404 view and return navigation)
- Verified quality gates pass: `test:unit` (196 tests passing across 25 files), `typecheck` (clean), `lint` (0 errors), `format:check` (clean), `build` (production bundle built cleanly), and Vite dev server (`http://localhost:5173` boots cleanly and serves interactive SPA shell).

---

### P5-S13

Title: Backend Integration Verification Vertical Slice and Build Verification

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement an integration verification vertical slice in `features/operations/calendar/` to verify live end-to-end communication with the running ASP.NET Core backend API (`/health` and `POST /api/operations/academic-years`), verifying HTTP proxying, correlation ID header propagation, 201 Created response handling, and 422 `ProblemDetails` validation error mapping.

Depends On: P5-S12, P2-S06

Repository: elsasa-sis-v3

Completion Criteria:
- `src/frontend/src/features/operations/calendar/api/academicYear.api.ts` exists providing:
  - `checkBackendHealth(): Promise<{ status: string }>` -> calls `GET /health`.
  - `createAcademicYear(payload: CreateAcademicYearPayload): Promise<AcademicYearDto>` -> calls `POST /api/operations/academic-years`.
- `src/frontend/src/features/operations/calendar/schemas/createAcademicYear.schema.ts` exists:
  - Zod schema mirroring backend FluentValidation rules in `CreateAcademicYearValidator.cs`:
    - `code`: required, 3..50 chars
    - `name`: required, 3..100 chars
    - `startDate`: required date format YYYY-MM-DD
    - `endDate`: required date format YYYY-MM-DD, must be strictly after `startDate`
- `src/frontend/src/features/operations/calendar/views/CalendarWorkspaceView.vue` exists:
  - Displays backend health connectivity status (live `/health` check).
  - Provides interactive `Create Academic Year` test form.
  - Validates client-side constraints via VeeValidate and Zod.
  - Submits payload to backend Minimal API:
    - On HTTP 201 Created: displays success toast with created Academic Year ID.
    - On HTTP 422 ProblemDetails (e.g. invalid date range or missing code): receives `ApiError`, maps field errors onto form fields, and displays error toast.
- Automated Vitest integration test `src/frontend/src/features/operations/calendar/__tests__/academicYear.api.spec.ts` verifies:
  - Successful API payload transmission and response unboxing.
  - RFC 7807 422 validation error handling and field error unbundling.
  - Request tracing headers (`X-Correlation-ID`).
- Full quality gate verification succeeds:
  - `pnpm --dir src/frontend typecheck` passes with 0 errors.
  - `pnpm --dir src/frontend lint` passes with 0 warnings and 0 errors.
  - `pnpm --dir src/frontend test:unit` passes 100% of tests.
  - `pnpm --dir src/frontend build` produces optimized production bundle in `src/frontend/dist/`.

Notes:
- Implemented backend API contract service in `src/frontend/src/features/operations/calendar/api/academicYear.api.ts`:
  - `checkBackendHealth()`: probes `GET /health` with correlation ID tracing header propagation and unboxes plain text or object status payload.
  - `createAcademicYear()`: dispatches typed `CreateAcademicYearPayload` to `POST /api/operations/academic-years`, propagating `X-Correlation-ID` UUID v4 header and unboxing `AcademicYearDto` on HTTP 201 Created.
- Implemented validation schema in `src/frontend/src/features/operations/calendar/schemas/createAcademicYear.schema.ts`:
  - Aligns client-side Zod constraints with backend `CreateAcademicYearValidator.cs` (`code` 3..50 chars, `name` 3..100 chars, `startDate` ISO YYYY-MM-DD, `endDate` ISO YYYY-MM-DD strictly greater than `startDate`).
- Implemented interactive operational view in `src/frontend/src/features/operations/calendar/views/CalendarWorkspaceView.vue`:
  - Displays live backend connectivity status card backed by `checkBackendHealth` with manual refresh trigger and `StatusPill` indicators.
  - Interactive `Create Academic Year` form integrated with VeeValidate `useForm` and Zod `toTypedSchema`.
  - Maps successful HTTP 201 Created responses to success toasts and session history records.
  - Intercepts RFC 7807 HTTP 422 ProblemDetails, translates `ApiError` field validation dictionaries onto form fields, and displays error toast and problem banner.
- Configured dev server proxy route in `vite.config.ts` to proxy both `/api` and `/health` to `http://localhost:5000`.
- Added automated Vitest suites:
  - `src/features/operations/calendar/__tests__/academicYear.api.spec.ts` (API transmission, response unboxing, correlation header propagation, 422 ProblemDetails unbundling).
  - `src/features/operations/calendar/schemas/__tests__/createAcademicYear.schema.spec.ts` (Zod schema boundary constraints and date range checks).
  - `src/features/operations/calendar/views/__tests__/CalendarWorkspaceView.spec.ts` (component rendering, health probe lifecycle, VeeValidate client validation, 201 success handling, 422 server error mapping).
- Verified full quality gates pass:
  - `pnpm --dir src/frontend typecheck`: 0 errors.
  - `pnpm --dir src/frontend lint`: 0 warnings, 0 errors.
  - `pnpm --dir src/frontend format:check`: 100% clean formatting.
  - `pnpm --dir src/frontend test:unit`: 222 tests passing across 28 test files (100%).
  - `pnpm --dir src/frontend build`: production bundle generated in `src/frontend/dist/` with vendor chunk splitting.

---

# 6. Change Log

| Version | Date | Author | Description of Changes |
|---|---|---|---|
| 1.0 | 2026-11-02 | ica-architect | Initial approved frontend initialization implementation plan covering workspace scaffolding, build pipeline, styling engine, shared abstractions, HTTP client, state management, layouts, and backend integration verification vertical slice. |
