const DEFAULT_PREFIX = 'elsasa:v3:';

/**
 * Type-safe wrapper around localStorage with JSON serialization and namespace isolation.
 */
export class StorageService {
  private readonly prefix: string;

  constructor(prefix: string = DEFAULT_PREFIX) {
    this.prefix = prefix;
  }

  private getKey(key: string): string {
    return key.startsWith(this.prefix) ? key : `${this.prefix}${key}`;
  }

  private getStorage(): Storage | null {
    if (typeof window !== 'undefined' && window.localStorage) {
      return window.localStorage;
    }
    if (typeof localStorage !== 'undefined') {
      return localStorage;
    }
    return null;
  }

  /**
   * Retrieves and deserializes a JSON value by key.
   */
  getItem<T>(key: string): T | null {
    const storageBackend = this.getStorage();
    if (!storageBackend) {
      return null;
    }
    try {
      const raw = storageBackend.getItem(this.getKey(key));
      if (raw === null) {
        return null;
      }
      return JSON.parse(raw) as T;
    } catch {
      return null;
    }
  }

  /**
   * Serializes and persists a value by key.
   */
  setItem<T>(key: string, value: T): void {
    const storageBackend = this.getStorage();
    if (!storageBackend) {
      return;
    }
    try {
      storageBackend.setItem(this.getKey(key), JSON.stringify(value));
    } catch (error) {
      console.error(`[storage] Failed to save key "${key}":`, error);
    }
  }

  /**
   * Removes an item by key.
   */
  removeItem(key: string): void {
    const storageBackend = this.getStorage();
    if (!storageBackend) {
      return;
    }
    try {
      storageBackend.removeItem(this.getKey(key));
    } catch (error) {
      console.error(`[storage] Failed to remove key "${key}":`, error);
    }
  }

  /**
   * Clears all keys matching the configured namespace prefix.
   */
  clear(): void {
    const storageBackend = this.getStorage();
    if (!storageBackend) {
      return;
    }
    try {
      const keysToRemove: string[] = [];
      for (let i = 0; i < storageBackend.length; i++) {
        const key = storageBackend.key(i);
        if (key && key.startsWith(this.prefix)) {
          keysToRemove.push(key);
        }
      }
      for (const key of keysToRemove) {
        storageBackend.removeItem(key);
      }
    } catch (error) {
      console.error('[storage] Failed to clear items:', error);
    }
  }
}

export const storage = new StorageService();
export const getItem = <T>(key: string): T | null => storage.getItem<T>(key);
export const setItem = <T>(key: string, value: T): void => storage.setItem<T>(key, value);
export const removeItem = (key: string): void => storage.removeItem(key);
export const clearStorage = (): void => storage.clear();
