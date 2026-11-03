// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as admissionApi from '../../api/admission.api';
import {
  useAdmissionQueueQuery,
  useAdmissionDossierQuery,
  useApplicationStatusQuery,
  useActiveAdmissionPeriodsQuery,
} from '../useAdmissionQueries';
import type {
  AdmissionApplicationListItemDto,
  AdmissionApplicationDetailDto,
  PublicApplicationStatusDto,
  AdmissionQueueFilterParams,
  AdmissionPeriodOptionDto,
} from '../../types/admission.types';

function withSetup<T>(composable: () => T) {
  let result!: T;
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });

  const app = createApp({
    setup() {
      result = composable();
      return () => {};
    },
  });

  app.use(VueQueryPlugin, { queryClient });
  app.mount(document.createElement('div'));

  return { result, queryClient, app };
}

describe('useAdmissionQueries', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useAdmissionQueueQuery', () => {
    it('fetches applications list with reactive filters', async () => {
      const mockList: AdmissionApplicationListItemDto[] = [
        {
          id: 'app-1',
          applicationNumber: 'ADM-2026-0001',
          applicantName: 'Ahmad Faiz',
          governmentIdNumber: '3201011234567890',
          admissionPeriodId: 'per-1',
          requestedEntryLevel: 'Grade 10',
          status: 'registered',
          registrationDate: '2026-02-15',
        },
      ];
      vi.spyOn(admissionApi, 'getAdmissionApplicationsList').mockResolvedValue(mockList);

      const filters = ref<AdmissionQueueFilterParams>({ status: 'registered' });
      const { result } = withSetup(() => useAdmissionQueueQuery(filters));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(admissionApi.getAdmissionApplicationsList).toHaveBeenCalledWith({ status: 'registered' });
      expect(result.data.value).toEqual(mockList);

      filters.value = { status: 'under_review' };
      await vi.waitFor(() =>
        expect(admissionApi.getAdmissionApplicationsList).toHaveBeenCalledWith({ status: 'under_review' }),
      );
    });

    it('honors enabled option', async () => {
      vi.spyOn(admissionApi, 'getAdmissionApplicationsList').mockResolvedValue([]);
      const isEnabled = ref(false);
      const { result } = withSetup(() => useAdmissionQueueQuery(undefined, { enabled: isEnabled }));

      expect(result.isFetching.value).toBe(false);
      expect(admissionApi.getAdmissionApplicationsList).not.toHaveBeenCalled();

      isEnabled.value = true;
      await vi.waitFor(() => expect(result.isFetching.value || result.isSuccess.value).toBe(true));
      expect(admissionApi.getAdmissionApplicationsList).toHaveBeenCalled();
    });
  });

  describe('useAdmissionDossierQuery', () => {
    it('does not execute when applicationId is null or empty', () => {
      vi.spyOn(admissionApi, 'getAdmissionApplicationById').mockResolvedValue({} as AdmissionApplicationDetailDto);
      const id = ref<string | null>(null);
      const { result } = withSetup(() => useAdmissionDossierQuery(id));

      expect(result.isFetching.value).toBe(false);
      expect(admissionApi.getAdmissionApplicationById).not.toHaveBeenCalled();
    });

    it('fetches complete dossier when applicationId is provided', async () => {
      const mockDossier: AdmissionApplicationDetailDto = {
        id: 'app-123',
        applicationNumber: 'ADM-2026-0001',
        applicantId: 'apl-123',
        academicYearId: 'ay-2026',
        admissionPeriodId: 'per-1',
        applicationType: 'new',
        requestedEntryLevel: 'Grade 7',
        requestedCampus: 'Main',
        requestedProgram: 'Standard',
        status: 'under_review',
        registrationDate: '2026-03-01',
        applicant: {
          id: 'apl-123',
          fullName: 'Budi Santoso',
          dateOfBirth: '2013-05-10',
          gender: 'male',
          nationality: 'Indonesian',
        },
        contacts: [],
        documents: [],
        assessmentEvidence: [],
        decision: null,
        createdAt: '2026-03-01T08:00:00Z',
      };
      vi.spyOn(admissionApi, 'getAdmissionApplicationById').mockResolvedValue(mockDossier);

      const id = ref<string | null>('app-123');
      const { result } = withSetup(() => useAdmissionDossierQuery(id));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(admissionApi.getAdmissionApplicationById).toHaveBeenCalledWith('app-123');
      expect(result.data.value).toEqual(mockDossier);
    });
  });

  describe('useApplicationStatusQuery', () => {
    it('is disabled when number or dob is missing', () => {
      vi.spyOn(admissionApi, 'getApplicationStatusByNumber').mockResolvedValue({} as PublicApplicationStatusDto);
      const number = ref('');
      const dob = ref('');
      const { result } = withSetup(() => useApplicationStatusQuery(number, dob));

      expect(result.isFetching.value).toBe(false);
      expect(admissionApi.getApplicationStatusByNumber).not.toHaveBeenCalled();
    });

    it('fetches status when both number and dob are provided', async () => {
      const mockStatus: PublicApplicationStatusDto = {
        applicationNumber: 'ADM-2026-0001',
        applicantFullName: 'Budi Santoso',
        requestedEntryLevel: 'Grade 7',
        status: 'under_review',
        registrationDate: '2026-03-01',
        documentStatuses: [],
        decisionNotice: null,
      };
      vi.spyOn(admissionApi, 'getApplicationStatusByNumber').mockResolvedValue(mockStatus);

      const number = ref('ADM-2026-0001');
      const dob = ref('2013-05-10');
      const { result } = withSetup(() => useApplicationStatusQuery(number, dob));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(admissionApi.getApplicationStatusByNumber).toHaveBeenCalledWith({
        applicationNumber: 'ADM-2026-0001',
        dateOfBirth: '2013-05-10',
      });
      expect(result.data.value).toEqual(mockStatus);
    });
  });

  describe('useActiveAdmissionPeriodsQuery', () => {
    it('fetches active admission periods from operations via admission api', async () => {
      const mockPeriods: AdmissionPeriodOptionDto[] = [
        {
          id: 'per-adm-1',
          academicYearId: 'ay-1',
          periodCode: 'ADM-W1-2026',
          name: 'Admission Window 1',
          periodType: 'admission_window',
          startDate: '2026-01-01',
          endDate: '2026-04-30',
          parentPeriodId: null,
          campusContext: 'All',
          status: 'active',
        },
      ];

      vi.spyOn(admissionApi, 'getActiveAdmissionPeriods').mockResolvedValue(mockPeriods);

      const { result } = withSetup(() => useActiveAdmissionPeriodsQuery());

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(admissionApi.getActiveAdmissionPeriods).toHaveBeenCalled();
      expect(result.data.value).toEqual(mockPeriods);
    });

    it('honors enabled option for active admission periods query', async () => {
      vi.spyOn(admissionApi, 'getActiveAdmissionPeriods').mockResolvedValue([]);
      const isEnabled = ref(false);
      const { result } = withSetup(() =>
        useActiveAdmissionPeriodsQuery({ enabled: isEnabled }),
      );

      expect(result.isFetching.value).toBe(false);
      expect(admissionApi.getActiveAdmissionPeriods).not.toHaveBeenCalled();

      isEnabled.value = true;
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(admissionApi.getActiveAdmissionPeriods).toHaveBeenCalled();
    });
  });
});
