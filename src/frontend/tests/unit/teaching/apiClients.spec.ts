// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import { ApiError } from '@/shared/types/common';
import {
  assignmentsApi,
  getTeachingDemands,
  getTeachingDemandById,
  createTeachingDemand,
  updateTeachingDemand,
  updateTeachingDemandStatus,
  evaluateCandidate,
  getCandidateEvaluations,
  getTeachingAssignments,
  getTeachingAssignmentById,
  proposeTeachingAssignment,
  approveTeachingAssignment,
  acknowledgeTeachingAssignment,
  updateTeachingAssignmentStatus,
  getAssignmentRoles,
  getCapacityConflicts,
  resolveCapacityConflict,
  getTeachingSubstitutions,
  arrangeEmergencySubstitution,
  approveSubstitutionException,
  cancelSubstitution,
  getTeachingReassignments,
  getTeachingReassignmentById,
  proposePermanentReassignment,
  approvePermanentReassignment,
  confirmReassignmentHandover,
  getAssignmentCompletions,
  concludeTeachingAssignment,
  getTeacherHistory,
} from '@/features/teaching/assignments/api/assignmentsApi';
import {
  workloadApi,
  getWorkloadPolicies,
  getWorkloadCommitments,
  recalculateWorkload,
  getWorkloadExceptions,
  requestWorkloadException,
  approveWorkloadException,
} from '@/features/teaching/workload/api/workloadApi';
import {
  availabilityApi,
  getTeacherAvailability,
  declareTeacherAvailability,
  getTeacherUnavailabilityWindows,
  getUnavailabilityWindows,
  createUnavailabilityWindow,
  cancelUnavailabilityWindow,
} from '@/features/teaching/availability/api/availabilityApi';

describe('Teaching API Clients & Error Handling (P9-S29)', () => {
  const periodId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  const teacherId = '11111111-1111-1111-1111-111111111111';
  const demandId = '22222222-2222-2222-2222-222222222222';
  const assignmentId = '33333333-3333-3333-3333-333333333333';
  const conflictId = '44444444-4444-4444-4444-444444444444';
  const substitutionId = '55555555-5555-5555-5555-555555555555';
  const reassignmentId = '66666666-6666-6666-6666-666666666666';
  const exceptionId = '77777777-7777-7777-7777-777777777777';
  const windowId = '88888888-8888-8888-8888-888888888888';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  // ==========================================================================
  // 1. assignmentsApi Tests
  // ==========================================================================
  describe('assignmentsApi Client', () => {
    it('verifies assignmentsApi barrel object export contains all methods', () => {
      expect(assignmentsApi.getTeachingDemands).toBe(getTeachingDemands);
      expect(assignmentsApi.getTeachingDemandById).toBe(getTeachingDemandById);
      expect(assignmentsApi.createTeachingDemand).toBe(createTeachingDemand);
      expect(assignmentsApi.updateTeachingDemand).toBe(updateTeachingDemand);
      expect(assignmentsApi.updateTeachingDemandStatus).toBe(updateTeachingDemandStatus);
      expect(assignmentsApi.evaluateCandidate).toBe(evaluateCandidate);
      expect(assignmentsApi.getCandidateEvaluations).toBe(getCandidateEvaluations);
      expect(assignmentsApi.getTeachingAssignments).toBe(getTeachingAssignments);
      expect(assignmentsApi.getTeachingAssignmentById).toBe(getTeachingAssignmentById);
      expect(assignmentsApi.proposeTeachingAssignment).toBe(proposeTeachingAssignment);
      expect(assignmentsApi.approveTeachingAssignment).toBe(approveTeachingAssignment);
      expect(assignmentsApi.acknowledgeTeachingAssignment).toBe(acknowledgeTeachingAssignment);
      expect(assignmentsApi.updateTeachingAssignmentStatus).toBe(updateTeachingAssignmentStatus);
      expect(assignmentsApi.getAssignmentRoles).toBe(getAssignmentRoles);
      expect(assignmentsApi.getCapacityConflicts).toBe(getCapacityConflicts);
      expect(assignmentsApi.resolveCapacityConflict).toBe(resolveCapacityConflict);
      expect(assignmentsApi.getTeachingSubstitutions).toBe(getTeachingSubstitutions);
      expect(assignmentsApi.arrangeEmergencySubstitution).toBe(arrangeEmergencySubstitution);
      expect(assignmentsApi.approveSubstitutionException).toBe(approveSubstitutionException);
      expect(assignmentsApi.cancelSubstitution).toBe(cancelSubstitution);
      expect(assignmentsApi.getTeachingReassignments).toBe(getTeachingReassignments);
      expect(assignmentsApi.getTeachingReassignmentById).toBe(getTeachingReassignmentById);
      expect(assignmentsApi.proposePermanentReassignment).toBe(proposePermanentReassignment);
      expect(assignmentsApi.approvePermanentReassignment).toBe(approvePermanentReassignment);
      expect(assignmentsApi.confirmReassignmentHandover).toBe(confirmReassignmentHandover);
      expect(assignmentsApi.getAssignmentCompletions).toBe(getAssignmentCompletions);
      expect(assignmentsApi.concludeTeachingAssignment).toBe(concludeTeachingAssignment);
      expect(assignmentsApi.getTeacherHistory).toBe(getTeacherHistory);
    });

    it('getTeachingDemands queries demands with filters and pagination serialization', async () => {
      const mockResult = {
        items: [{ id: demandId, demandCode: 'DEM-001' }],
        totalCount: 1,
        pageNumber: 1,
        pageSize: 20,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResult as never);

      const params = {
        academicPeriodId: periodId,
        sourceDomain: 'Academics',
        status: 'received' as const,
        campusScope: 'Kampus Utama',
        page: 2,
        pageSize: 10,
      };

      const res = await getTeachingDemands(params);
      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/demands', {
        params: {
          academicPeriodId: periodId,
          sourceDomain: 'Academics',
          status: 'received',
          campusScope: 'Kampus Utama',
          page: 2,
          pageSize: 10,
        },
      });
      expect(res).toEqual(mockResult);
    });

    it('getTeachingDemandById encodes demandId path param', async () => {
      const mockDemand = { id: demandId, demandCode: 'DEM-001' };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockDemand as never);

      const res = await getTeachingDemandById(demandId);
      expect(getSpy).toHaveBeenCalledWith(`/v1/teaching/demands/${encodeURIComponent(demandId)}`);
      expect(res).toEqual(mockDemand);
    });

    it('createTeachingDemand sends POST with payload and receives created entity', async () => {
      const payload = {
        sourceDomain: 'Academics',
        sourceWorkReference: 'ref-1',
        sourceWorkLabel: 'Matematika SD',
        academicPeriodId: periodId,
        requiredCapabilityCode: 'CAP-MATH',
        requiredAssignmentRoleId: 'role-1',
        expectedWorkloadHours: 24,
        requestedStartDate: '2026-07-01',
        requestedEndDate: '2026-12-31',
        campusScope: 'Kampus Utama',
      };
      const mockCreated = { id: demandId, ...payload };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockCreated as never);

      const res = await createTeachingDemand(payload);
      expect(postSpy).toHaveBeenCalledWith('/v1/teaching/demands', payload);
      expect(res).toEqual(mockCreated);
    });

    it('updateTeachingDemand sends PUT to update demand details', async () => {
      const updatePayload = {
        sourceWorkLabel: 'Matematika SD Kelas 4B',
        expectedWorkloadHours: 20,
      };
      const putSpy = vi.spyOn(apiClient, 'put').mockResolvedValueOnce({ id: demandId, ...updatePayload } as never);

      const res = await updateTeachingDemand(demandId, updatePayload);
      expect(putSpy).toHaveBeenCalledWith(`/v1/teaching/demands/${encodeURIComponent(demandId)}`, updatePayload);
      expect(res.sourceWorkLabel).toBe('Matematika SD Kelas 4B');
    });

    it('updateTeachingDemandStatus executes lifecycle status transition', async () => {
      const statusPayload = { status: 'declined' as const, declineReason: 'Insufficient staff' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: demandId, status: 'declined' } as never);

      const res = await updateTeachingDemandStatus(demandId, statusPayload);
      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/demands/${encodeURIComponent(demandId)}/status`, statusPayload);
      expect(res.status).toBe('declined');
    });

    it('evaluateCandidate evaluates faculty eligibility against demand', async () => {
      const evalPayload = { teachingDemandId: demandId, teacherProfileId: teacherId };
      const mockEval = {
        evaluationId: 'eval-1',
        isEligible: true,
        determinationStatus: 'eligible',
        explanation: 'Candidate meets all requirements',
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockEval as never);

      const res = await evaluateCandidate(evalPayload);
      expect(postSpy).toHaveBeenCalledWith('/v1/teaching/eligibility/evaluate', evalPayload);
      expect(res.isEligible).toBe(true);
    });

    it('getCandidateEvaluations serializes filter params', async () => {
      const mockEvals = [{ evaluationId: 'eval-1', teachingDemandId: demandId }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockEvals as never);

      const res = await getCandidateEvaluations({ teachingDemandId: demandId, teacherProfileId: teacherId });
      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/eligibility/evaluations', {
        params: { teachingDemandId: demandId, teacherProfileId: teacherId },
      });
      expect(res).toEqual(mockEvals);
    });

    it('getTeachingAssignments and getTeachingAssignmentById serialize query params and path', async () => {
      const mockResult = { items: [{ id: assignmentId }] };
      const getSpy = vi.spyOn(apiClient, 'get')
        .mockResolvedValueOnce(mockResult as never)
        .mockResolvedValueOnce({ id: assignmentId, status: 'approved' } as never);

      const params = {
        academicPeriodId: periodId,
        teacherProfileId: teacherId,
        status: 'approved' as const,
      };
      const res = await getTeachingAssignments(params);
      expect(getSpy).toHaveBeenNthCalledWith(1, '/v1/teaching/assignments', {
        params: {
          academicPeriodId: periodId,
          teacherProfileId: teacherId,
          status: 'approved',
        },
      });
      expect(res).toEqual(mockResult);

      const single = await getTeachingAssignmentById(assignmentId);
      expect(getSpy).toHaveBeenNthCalledWith(2, `/v1/teaching/assignments/${encodeURIComponent(assignmentId)}`);
      expect(single.id).toBe(assignmentId);
    });

    it('proposeTeachingAssignment and approveTeachingAssignment dispatch correct requests', async () => {
      const proposePayload = {
        teachingDemandId: demandId,
        teacherProfileId: teacherId,
        assignmentRoleId: 'role-1',
        assignmentScope: 'learning_unit' as const,
        scopeReferenceId: 'scope-1',
        scopeLabel: 'Math 4',
        academicPeriodId: periodId,
        effectiveStartDate: '2026-07-01',
        effectiveEndDate: '2026-12-31',
        workloadCommitmentValue: 12,
      };
      const postProposeSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: assignmentId, status: 'proposed' } as never);
      const resProp = await proposeTeachingAssignment(proposePayload);
      expect(postProposeSpy).toHaveBeenCalledWith('/v1/teaching/assignments/propose', proposePayload);
      expect(resProp.status).toBe('proposed');

      const postApproveSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: assignmentId, status: 'approved' } as never);
      const resAppr = await approveTeachingAssignment(assignmentId, { approvalNotes: 'All checks passed' });
      expect(postApproveSpy).toHaveBeenCalledWith(
        `/v1/teaching/assignments/${encodeURIComponent(assignmentId)}/approve`,
        { approvalNotes: 'All checks passed' },
      );
      expect(resAppr.status).toBe('approved');
    });

    it('acknowledgeTeachingAssignment and updateTeachingAssignmentStatus post to correct paths', async () => {
      const ackSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: assignmentId, acknowledged: true } as never);
      await acknowledgeTeachingAssignment(assignmentId);
      expect(ackSpy).toHaveBeenCalledWith(
        `/v1/teaching/assignments/${encodeURIComponent(assignmentId)}/acknowledge`,
        {},
      );

      const statusSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: assignmentId, status: 'suspended' } as never);
      await updateTeachingAssignmentStatus(assignmentId, { status: 'suspended', reason: 'Medical absence' });
      expect(statusSpy).toHaveBeenCalledWith(`/v1/teaching/assignments/${encodeURIComponent(assignmentId)}/status`, {
        status: 'suspended',
        reason: 'Medical absence',
      });
    });

    it('getAssignmentRoles and getCapacityConflicts invoke GET endpoints', async () => {
      const getSpy = vi.spyOn(apiClient, 'get')
        .mockResolvedValueOnce([{ id: 'r1', code: 'ROLE-01' }] as never)
        .mockResolvedValueOnce([{ id: conflictId, severity: 'blocking' }] as never);

      const roles = await getAssignmentRoles();
      expect(roles).toHaveLength(1);
      expect(getSpy).toHaveBeenNthCalledWith(1, '/v1/teaching/assignment-roles', undefined);

      const conflicts = await getCapacityConflicts({ academicPeriodId: periodId, severity: 'blocking' });
      expect(conflicts).toHaveLength(1);
      expect(getSpy).toHaveBeenNthCalledWith(2, '/v1/teaching/conflicts', {
        params: { academicPeriodId: periodId, severity: 'blocking' },
      });
    });

    it('resolveCapacityConflict posts resolution payload', async () => {
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: conflictId, status: 'resolved' } as never);
      const res = await resolveCapacityConflict(conflictId, {
        status: 'resolved',
        resolutionAction: 'Reassigned teacher',
      });
      expect(postSpy).toHaveBeenCalledWith(`/v1/teaching/conflicts/${encodeURIComponent(conflictId)}/resolve`, {
        status: 'resolved',
        resolutionAction: 'Reassigned teacher',
      });
      expect(res.status).toBe('resolved');
    });

    it('getTeachingSubstitutions, arrangeEmergencySubstitution, approveSubstitutionException, and cancelSubstitution test coverage', async () => {
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce([{ id: substitutionId }] as never);
      const subs = await getTeachingSubstitutions({ originalAssignmentId: assignmentId, status: 'active' });
      expect(subs).toHaveLength(1);
      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/substitutions', {
        params: { originalAssignmentId: assignmentId, status: 'active' },
      });

      const subPayload = {
        originalAssignmentId: assignmentId,
        substituteTeacherId: 'teacher-2',
        startDate: '2026-09-01',
        endDate: '2026-09-15',
        coverageScopeType: 'full' as const,
        reason: 'Medical leave',
      };
      const postSubSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: substitutionId, status: 'active' } as never);
      const resSub = await arrangeEmergencySubstitution(subPayload);
      expect(postSubSpy).toHaveBeenCalledWith('/v1/teaching/substitutions', subPayload);
      expect(resSub.id).toBe(substitutionId);

      const apprSubSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: substitutionId, status: 'approved' } as never);
      await approveSubstitutionException(substitutionId, { approvalNotes: 'Exception allowed' });
      expect(apprSubSpy).toHaveBeenCalledWith(
        `/v1/teaching/substitutions/${encodeURIComponent(substitutionId)}/approve`,
        { approvalNotes: 'Exception allowed' },
      );

      const cancelSubSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: substitutionId, status: 'cancelled' } as never);
      await cancelSubstitution(substitutionId, { reason: 'Teacher returned early' });
      expect(cancelSubSpy).toHaveBeenCalledWith(
        `/v1/teaching/substitutions/${encodeURIComponent(substitutionId)}/cancel`,
        { reason: 'Teacher returned early' },
      );
    });

    it('permanent reassignments (get, propose, approve, handover) and conclusions test coverage', async () => {
      const getSpy = vi.spyOn(apiClient, 'get')
        .mockResolvedValueOnce([{ id: reassignmentId }] as never)
        .mockResolvedValueOnce({ id: reassignmentId, status: 'approved' } as never)
        .mockResolvedValueOnce([{ id: 'comp-1' }] as never);

      const reassignments = await getTeachingReassignments({ originalAssignmentId: assignmentId, status: 'approved' });
      expect(reassignments).toHaveLength(1);
      expect(getSpy).toHaveBeenNthCalledWith(1, '/v1/teaching/reassignments', {
        params: { originalAssignmentId: assignmentId, status: 'approved' },
      });

      const singleRea = await getTeachingReassignmentById(reassignmentId);
      expect(singleRea.id).toBe(reassignmentId);
      expect(getSpy).toHaveBeenNthCalledWith(2, `/v1/teaching/reassignments/${encodeURIComponent(reassignmentId)}`);

      const completions = await getAssignmentCompletions({ academicPeriodId: periodId });
      expect(completions).toHaveLength(1);
      expect(getSpy).toHaveBeenNthCalledWith(3, '/v1/teaching/completions', {
        params: { academicPeriodId: periodId },
      });

      const reassignPayload = {
        originalAssignmentId: assignmentId,
        newTeacherId: 'teacher-3',
        effectiveDate: '2026-10-01',
        reason: 'Permanent transfer',
      };
      const propReaSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: reassignmentId, status: 'proposed' } as never);
      await proposePermanentReassignment(reassignPayload);
      expect(propReaSpy).toHaveBeenCalledWith('/v1/teaching/reassignments', reassignPayload);

      const apprReaSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: reassignmentId, status: 'approved' } as never);
      await approvePermanentReassignment(reassignmentId, { approvalNotes: 'Transfer approved' });
      expect(apprReaSpy).toHaveBeenCalledWith(
        `/v1/teaching/reassignments/${encodeURIComponent(reassignmentId)}/approve`,
        { approvalNotes: 'Transfer approved' },
      );

      const handoverSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: reassignmentId, handoverStatus: 'confirmed' } as never);
      await confirmReassignmentHandover(reassignmentId, { handoverNotes: 'Materials passed' });
      expect(handoverSpy).toHaveBeenCalledWith(
        `/v1/teaching/reassignments/${encodeURIComponent(reassignmentId)}/handover`,
        { handoverNotes: 'Materials passed' },
      );

      const concludePayload = {
        completionDate: '2026-12-31',
        academicConfirmationReceived: true,
        handoverConfirmed: true,
      };
      const concludeSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: 'comp-1', assignmentId } as never);
      await concludeTeachingAssignment(assignmentId, concludePayload);
      expect(concludeSpy).toHaveBeenCalledWith(
        `/v1/teaching/assignments/${encodeURIComponent(assignmentId)}/conclude`,
        concludePayload,
      );
    });

    it('getTeacherHistory serializes teacher ID and query filters', async () => {
      const mockHistory = [{ id: 'hist-1', eventType: 'ASSIGNMENT_APPROVED' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockHistory as never);

      const res = await getTeacherHistory(teacherId, { eventType: 'ASSIGNMENT_APPROVED' });
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/history`,
        { params: { eventType: 'ASSIGNMENT_APPROVED' } },
      );
      expect(res).toEqual(mockHistory);
    });
  });

  // ==========================================================================
  // 2. workloadApi Tests
  // ==========================================================================
  describe('workloadApi Client', () => {
    it('verifies workloadApi barrel object export contains all methods', () => {
      expect(workloadApi.getWorkloadPolicies).toBe(getWorkloadPolicies);
      expect(workloadApi.getWorkloadCommitments).toBe(getWorkloadCommitments);
      expect(workloadApi.recalculateWorkload).toBe(recalculateWorkload);
      expect(workloadApi.getWorkloadExceptions).toBe(getWorkloadExceptions);
      expect(workloadApi.requestWorkloadException).toBe(requestWorkloadException);
      expect(workloadApi.approveWorkloadException).toBe(approveWorkloadException);
    });

    it('getWorkloadPolicies queries active policies with query params', async () => {
      const mockPolicies = [{ id: 'pol-1', code: 'WPOL-2026' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockPolicies as never);

      const res = await getWorkloadPolicies({ status: 'active', academicYearId: 'year-1' });
      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/workload/policies', {
        params: { status: 'active', academicYearId: 'year-1' },
      });
      expect(res).toEqual(mockPolicies);
    });

    it('getWorkloadCommitments queries commitments with period and status filters', async () => {
      const mockCommitments = [{ id: 'comm-1', status: 'normal' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockCommitments as never);

      const res = await getWorkloadCommitments({ academicPeriodId: periodId, status: 'normal' });
      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/workload/commitments', {
        params: { academicPeriodId: periodId, status: 'normal' },
      });
      expect(res).toEqual(mockCommitments);
    });

    it('getWorkloadExceptions queries exceptions with parameters', async () => {
      const mockExceptions = [{ id: exceptionId, status: 'requested' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockExceptions as never);

      const res = await getWorkloadExceptions({ academicPeriodId: periodId, teacherProfileId: teacherId });
      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/workload/exceptions', {
        params: { academicPeriodId: periodId, teacherProfileId: teacherId },
      });
      expect(res).toEqual(mockExceptions);
    });

    it('recalculateWorkload posts academic period recalculation command', async () => {
      const mockRecalc = { academicPeriodId: periodId, recalculatedFacultyCount: 42 };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockRecalc as never);

      const res = await recalculateWorkload({ academicPeriodId: periodId });
      expect(postSpy).toHaveBeenCalledWith('/v1/teaching/workload/recalculate', { academicPeriodId: periodId });
      expect(res.recalculatedFacultyCount).toBe(42);
    });

    it('requestWorkloadException and approveWorkloadException dispatch correct requests', async () => {
      const excPayload = {
        teacherProfileId: teacherId,
        academicPeriodId: periodId,
        requestedOverloadHours: 4,
        coordinatorJustification: 'Special Cambridge syllabus demands',
      };
      const postExcSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: exceptionId, status: 'requested' } as never);
      const resExc = await requestWorkloadException(excPayload);
      expect(postExcSpy).toHaveBeenCalledWith('/v1/teaching/workload/exceptions', excPayload);
      expect(resExc.id).toBe(exceptionId);

      const postApprSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: exceptionId, status: 'approved' } as never);
      const resAppr = await approveWorkloadException(exceptionId, { decisionRemarks: 'Director Approved' });
      expect(postApprSpy).toHaveBeenCalledWith(
        `/v1/teaching/workload/exceptions/${encodeURIComponent(exceptionId)}/approve`,
        { decisionRemarks: 'Director Approved' },
      );
      expect(resAppr.status).toBe('approved');
    });
  });

  // ==========================================================================
  // 3. availabilityApi Tests
  // ==========================================================================
  describe('availabilityApi Client', () => {
    it('verifies availabilityApi barrel object export contains all methods', () => {
      expect(availabilityApi.getTeacherAvailability).toBe(getTeacherAvailability);
      expect(availabilityApi.declareTeacherAvailability).toBe(declareTeacherAvailability);
      expect(availabilityApi.getTeacherUnavailabilityWindows).toBe(getTeacherUnavailabilityWindows);
      expect(availabilityApi.getUnavailabilityWindows).toBe(getUnavailabilityWindows);
      expect(availabilityApi.createUnavailabilityWindow).toBe(createUnavailabilityWindow);
      expect(availabilityApi.cancelUnavailabilityWindow).toBe(cancelUnavailabilityWindow);
    });

    it('getTeacherAvailability queries teacher weekly availability', async () => {
      const mockAvail = { id: 'avail-1', teacherProfileId: teacherId, maxAvailableHours: 24 };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockAvail as never);

      const res = await getTeacherAvailability(teacherId, periodId);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/availability`,
        { params: { academicPeriodId: periodId } },
      );
      expect(res).toEqual(mockAvail);
    });

    it('declareTeacherAvailability posts schedule and preferences', async () => {
      const declPayload = {
        academicPeriodId: periodId,
        maxAvailableHours: 20,
        effectiveStartDate: '2026-07-01',
        effectiveEndDate: '2026-12-31',
      };
      const mockSaved = { id: 'avail-1', ...declPayload };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockSaved as never);

      const res = await declareTeacherAvailability(teacherId, declPayload);
      expect(postSpy).toHaveBeenCalledWith(
        `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/availability`,
        declPayload,
      );
      expect(res).toEqual(mockSaved);
    });

    it('getTeacherUnavailabilityWindows, getUnavailabilityWindows, and create/cancel window requests', async () => {
      const mockWindows = [{ id: windowId, leaveType: 'medical_leave' }];
      const getSpy = vi.spyOn(apiClient, 'get')
        .mockResolvedValueOnce(mockWindows as never)
        .mockResolvedValueOnce(mockWindows as never);

      const resList = await getTeacherUnavailabilityWindows(teacherId, { status: 'active' });
      expect(getSpy).toHaveBeenNthCalledWith(1, 
        `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/unavailability`,
        { params: { status: 'active' } },
      );
      expect(resList).toEqual(mockWindows);

      const resLegacy = await getUnavailabilityWindows(teacherId);
      expect(getSpy).toHaveBeenNthCalledWith(2,
        `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/unavailability`,
        undefined,
      );
      expect(resLegacy).toEqual(mockWindows);

      const winPayload = {
        startDate: '2026-09-01',
        endDate: '2026-09-05',
        unavailabilityType: 'medical_leave' as const,
        reason: 'Recovery',
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: windowId, ...winPayload } as never);
      const resCreate = await createUnavailabilityWindow(teacherId, winPayload);
      expect(postSpy).toHaveBeenCalledWith(
        `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/unavailability`,
        winPayload,
      );
      expect(resCreate.id).toBe(windowId);

      const cancelSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: windowId, status: 'cancelled' } as never);
      await cancelUnavailabilityWindow(windowId);
      expect(cancelSpy).toHaveBeenCalledWith(
        `/v1/teaching/unavailability/${encodeURIComponent(windowId)}/cancel`,
      );
    });
  });

  // ==========================================================================
  // 4. API Error Handling Across HTTP Statuses
  // ==========================================================================
  describe('API Client Error Handling', () => {
    it('propagates 400 Bad Request with validation problem details', async () => {
      const validationError = new ApiError(
        400,
        'Bad Request',
        'One or more validation errors occurred.',
        'VALIDATION_FAILED',
        { requestedStartDate: ['Requested start date must be valid.'] },
      );
      vi.spyOn(apiClient, 'post').mockRejectedValueOnce(validationError);

      await expect(
        createTeachingDemand({
          sourceDomain: 'Academics',
          sourceWorkReference: 'ref-1',
          sourceWorkLabel: 'Math',
          academicPeriodId: periodId,
          requiredCapabilityCode: 'CAP-MATH',
          requiredAssignmentRoleId: 'role-1',
          expectedWorkloadHours: 24,
          requestedStartDate: 'invalid',
          requestedEndDate: '2026-12-31',
        }),
      ).rejects.toThrow(ApiError);
    });

    it('propagates 404 Not Found error for missing entity', async () => {
      const notFoundError = new ApiError(404, 'Resource Not Found', 'Demand not found.');
      vi.spyOn(apiClient, 'get').mockRejectedValueOnce(notFoundError);

      await expect(getTeachingDemandById('missing-id')).rejects.toThrow(ApiError);
    });

    it('propagates 409 Conflict error for concurrent or policy breach', async () => {
      const conflictError = new ApiError(
        409,
        'Business Rule Conflict',
        'Workload limit already breached without exception.',
        'WORKLOAD_LIMIT_BREACH',
      );
      vi.spyOn(apiClient, 'post').mockRejectedValueOnce(conflictError);

      await expect(
        approveTeachingAssignment(assignmentId, { approvalNotes: 'Proceed' }),
      ).rejects.toThrow(ApiError);
    });

    it('propagates 500 Server Error', async () => {
      const serverError = new ApiError(500, 'Internal Server Error', 'An unexpected database error occurred.');
      vi.spyOn(apiClient, 'post').mockRejectedValueOnce(serverError);

      await expect(
        recalculateWorkload({ academicPeriodId: periodId }),
      ).rejects.toThrow(ApiError);
    });

    it('propagates network disconnect error (status 0)', async () => {
      const networkError = new ApiError(0, 'Network Error', 'Unable to reach the server.');
      vi.spyOn(apiClient, 'get').mockRejectedValueOnce(networkError);

      await expect(getTeacherAvailability(teacherId, periodId)).rejects.toThrow('Unable to reach the server.');
    });
  });
});
