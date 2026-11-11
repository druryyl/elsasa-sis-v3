import { apiClient } from '@/shared/services/http';
import type {
  DomainKpiDashboardResponse,
  ExecutiveDashboardResponse,
  GlobalKpiFilterDto,
  OperationalKpiDomainKey,
} from '../types/dashboard.types';

function buildKpiFilterParams(filter?: GlobalKpiFilterDto): Record<string, string> {
  const params: Record<string, string> = {};

  if (filter?.academicYearId?.trim()) {
    params.academicYearId = filter.academicYearId.trim();
  }
  if (filter?.academicPeriodId?.trim()) {
    params.academicPeriodId = filter.academicPeriodId.trim();
  }
  if (filter?.campus?.trim()) {
    params.campus = filter.campus.trim();
  }
  if (filter?.schoolLevel?.trim()) {
    params.schoolLevel = filter.schoolLevel.trim();
  }

  return params;
}

/**
 * Retrieves the Level 1 Executive Overview KPI dashboard payload.
 * GET /api/dashboard/executive
 */
export async function getExecutiveDashboard(
  filter?: GlobalKpiFilterDto,
): Promise<ExecutiveDashboardResponse> {
  const response = (await apiClient.get('/dashboard/executive', {
    params: buildKpiFilterParams(filter),
  })) as unknown as ExecutiveDashboardResponse;

  return response;
}

/**
 * Retrieves a Level 2 Domain KPI dashboard payload for the specified operational domain.
 * GET /api/{domain}/kpis/dashboard
 */
export async function getDomainKpiDashboard(
  domain: OperationalKpiDomainKey | string,
  filter?: GlobalKpiFilterDto,
): Promise<DomainKpiDashboardResponse> {
  const response = (await apiClient.get(`/${encodeURIComponent(domain)}/kpis/dashboard`, {
    params: buildKpiFilterParams(filter),
  })) as unknown as DomainKpiDashboardResponse;

  return response;
}
