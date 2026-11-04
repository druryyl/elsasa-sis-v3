import { z } from 'zod';

/**
 * Zod validation schema for searching student financial accounts (SCR-FIN-03).
 */
export const searchStudentAccountsSchema = z.object({
  searchTerm: z
    .string()
    .trim()
    .max(100, 'Search term cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  campus: z
    .string()
    .trim()
    .max(64, 'Campus cannot exceed 64 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  grade: z
    .string()
    .trim()
    .max(32, 'Grade cannot exceed 32 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  status: z
    .string()
    .trim()
    .max(32, 'Status cannot exceed 32 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  pageNumber: z.number().int().min(1).default(1),
  pageSize: z.number().int().min(1).max(100).default(20),
});

export type SearchStudentAccountsSchemaInput = z.infer<typeof searchStudentAccountsSchema>;

/**
 * Zod validation schema for student account dossier lookup by student UUID.
 */
export const studentAccountParamsSchema = z.object({
  studentId: z
    .string({ required_error: 'Student ID is required.' })
    .uuid('Invalid student ID format.'),
});

export type StudentAccountParamsSchemaInput = z.infer<typeof studentAccountParamsSchema>;
