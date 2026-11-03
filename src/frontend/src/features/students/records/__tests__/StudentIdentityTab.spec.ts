// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import PrimeVue from 'primevue/config';
import StudentIdentityTab from '../components/StudentIdentityTab.vue';
import type { StudentDetail } from '../types/records.types';

const mockStudentIdentity: StudentDetail = {
  id: 'student-200',
  studentNumber: 'STD-2026-0099',
  applicantId: 'applicant-200',
  fullName: 'Aisyah Putri Permata',
  preferredName: 'Aisyah',
  dateOfBirth: '2017-09-15',
  placeOfBirth: 'Surabaya',
  gender: 'Female',
  nationality: 'Indonesian',
  governmentIdType: 'NIK',
  governmentIdNumber: '3578010000000099',
  address: 'Jl. Pemuda No. 12, Surabaya',
  phone: '081388887777',
  email: 'aisyah.putri@example.com',
  isIndependent: false,
  currentStatus: 'active',
  enrollmentDate: '2026-07-01',
  notes: 'Transferred from overseas school.',
  createdAt: '2026-07-01T08:00:00Z',
  createdBy: 'admissions_officer',
  updatedAt: '2026-07-10T14:30:00Z',
  updatedBy: 'records_officer',
  enrollmentHistory: [],
  guardianRelationships: [],
  documents: [],
};

describe('StudentIdentityTab.spec.ts (SCR-STU-02 Certified Identity Tab)', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  function mountTab(props = {}) {
    return mount(StudentIdentityTab, {
      props: {
        student: mockStudentIdentity,
        canEdit: true,
        ...props,
      },
      global: {
        plugins: [PrimeVue],
      },
    });
  }

  it('renders certified biographical attributes and government ID (NIK)', () => {
    const wrapper = mountTab();

    expect(wrapper.find('[data-testid="identity-full-name"]').text()).toBe('Aisyah Putri Permata');
    expect(wrapper.find('[data-testid="identity-preferred-name"]').text()).toBe('Aisyah');
    expect(wrapper.find('[data-testid="identity-gov-id-type"]').text()).toBe('NIK');
    expect(wrapper.find('[data-testid="identity-government-id"]').text()).toBe('3578010000000099');
    expect(wrapper.find('[data-testid="identity-dob-pob"]').text()).toContain('Surabaya');
    expect(wrapper.find('[data-testid="identity-gender-nationality"]').text()).toContain('Female • Indonesian');
  });

  it('renders residential address and contact details', () => {
    const wrapper = mountTab();

    expect(wrapper.find('[data-testid="identity-address"]').text()).toBe('Jl. Pemuda No. 12, Surabaya');
    expect(wrapper.find('[data-testid="identity-phone"]').text()).toBe('081388887777');
    expect(wrapper.find('[data-testid="identity-email"]').text()).toBe('aisyah.putri@example.com');
  });

  it('renders administrative record governance, independent learner badge, and audit timestamps', () => {
    const wrapper = mountTab();

    expect(wrapper.find('[data-testid="identity-independent-badge"]').text()).toContain('No — Family/guardian governance');
    expect(wrapper.find('[data-testid="identity-created-audit"]').text()).toContain('admissions_officer');
    expect(wrapper.find('[data-testid="identity-updated-audit"]').text()).toContain('records_officer');
    expect(wrapper.find('[data-testid="identity-notes"]').text()).toBe('Transferred from overseas school.');
  });

  it('renders self-managed learner badge when student is independent', () => {
    const wrapper = mountTab({
      student: {
        ...mockStudentIdentity,
        isIndependent: true,
      },
    });

    expect(wrapper.find('[data-testid="identity-independent-badge"]').text()).toContain('Yes — Self-managed learner');
  });

  it('emits edit event when Edit Identity button is clicked', async () => {
    const wrapper = mountTab();

    const editBtn = wrapper.find('[data-testid="tab-edit-identity-button"]');
    expect(editBtn.exists()).toBe(true);
    await editBtn.trigger('click');

    expect(wrapper.emitted('edit')).toHaveLength(1);
  });

  it('hides Edit Identity button when canEdit is false', () => {
    const wrapper = mountTab({ canEdit: false });

    expect(wrapper.find('[data-testid="tab-edit-identity-button"]').exists()).toBe(false);
  });
});
