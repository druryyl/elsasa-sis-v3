import { apiClient } from '@/shared/services/http';

/**
 * Authenticated user profile returned by the backend login endpoint.
 * Mirrors the backend `UserSessionDto` contract (ADR-009).
 */
export interface AuthenticatedUser {
  userId: string;
  username: string;
  fullName: string;
  roles: string[];
  permissions: string[];
  campusId: string | null;
  studentId?: string | null;
}

export interface LoginResponse {
  token: string;
  user: AuthenticatedUser;
}

/**
 * Authenticates a user against the backend and returns a signed JWT plus the
 * authoritative session profile (including the student identity claim).
 * POST /api/auth/login
 */
export async function login(username: string, password: string): Promise<LoginResponse> {
  const response = (await apiClient.post('/auth/login', {
    username,
    password,
  })) as unknown as LoginResponse;
  return response;
}
