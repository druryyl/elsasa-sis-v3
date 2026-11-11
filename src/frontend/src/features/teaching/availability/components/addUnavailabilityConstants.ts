import type { UnavailabilityType } from '../types';

export interface LeaveTypeOption {
  label: string;
  value: UnavailabilityType;
  description: string;
}

export const LEAVE_TYPE_OPTIONS: LeaveTypeOption[] = [
  {
    label: 'Medical Leave',
    value: 'medical_leave',
    description: 'Illness, hospitalization, or doctor-mandated medical rest',
  },
  {
    label: 'Official Training',
    value: 'official_training',
    description: 'Mandatory ministry training, workshop, or professional seminar',
  },
  {
    label: 'Personal Leave',
    value: 'personal_leave',
    description: 'Authorized personal leave, family emergency, or pilgrimage',
  },
  {
    label: 'Blackout Window',
    value: 'blackout',
    description: 'Institutional blackout period or pre-agreed unavailable block',
  },
];
