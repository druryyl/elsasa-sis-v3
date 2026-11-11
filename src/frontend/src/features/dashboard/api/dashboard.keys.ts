import type { GlobalKpiFilterDto } from '../types/dashboard.types';

function normalizeKpiFilter(filter?: GlobalKpiFilterDto) {
  return {
    academicYearId: filter?.academicYearId ?? null,
    academicPeriodId: filter?.academicPeriodId ?? null,
    campus: filter?.campus ?? null,
    schoolLevel: filter?.schoolLevel ?? null,
  };
}

/**
 * Hierarchical TanStack Query key factory for Level 1 Executive and Level 2 Domain KPI dashboards.
 */
export const dashboardKeys = {
  all: ['dashboard'] as const,
  executives: () => [...dashboardKeys.all, 'executive'] as const,
  executive: (filter?: GlobalKpiFilterDto) =>
    [...dashboardKeys.executives(), normalizeKpiFilter(filter)] as const,
  domains: () => [...dashboardKeys.all, 'domain'] as const,
  domainRoot: (domain: string) => [...dashboardKeys.domains(), domain] as const,
  domain: (domain: string, filter?: GlobalKpiFilterDto) =>
    [...dashboardKeys.domainRoot(domain), normalizeKpiFilter(filter)] as const,
};
