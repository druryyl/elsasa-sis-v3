// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  createDemandSchema,
  updateDemandStatusSchema,
  evaluateCandidateSchema,
  proposeAssignmentSchema,
  resolveConflictSchema,
  arrangeSubstituteSchema,
  reassignmentSchema,
  concludeAssignmentSchema,
} from '@/features/teaching/assignments/api/schemas';
import {
  requestExceptionSchema,
  approveExceptionSchema,
  recalculateWorkloadSchema,
} from '@/features/teaching/workload/api/schemas';
import {
  declareAvailabilitySchema,
  createUnavailabilityWindowSchema,
} from '@/features/teaching/availability/api/schemas';

describe('Teaching Domain Validation Schemas (P9-S29)', () => {
  const periodId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  const teacherId = '11111111-1111-1111-1111-111111111111';
  const roleId = '22222222-2222-2222-2222-222222222222';
  const demandId = '33333333-3333-3333-3333-333333333333';
  const assignmentId = '44444444-4444-4444-4444-444444444444';
  const newTeacherId = '55555555-5555-5555-5555-555555555555';

  // ==========================================================================
  // 1. Assignments Module Schemas
  // ==========================================================================
  describe('Assignments Schemas', () => {
    describe('createDemandSchema', () => {
      const validPayload = {
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

      it('accepts valid full payload and applies transformations and defaults', () => {
        const result = createDemandSchema.safeParse(validPayload);
        expect(result.success).toBe(true);
        if (result.success) {
          expect(result.data.workloadUnit).toBe('Teaching Hours');
          expect(result.data.requiresAssessmentAuthority).toBe(true);
          expect(result.data.requiredTeachersCount).toBe(1);
          expect(result.data.campusScope).toBe('Kampus Utama');
        }
      });

      it('rejects missing required fields', () => {
        const missing = { ...validPayload };
        // @ts-expect-error intentionally delete property
        delete missing.sourceDomain;
        const res = createDemandSchema.safeParse(missing);
        expect(res.success).toBe(false);
      });

      it('rejects invalid UUID for sourceWorkReference or academicPeriodId', () => {
        const invalidUuid = { ...validPayload, sourceWorkReference: 'not-a-uuid' };
        const res = createDemandSchema.safeParse(invalidUuid);
        expect(res.success).toBe(false);
        if (!res.success) {
          expect(res.error.issues[0]?.message).toMatch(/valid UUID/i);
        }
      });

      it('rejects non-positive expectedWorkloadHours', () => {
        const zeroHours = { ...validPayload, expectedWorkloadHours: 0 };
        const negativeHours = { ...validPayload, expectedWorkloadHours: -5 };
        expect(createDemandSchema.safeParse(zeroHours).success).toBe(false);
        expect(createDemandSchema.safeParse(negativeHours).success).toBe(false);
      });

      it('rejects requestedEndDate before requestedStartDate', () => {
        const reversedDates = {
          ...validPayload,
          requestedStartDate: '2026-12-31',
          requestedEndDate: '2026-07-01',
        };
        const res = createDemandSchema.safeParse(reversedDates);
        expect(res.success).toBe(false);
        if (!res.success) {
          expect(res.error.issues[0]?.message).toMatch(/on or after requested start date/i);
        }
      });

      it('validates demandCode format when provided', () => {
        const invalidCode = { ...validPayload, demandCode: 'ab' }; // too short (< 3)
        expect(createDemandSchema.safeParse(invalidCode).success).toBe(false);

        const validCode = { ...validPayload, demandCode: 'DEM-2026-MATH01' };
        expect(createDemandSchema.safeParse(validCode).success).toBe(true);
      });
    });

    describe('updateDemandStatusSchema', () => {
      it('validates accepted_for_staffing transition', () => {
        const res = updateDemandStatusSchema.safeParse({
          status: 'accepted_for_staffing',
        });
        expect(res.success).toBe(true);
      });

      it('requires declineReason when status is declined', () => {
        const withoutReason = updateDemandStatusSchema.safeParse({
          status: 'declined',
        });
        expect(withoutReason.success).toBe(false);

        const withReason = updateDemandStatusSchema.safeParse({
          status: 'declined',
          declineReason: 'No qualified faculty available for this semester.',
        });
        expect(withReason.success).toBe(true);
      });

      it('rejects invalid status transitions', () => {
        const res = updateDemandStatusSchema.safeParse({
          status: 'invalid_status',
        });
        expect(res.success).toBe(false);
      });
    });

    describe('evaluateCandidateSchema', () => {
      it('validates valid candidate evaluation payload', () => {
        const res = evaluateCandidateSchema.safeParse({
          teachingDemandId: demandId,
          teacherProfileId: teacherId,
          coordinatorRemarks: 'Priority candidate for primary math instruction.',
        });
        expect(res.success).toBe(true);
      });

      it('rejects missing or invalid UUID fields', () => {
        const res = evaluateCandidateSchema.safeParse({
          teachingDemandId: 'invalid-id',
          teacherProfileId: teacherId,
        });
        expect(res.success).toBe(false);
      });
    });

    describe('proposeAssignmentSchema', () => {
      const validProposal = {
        teachingDemandId: demandId,
        teacherProfileId: teacherId,
        assignmentRoleId: roleId,
        assignmentScope: 'learning_unit',
        scopeReferenceId: '99999999-9999-9999-9999-999999999999',
        scopeLabel: 'Matematika SD Kelas 4A',
        academicPeriodId: periodId,
        effectiveStartDate: '2026-07-01',
        effectiveEndDate: '2026-12-31',
        workloadCommitmentValue: 12,
      };

      it('accepts valid proposed assignment payload', () => {
        const res = proposeAssignmentSchema.safeParse(validProposal);
        expect(res.success).toBe(true);
        if (res.success) {
          expect(res.data.workloadUnit).toBe('Teaching Hours');
        }
      });

      it('rejects invalid scope category', () => {
        const invalidScope = { ...validProposal, assignmentScope: 'invalid_scope' };
        expect(proposeAssignmentSchema.safeParse(invalidScope).success).toBe(false);
      });

      it('rejects effectiveEndDate before effectiveStartDate', () => {
        const reversed = {
          ...validProposal,
          effectiveStartDate: '2026-12-31',
          effectiveEndDate: '2026-07-01',
        };
        const res = proposeAssignmentSchema.safeParse(reversed);
        expect(res.success).toBe(false);
        if (!res.success) {
          expect(res.error.issues[0]?.message).toMatch(/before or equal to effective end date/i);
        }
      });

      it('rejects negative or excessive workloadCommitmentValue', () => {
        expect(proposeAssignmentSchema.safeParse({ ...validProposal, workloadCommitmentValue: -1 }).success).toBe(false);
        expect(proposeAssignmentSchema.safeParse({ ...validProposal, workloadCommitmentValue: 101 }).success).toBe(false);
      });
    });

    describe('resolveConflictSchema', () => {
      it('validates resolved status', () => {
        const res = resolveConflictSchema.safeParse({
          status: 'resolved',
          resolutionAction: 'Reassigned co-teacher',
          resolutionNotes: 'Conflict resolved by reallocating 4 hours.',
        });
        expect(res.success).toBe(true);
      });

      it('validates waived_with_exception status', () => {
        const res = resolveConflictSchema.safeParse({
          status: 'waived_with_exception',
          resolutionAction: 'Approved by Academic Director',
        });
        expect(res.success).toBe(true);
      });

      it('rejects unknown status', () => {
        const res = resolveConflictSchema.safeParse({
          status: 'pending',
        });
        expect(res.success).toBe(false);
      });
    });

    describe('arrangeSubstituteSchema', () => {
      const validSub = {
        originalAssignmentId: assignmentId,
        substituteTeacherId: newTeacherId,
        startDate: '2026-09-01',
        endDate: '2026-09-15',
        coverageScopeType: 'full',
        coveredHoursPerWeek: 12,
        reason: 'Emergency medical leave coverage',
      };

      it('accepts valid substitute arrangement payload', () => {
        const res = arrangeSubstituteSchema.safeParse(validSub);
        expect(res.success).toBe(true);
      });

      it('rejects invalid coverageScopeType', () => {
        const invalid = { ...validSub, coverageScopeType: 'custom' };
        expect(arrangeSubstituteSchema.safeParse(invalid).success).toBe(false);
      });

      it('rejects endDate earlier than startDate', () => {
        const invalid = { ...validSub, startDate: '2026-09-15', endDate: '2026-09-01' };
        const res = arrangeSubstituteSchema.safeParse(invalid);
        expect(res.success).toBe(false);
        if (!res.success) {
          expect(res.error.issues[0]?.message).toMatch(/on or after start date/i);
        }
      });

      it('rejects empty reason', () => {
        const invalid = { ...validSub, reason: '' };
        expect(arrangeSubstituteSchema.safeParse(invalid).success).toBe(false);
      });
    });

    describe('reassignmentSchema', () => {
      const validReassignment = {
        originalAssignmentId: assignmentId,
        newTeacherId: newTeacherId,
        effectiveDate: '2026-10-01',
        reason: 'Permanent transfer due to relocation of primary teacher.',
      };

      it('accepts valid permanent reassignment payload with defaults', () => {
        const res = reassignmentSchema.safeParse(validReassignment);
        expect(res.success).toBe(true);
        if (res.success) {
          expect(res.data.requiresHandover).toBe(true);
          expect(res.data.reassignmentType).toBe('permanent_transfer');
        }
      });

      it('rejects invalid date format or empty reason', () => {
        expect(reassignmentSchema.safeParse({ ...validReassignment, effectiveDate: '01-10-2026' }).success).toBe(false);
        expect(reassignmentSchema.safeParse({ ...validReassignment, reason: '' }).success).toBe(false);
      });
    });

    describe('concludeAssignmentSchema', () => {
      const validConclusion = {
        completionDate: '2026-12-31',
        academicConfirmationReceived: true,
        academicConfirmationSource: 'Academics Portal Verification',
        handoverConfirmed: true,
        unresolvedItemsStatus: 'none',
        notes: 'Semester successfully completed with all grades submitted.',
      };

      it('accepts valid assignment conclusion payload', () => {
        const res = concludeAssignmentSchema.safeParse(validConclusion);
        expect(res.success).toBe(true);
        if (res.success) {
          expect(res.data.unresolvedItemsStatus).toBe('none');
          expect(res.data.academicConfirmationReceived).toBe(true);
        }
      });

      it('rejects invalid unresolved items status', () => {
        const res = concludeAssignmentSchema.safeParse({
          ...validConclusion,
          unresolvedItemsStatus: 'unknown_status',
        });
        expect(res.success).toBe(false);
      });
    });
  });

  // ==========================================================================
  // 2. Workload Module Schemas
  // ==========================================================================
  describe('Workload Schemas', () => {
    describe('requestExceptionSchema', () => {
      const validException = {
        teacherProfileId: teacherId,
        academicPeriodId: periodId,
        teachingAssignmentId: assignmentId,
        requestedOverloadHours: 4,
        coordinatorJustification: 'Exceptional co-teaching requirement for Cambridge bilingual stream.',
        effectiveStartDate: '2026-07-01',
        effectiveEndDate: '2026-12-31',
      };

      it('accepts valid workload exception request', () => {
        const res = requestExceptionSchema.safeParse(validException);
        expect(res.success).toBe(true);
        if (res.success) {
          expect(res.data.status).toBe('requested');
        }
      });

      it('rejects requestedOverloadHours <= 0 or > 80', () => {
        expect(requestExceptionSchema.safeParse({ ...validException, requestedOverloadHours: 0 }).success).toBe(false);
        expect(requestExceptionSchema.safeParse({ ...validException, requestedOverloadHours: 85 }).success).toBe(false);
      });

      it('rejects empty coordinator justification', () => {
        const res = requestExceptionSchema.safeParse({ ...validException, coordinatorJustification: '' });
        expect(res.success).toBe(false);
      });

      it('rejects effectiveEndDate earlier than effectiveStartDate', () => {
        const reversed = {
          ...validException,
          effectiveStartDate: '2026-12-31',
          effectiveEndDate: '2026-07-01',
        };
        const res = requestExceptionSchema.safeParse(reversed);
        expect(res.success).toBe(false);
        if (!res.success) {
          expect(res.error.issues[0]?.message).toMatch(/cannot precede effective start date/i);
        }
      });
    });

    describe('approveExceptionSchema', () => {
      it('accepts valid approval decision with remarks', () => {
        const res = approveExceptionSchema.safeParse({
          decisionRemarks: 'Approved following department head review.',
        });
        expect(res.success).toBe(true);
        if (res.success) {
          expect(res.data.decisionRemarks).toBe('Approved following department head review.');
        }
      });

      it('accepts approval decision with null remarks', () => {
        const res = approveExceptionSchema.safeParse({});
        expect(res.success).toBe(true);
      });
    });

    describe('recalculateWorkloadSchema', () => {
      it('accepts valid period UUID', () => {
        const res = recalculateWorkloadSchema.safeParse({ academicPeriodId: periodId });
        expect(res.success).toBe(true);
      });

      it('rejects invalid or non-UUID period', () => {
        expect(recalculateWorkloadSchema.safeParse({ academicPeriodId: 'invalid' }).success).toBe(false);
        expect(recalculateWorkloadSchema.safeParse({}).success).toBe(false);
      });
    });
  });

  // ==========================================================================
  // 3. Availability Module Schemas
  // ==========================================================================
  describe('Availability Schemas', () => {
    describe('declareAvailabilitySchema', () => {
      const validAvailability = {
        academicPeriodId: periodId,
        maxAvailableHours: 24,
        effectiveStartDate: '2026-07-01',
        effectiveEndDate: '2026-12-31',
        campusScope: 'Kampus Utama',
        availableDeliveryModes: 'in_person',
        recurringAvailableSchedule: '{"monday":["morning","afternoon"]}',
      };

      it('accepts valid availability declaration payload', () => {
        const res = declareAvailabilitySchema.safeParse(validAvailability);
        expect(res.success).toBe(true);
      });

      it('rejects maxAvailableHours exceeding 168 or <= 0', () => {
        expect(declareAvailabilitySchema.safeParse({ ...validAvailability, maxAvailableHours: 0 }).success).toBe(false);
        expect(declareAvailabilitySchema.safeParse({ ...validAvailability, maxAvailableHours: 200 }).success).toBe(false);
      });

      it('rejects reversed effective dates', () => {
        const reversed = {
          ...validAvailability,
          effectiveStartDate: '2026-12-31',
          effectiveEndDate: '2026-07-01',
        };
        const res = declareAvailabilitySchema.safeParse(reversed);
        expect(res.success).toBe(false);
      });
    });

    describe('createUnavailabilityWindowSchema', () => {
      const validWindow = {
        startDate: '2026-09-01',
        endDate: '2026-09-10',
        unavailabilityType: 'medical_leave',
        reason: 'Outpatient treatment and recovery',
        startTime: '08:00',
        endTime: '12:00',
        emergencyCoverageRequired: true,
      };

      it('accepts valid leave window payload', () => {
        const res = createUnavailabilityWindowSchema.safeParse(validWindow);
        expect(res.success).toBe(true);
        if (res.success) {
          expect(res.data.emergencyCoverageRequired).toBe(true);
        }
      });

      it('rejects invalid leave type', () => {
        const res = createUnavailabilityWindowSchema.safeParse({
          ...validWindow,
          unavailabilityType: 'annual_vacation',
        });
        expect(res.success).toBe(false);
      });

      it('rejects endDate before startDate', () => {
        const reversed = {
          ...validWindow,
          startDate: '2026-09-10',
          endDate: '2026-09-01',
        };
        const res = createUnavailabilityWindowSchema.safeParse(reversed);
        expect(res.success).toBe(false);
      });

      it('rejects endTime before startTime', () => {
        const reversedTime = {
          ...validWindow,
          startTime: '14:00',
          endTime: '10:00',
        };
        const res = createUnavailabilityWindowSchema.safeParse(reversedTime);
        expect(res.success).toBe(false);
        if (!res.success) {
          expect(res.error.issues[0]?.message).toMatch(/End time must be on or after start time/i);
        }
      });

      it('rejects invalid time format', () => {
        const invalidTime = {
          ...validWindow,
          startTime: '25:99',
        };
        const res = createUnavailabilityWindowSchema.safeParse(invalidTime);
        expect(res.success).toBe(false);
      });
    });
  });
});
