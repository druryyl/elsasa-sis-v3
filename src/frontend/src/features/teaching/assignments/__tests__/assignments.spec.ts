// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  acknowledgeTeachingAssignment,
  approvePermanentReassignment,
  approveSubstitutionException,
  approveTeachingAssignment,
  arrangeEmergencySubstitution,
  assignmentsApi,
  cancelSubstitution,
  concludeTeachingAssignment,
  confirmReassignmentHandover,
  createTeachingDemand,
  evaluateCandidate,
  getAssignmentCompletions,
  getAssignmentRoles,
  getCandidateEvaluations,
  getCapacityConflicts,
  getTeacherHistory,
  getTeachingAssignmentById,
  getTeachingAssignments,
  getTeachingDemandById,
  getTeachingDemands,
  getTeachingReassignmentById,
  getTeachingReassignments,
  getTeachingSubstitutions,
  proposePermanentReassignment,
  proposeTeachingAssignment,
  resolveCapacityConflict,
  updateTeachingAssignmentStatus,
  updateTeachingDemand,
  updateTeachingDemandStatus,
} from '../api/assignmentsApi';
import {
  arrangeSubstituteSchema,
  concludeAssignmentSchema,
  createDemandSchema,
  evaluateCandidateSchema,
  proposeAssignmentSchema,
  reassignmentSchema,
  resolveConflictSchema,
  updateDemandStatusSchema,
} from '../api/schemas';
import {
  assignmentRolesKeys,
  assignmentsKeys,
  completionsKeys,
  conflictsKeys,
  demandsKeys,
  evaluationsKeys,
  historyKeys,
  reassignmentsKeys,
  substitutionsKeys,
  teachingAssignmentsKeys,
} from '../api/queryKeys';

describe('Teaching Assignments API Client & Schemas (P7-S21)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const periodId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  const teacherId = '11111111-1111-1111-1111-111111111111';
  const demandId = '22222222-2222-2222-2222-222222222222';
  const assignmentId = '33333333-3333-3333-3333-333333333333';
  const roleId = '44444444-4444-4444-4444-444444444444';
  const conflictId = '55555555-5555-5555-5555-555555555555';
  const substituteTeacherId = '66666666-6666-6666-6666-666666666666';
  const substitutionId = '77777777-7777-7777-7777-777777777777';
  const reassignmentId = '88888888-8888-8888-8888-888888888888';

  // ==========================================================================
  // Zod Schemas Validation Tests
  // ==========================================================================
  describe('Zod Validation Schemas', () => {
    it('validates createDemandSchema successfully', () => {
      const valid = {
        sourceDomain: 'Academics',
        sourceWorkReference: '99999999-9999-9999-9999-999999999999',
        sourceWorkLabel: 'Matematika SD Kelas 4',
        academicPeriodId: periodId,
        requiredCapabilityCode: 'CAP-MATH-PRI',
        requiredAssignmentRoleId: roleId,
        expectedWorkloadHours: 24,
        requestedStartDate: '2026-07-01',
        requestedEndDate: '2026-12-31',
        campusScope: 'Kampus Utama',
        requiredTeachersCount: 1,
      };

      const result = createDemandSchema.safeParse(valid);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.workloadUnit).toBe('Teaching Hours');
        expect(result.data.requiresAssessmentAuthority).toBe(true);
      }
    });

    it('rejects createDemandSchema when end date precedes start date', () => {
      const invalid = {
        sourceDomain: 'Academics',
        sourceWorkReference: '99999999-9999-9999-9999-999999999999',
        sourceWorkLabel: 'Matematika SD Kelas 4',
        academicPeriodId: periodId,
        requiredCapabilityCode: 'CAP-MATH-PRI',
        requiredAssignmentRoleId: roleId,
        expectedWorkloadHours: 24,
        requestedStartDate: '2026-12-31',
        requestedEndDate: '2026-07-01',
      };

      const result = createDemandSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0]?.message).toContain('Requested end date must be on or after requested start date.');
      }
    });

    it('validates updateDemandStatusSchema requiring declineReason on decline', () => {
      const declinedWithoutReason = {
        status: 'declined',
      };
      const r1 = updateDemandStatusSchema.safeParse(declinedWithoutReason);
      expect(r1.success).toBe(false);

      const declinedWithReason = {
        status: 'declined',
        declineReason: 'No qualified faculty available.',
      };
      const r2 = updateDemandStatusSchema.safeParse(declinedWithReason);
      expect(r2.success).toBe(true);
    });

    it('validates evaluateCandidateSchema', () => {
      const valid = {
        teachingDemandId: demandId,
        teacherProfileId: teacherId,
        coordinatorRemarks: 'Priority candidate for primary math.',
      };
      const result = evaluateCandidateSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates proposeAssignmentSchema successfully', () => {
      const valid = {
        teachingDemandId: demandId,
        teacherProfileId: teacherId,
        assignmentRoleId: roleId,
        assignmentScope: 'learning_unit',
        scopeReferenceId: '99999999-9999-9999-9999-999999999999',
        scopeLabel: 'Matematika SD Kelas 4',
        academicPeriodId: periodId,
        effectiveStartDate: '2026-07-01',
        effectiveEndDate: '2026-12-31',
        workloadCommitmentValue: 18,
      };

      const result = proposeAssignmentSchema.safeParse(valid);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.workloadUnit).toBe('Teaching Hours');
      }
    });

    it('validates resolveConflictSchema', () => {
      const valid = {
        status: 'resolved',
        resolutionNotes: 'Approved workload exception granted by authority.',
        resolutionAction: 'workload_exception_granted',
      };
      const result = resolveConflictSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates arrangeSubstituteSchema', () => {
      const valid = {
        originalAssignmentId: assignmentId,
        substituteTeacherId,
        startDate: '2026-09-01',
        endDate: '2026-09-14',
        coverageScopeType: 'full',
        coveredHoursPerWeek: 18,
        reason: 'medical_leave',
        handoverNotes: 'Syllabus chapter 4 provided in LMS.',
      };
      const result = arrangeSubstituteSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates reassignmentSchema', () => {
      const valid = {
        originalAssignmentId: assignmentId,
        newTeacherId: substituteTeacherId,
        effectiveDate: '2026-10-01',
        reason: 'Permanent maternity transfer.',
        requiresHandover: true,
      };
      const result = reassignmentSchema.safeParse(valid);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.reassignmentType).toBe('permanent_transfer');
      }
    });

    it('validates concludeAssignmentSchema', () => {
      const valid = {
        completionDate: '2026-12-20',
        academicConfirmationReceived: true,
        academicConfirmationSource: 'Academic Dean Memo 2026/12',
        handoverConfirmed: true,
        unresolvedItemsStatus: 'none',
      };
      const result = concludeAssignmentSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });
  });

  // ==========================================================================
  // API Client Functions Tests
  // ==========================================================================
  describe('assignmentsApi Client Functions', () => {
    it('getTeachingDemands invokes GET /v1/teaching/demands with query parameters', async () => {
      const mockData = {
        items: [],
        pageNumber: 1,
        pageSize: 20,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockData as never);

      const res = await getTeachingDemands({
        academicPeriodId: periodId,
        status: 'received',
        page: 1,
        pageSize: 20,
      });

      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/demands', {
        params: {
          academicPeriodId: periodId,
          status: 'received',
          page: 1,
          pageSize: 20,
        },
      });
      expect(res).toEqual(mockData);
    });

    it('getTeachingDemandById invokes GET /v1/teaching/demands/{id}', async () => {
      const mockDemand = { id: demandId, demandCode: 'DEM-2026-0001' };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockDemand as never);

      const res = await getTeachingDemandById(demandId);
      expect(getSpy).toHaveBeenCalledWith(`/v1/teaching/demands/${demandId}`);
      expect(res).toEqual(mockDemand);
    });

    it('createTeachingDemand invokes POST /v1/teaching/demands', async () => {
      const payload = {
        sourceDomain: 'Academics',
        sourceWorkReference: '99999999-9999-9999-9999-999999999999',
        sourceWorkLabel: 'Matematika SD Kelas 4',
        academicPeriodId: periodId,
        requiredCapabilityCode: 'CAP-MATH-PRI',
        requiredAssignmentRoleId: roleId,
        expectedWorkloadHours: 24,
        requestedStartDate: '2026-07-01',
        requestedEndDate: '2026-12-31',
      };
      const mockResponse = { id: demandId, demandCode: 'DEM-2026-0001', status: 'received' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await createTeachingDemand(payload);
      expect(postSpy).toHaveBeenCalledWith('/v1/teaching/demands', payload);
      expect(res).toEqual(mockResponse);
    });

    it('updateTeachingDemand invokes PUT /v1/teaching/demands/{id}', async () => {
      const payload = {
        sourceWorkLabel: 'Matematika SD Kelas 4 Updated',
        requiredCapabilityCode: 'CAP-MATH-PRI',
        requiredAssignmentRoleId: roleId,
        expectedWorkloadHours: 20,
        requestedStartDate: '2026-07-01',
        requestedEndDate: '2026-12-31',
      };
      const mockResponse = { id: demandId, sourceWorkLabel: payload.sourceWorkLabel };
      const putSpy = vi.spyOn(apiClient, 'put').mockResolvedValueOnce(mockResponse as never);

      const res = await updateTeachingDemand(demandId, payload);
      expect(putSpy).toHaveBeenCalledWith(`/v1/teaching/demands/${demandId}`, payload);
      expect(res).toEqual(mockResponse);
    });

    it('updateTeachingDemandStatus invokes POST /v1/teaching/demands/{id}/status', async () => {
      const payload = { status: 'accepted_for_staffing' };
      const mockResponse = { id: demandId, status: 'accepted_for_staffing' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await updateTeachingDemandStatus(demandId, payload);
      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/demands/${demandId}/status`, payload);
      expect(res).toEqual(mockResponse);
    });

    it('evaluateCandidate invokes POST /v1/teaching/eligibility/evaluate', async () => {
      const payload = { teachingDemandId: demandId, teacherProfileId: teacherId };
      const mockResponse = { id: 'eval-1', isEligible: true, eligibilityStatus: 'eligible' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await evaluateCandidate(payload);
      expect(postSpy).toHaveBeenCalledWith('/v1/teaching/eligibility/evaluate', payload);
      expect(res).toEqual(mockResponse);
    });

    it('getCandidateEvaluations invokes GET /v1/teaching/eligibility/evaluations', async () => {
      const mockEvaluations = [{ id: 'eval-1', demandCode: 'DEM-2026-0001' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockEvaluations as never);

      const res = await getCandidateEvaluations({ teachingDemandId: demandId });
      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/eligibility/evaluations', {
        params: { teachingDemandId: demandId },
      });
      expect(res).toEqual(mockEvaluations);
    });

    it('getTeachingAssignments invokes GET /v1/teaching/assignments', async () => {
      const mockData = { items: [], totalCount: 0 };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockData as never);

      const res = await getTeachingAssignments({ status: 'approved' });
      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/assignments', {
        params: { status: 'approved' },
      });
      expect(res).toEqual(mockData);
    });

    it('proposeTeachingAssignment invokes POST /v1/teaching/assignments/propose', async () => {
      const payload = {
        teachingDemandId: demandId,
        teacherProfileId: teacherId,
        assignmentRoleId: roleId,
        assignmentScope: 'learning_unit',
        scopeReferenceId: '99999999-9999-9999-9999-999999999999',
        scopeLabel: 'Matematika SD Kelas 4',
        academicPeriodId: periodId,
        effectiveStartDate: '2026-07-01',
        effectiveEndDate: '2026-12-31',
        workloadCommitmentValue: 18,
      };
      const mockResponse = { id: assignmentId, assignmentNumber: 'ASN-2026-0001', status: 'proposed' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await proposeTeachingAssignment(payload);
      expect(postSpy).toHaveBeenCalledWith('/v1/teaching/assignments/propose', payload);
      expect(res).toEqual(mockResponse);
    });

    it('approveTeachingAssignment invokes POST /v1/teaching/assignments/{id}/approve', async () => {
      const mockResponse = { id: assignmentId, assignmentNumber: 'ASN-2026-0001', status: 'approved' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await approveTeachingAssignment(assignmentId, { notes: 'Approved' });
      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/assignments/${assignmentId}/approve`, {
        notes: 'Approved',
      });
      expect(res).toEqual(mockResponse);
    });

    it('acknowledgeTeachingAssignment invokes POST /v1/teaching/assignments/{id}/acknowledge', async () => {
      const mockResponse = { id: assignmentId, teacherAcknowledged: true };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await acknowledgeTeachingAssignment(assignmentId);
      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/assignments/${assignmentId}/acknowledge`, {});
      expect(res).toEqual(mockResponse);
    });

    it('updateTeachingAssignmentStatus invokes POST /v1/teaching/assignments/{id}/status', async () => {
      const mockResponse = { id: assignmentId, status: 'suspended' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await updateTeachingAssignmentStatus(assignmentId, { status: 'suspended' });
      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/assignments/${assignmentId}/status`, {
        status: 'suspended',
      });
      expect(res).toEqual(mockResponse);
    });

    it('getAssignmentRoles invokes GET /v1/teaching/assignment-roles', async () => {
      const mockRoles = [{ id: roleId, code: 'LEAD_TEACHER', name: 'Guru Utama' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockRoles as never);

      const res = await getAssignmentRoles({ activeOnly: true });
      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/assignment-roles', {
        params: { activeOnly: true },
      });
      expect(res).toEqual(mockRoles);
    });

    it('getCapacityConflicts invokes GET /v1/teaching/conflicts', async () => {
      const mockData = { items: [] };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockData as never);

      const res = await getCapacityConflicts({ severity: 'blocking' });
      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/conflicts', {
        params: { severity: 'blocking' },
      });
      expect(res).toEqual(mockData);
    });

    it('resolveCapacityConflict invokes POST /v1/teaching/conflicts/{id}/resolve', async () => {
      const payload = { status: 'resolved', resolutionNotes: 'Waived' };
      const mockResponse = { id: conflictId, status: 'resolved' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await resolveCapacityConflict(conflictId, payload);
      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/conflicts/${conflictId}/resolve`, payload);
      expect(res).toEqual(mockResponse);
    });

    it('getTeachingSubstitutions invokes GET /v1/teaching/substitutions', async () => {
      const mockData = { items: [] };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockData as never);

      const res = await getTeachingSubstitutions({ status: 'approved' });
      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/substitutions', {
        params: { status: 'approved' },
      });
      expect(res).toEqual(mockData);
    });

    it('arrangeEmergencySubstitution invokes POST /v1/teaching/substitutions', async () => {
      const payload = {
        originalAssignmentId: assignmentId,
        substituteTeacherId,
        startDate: '2026-09-01',
        endDate: '2026-09-14',
        coverageScopeType: 'full',
        reason: 'medical_leave',
        workloadImpactHours: 18,
      };
      const mockResponse = {
        id: substitutionId,
        substitutionNumber: 'SUB-2026-0001',
        status: 'approved',
        isDelegatedApproval: true,
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await arrangeEmergencySubstitution(payload);
      expect(postSpy).toHaveBeenCalledWith('/v1/teaching/substitutions', payload);
      expect(res).toEqual(mockResponse);
    });

    it('approveSubstitutionException invokes POST /v1/teaching/substitutions/{id}/approve', async () => {
      const mockResponse = { id: substitutionId, status: 'approved' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await approveSubstitutionException(substitutionId, { decisionRemarks: 'Approved' });
      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/substitutions/${substitutionId}/approve`, {
        decisionRemarks: 'Approved',
      });
      expect(res).toEqual(mockResponse);
    });

    it('cancelSubstitution invokes POST /v1/teaching/substitutions/{id}/cancel', async () => {
      const mockResponse = { id: substitutionId, status: 'cancelled' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await cancelSubstitution(substitutionId, { reason: 'Teacher returned early' });
      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/substitutions/${substitutionId}/cancel`, {
        reason: 'Teacher returned early',
      });
      expect(res).toEqual(mockResponse);
    });

    it('getTeachingReassignments invokes GET /v1/teaching/reassignments', async () => {
      const mockData = { items: [] };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockData as never);

      const res = await getTeachingReassignments({ status: 'proposed' });
      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/reassignments', {
        params: { status: 'proposed' },
      });
      expect(res).toEqual(mockData);
    });

    it('proposePermanentReassignment invokes POST /v1/teaching/reassignments', async () => {
      const payload = {
        originalAssignmentId: assignmentId,
        newTeacherId: substituteTeacherId,
        effectiveDate: '2026-10-01',
        reason: 'Permanent transfer',
      };
      const mockResponse = {
        reassignment: {
          id: reassignmentId,
          reassignmentNumber: 'REA-2026-0001',
          status: 'proposed',
        },
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await proposePermanentReassignment(payload);
      expect(postSpy).toHaveBeenCalledWith('/v1/teaching/reassignments', payload);
      expect(res).toEqual(mockResponse);
    });

    it('approvePermanentReassignment invokes POST /v1/teaching/reassignments/{id}/approve', async () => {
      const mockResponse = { id: reassignmentId, status: 'approved' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await approvePermanentReassignment(reassignmentId);
      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/reassignments/${reassignmentId}/approve`, {});
      expect(res).toEqual(mockResponse);
    });

    it('confirmReassignmentHandover invokes POST /v1/teaching/reassignments/{id}/handover', async () => {
      const mockResponse = { id: reassignmentId, handoverStatus: 'completed' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await confirmReassignmentHandover(reassignmentId, { handoverStatus: 'completed' });
      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/reassignments/${reassignmentId}/handover`, {
        handoverStatus: 'completed',
      });
      expect(res).toEqual(mockResponse);
    });

    it('getAssignmentCompletions invokes GET /v1/teaching/completions', async () => {
      const mockCompletions = [{ id: 'comp-1', teachingAssignmentId: assignmentId }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockCompletions as never);

      const res = await getAssignmentCompletions({ academicPeriodId: periodId });
      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/completions', {
        params: { academicPeriodId: periodId },
      });
      expect(res).toEqual(mockCompletions);
    });

    it('concludeTeachingAssignment invokes POST /v1/teaching/assignments/{id}/conclude', async () => {
      const payload = {
        completionDate: '2026-12-20',
        academicConfirmationReceived: true,
        handoverConfirmed: true,
      };
      const mockResponse = {
        id: 'comp-1',
        assignmentNumber: 'ASN-2026-0001',
        status: 'completed',
        releasedWorkloadHours: 18,
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await concludeTeachingAssignment(assignmentId, payload);
      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/assignments/${assignmentId}/conclude`, payload);
      expect(res).toEqual(mockResponse);
    });

    it('getTeacherHistory invokes GET /v1/teaching/teachers/{teacherId}/history', async () => {
      const mockHistory = [{ id: 'hist-1', eventType: 'AssignmentApproved' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockHistory as never);

      const res = await getTeacherHistory(teacherId, { eventType: 'AssignmentApproved' });
      expect(getSpy).toHaveBeenCalledWith(`/v1/teaching/teachers/${teacherId}/history`, {
        params: { eventType: 'AssignmentApproved' },
      });
      expect(res).toEqual(mockHistory);
    });
  });

  // ==========================================================================
  // Query Keys Hierarchy Tests
  // ==========================================================================
  describe('Query Keys Hierarchy', () => {
    it('generates consistent hierarchical query keys', () => {
      expect(demandsKeys.all).toEqual(['teaching', 'demands']);
      expect(demandsKeys.list({ status: 'received' })).toEqual([
        'teaching',
        'demands',
        'list',
        { status: 'received' },
      ]);
      expect(demandsKeys.detail(demandId)).toEqual(['teaching', 'demands', 'detail', demandId]);

      expect(evaluationsKeys.all).toEqual(['teaching', 'evaluations']);
      expect(assignmentsKeys.all).toEqual(['teaching', 'assignments']);
      expect(assignmentRolesKeys.all).toEqual(['teaching', 'assignment-roles']);
      expect(conflictsKeys.all).toEqual(['teaching', 'conflicts']);
      expect(substitutionsKeys.all).toEqual(['teaching', 'substitutions']);
      expect(reassignmentsKeys.all).toEqual(['teaching', 'reassignments']);
      expect(completionsKeys.all).toEqual(['teaching', 'completions']);
      expect(historyKeys.all).toEqual(['teaching', 'history']);
      expect(teachingAssignmentsKeys.demands).toBe(demandsKeys);
    });
  });
});
