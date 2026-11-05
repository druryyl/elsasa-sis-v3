// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  submitCourseworkSchema,
  fileUploadSchema,
  SUBMISSION_TYPES,
  MAX_FILE_SIZE_BYTES,
  ALLOWED_MIME_TYPES,
  ALLOWED_FILE_EXTENSIONS,
} from '../schemas/learning.schema';

describe('Student Learning Zod Validation Schemas (P9-S24)', () => {
  // ==========================================================================
  // 1. Constants
  // ==========================================================================
  describe('Constants', () => {
    it('defines expected SUBMISSION_TYPES', () => {
      expect(SUBMISSION_TYPES).toEqual(['link', 'text', 'file']);
    });

    it('defines MAX_FILE_SIZE_BYTES as 25 MB', () => {
      expect(MAX_FILE_SIZE_BYTES).toBe(25 * 1024 * 1024);
    });

    it('defines expected ALLOWED_MIME_TYPES', () => {
      expect(ALLOWED_MIME_TYPES).toContain('application/pdf');
      expect(ALLOWED_MIME_TYPES).toContain('image/jpeg');
      expect(ALLOWED_MIME_TYPES).toContain('image/png');
      expect(ALLOWED_MIME_TYPES).toContain(
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      );
    });

    it('defines expected ALLOWED_FILE_EXTENSIONS', () => {
      expect(ALLOWED_FILE_EXTENSIONS).toEqual(['.pdf', '.jpg', '.jpeg', '.png', '.docx']);
    });
  });

  // ==========================================================================
  // 2. submitCourseworkSchema (SCR-SELF-03, UC-ACD-01)
  // ==========================================================================
  describe('submitCourseworkSchema', () => {
    const validStudentId = '11111111-1111-1111-1111-111111111111';

    describe('link submissions', () => {
      it('passes for valid HTTP and HTTPS URLs', () => {
        const httpsResult = submitCourseworkSchema.safeParse({
          studentId: validStudentId,
          submissionType: 'link',
          submissionReference: 'https://docs.google.com/document/d/12345/edit',
          notes: 'Please review section 2.',
        });
        expect(httpsResult.success).toBe(true);

        const httpResult = submitCourseworkSchema.safeParse({
          studentId: validStudentId,
          submissionType: 'link',
          submissionReference: 'http://myproject.internal/repo',
        });
        expect(httpResult.success).toBe(true);
      });

      it('rejects invalid URL strings for link submission', () => {
        const invalidUrlResult = submitCourseworkSchema.safeParse({
          studentId: validStudentId,
          submissionType: 'link',
          submissionReference: 'just a plain string note',
        });
        expect(invalidUrlResult.success).toBe(false);
        if (!invalidUrlResult.success) {
          expect(invalidUrlResult.error.issues[0].message).toContain(
            'must be a valid HTTP or HTTPS URL',
          );
        }
      });

      it('rejects non-HTTP protocols like ftp or javascript', () => {
        const ftpResult = submitCourseworkSchema.safeParse({
          studentId: validStudentId,
          submissionType: 'link',
          submissionReference: 'ftp://files.example.com/essay.pdf',
        });
        expect(ftpResult.success).toBe(false);

        const jsResult = submitCourseworkSchema.safeParse({
          studentId: validStudentId,
          submissionType: 'link',
          submissionReference: 'javascript:alert(1)',
        });
        expect(jsResult.success).toBe(false);
      });
    });

    describe('text and file submissions', () => {
      it('passes for non-URL text response when submissionType is text', () => {
        const textResult = submitCourseworkSchema.safeParse({
          studentId: validStudentId,
          submissionType: 'text',
          submissionReference: 'My comprehensive essay answering question 1...',
          notes: 'Completed in class',
        });
        expect(textResult.success).toBe(true);
      });

      it('passes for storage key or file URI when submissionType is file', () => {
        const fileResult = submitCourseworkSchema.safeParse({
          studentId: validStudentId,
          submissionType: 'file',
          submissionReference: '/uploads/academics/2026/submissions/essay-final.pdf',
        });
        expect(fileResult.success).toBe(true);
      });
    });

    describe('field validations', () => {
      it('rejects missing or empty studentId', () => {
        const emptyStudent = submitCourseworkSchema.safeParse({
          studentId: '   ',
          submissionType: 'text',
          submissionReference: 'Some text',
        });
        expect(emptyStudent.success).toBe(false);
      });

      it('rejects invalid submissionType', () => {
        const invalidType = submitCourseworkSchema.safeParse({
          studentId: validStudentId,
          submissionType: 'email',
          submissionReference: 'test@example.com',
        });
        expect(invalidType.success).toBe(false);
      });

      it('rejects empty or whitespace-only submissionReference', () => {
        const emptyRef = submitCourseworkSchema.safeParse({
          studentId: validStudentId,
          submissionType: 'text',
          submissionReference: '   ',
        });
        expect(emptyRef.success).toBe(false);
        if (!emptyRef.success) {
          expect(emptyRef.error.issues[0].message).toContain('required');
        }
      });

      it('rejects submissionReference exceeding 1000 characters', () => {
        const tooLongRef = submitCourseworkSchema.safeParse({
          studentId: validStudentId,
          submissionType: 'text',
          submissionReference: 'X'.repeat(1001),
        });
        expect(tooLongRef.success).toBe(false);
      });

      it('rejects notes exceeding 2000 characters', () => {
        const tooLongNotes = submitCourseworkSchema.safeParse({
          studentId: validStudentId,
          submissionType: 'text',
          submissionReference: 'Valid answer',
          notes: 'N'.repeat(2001),
        });
        expect(tooLongNotes.success).toBe(false);
      });
    });
  });

  // ==========================================================================
  // 3. fileUploadSchema (TD-07, P5-S11)
  // ==========================================================================
  describe('fileUploadSchema', () => {
    it('passes for a valid PDF file within size limits', () => {
      const validPdf = new File(['%PDF-1.4 dummy content'], 'assignment.pdf', {
        type: 'application/pdf',
      });
      const result = fileUploadSchema.safeParse({ file: validPdf });
      expect(result.success).toBe(true);
    });

    it('passes for allowed image files (PNG, JPEG)', () => {
      const pngFile = new File(['png content'], 'diagram.png', {
        type: 'image/png',
      });
      expect(fileUploadSchema.safeParse({ file: pngFile }).success).toBe(true);

      const jpgFile = new File(['jpg content'], 'scan.jpg', {
        type: 'image/jpeg',
      });
      expect(fileUploadSchema.safeParse({ file: jpgFile }).success).toBe(true);
    });

    it('passes for allowed Word document (.docx)', () => {
      const docxFile = new File(['docx content'], 'report.docx', {
        type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      });
      expect(fileUploadSchema.safeParse({ file: docxFile }).success).toBe(true);
    });

    it('rejects empty file (size === 0)', () => {
      const emptyFile = new File([], 'empty.pdf', {
        type: 'application/pdf',
      });
      const result = fileUploadSchema.safeParse({ file: emptyFile });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('File cannot be empty');
      }
    });

    it('rejects file exceeding MAX_FILE_SIZE_BYTES (25 MB)', () => {
      const oversizedFile = new File(['x'], 'large.pdf', {
        type: 'application/pdf',
      });
      // Mock the size property
      Object.defineProperty(oversizedFile, 'size', {
        value: 26 * 1024 * 1024,
      });

      const result = fileUploadSchema.safeParse({ file: oversizedFile });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('File size cannot exceed 25 MB');
      }
    });

    it('rejects unpermitted file extensions and MIME types', () => {
      const exeFile = new File(['MZ executable content'], 'malware.exe', {
        type: 'application/x-msdownload',
      });
      const result = fileUploadSchema.safeParse({ file: exeFile });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('File type not allowed');
      }
    });

    it('rejects non-File values', () => {
      const result = fileUploadSchema.safeParse({ file: 'not-a-file' });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('A file must be selected');
      }
    });
  });
});
