import { z } from 'zod';

export const SUBMISSION_TYPES = ['link', 'text', 'file'] as const;

export const MAX_FILE_SIZE_BYTES = 25 * 1024 * 1024; // 25 MB

export const ALLOWED_MIME_TYPES = [
  'application/pdf',
  'image/jpeg',
  'image/png',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
] as const;

export const ALLOWED_FILE_EXTENSIONS = ['.pdf', '.jpg', '.jpeg', '.png', '.docx'] as const;

// ============================================================================
// Student Coursework Submission Schema (SCR-SELF-03, TD-07, UC-ACD-01)
// ============================================================================

export const submitCourseworkSchema = z
  .object({
    studentId: z
      .string({ required_error: 'Student ID is required.' })
      .trim()
      .min(1, 'Student ID is required.'),
    submissionType: z.enum(SUBMISSION_TYPES, {
      required_error: 'Submission type is required.',
    }),
    submissionReference: z
      .string({ required_error: 'Submission reference is required.' })
      .trim()
      .min(1, 'Submission content or link is required.')
      .max(1000, 'Submission reference cannot exceed 1000 characters.'),
    notes: z
      .string()
      .trim()
      .max(2000, 'Notes cannot exceed 2000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .refine(
    (data) => {
      if (data.submissionType === 'link') {
        try {
          const parsed = new URL(data.submissionReference);
          return parsed.protocol === 'http:' || parsed.protocol === 'https:';
        } catch {
          return false;
        }
      }
      return true;
    },
    {
      message: 'Submission reference must be a valid HTTP or HTTPS URL when submission type is link.',
      path: ['submissionReference'],
    },
  );

export type SubmitCourseworkFormValues = z.infer<typeof submitCourseworkSchema>;

// ============================================================================
// File Upload Validation Schema (TD-07, P5-S11)
// ============================================================================

export const fileUploadSchema = z.object({
  file: z
    .custom<File>((val) => val instanceof File, 'A file must be selected.')
    .refine((file) => file.size > 0, 'File cannot be empty.')
    .refine(
      (file) => file.size <= MAX_FILE_SIZE_BYTES,
      `File size cannot exceed 25 MB.`,
    )
    .refine(
      (file) =>
        ALLOWED_MIME_TYPES.includes(file.type as (typeof ALLOWED_MIME_TYPES)[number]) ||
        ALLOWED_FILE_EXTENSIONS.some((ext) =>
          file.name.toLowerCase().endsWith(ext),
        ),
      'File type not allowed. Supported formats: PDF, JPEG, PNG, DOCX.',
    ),
});

export type FileUploadFormValues = z.infer<typeof fileUploadSchema>;
