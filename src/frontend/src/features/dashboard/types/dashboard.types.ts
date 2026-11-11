/**
 * Canonical TypeScript contracts for Level 1 Executive and Level 2 Domain KPI Dashboards.
 * Mirrors backend DTO records in `Elsasa.Sis.Contracts/Kpis/KpiContracts.cs`
 * with ASP.NET Core camelCase JSON serialization.
 */

export type KpiHealthStatus = 'healthy' | 'warning' | 'critical' | 'neutral';

export type KpiDomainStatus = 'healthy' | 'warning' | 'critical' | 'pending_stage';

export type KpiTrendDirection = 'up' | 'down' | 'flat';

export type KpiUnit = 'count' | 'percentage' | 'currency' | 'days';

export type KpiChartType = 'horizontal-bar' | 'stacked-bar' | 'funnel';

export type KpiAttentionSeverity = 'critical' | 'warning';

export type OperationalKpiDomainKey =
  | 'students'
  | 'academics'
  | 'finance'
  | 'teaching'
  | 'operations';

export type KpiDomainKey = OperationalKpiDomainKey | 'development';

export type SchoolLevelCode = 'ELC' | 'JHLC' | 'HSLC';

export type CampusCode = 'campus-main' | 'campus-east';

export interface GlobalKpiFilterDto {
  academicYearId?: string | null | undefined;
  academicPeriodId?: string | null | undefined;
  campus?: string | null | undefined;
  schoolLevel?: string | null | undefined;
}

export interface ResolvedKpiContextDto {
  academicYearId: string;
  academicYearCode: string;
  academicPeriodId: string;
  academicPeriodName: string;
  priorAcademicPeriodId: string | null;
  priorAcademicPeriodName: string | null;
  campus: string | null;
  schoolLevel: string | null;
}

export interface KpiPeriodOptionDto {
  id: string;
  code: string;
  name: string;
  status: string;
  academicYearId: string;
  academicYearCode: string;
}

export interface KpiMetricCardDto {
  code: string;
  label: string;
  domain: string;
  value: number;
  formattedValue: string;
  unit: KpiUnit | string;
  priorValue: number | null;
  deltaPercent: number | null;
  deltaLabel: string | null;
  trendDirection: KpiTrendDirection | string;
  status: KpiHealthStatus | string;
  targetLabel: string | null;
  subtitle: string | null;
  drillDownRoute: string;
}

export interface KpiBreakdownItemDto {
  key: string;
  label: string;
  count: number;
  percentage: number;
  amount: number | null;
  formattedAmount: string | null;
  status: KpiHealthStatus | string;
  drillDownRoute: string;
}

export interface KpiBreakdownDto {
  code: string;
  title: string;
  subtitle: string;
  chartType: KpiChartType | string;
  items: KpiBreakdownItemDto[];
}

export interface AttentionSignalDto {
  signalCode: string;
  domain: string;
  severity: KpiAttentionSeverity | string;
  title: string;
  description: string;
  affectedCount: number;
  impactLabel: string;
  actionLabel: string;
  drillDownRoute: string;
}

export interface DomainHealthSummaryDto {
  domainKey: KpiDomainKey | string;
  domainName: string;
  status: KpiDomainStatus | string;
  statusLabel: string;
  headlineLabel: string;
  headlineValue: string;
  secondaryLabel: string;
  secondaryValue: string;
  warningCount: number;
  criticalCount: number;
  dashboardRoute: string;
  isAccessible: boolean;
}

export interface ExecutiveDashboardResponse {
  generatedAtUtc: string;
  context: ResolvedKpiContextDto;
  availablePeriods: KpiPeriodOptionDto[];
  headlineKpis: KpiMetricCardDto[];
  domainSummaries: DomainHealthSummaryDto[];
  needsAttentionQueue: AttentionSignalDto[];
}

export interface DomainKpiDashboardResponse {
  domainKey: string;
  domainName: string;
  generatedAtUtc: string;
  context: ResolvedKpiContextDto;
  availablePeriods: KpiPeriodOptionDto[];
  headlineKpis: KpiMetricCardDto[];
  breakdowns: KpiBreakdownDto[];
  attentionSignals: AttentionSignalDto[];
}
