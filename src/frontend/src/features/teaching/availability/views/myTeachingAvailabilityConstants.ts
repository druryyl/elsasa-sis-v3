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

export interface TeacherPersonaOption {
  id: string;
  name: string;
  role: string;
}

export const DEMO_TEACHER_PERSONAS: TeacherPersonaOption[] = [
  {
    id: '00000000-0000-0000-0006-000000000001',
    name: 'Budi Hartono, M.Pd.',
    role: 'Guru Utama (Matematika)',
  },
  {
    id: '00000000-0000-0000-0006-000000000002',
    name: 'Siti Nurhaliza, S.Pd.',
    role: 'Guru Pendamping (Bahasa Inggris)',
  },
  {
    id: '00000000-0000-0000-0006-000000000003',
    name: 'John Miller, Ph.D.',
    role: 'Dosen Tamu / Visiting Lecturer',
  },
  {
    id: '00000000-0000-0000-0006-000000000004',
    name: 'Hendra Setiawan, S.Si.',
    role: 'Guru Sains (Calon)',
  },
];
