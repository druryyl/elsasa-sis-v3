import type { WorkloadCommitmentStatus } from '../types';
import type { StatusVariant } from '@/shared/components';

export interface PeriodOption {
  id: string;
  label: string;
  startDate: string;
  endDate: string;
  isActive?: boolean;
}

export const PERIOD_PRESETS: PeriodOption[] = [
  {
    id: '00000000-0000-0000-0002-000000000002',
    label: 'Semester 2 TP 2026/2027 (Active)',
    startDate: '2027-01-05',
    endDate: '2027-06-25',
    isActive: true,
  },
  {
    id: '00000000-0000-0000-0002-000000000001',
    label: 'Semester 1 TP 2026/2027 (Past)',
    startDate: '2026-07-15',
    endDate: '2026-12-20',
  },
  {
    id: '00000000-0000-0000-0002-000000000003',
    label: 'Semester 1 TP 2027/2028 (Planned)',
    startDate: '2027-07-15',
    endDate: '2027-12-20',
  },
];

export interface WorkloadStatusMeta {
  label: string;
  variant: StatusVariant;
  colorClass: string;
  badgeClass: string;
  barColorClass: string;
  description: string;
}

export const WORKLOAD_STATUS_MAP: Record<WorkloadCommitmentStatus, WorkloadStatusMeta> = {
  normal: {
    label: 'Normal Load',
    variant: 'emerald',
    colorClass: 'text-emerald-700 bg-emerald-50 border-emerald-200',
    badgeClass: 'bg-emerald-100 text-emerald-800 border-emerald-200',
    barColorClass: 'bg-emerald-500',
    description: 'Workload within institutional limits (up to warning threshold).',
  },
  under_allocated: {
    label: 'Under Allocated',
    variant: 'sky',
    colorClass: 'text-sky-700 bg-sky-50 border-sky-200',
    badgeClass: 'bg-sky-100 text-sky-800 border-sky-200',
    barColorClass: 'bg-sky-500',
    description: 'Available capacity remaining; faculty is under-utilized.',
  },
  approaching_limit: {
    label: 'Approaching Limit',
    variant: 'amber',
    colorClass: 'text-amber-700 bg-amber-50 border-amber-200',
    badgeClass: 'bg-amber-100 text-amber-800 border-amber-200',
    barColorClass: 'bg-amber-500',
    description: 'Workload in warning zone (approaching prohibited capacity).',
  },
  overloaded_with_exception: {
    label: 'Overloaded (Exception)',
    variant: 'slate',
    colorClass: 'text-purple-700 bg-purple-50 border-purple-200',
    badgeClass: 'bg-purple-100 text-purple-800 border-purple-200',
    barColorClass: 'bg-purple-600',
    description: 'Overload authorized via approved workload exception.',
  },
  breached: {
    label: 'Limit Breached',
    variant: 'rose',
    colorClass: 'text-rose-700 bg-rose-50 border-rose-200',
    badgeClass: 'bg-rose-100 text-rose-800 border-rose-200',
    barColorClass: 'bg-rose-600',
    description: 'Workload exceeds prohibited ceiling without authorized exception.',
  },
};

export const WORKLOAD_PERMISSIONS = {
  VIEW: 'Permissions.Teaching.Workload.View',
  RECALCULATE: 'Permissions.Teaching.Workload.Recalculate',
  REQUEST_EXCEPTION: 'Permissions.Teaching.Workload.RequestException',
  APPROVE_EXCEPTION: 'Permissions.Teaching.Workload.ApproveException',
} as const;

export const STANDARD_BASELINE_HOURS = 24;
export const STANDARD_WARNING_HOURS = 20;
