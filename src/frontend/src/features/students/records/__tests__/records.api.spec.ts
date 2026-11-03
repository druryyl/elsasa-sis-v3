// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import type { InternalAxiosRequestConfig } from 'axios';
import { apiClient } from '@/shared/services/http';
import {
  searchStudents,
  getStudentById,
  updateStudentIdentity,
  addGuardianRelationship,
  updateGuardianRelationship,
  uploadStudentDocument,
  getStudentDocumentDownloadUrl,
  verifyStudentDocument,
} from '../api/records.api';
import type {
  AddGuardianPayload,
  StudentDetail,
  StudentSearchParams,
  UpdateGuardianPayload,
  UpdateIdentityPayload,
} from '../types/records.types';

describe('Student Records API Client (records.api.ts)', () => {
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

  describe('searchStudents', () => {
    it('calls GET /students with sanitized query parameters', async () => {
      const mockResult = {
        items: [
          {
            id: 'stu-1',
            studentNumber: 'STD-2026-0001',
            fullName: 'Ahmad Dahlan',
            currentStatus: 'enrolled',
            enrollmentDate: '2026-05-01',
            initialCampus: 'Main Campus',
            initialLevel: 'Grade 7',
            isIndependent: false,
          },
        ],
        pageNumber: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      };

      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: mockResult,
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        };
      };

      const params: StudentSearchParams = {
        searchTerm: 'Ahmad',
        campus: 'Main Campus',
        level: 'all',
        status: 'enrolled',
        pageNumber: 1,
        pageSize: 20,
      };

      const result = await searchStudents(params);

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig?.method?.toLowerCase()).toBe('get');
      expect(capturedConfig?.url).toBe('/students');
      expect(capturedConfig?.params).toEqual({
        pageNumber: 1,
        pageSize: 20,
        searchTerm: 'Ahmad',
        campus: 'Main Campus',
        status: 'enrolled',
      });
      expect(result.items).toHaveLength(1);
      expect(result.items[0]?.studentNumber).toBe('STD-2026-0001');
    });
  });

  describe('getStudentById', () => {
    it('calls GET /students/{id} and returns full dossier', async () => {
      const mockDetail: Partial<StudentDetail> = {
        id: '11111111-1111-1111-1111-111111111111',
        studentNumber: 'STD-2026-0001',
        fullName: 'Ahmad Dahlan',
        currentStatus: 'enrolled',
        enrollmentHistory: [],
        guardianRelationships: [],
        documents: [],
      };

      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: mockDetail,
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        };
      };

      const result = await getStudentById('11111111-1111-1111-1111-111111111111');

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig?.method?.toLowerCase()).toBe('get');
      expect(capturedConfig?.url).toBe('/students/11111111-1111-1111-1111-111111111111');
      expect(result.studentNumber).toBe('STD-2026-0001');
    });
  });

  describe('updateStudentIdentity', () => {
    it('calls PUT /students/{id}/identity with payload', async () => {
      const payload: UpdateIdentityPayload = {
        fullName: 'Ahmad Dahlan Al-Mansur',
        preferredName: 'Ahmad',
        dateOfBirth: '2010-05-15',
        gender: 'Male',
        nationality: 'Indonesian',
        isIndependent: false,
      };

      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: null,
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        };
      };

      await updateStudentIdentity('11111111-1111-1111-1111-111111111111', payload);

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig?.method?.toLowerCase()).toBe('put');
      expect(capturedConfig?.url).toBe('/students/11111111-1111-1111-1111-111111111111/identity');
      expect(JSON.parse(capturedConfig?.data as string)).toEqual(payload);
    });
  });

  describe('addGuardianRelationship', () => {
    it('calls POST /students/{id}/guardians and returns relationshipId', async () => {
      const payload: AddGuardianPayload = {
        fullName: 'Siti Aminah',
        relationshipType: 'mother',
        phone: '+6281299999999',
        isPrimaryContact: true,
      };

      const mockResponse = {
        relationshipId: 'rel-999',
        guardianId: 'guard-123',
      };

      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: mockResponse,
          status: 201,
          statusText: 'Created',
          headers: {},
          config,
        };
      };

      const relId = await addGuardianRelationship('11111111-1111-1111-1111-111111111111', payload);

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig?.method?.toLowerCase()).toBe('post');
      expect(capturedConfig?.url).toBe('/students/11111111-1111-1111-1111-111111111111/guardians');
      expect(relId).toBe('rel-999');
    });
  });

  describe('updateGuardianRelationship', () => {
    it('calls PUT /students/{id}/guardians/{relId} with updated authority flags', async () => {
      const payload: UpdateGuardianPayload = {
        relationshipType: 'mother',
        isPrimaryContact: true,
        isActive: true,
        phone: '+6281299998888',
      };

      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: null,
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        };
      };

      await updateGuardianRelationship('11111111-1111-1111-1111-111111111111', 'rel-999', payload);

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig?.method?.toLowerCase()).toBe('put');
      expect(capturedConfig?.url).toBe('/students/11111111-1111-1111-1111-111111111111/guardians/rel-999');
      expect(JSON.parse(capturedConfig?.data as string)).toEqual(payload);
    });
  });

  describe('uploadStudentDocument', () => {
    it('calls POST /students/{id}/documents with multipart/form-data and returns documentId', async () => {
      const mockFile = new File(['dummy-content'], 'birth_cert.pdf', { type: 'application/pdf' });
      const mockResponse = {
        id: 'doc-777',
        studentId: '11111111-1111-1111-1111-111111111111',
        documentType: 'Birth Certificate',
        submissionStatus: 'submitted',
      };

      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: mockResponse,
          status: 201,
          statusText: 'Created',
          headers: {},
          config,
        };
      };

      const docId = await uploadStudentDocument(
        '11111111-1111-1111-1111-111111111111',
        mockFile,
        'Birth Certificate',
      );

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig?.method?.toLowerCase()).toBe('post');
      expect(capturedConfig?.url).toBe('/students/11111111-1111-1111-1111-111111111111/documents');
      expect(capturedConfig?.data instanceof FormData).toBe(true);
      expect(docId).toBe('doc-777');
    });
  });

  describe('getStudentDocumentDownloadUrl', () => {
    it('constructs correct streaming URL for downloading official document', () => {
      const url = getStudentDocumentDownloadUrl('student-123', 'doc-456');
      expect(url).toContain('/students/student-123/documents/doc-456/file');
    });
  });

  describe('verifyStudentDocument', () => {
    it('calls PUT /students/{id}/documents/{docId}/verify with status and notes', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: null,
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        };
      };

      await verifyStudentDocument('student-123', 'doc-456', 'verified', 'Official stamp validated');

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig?.method?.toLowerCase()).toBe('put');
      expect(capturedConfig?.url).toBe('/students/student-123/documents/doc-456/verify');
      expect(JSON.parse(capturedConfig?.data as string)).toEqual({
        submissionStatus: 'verified',
        notes: 'Official stamp validated',
      });
    });
  });
});
