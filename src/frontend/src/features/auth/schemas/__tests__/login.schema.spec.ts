import { describe, it, expect } from 'vitest';
import { loginSchema } from '../login.schema';

describe('login.schema.ts', () => {
  it('validates valid credentials successfully', () => {
    const result = loginSchema.safeParse({
      username: 'admin',
      password: 'secretpassword',
    });

    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.username).toBe('admin');
      expect(result.data.password).toBe('secretpassword');
    }
  });

  it('trims leading and trailing whitespace from username', () => {
    const result = loginSchema.safeParse({
      username: '  admin_user  ',
      password: 'password123',
    });

    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.username).toBe('admin_user');
    }
  });

  it('rejects empty username', () => {
    const result = loginSchema.safeParse({
      username: '',
      password: 'password123',
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      const issue = result.error.issues.find((i) => i.path.includes('username'));
      expect(issue).toBeDefined();
      expect(issue?.message).toBe('Username is required');
    }
  });

  it('rejects whitespace-only username', () => {
    const result = loginSchema.safeParse({
      username: '   ',
      password: 'password123',
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      const issue = result.error.issues.find((i) => i.path.includes('username'));
      expect(issue).toBeDefined();
      expect(issue?.message).toBe('Username is required');
    }
  });

  it('rejects empty password', () => {
    const result = loginSchema.safeParse({
      username: 'admin',
      password: '',
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      const issue = result.error.issues.find((i) => i.path.includes('password'));
      expect(issue).toBeDefined();
      expect(issue?.message).toBe('Password is required');
    }
  });
});
