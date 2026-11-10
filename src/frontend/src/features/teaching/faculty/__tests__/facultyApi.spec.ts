// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  getTeachers,
  getTeacherById,
  getRoles,
  getCapabilityTypes,
  getQualificationTypes,
  registerTeacher,
  updateTeacher,
  approveTeacher,
  changeStanding,
  addCapability,
  removeCapability,
  submitEvidence,
  verifyEvidence,
  rejectEvidence,
  grantAuthorization,
  updateAuthorizationStatus,
  addSupervision,
  resolveSupervision,
  applyCorrection,
  approveCorrection,
  getCorrections,
  facultyApi,
  teachingApiClient,
} from '../api/facultyApi';

describe('Teaching facultyApi Client Methods', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const teacherId = '11111111-1111-1111-1111-111111111111';
  const capabilityId = '22222222-2222-2222-2222-222222222222';
  const qualificationId = '33333333-3333-3333-3333-333333333333';
  const authorizationId = '44444444-4444-4444-4444-444444444444';
  const supervisionId = '55555555-5555-5555-5555-555555555555';
  const correctionId = '66666666-6666-6666-6666-666666666666';

  describe('Faculty Profile Queries & Commands', () => {
    it('getTeachers invokes GET /v1/teaching/teachers with params', async () => {
      const mockResult = {
        items: [{ id: teacherId, fullName: 'Budi Hartono' }],
        totalCount: 1,
        pageNumber: 1,
        pageSize: 10,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResult as never);

      const params = { queueFilter: 'pending_approval' as const, search: 'Budi', campus: 'Main' };
      const res = await getTeachers(params);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/teachers', { params });
      expect(res).toEqual(mockResult);
    });

    it('getTeacherById invokes GET /v1/teaching/teachers/{id} with URL-encoded ID', async () => {
      const mockProfile = { id: teacherId, fullName: 'Budi Hartono', standing: 'active' };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockProfile as never);

      const res = await getTeacherById(teacherId);

      expect(getSpy).toHaveBeenCalledWith(`/v1/teaching/teachers/${encodeURIComponent(teacherId)}`);
      expect(res).toEqual(mockProfile);
    });

    it('registerTeacher invokes POST /v1/teaching/teachers with candidate payload', async () => {
      const payload = {
        fullName: 'Budi Hartono',
        teachingRoleId: 'role-01',
        campusAffiliation: 'Main Campus',
        effectiveFrom: '2026-07-01',
      };
      const mockResponse = { id: teacherId, teacherNumber: 'TCH-2026-001', standing: 'pending_approval' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await registerTeacher(payload);

      expect(postSpy).toHaveBeenCalledWith('/v1/teaching/teachers', payload);
      expect(res).toEqual(mockResponse);
    });

    it('updateTeacher invokes PUT /v1/teaching/teachers/{id} with mutable fields', async () => {
      const payload = { displayName: 'Dr. Budi', department: 'Physics' };
      const putSpy = vi.spyOn(apiClient, 'put').mockResolvedValueOnce(undefined as never);

      await updateTeacher(teacherId, payload);

      expect(putSpy).toHaveBeenCalledWith(`/v1/teaching/teachers/${encodeURIComponent(teacherId)}`, payload);
    });

    it('approveTeacher invokes POST /v1/teaching/teachers/{id}/approve', async () => {
      const mockResponse = { id: teacherId, standing: 'active' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await approveTeacher(teacherId);

      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/teachers/${encodeURIComponent(teacherId)}/approve`);
      expect(res).toEqual(mockResponse);
    });

    it('changeStanding invokes POST /v1/teaching/teachers/{id}/standing with justification rationale', async () => {
      const payload = { standing: 'restricted' as const, standingReason: 'Medical leave' };
      const mockResponse = { id: teacherId, standing: 'restricted' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await changeStanding(teacherId, payload);

      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/teachers/${encodeURIComponent(teacherId)}/standing`, payload);
      expect(res).toEqual(mockResponse);
    });
  });

  describe('Taxonomy & Reference Queries', () => {
    it('getRoles invokes GET /v1/teaching/roles', async () => {
      const mockRoles = [{ id: 'role-1', code: 'GURU_TETAP', name: 'Guru Tetap' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockRoles as never);

      const res = await getRoles();

      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/roles');
      expect(res).toEqual(mockRoles);
    });

    it('getCapabilityTypes invokes GET /v1/teaching/capability-types', async () => {
      const mockTypes = [{ id: 'cap-1', code: 'SUBJECT', name: 'Subject Area' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockTypes as never);

      const res = await getCapabilityTypes();

      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/capability-types');
      expect(res).toEqual(mockTypes);
    });

    it('getQualificationTypes invokes GET /v1/teaching/qualification-types', async () => {
      const mockTypes = [{ id: 'qual-1', code: 'SERDIK', name: 'Sertifikat Pendidik' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockTypes as never);

      const res = await getQualificationTypes();

      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/qualification-types');
      expect(res).toEqual(mockTypes);
    });
  });

  describe('Teaching Capabilities Commands', () => {
    it('addCapability invokes POST /v1/teaching/teachers/{id}/capabilities', async () => {
      const payload = {
        capabilityTypeId: 'cap-type-01',
        capabilityCode: 'MATH_SEC',
        capabilityName: 'Secondary Mathematics',
      };
      const mockResponse = { id: capabilityId };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await addCapability(teacherId, payload);

      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/teachers/${encodeURIComponent(teacherId)}/capabilities`, payload);
      expect(res).toEqual(mockResponse);
    });

    it('removeCapability invokes DELETE /v1/teaching/teachers/{id}/capabilities/{capabilityId}', async () => {
      const delSpy = vi.spyOn(apiClient, 'delete').mockResolvedValueOnce(undefined as never);

      await removeCapability(teacherId, capabilityId);

      expect(delSpy).toHaveBeenCalledWith(
        `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/capabilities/${encodeURIComponent(capabilityId)}`,
      );
    });
  });

  describe('Qualification Evidence Commands', () => {
    it('submitEvidence invokes POST /v1/teaching/teachers/{id}/qualifications', async () => {
      const payload = {
        qualificationTypeId: 'qtype-01',
        title: 'Ijazah S1 Pendidikan',
        issuingAuthority: 'Universitas Indonesia',
        issueDate: '2020-08-01',
      };
      const mockResponse = { id: qualificationId, title: payload.title };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await submitEvidence(teacherId, payload);

      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/teachers/${encodeURIComponent(teacherId)}/qualifications`, payload);
      expect(res).toEqual(mockResponse);
    });

    it('verifyEvidence invokes POST /v1/teaching/qualifications/{id}/verify', async () => {
      const payload = { verificationNotes: 'Document verified against university registry' };
      const mockResponse = { id: qualificationId, verificationState: 'verified' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await verifyEvidence(qualificationId, payload);

      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/qualifications/${encodeURIComponent(qualificationId)}/verify`, payload);
      expect(res).toEqual(mockResponse);
    });

    it('rejectEvidence invokes POST /v1/teaching/qualifications/{id}/reject', async () => {
      const payload = { rejectionReason: 'Illegible photocopy' };
      const mockResponse = { id: qualificationId, verificationState: 'rejected' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await rejectEvidence(qualificationId, payload);

      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/qualifications/${encodeURIComponent(qualificationId)}/reject`, payload);
      expect(res).toEqual(mockResponse);
    });
  });

  describe('Teaching Authorization & Supervision Commands', () => {
    it('grantAuthorization invokes POST /v1/teaching/teachers/{id}/authorizations', async () => {
      const payload = {
        scopeDescription: 'Teaching Physics Grade 10-12',
        campusScope: 'Main Campus',
        effectiveStartDate: '2026-07-01',
      };
      const mockResponse = { id: authorizationId, authorizationNumber: 'AUTH-2026-0001' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await grantAuthorization(teacherId, payload);

      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/teachers/${encodeURIComponent(teacherId)}/authorizations`, payload);
      expect(res).toEqual(mockResponse);
    });

    it('updateAuthorizationStatus invokes POST /v1/teaching/authorizations/{id}/status', async () => {
      const payload = { status: 'suspended' as const, statusReason: 'Pending credential re-verification' };
      const mockResponse = { id: authorizationId, status: 'suspended' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await updateAuthorizationStatus(authorizationId, payload);

      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/authorizations/${encodeURIComponent(authorizationId)}/status`, payload);
      expect(res).toEqual(mockResponse);
    });

    it('addSupervision invokes POST /v1/teaching/authorizations/{id}/supervision', async () => {
      const payload = {
        supervisionType: 'co_teaching' as const,
        description: 'Bi-weekly co-teaching sessions',
        startDate: '2026-07-01',
      };
      const mockResponse = { id: supervisionId, status: 'active' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await addSupervision(authorizationId, payload);

      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/authorizations/${encodeURIComponent(authorizationId)}/supervision`, payload);
      expect(res).toEqual(mockResponse);
    });

    it('resolveSupervision invokes POST /v1/teaching/supervision/{id}/resolve', async () => {
      const payload = {
        resolutionStatus: 'completed' as const,
        resolutionNotes: 'Successfully completed mentoring',
      };
      const mockResponse = { id: supervisionId, status: 'completed' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await resolveSupervision(supervisionId, payload);

      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/supervision/${encodeURIComponent(supervisionId)}/resolve`, payload);
      expect(res).toEqual(mockResponse);
    });
  });

  describe('Auditable Record Corrections Commands & Queries', () => {
    it('applyCorrection invokes POST /v1/teaching/corrections', async () => {
      const payload = {
        targetTableName: 'teacher_profiles',
        targetRecordId: teacherId,
        fieldName: 'phone',
        originalValue: '+628111111111',
        correctedValue: '+628222222222',
        reasonForCorrection: 'Updated phone per faculty request',
      };
      const mockResponse = { id: correctionId, correctionNumber: 'CORR-2026-0001' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await applyCorrection(payload);

      expect(postSpy).toHaveBeenCalledWith('/v1/teaching/corrections', payload);
      expect(res).toEqual(mockResponse);
    });

    it('approveCorrection invokes POST /v1/teaching/corrections/{id}/approve', async () => {
      const mockResponse = { id: correctionId, status: 'approved' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await approveCorrection(correctionId);

      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/corrections/${encodeURIComponent(correctionId)}/approve`);
      expect(res).toEqual(mockResponse);
    });

    it('getCorrections invokes GET /v1/teaching/corrections with query params', async () => {
      const mockCorrections = [{ id: correctionId, correctionNumber: 'CORR-2026-0001' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockCorrections as never);

      const params = { teacherProfileId: teacherId, status: 'requested' as const };
      const res = await getCorrections(params);

      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/corrections', { params });
      expect(res).toEqual(mockCorrections);
    });
  });

  describe('Object Alias Export Integrity', () => {
    it('facultyApi object exposes all functions matching teachingApiClient', () => {
      expect(facultyApi).toBe(teachingApiClient);
      expect(typeof facultyApi.getTeachers).toBe('function');
      expect(typeof facultyApi.getTeacherById).toBe('function');
      expect(typeof facultyApi.registerTeacher).toBe('function');
      expect(typeof facultyApi.updateTeacher).toBe('function');
      expect(typeof facultyApi.approveTeacher).toBe('function');
      expect(typeof facultyApi.changeStanding).toBe('function');
      expect(typeof facultyApi.addCapability).toBe('function');
      expect(typeof facultyApi.removeCapability).toBe('function');
      expect(typeof facultyApi.submitEvidence).toBe('function');
      expect(typeof facultyApi.verifyEvidence).toBe('function');
      expect(typeof facultyApi.rejectEvidence).toBe('function');
      expect(typeof facultyApi.grantAuthorization).toBe('function');
      expect(typeof facultyApi.updateAuthorizationStatus).toBe('function');
      expect(typeof facultyApi.addSupervision).toBe('function');
      expect(typeof facultyApi.resolveSupervision).toBe('function');
      expect(typeof facultyApi.applyCorrection).toBe('function');
      expect(typeof facultyApi.approveCorrection).toBe('function');
      expect(typeof facultyApi.getCorrections).toBe('function');
    });
  });
});
