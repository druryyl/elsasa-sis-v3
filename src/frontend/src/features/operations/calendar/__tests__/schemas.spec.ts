// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  createAcademicYearSchema,
  updateAcademicYearSchema,
  approveAcademicYearSchema,
  createOperationalPeriodSchema,
  updateOperationalPeriodSchema,
  OPERATIONAL_PERIOD_TYPES,
  calendarEntrySchema,
  CALENDAR_DESIGNATIONS,
  declareSchoolClosureSchema,
  CLOSURE_REASON_CATEGORIES,
  AFFECTED_POPULATIONS,
  recordCorrectionSchema,
  CORRECTION_TARGET_TABLES,
} from '../schemas';

describe('Operations Calendar Zod Validation Schemas (schemas.spec.ts)', () => {
  // --------------------------------------------------------------------------
  // 1. Academic Year Schemas
  // --------------------------------------------------------------------------
  describe('Academic Year Schemas', () => {
    describe('createAcademicYearSchema', () => {
      it('validates a correct academic year payload', () => {
        const validData = {
          code: 'AY-2026-2027',
          name: 'Academic Year 2026/2027',
          startDate: '2026-07-01',
          endDate: '2027-06-30',
          notes: 'Standard academic calendar cycle',
        };

        const result = createAcademicYearSchema.safeParse(validData);
        expect(result.success).toBe(true);
        if (result.success) {
          expect(result.data.code).toBe('AY-2026-2027');
          expect(result.data.name).toBe('Academic Year 2026/2027');
        }
      });

      it('rejects missing or short code', () => {
        const result = createAcademicYearSchema.safeParse({
          code: 'AY',
          name: 'Academic Year 2026/2027',
          startDate: '2026-07-01',
          endDate: '2027-06-30',
        });
        expect(result.success).toBe(false);
        if (!result.success) {
          const errors = result.error.flatten().fieldErrors;
          expect(errors.code).toContain('Code must be at least 3 characters.');
        }
      });

      it('rejects invalid ISO dates', () => {
        const result = createAcademicYearSchema.safeParse({
          code: 'AY-2026-2027',
          name: 'Academic Year 2026/2027',
          startDate: '01/07/2026',
          endDate: '2027-06-30',
        });
        expect(result.success).toBe(false);
        if (!result.success) {
          const errors = result.error.flatten().fieldErrors;
          expect(errors.startDate).toContain('Start date must be in YYYY-MM-DD format.');
        }
      });

      it('rejects end date earlier than or equal to start date', () => {
        const result = createAcademicYearSchema.safeParse({
          code: 'AY-2026-2027',
          name: 'Academic Year 2026/2027',
          startDate: '2026-07-01',
          endDate: '2026-07-01',
        });
        expect(result.success).toBe(false);
        if (!result.success) {
          const errors = result.error.flatten().fieldErrors;
          expect(errors.endDate).toContain('End date must be greater than start date.');
        }
      });
    });

    describe('updateAcademicYearSchema', () => {
      it('validates a valid update payload', () => {
        const result = updateAcademicYearSchema.safeParse({
          name: 'Updated Name 2026/2027',
          startDate: '2026-08-01',
          endDate: '2027-07-31',
          notes: 'Updated note',
        });
        expect(result.success).toBe(true);
      });

      it('rejects invalid date sequence on update', () => {
        const result = updateAcademicYearSchema.safeParse({
          name: 'Updated Name',
          startDate: '2027-08-01',
          endDate: '2026-07-31',
        });
        expect(result.success).toBe(false);
      });
    });

    describe('approveAcademicYearSchema', () => {
      it('validates optional approval payload', () => {
        const result = approveAcademicYearSchema.safeParse({
          approvedBy: 'Academic Senate Dean',
          notes: 'Approved without reservations',
        });
        expect(result.success).toBe(true);
      });

      it('validates empty approval payload', () => {
        const result = approveAcademicYearSchema.safeParse({});
        expect(result.success).toBe(true);
      });
    });
  });

  // --------------------------------------------------------------------------
  // 2. Operational Period Schemas
  // --------------------------------------------------------------------------
  describe('Operational Period Schemas', () => {
    describe('createOperationalPeriodSchema', () => {
      it('validates a valid operational period across all 8 period types', () => {
        for (const type of OPERATIONAL_PERIOD_TYPES) {
          const result = createOperationalPeriodSchema.safeParse({
            periodCode: `P-${type.toUpperCase()}`,
            name: `Period ${type}`,
            periodType: type,
            startDate: '2026-07-15',
            endDate: '2026-12-20',
            campusContext: 'All',
            notes: 'Test notes',
          });
          expect(result.success).toBe(true);
        }
      });

      it('rejects unrecognized period type', () => {
        const result = createOperationalPeriodSchema.safeParse({
          periodCode: 'SEM-1',
          name: 'Semester 1',
          periodType: 'invalid_period_type',
          startDate: '2026-07-15',
          endDate: '2026-12-20',
        });
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.flatten().fieldErrors.periodType).toBeDefined();
        }
      });

      it('accepts valid UUID parentPeriodId and null/empty string', () => {
        const withUuid = createOperationalPeriodSchema.safeParse({
          periodCode: 'TERM-1A',
          name: 'Term 1A',
          periodType: 'term',
          startDate: '2026-07-15',
          endDate: '2026-09-30',
          parentPeriodId: 'e107df43-162e-4b47-b50a-e8d1a1005fb8',
        });
        expect(withUuid.success).toBe(true);

        const withEmpty = createOperationalPeriodSchema.safeParse({
          periodCode: 'SEM-1',
          name: 'Semester 1',
          periodType: 'semester',
          startDate: '2026-07-15',
          endDate: '2026-12-20',
          parentPeriodId: '',
        });
        expect(withEmpty.success).toBe(true);
      });

      it('rejects invalid UUID parentPeriodId', () => {
        const result = createOperationalPeriodSchema.safeParse({
          periodCode: 'TERM-1A',
          name: 'Term 1A',
          periodType: 'term',
          startDate: '2026-07-15',
          endDate: '2026-09-30',
          parentPeriodId: 'not-a-valid-uuid',
        });
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.flatten().fieldErrors.parentPeriodId).toContain(
            'Parent period ID must be a valid UUID.',
          );
        }
      });

      it('rejects endDate prior to startDate', () => {
        const result = createOperationalPeriodSchema.safeParse({
          periodCode: 'SEM-1',
          name: 'Semester 1',
          periodType: 'semester',
          startDate: '2026-12-20',
          endDate: '2026-07-15',
        });
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.flatten().fieldErrors.endDate).toContain(
            'End date must be greater than or equal to start date.',
          );
        }
      });
    });

    describe('updateOperationalPeriodSchema', () => {
      it('validates a valid operational period update', () => {
        const result = updateOperationalPeriodSchema.safeParse({
          name: 'Updated Semester 1 Name',
          startDate: '2026-07-20',
          endDate: '2026-12-23',
          campusContext: 'North Campus',
          notes: 'Extended by 3 days',
        });
        expect(result.success).toBe(true);
      });
    });
  });

  // --------------------------------------------------------------------------
  // 3. Calendar Entry Schema
  // --------------------------------------------------------------------------
  describe('Calendar Entry Schema', () => {
    it('validates a standard holiday calendar entry', () => {
      const result = calendarEntrySchema.safeParse({
        entryDate: '2026-08-17',
        designation: 'holiday',
        name: 'Hari Kemerdekaan RI',
        description: 'Indonesian Independence Day',
        isInstructional: false,
        appliesTo: 'All',
      });
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.isInstructional).toBe(false);
        expect(result.data.designation).toBe('holiday');
      }
    });

    it('validates a multi-day exam window with optional endDate', () => {
      const result = calendarEntrySchema.safeParse({
        entryDate: '2026-10-05',
        endDate: '2026-10-09',
        designation: 'exam_day',
        name: 'Midterm Examination Week',
        isInstructional: true,
      });
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.endDate).toBe('2026-10-09');
        expect(result.data.isInstructional).toBe(true);
      }
    });

    it('accepts all valid calendar designations', () => {
      for (const designation of CALENDAR_DESIGNATIONS) {
        const result = calendarEntrySchema.safeParse({
          entryDate: '2026-09-01',
          designation,
          name: `Entry for ${designation}`,
        });
        expect(result.success).toBe(true);
      }
    });

    it('rejects invalid designation and end date before entry date', () => {
      const invalidDesignation = calendarEntrySchema.safeParse({
        entryDate: '2026-09-01',
        designation: 'party_time',
        name: 'Celebration',
      });
      expect(invalidDesignation.success).toBe(false);

      const invalidDateSequence = calendarEntrySchema.safeParse({
        entryDate: '2026-09-10',
        endDate: '2026-09-05',
        designation: 'holiday',
        name: 'Invalid span',
      });
      expect(invalidDateSequence.success).toBe(false);
      if (!invalidDateSequence.success) {
        expect(invalidDateSequence.error.flatten().fieldErrors.endDate).toContain(
          'End date must be greater than or equal to entry date.',
        );
      }
    });
  });

  // --------------------------------------------------------------------------
  // 4. School Closure Schema (UC-OPS-12)
  // --------------------------------------------------------------------------
  describe('School Closure Schema (declareSchoolClosureSchema)', () => {
    it('validates an emergency campus closure declaration', () => {
      const validData = {
        academicYearId: 'e107df43-162e-4b47-b50a-e8d1a1005fb8',
        reasonCategory: 'weather_disaster',
        reasonDetails: 'Heavy flooding blocking access roads to campus',
        startDate: '2026-09-15',
        endDate: '2026-09-17',
        affectedCampus: 'Main Campus',
        affectedPopulation: 'all',
        authorizedBy: 'Operations Authority',
        recoveryPlanNotes: 'Saturday make-up instructional sessions scheduled',
      };

      const result = declareSchoolClosureSchema.safeParse(validData);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.reasonCategory).toBe('weather_disaster');
        expect(result.data.affectedPopulation).toBe('all');
      }
    });

    it('validates all allowed closure reason categories and affected populations', () => {
      for (const category of CLOSURE_REASON_CATEGORIES) {
        for (const pop of AFFECTED_POPULATIONS) {
          const result = declareSchoolClosureSchema.safeParse({
            reasonCategory: category,
            reasonDetails: `Legitimate operational closure reason details for ${category}`,
            startDate: '2026-11-01',
            endDate: '2026-11-01',
            affectedPopulation: pop,
          });
          expect(result.success).toBe(true);
        }
      }
    });

    it('rejects short reason details (< 5 characters)', () => {
      const result = declareSchoolClosureSchema.safeParse({
        reasonCategory: 'health_safety',
        reasonDetails: 'Flu',
        startDate: '2026-11-01',
        endDate: '2026-11-01',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.flatten().fieldErrors.reasonDetails).toContain(
          'Reason details must be at least 5 characters.',
        );
      }
    });

    it('rejects invalid academicYearId if provided as non-UUID', () => {
      const result = declareSchoolClosureSchema.safeParse({
        academicYearId: 'non-uuid-value',
        reasonCategory: 'emergency',
        reasonDetails: 'Local electrical grid failure',
        startDate: '2026-11-01',
        endDate: '2026-11-01',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.flatten().fieldErrors.academicYearId).toContain(
          'Academic Year ID must be a valid UUID.',
        );
      }
    });
  });

  // --------------------------------------------------------------------------
  // 5. Record Correction Schema (UC-OPS-13)
  // --------------------------------------------------------------------------
  describe('Record Correction Schema (recordCorrectionSchema)', () => {
    it('validates a traceable record correction for academic_years', () => {
      const validData = {
        academicYearId: 'e107df43-162e-4b47-b50a-e8d1a1005fb8',
        targetTableName: 'academic_years',
        targetRecordId: 'e107df43-162e-4b47-b50a-e8d1a1005fb8',
        fieldName: 'name',
        newValue: 'Tahun Pelajaran 2026/2027 (Revisi Senat)',
        justificationReason: 'Correcting typo in official Indonesian nomenclature',
        authorizedBy: 'Headmaster Academic',
      };

      const result = recordCorrectionSchema.safeParse(validData);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.targetTableName).toBe('academic_years');
        expect(result.data.justificationReason).toBe(
          'Correcting typo in official Indonesian nomenclature',
        );
      }
    });

    it('validates a record correction for operational_periods', () => {
      const result = recordCorrectionSchema.safeParse({
        targetTableName: 'operational_periods',
        targetRecordId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
        fieldName: 'end_date',
        newValue: '2026-12-24',
        justificationReason: 'Extending examination window by 2 days',
      });
      expect(result.success).toBe(true);
    });

    it('validates CORRECTION_TARGET_TABLES canonical constants', () => {
      expect(CORRECTION_TARGET_TABLES).toEqual(['academic_years', 'operational_periods']);
    });

    it('rejects disallowed target table names', () => {
      const result = recordCorrectionSchema.safeParse({
        targetTableName: 'users_passwords',
        targetRecordId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
        fieldName: 'password_hash',
        newValue: 'newhash',
        justificationReason: 'Unauthorized change attempt',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.flatten().fieldErrors.targetTableName).toContain(
          'Target table must be academic_years or operational_periods.',
        );
      }
    });

    it('rejects non-UUID targetRecordId', () => {
      const result = recordCorrectionSchema.safeParse({
        targetTableName: 'operational_periods',
        targetRecordId: 'period-123',
        fieldName: 'name',
        newValue: 'New Period Name',
        justificationReason: 'Clerical amendment per registrar decree',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.flatten().fieldErrors.targetRecordId).toContain(
          'Target record ID must be a valid UUID.',
        );
      }
    });

    it('rejects missing or short justification reason (< 5 characters)', () => {
      const result = recordCorrectionSchema.safeParse({
        targetTableName: 'operational_periods',
        targetRecordId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
        fieldName: 'name',
        newValue: 'New Name',
        justificationReason: 'fix',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.flatten().fieldErrors.justificationReason).toContain(
          'Justification reason must be at least 5 characters.',
        );
      }
    });
  });
});
