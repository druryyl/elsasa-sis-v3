import { z } from 'zod';

/**
 * Zod validation schema for administrative user authentication.
 */
export const loginSchema = z.object({
  username: z.string().trim().min(1, { message: 'Username is required' }),
  password: z.string().min(1, { message: 'Password is required' }),
});

export type LoginFormValues = z.infer<typeof loginSchema>;
