// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest';
import { storage, getItem, setItem, removeItem, clearStorage } from '../storage';

describe('storage service', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('prefixes keys with "elsasa:v3:" and stores serialized JSON', () => {
    storage.setItem('user', { id: 'u-1', name: 'Admin' });

    const raw = localStorage.getItem('elsasa:v3:user');
    expect(raw).toBe(JSON.stringify({ id: 'u-1', name: 'Admin' }));

    const retrieved = storage.getItem<{ id: string; name: string }>('user');
    expect(retrieved).toEqual({ id: 'u-1', name: 'Admin' });
  });

  it('handles existing prefixed keys without double prefixing', () => {
    storage.setItem('elsasa:v3:settings', { theme: 'dark' });
    expect(localStorage.getItem('elsasa:v3:settings')).toBe(JSON.stringify({ theme: 'dark' }));
    expect(storage.getItem('elsasa:v3:settings')).toEqual({ theme: 'dark' });
  });

  it('removes item properly', () => {
    storage.setItem('token', 'xyz-123');
    expect(storage.getItem<string>('token')).toBe('xyz-123');

    storage.removeItem('token');
    expect(storage.getItem<string>('token')).toBeNull();
  });

  it('clears only items matching namespace prefix', () => {
    storage.setItem('item1', 'val1');
    storage.setItem('item2', 'val2');
    localStorage.setItem('other:app:item', 'unaffected');

    storage.clear();

    expect(storage.getItem('item1')).toBeNull();
    expect(storage.getItem('item2')).toBeNull();
    expect(localStorage.getItem('other:app:item')).toBe('unaffected');
  });

  it('handles invalid JSON gracefully by returning null', () => {
    localStorage.setItem('elsasa:v3:corrupt', '{not-valid-json');
    expect(storage.getItem('corrupt')).toBeNull();
  });

  it('works via top-level exported helper functions', () => {
    setItem('helperKey', [1, 2, 3]);
    expect(getItem<number[]>('helperKey')).toEqual([1, 2, 3]);

    removeItem('helperKey');
    expect(getItem('helperKey')).toBeNull();

    setItem('temp', 42);
    clearStorage();
    expect(getItem('temp')).toBeNull();
  });
});
