// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { apiClient } from '@/shared/services/http';
import {
  getAdmissionApplicationsList,
  getAdmissionApplicationById,
  getApplicationStatusByNumber,
  registerApplication,
  startApplicationReview,
  recordAdmissionDecision,
  withdrawApplication,
  uploadApplicantDocument,
  downloadApplicantDocumentUrl,
  verifyApplicantDocument,
  addAssessmentEvidence,
  updateAssessmentEvidence,
  deleteAssessmentEvidence,
  getActiveAdmissionPeriods,
} from '../api/admission.api';

describe('Admission API Client (admission.api.ts)', () => {
  let capturedConfig: InternalAxiosRequestConfig | null = null;
  const originalAdapter = apiClient.defaults.adapter;

  beforeEach(() => {
    capturedConfig = null;
  });

  afterEach(() => {
    if (originalAdapter) {
      apiClient.defaults.adapter = originalAdapter;
    } else {
      delete apiClient.defaults.adapter;
    }
    vi.restoreAllMocks();
  });

  describe('getAdmissionApplicationsList', () => {
    it('calls GET /students/admission/applications with clean query params', async () => {
      const mockItems = [
        {
          id: 'app-1',
          applicationNumber: 'ADM-2026-0001',
          applicantName: 'Budi Santoso',
          admissionPeriodId: 'period-1',
          requestedEntryLevel: 'Grade 1',
          status: 'registered',
          registrationDate: '2026-04-01',
        },
      ];

      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: mockItems,
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const result = await getAdmissionApplicationsList({
        admissionPeriodId: 'period-1',
        status: 'registered',
        requestedEntryLevel: 'Grade 1',
        searchTerm: 'Budi',
        page: 1,
        pageSize: 10,
      });

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig!.url).toBe('/students/admission/applications');
      expect(capturedConfig!.method?.toLowerCase()).toBe('get');
      expect(capturedConfig!.params).toEqual({
        admissionPeriodId: 'period-1',
        status: 'registered',
        requestedEntryLevel: 'Grade 1',
        searchTerm: 'Budi',
        page: 1,
        pageSize: 10,
      });
      expect(result).toEqual(mockItems);
    });
  });

  describe('getAdmissionApplicationById', () => {
    it('calls GET /students/admission/applications/{id}', async () => {
      const mockDetail = {
        id: 'app-123',
        applicationNumber: 'ADM-2026-0042',
        applicantId: 'applicant-1',
        applicant: { id: 'applicant-1', fullName: 'Dewi Sartika' },
        contacts: [],
        documents: [],
        assessmentEvidence: [],
      };

      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: mockDetail,
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const result = await getAdmissionApplicationById('app-123');

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig!.url).toBe('/students/admission/applications/app-123');
      expect(capturedConfig!.method?.toLowerCase()).toBe('get');
      expect(result).toEqual(mockDetail);
    });
  });

  describe('getApplicationStatusByNumber', () => {
    it('calls GET /students/admission/applications/status with number and DOB', async () => {
      const mockStatus = {
        applicationNumber: 'ADM-2026-0001',
        applicantFullName: 'Budi Santoso',
        requestedEntryLevel: 'Grade 1',
        status: 'registered',
        registrationDate: '2026-04-01',
        documentStatuses: [],
        decisionNotice: null,
      };

      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: mockStatus,
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const result = await getApplicationStatusByNumber({
        applicationNumber: 'ADM-2026-0001',
        dateOfBirth: '2015-05-20',
      });

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig!.url).toBe('/students/admission/applications/status');
      expect(capturedConfig!.params).toEqual({
        applicationNumber: 'ADM-2026-0001',
        dateOfBirth: '2015-05-20',
      });
      expect(result).toEqual(mockStatus);
    });
  });

  describe('registerApplication', () => {
    it('calls staff endpoint when isPublicPortal is false', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: {
            id: 'app-new',
            applicationNumber: 'ADM-2026-0002',
            applicantId: 'applicant-new',
            status: 'registered',
            registrationDate: '2026-04-02',
          },
          status: 201,
          statusText: 'Created',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const payload = {
        fullName: 'Jane Doe',
        admissionPeriodId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
        requestedEntryLevel: 'Grade 1',
      };

      const result = await registerApplication(payload, false);

      expect(capturedConfig!.url).toBe('/students/admission/applications');
      expect(capturedConfig!.method?.toLowerCase()).toBe('post');
      expect(result.applicationNumber).toBe('ADM-2026-0002');
    });

    it('calls portal endpoint when isPublicPortal is true', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: {
            id: 'app-portal',
            applicationNumber: 'ADM-2026-0003',
            applicantId: 'applicant-portal',
            status: 'registered',
            registrationDate: '2026-04-03',
          },
          status: 201,
          statusText: 'Created',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const payload = {
        fullName: 'Portal Applicant',
        admissionPeriodId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
        requestedEntryLevel: 'TK-A',
      };

      const result = await registerApplication(payload, true);

      expect(capturedConfig!.url).toBe('/students/admission/portal/applications');
      expect(capturedConfig!.method?.toLowerCase()).toBe('post');
      expect(result.applicationNumber).toBe('ADM-2026-0003');
    });
  });

  describe('startApplicationReview', () => {
    it('calls POST /students/admission/applications/{id}/start-review', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: { id: 'app-1', status: 'under_review' },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const result = await startApplicationReview('app-1');
      expect(capturedConfig!.url).toBe('/students/admission/applications/app-1/start-review');
      expect(result.status).toBe('under_review');
    });
  });

  describe('recordAdmissionDecision', () => {
    it('calls POST /students/admission/applications/{id}/decisions', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: {
            decisionId: 'dec-1',
            applicationId: 'app-1',
            decisionType: 'accepted',
            effectiveDate: '2026-07-01',
          },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const result = await recordAdmissionDecision('app-1', {
        decisionType: 'accepted',
        decisionDate: '2026-05-15',
        effectiveDate: '2026-07-01',
      });

      expect(capturedConfig!.url).toBe('/students/admission/applications/app-1/decisions');
      expect(result.decisionType).toBe('accepted');
    });
  });

  describe('withdrawApplication', () => {
    it('calls POST /students/admission/applications/{id}/withdraw', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: { id: 'app-1', status: 'withdrawn' },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const result = await withdrawApplication('app-1', {
        withdrawalReason: 'Family relocated out of country.',
      });

      expect(capturedConfig!.url).toBe('/students/admission/applications/app-1/withdraw');
      expect(result.status).toBe('withdrawn');
    });
  });

  describe('uploadApplicantDocument & downloadApplicantDocumentUrl', () => {
    it('calls POST /students/admission/applications/{id}/documents with multipart data', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: {
            id: 'doc-1',
            applicationId: 'app-1',
            documentType: 'birth_certificate',
            submissionStatus: 'submitted',
            fileReference: 'ref-1',
            fileName: 'akta.pdf',
            fileSizeBytes: 1024,
            contentType: 'application/pdf',
          },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const file = new File(['mock content'], 'akta.pdf', { type: 'application/pdf' });
      const result = await uploadApplicantDocument('app-1', 'birth_certificate', file);

      expect(capturedConfig!.url).toBe('/students/admission/applications/app-1/documents');
      expect(capturedConfig!.headers?.['Content-Type']).toBe('multipart/form-data');
      expect(result.documentType).toBe('birth_certificate');
    });

    it('downloadApplicantDocumentUrl builds correct endpoint path', () => {
      const url = downloadApplicantDocumentUrl('app-1', 'doc-1');
      expect(url).toContain('/students/admission/applications/app-1/documents/doc-1/file');
    });
  });

  describe('verifyApplicantDocument', () => {
    it('calls PUT /students/admission/applications/{id}/documents/{docId}/verify', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: {
            id: 'doc-1',
            applicationId: 'app-1',
            documentType: 'birth_certificate',
            submissionStatus: 'verified',
            verifiedBy: 'admin',
          },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const result = await verifyApplicantDocument('app-1', 'doc-1', {
        submissionStatus: 'verified',
        notes: 'Verified original stamped photocopy.',
      });

      expect(capturedConfig!.url).toBe(
        '/students/admission/applications/app-1/documents/doc-1/verify',
      );
      expect(capturedConfig!.method?.toLowerCase()).toBe('put');
      expect(result.submissionStatus).toBe('verified');
    });
  });

  describe('assessmentEvidence CRUD', () => {
    it('adds assessment evidence via POST', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: {
            id: 'evidence-1',
            applicationId: 'app-1',
            assessmentType: 'reading_readiness',
            score: '85/100',
            source: 'External Center',
            createdAt: '2026-04-10T00:00:00Z',
          },
          status: 201,
          statusText: 'Created',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const result = await addAssessmentEvidence('app-1', {
        assessmentType: 'reading_readiness',
        score: '85/100',
      });

      expect(capturedConfig!.url).toBe(
        '/students/admission/applications/app-1/assessment-evidence',
      );
      expect(capturedConfig!.method?.toLowerCase()).toBe('post');
      expect(result.score).toBe('85/100');
    });

    it('updates assessment evidence via PUT', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: {
            id: 'evidence-1',
            applicationId: 'app-1',
            assessmentType: 'reading_readiness',
            score: '90/100',
            source: 'External Center',
          },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const result = await updateAssessmentEvidence('app-1', 'evidence-1', {
        assessmentType: 'reading_readiness',
        score: '90/100',
      });

      expect(capturedConfig!.url).toBe(
        '/students/admission/applications/app-1/assessment-evidence/evidence-1',
      );
      expect(capturedConfig!.method?.toLowerCase()).toBe('put');
      expect(result.score).toBe('90/100');
    });

    it('deletes assessment evidence via DELETE', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: null,
          status: 204,
          statusText: 'No Content',
          headers: {},
          config,
        } as AxiosResponse;
      };

      await deleteAssessmentEvidence('app-1', 'evidence-1');

      expect(capturedConfig!.url).toBe(
        '/students/admission/applications/app-1/assessment-evidence/evidence-1',
      );
      expect(capturedConfig!.method?.toLowerCase()).toBe('delete');
    });
  });

  describe('getActiveAdmissionPeriods', () => {
    it('queries active academic years and fetches child admission periods', async () => {
      const mockYears = [
        { id: 'ay-2026', code: 'AY-2026', name: '2026/2027', status: 'active' },
      ];
      const mockPeriods = [
        {
          id: 'p-1',
          academicYearId: 'ay-2026',
          periodCode: 'PPDB-1',
          name: 'PPDB Gelombang 1',
          periodType: 'admission_window',
          status: 'active',
        },
      ];

      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        if (config.url === '/operations/academic-years') {
          return {
            data: mockYears,
            status: 200,
            statusText: 'OK',
            headers: {},
            config,
          } as AxiosResponse;
        }
        if (config.url?.includes('/operations/academic-years/ay-2026/periods')) {
          return {
            data: mockPeriods,
            status: 200,
            statusText: 'OK',
            headers: {},
            config,
          } as AxiosResponse;
        }
        return {
          data: [],
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const result = await getActiveAdmissionPeriods();
      expect(result).toHaveLength(1);
      expect(result[0].id).toBe('p-1');
      expect(result[0].periodType).toBe('admission_window');
    });
  });
});
