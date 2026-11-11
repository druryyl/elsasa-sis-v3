import type { StatusVariant } from '@/shared/components/StatusPill.vue';

export interface KpiSemanticStyle {
  pillVariant: StatusVariant;
  accentBarClass: string;
  barFillClass: string;
  svgFillHex: string;
  dotClass: string;
  badgeClass: string;
  statusLabel: string;
}

const HEALTH_STYLE_MAP: Record<string, KpiSemanticStyle> = {
  healthy: {
    pillVariant: 'emerald',
    accentBarClass: 'from-emerald-500 via-teal-500 to-emerald-600',
    barFillClass: 'bg-emerald-500',
    svgFillHex: '#10b981',
    dotClass: 'bg-emerald-500',
    badgeClass: 'bg-emerald-50 text-emerald-700 border-emerald-200/80',
    statusLabel: 'Healthy',
  },
  warning: {
    pillVariant: 'amber',
    accentBarClass: 'from-amber-400 via-orange-400 to-amber-500',
    barFillClass: 'bg-amber-500',
    svgFillHex: '#f59e0b',
    dotClass: 'bg-amber-500',
    badgeClass: 'bg-amber-50 text-amber-700 border-amber-200/80',
    statusLabel: 'Warning',
  },
  critical: {
    pillVariant: 'rose',
    accentBarClass: 'from-rose-500 via-red-500 to-rose-600',
    barFillClass: 'bg-rose-500',
    svgFillHex: '#f43f5e',
    dotClass: 'bg-rose-500',
    badgeClass: 'bg-rose-50 text-rose-700 border-rose-200/80',
    statusLabel: 'Critical',
  },
  neutral: {
    pillVariant: 'sky',
    accentBarClass: 'from-sky-500 via-blue-500 to-indigo-500',
    barFillClass: 'bg-sky-500',
    svgFillHex: '#0ea5e9',
    dotClass: 'bg-sky-500',
    badgeClass: 'bg-sky-50 text-sky-700 border-sky-200/80',
    statusLabel: 'Neutral',
  },
  pending_stage: {
    pillVariant: 'slate',
    accentBarClass: 'from-slate-300 via-slate-400 to-slate-300',
    barFillClass: 'bg-slate-400',
    svgFillHex: '#94a3b8',
    dotClass: 'bg-slate-400',
    badgeClass: 'bg-slate-100 text-slate-600 border-slate-200',
    statusLabel: 'Stage 3 Pending',
  },
};

export function resolveKpiSemanticStyle(status: string | null | undefined): KpiSemanticStyle {
  const normalized = (status ?? '').toLowerCase().trim();
  return (
    HEALTH_STYLE_MAP[normalized] ?? {
      pillVariant: 'slate',
      accentBarClass: 'from-slate-400 via-slate-500 to-slate-400',
      barFillClass: 'bg-slate-500',
      svgFillHex: '#64748b',
      dotClass: 'bg-slate-400',
      badgeClass: 'bg-slate-100 text-slate-700 border-slate-200',
      statusLabel: normalized || 'Neutral',
    }
  );
}

/**
 * Clamps a numeric percentage safely into [0, 100] for SVG/CSS bar widths.
 */
export function clampPercentage(value: number): number {
  if (!Number.isFinite(value)) return 0;
  return Math.max(0, Math.min(100, value));
}
