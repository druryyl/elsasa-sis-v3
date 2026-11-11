import type { TeachingDemandStatus, TeacherEligibilityStatus } from '../types';
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

export interface CampusOption {
  label: string;
  value: string;
}

export const CAMPUS_OPTIONS: CampusOption[] = [
  { label: 'All Campuses', value: 'all' },
  { label: 'Kampus Utama', value: 'Kampus Utama' },
  { label: 'Kampus Barat', value: 'Kampus Barat' },
  { label: 'Kampus Timur', value: 'Kampus Timur' },
];

export const TEACHING_ASSIGNMENT_PERMISSIONS = {
  VIEW: 'Permissions.Teaching.Assignments.View',
  MANAGE: 'Permissions.Teaching.Assignments.Manage',
  APPROVE: 'Permissions.Teaching.Assignments.Approve',
  SUBSTITUTIONS_ARRANGE: 'Permissions.Teaching.Substitutions.Arrange',
  SUBSTITUTIONS_APPROVE: 'Permissions.Teaching.Substitutions.ApproveException',
  WORKLOAD_VIEW: 'Permissions.Teaching.Workload.View',
} as const;

export interface DemandStatusMeta {
  label: string;
  variant: StatusVariant;
  badgeClass: string;
  description: string;
}

export const DEMAND_STATUS_MAP: Record<TeachingDemandStatus, DemandStatusMeta> = {
  received: {
    label: 'Received',
    variant: 'sky',
    badgeClass: 'bg-sky-100 text-sky-800 border-sky-200 dark:bg-sky-950 dark:text-sky-300 dark:border-sky-800',
    description: 'Demand received from originating domain, awaiting coordinator review.',
  },
  accepted_for_staffing: {
    label: 'Accepted for Staffing',
    variant: 'emerald',
    badgeClass: 'bg-emerald-100 text-emerald-800 border-emerald-200 dark:bg-emerald-950 dark:text-emerald-300 dark:border-emerald-800',
    description: 'Demand accepted; ready for candidate eligibility evaluation and staffing.',
  },
  returned_for_clarification: {
    label: 'Clarification Needed',
    variant: 'amber',
    badgeClass: 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-950 dark:text-amber-300 dark:border-amber-800',
    description: 'Demand returned to originator requesting clarified syllabus, effort, or dates.',
  },
  changed: {
    label: 'Changed',
    variant: 'amber',
    badgeClass: 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-950 dark:text-amber-300 dark:border-amber-800',
    description: 'Demand requirements modified by requesting domain.',
  },
  cancelled: {
    label: 'Cancelled',
    variant: 'rose',
    badgeClass: 'bg-rose-100 text-rose-800 border-rose-200 dark:bg-rose-950 dark:text-rose-300 dark:border-rose-800',
    description: 'Demand cancelled by originator.',
  },
  declined: {
    label: 'Declined',
    variant: 'rose',
    badgeClass: 'bg-rose-100 text-rose-800 border-rose-200 dark:bg-rose-950 dark:text-rose-300 dark:border-rose-800',
    description: 'Demand formally declined with recorded justification.',
  },
  staffed: {
    label: 'Staffed',
    variant: 'slate',
    badgeClass: 'bg-slate-100 text-slate-800 border-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:border-slate-700',
    description: 'All required teacher positions successfully staffed with approved assignments.',
  },
};

export interface EligibilityStatusMeta {
  label: string;
  variant: StatusVariant;
  colorClass: string;
  description: string;
}

export const ELIGIBILITY_STATUS_MAP: Record<TeacherEligibilityStatus, EligibilityStatusMeta> = {
  eligible: {
    label: 'Fully Eligible',
    variant: 'emerald',
    colorClass: 'text-emerald-700 bg-emerald-50 border-emerald-200 dark:bg-emerald-950 dark:text-emerald-300 dark:border-emerald-800',
    description: 'All 5 institutional policy checks satisfied with active credentials.',
  },
  eligible_under_supervision: {
    label: 'Eligible (Supervision)',
    variant: 'amber',
    colorClass: 'text-amber-700 bg-amber-50 border-amber-200 dark:bg-amber-950 dark:text-amber-300 dark:border-amber-800',
    description: 'Eligible with condition: requires assigned mentor or supervisory co-teacher.',
  },
  ineligible_unqualified: {
    label: 'Ineligible (Unqualified)',
    variant: 'rose',
    colorClass: 'text-rose-700 bg-rose-50 border-rose-200 dark:bg-rose-950 dark:text-rose-300 dark:border-rose-800',
    description: 'Candidate lacks active profile standing, matching authorization, or verified qualification.',
  },
  ineligible_conflicted: {
    label: 'Ineligible (Conflicted)',
    variant: 'rose',
    colorClass: 'text-rose-700 bg-rose-50 border-rose-200 dark:bg-rose-950 dark:text-rose-300 dark:border-rose-800',
    description: 'Candidate has an overlapping unavailability window or weekly schedule conflict.',
  },
  ineligible_capacity_exceeded: {
    label: 'Ineligible (Capacity Exceeded)',
    variant: 'rose',
    colorClass: 'text-rose-700 bg-rose-50 border-rose-200 dark:bg-rose-950 dark:text-rose-300 dark:border-rose-800',
    description: 'Committed workload exceeds policy limits without an approved exception.',
  },
};

export interface CandidateFacultyOption {
  id: string;
  teacherNumber: string;
  fullName: string;
  standing: string;
  primaryCapability: string;
  campusAffiliation: string;
}

export const DEFAULT_FACULTY_CANDIDATES: CandidateFacultyOption[] = [
  {
    id: '00000000-0000-0000-0006-000000000001',
    teacherNumber: 'TCH-001',
    fullName: 'Budi Hartono, M.Pd.',
    standing: 'active',
    primaryCapability: 'CAP-MATH-PRI',
    campusAffiliation: 'Kampus Utama',
  },
  {
    id: '00000000-0000-0000-0006-000000000002',
    teacherNumber: 'TCH-002',
    fullName: 'Siti Nurhaliza, S.Pd.',
    standing: 'active',
    primaryCapability: 'CAP-ENG-SEC',
    campusAffiliation: 'Kampus Utama',
  },
  {
    id: '00000000-0000-0000-0006-000000000003',
    teacherNumber: 'TCH-003',
    fullName: 'John Miller, M.A.',
    standing: 'active',
    primaryCapability: 'CAP-ENG-SEC',
    campusAffiliation: 'Kampus Barat',
  },
  {
    id: '00000000-0000-0000-0006-000000000004',
    teacherNumber: 'TCH-004',
    fullName: 'Hendra Setiawan, S.Pd.',
    standing: 'pending_approval',
    primaryCapability: 'CAP-SCI-PRI',
    campusAffiliation: 'Kampus Timur',
  },
];

export interface ItemStatusMeta {
  label: string;
  variant: StatusVariant;
  badgeClass: string;
}

export const ASSIGNMENT_STATUS_MAP: Record<string, ItemStatusMeta> = {
  proposed: {
    label: 'Proposed',
    variant: 'amber',
    badgeClass: 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-950 dark:text-amber-300 dark:border-amber-800',
  },
  approved: {
    label: 'Approved',
    variant: 'emerald',
    badgeClass: 'bg-emerald-100 text-emerald-800 border-emerald-200 dark:bg-emerald-950 dark:text-emerald-300 dark:border-emerald-800',
  },
  rejected: {
    label: 'Rejected',
    variant: 'rose',
    badgeClass: 'bg-rose-100 text-rose-800 border-rose-200 dark:bg-rose-950 dark:text-rose-300 dark:border-rose-800',
  },
  withdrawn: {
    label: 'Withdrawn',
    variant: 'slate',
    badgeClass: 'bg-slate-100 text-slate-800 border-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:border-slate-700',
  },
  returned_for_revision: {
    label: 'Revision Needed',
    variant: 'amber',
    badgeClass: 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-950 dark:text-amber-300 dark:border-amber-800',
  },
  suspended: {
    label: 'Suspended',
    variant: 'rose',
    badgeClass: 'bg-rose-100 text-rose-800 border-rose-200 dark:bg-rose-950 dark:text-rose-300 dark:border-rose-800',
  },
  cancelled: {
    label: 'Cancelled',
    variant: 'slate',
    badgeClass: 'bg-slate-100 text-slate-800 border-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:border-slate-700',
  },
  completed: {
    label: 'Completed',
    variant: 'indigo',
    badgeClass: 'bg-indigo-100 text-indigo-800 border-indigo-200 dark:bg-indigo-950 dark:text-indigo-300 dark:border-indigo-800',
  },
};

export const SUBSTITUTION_STATUS_MAP: Record<string, ItemStatusMeta> = {
  proposed: {
    label: 'Proposed (Escalated)',
    variant: 'amber',
    badgeClass: 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-950 dark:text-amber-300 dark:border-amber-800',
  },
  approved: {
    label: 'Approved (Pre-Authorized)',
    variant: 'emerald',
    badgeClass: 'bg-emerald-100 text-emerald-800 border-emerald-200 dark:bg-emerald-950 dark:text-emerald-300 dark:border-emerald-800',
  },
  active: {
    label: 'Active Coverage',
    variant: 'sky',
    badgeClass: 'bg-sky-100 text-sky-800 border-sky-200 dark:bg-sky-950 dark:text-sky-300 dark:border-sky-800',
  },
  completed: {
    label: 'Completed',
    variant: 'slate',
    badgeClass: 'bg-slate-100 text-slate-800 border-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:border-slate-700',
  },
  cancelled: {
    label: 'Cancelled',
    variant: 'rose',
    badgeClass: 'bg-rose-100 text-rose-800 border-rose-200 dark:bg-rose-950 dark:text-rose-300 dark:border-rose-800',
  },
};

export const REASSIGNMENT_STATUS_MAP: Record<string, ItemStatusMeta> = {
  proposed: {
    label: 'Proposed',
    variant: 'amber',
    badgeClass: 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-950 dark:text-amber-300 dark:border-amber-800',
  },
  approved: {
    label: 'Approved',
    variant: 'emerald',
    badgeClass: 'bg-emerald-100 text-emerald-800 border-emerald-200 dark:bg-emerald-950 dark:text-emerald-300 dark:border-emerald-800',
  },
  executed: {
    label: 'Executed',
    variant: 'sky',
    badgeClass: 'bg-sky-100 text-sky-800 border-sky-200 dark:bg-sky-950 dark:text-sky-300 dark:border-sky-800',
  },
  cancelled: {
    label: 'Cancelled',
    variant: 'rose',
    badgeClass: 'bg-rose-100 text-rose-800 border-rose-200 dark:bg-rose-950 dark:text-rose-300 dark:border-rose-800',
  },
};

export const HANDOVER_STATUS_MAP: Record<string, ItemStatusMeta> = {
  pending: {
    label: 'Handover Pending',
    variant: 'amber',
    badgeClass: 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-950 dark:text-amber-300 dark:border-amber-800',
  },
  completed: {
    label: 'Handover Completed',
    variant: 'emerald',
    badgeClass: 'bg-emerald-100 text-emerald-800 border-emerald-200 dark:bg-emerald-950 dark:text-emerald-300 dark:border-emerald-800',
  },
  waived: {
    label: 'Handover Waived',
    variant: 'slate',
    badgeClass: 'bg-slate-100 text-slate-800 border-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:border-slate-700',
  },
};

export const COMPLETION_STATUS_MAP: Record<string, ItemStatusMeta> = {
  completed: {
    label: 'Completed',
    variant: 'emerald',
    badgeClass: 'bg-emerald-100 text-emerald-800 border-emerald-200 dark:bg-emerald-950 dark:text-emerald-300 dark:border-emerald-800',
  },
  completed_with_exceptions: {
    label: 'Completed with Exceptions',
    variant: 'amber',
    badgeClass: 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-950 dark:text-amber-300 dark:border-amber-800',
  },
};

export const SCOPE_CATEGORY_OPTIONS = [
  { label: 'Learning Unit (Mata Pelajaran)', value: 'learning_unit' },
  { label: 'Cohort Group (Kelas / Wali Kelas)', value: 'cohort_group' },
  { label: 'Individual Student (Privat / Bimbingan)', value: 'individual_student' },
  { label: 'Extracurricular Group (Ekskul)', value: 'extracurricular_group' },
];

export const UNRESOLVED_ITEMS_OPTIONS = [
  { label: 'None (Clean Concluded)', value: 'none' },
  { label: 'Minor Pending Items', value: 'minor_pending' },
  { label: 'Escalated Defect', value: 'escalated_defect' },
  { label: 'Resolved During Review', value: 'resolved' },
  { label: 'Escalated to Leadership', value: 'escalated' },
];

