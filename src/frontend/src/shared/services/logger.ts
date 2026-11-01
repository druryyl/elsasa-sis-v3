export type LogLevel = 'debug' | 'info' | 'warn' | 'error' | 'none';

const LEVEL_WEIGHTS: Record<LogLevel, number> = {
  debug: 0,
  info: 1,
  warn: 2,
  error: 3,
  none: 4,
};

export interface LogEntry {
  level: LogLevel;
  message: string;
  timestamp: string;
  context?: unknown;
}

/**
 * Structured client logging service respecting environment log levels.
 */
export class LoggerService {
  private currentLevel: LogLevel;

  constructor(initialLevel?: LogLevel) {
    if (initialLevel) {
      this.currentLevel = initialLevel;
    } else {
      const envLevel = import.meta.env?.VITE_LOG_LEVEL?.toLowerCase() as LogLevel | undefined;
      if (envLevel && envLevel in LEVEL_WEIGHTS) {
        this.currentLevel = envLevel;
      } else {
        this.currentLevel = import.meta.env?.DEV ? 'debug' : 'info';
      }
    }
  }

  getLevel(): LogLevel {
    return this.currentLevel;
  }

  setLevel(level: LogLevel): void {
    if (level in LEVEL_WEIGHTS) {
      this.currentLevel = level;
    }
  }

  private shouldLog(level: LogLevel): boolean {
    return LEVEL_WEIGHTS[level] >= LEVEL_WEIGHTS[this.currentLevel];
  }

  private formatPrefix(level: LogLevel): string {
    const timestamp = new Date().toISOString();
    return `[ELSASA] [${timestamp}] [${level.toUpperCase()}]`;
  }

  debug(message: string, context?: unknown, ...args: unknown[]): void {
    if (!this.shouldLog('debug')) {
      return;
    }
    const prefix = this.formatPrefix('debug');
    if (context !== undefined) {
      console.debug(prefix, message, context, ...args);
    } else {
      console.debug(prefix, message, ...args);
    }
  }

  info(message: string, context?: unknown, ...args: unknown[]): void {
    if (!this.shouldLog('info')) {
      return;
    }
    const prefix = this.formatPrefix('info');
    if (context !== undefined) {
      console.info(prefix, message, context, ...args);
    } else {
      console.info(prefix, message, ...args);
    }
  }

  warn(message: string, context?: unknown, ...args: unknown[]): void {
    if (!this.shouldLog('warn')) {
      return;
    }
    const prefix = this.formatPrefix('warn');
    if (context !== undefined) {
      console.warn(prefix, message, context, ...args);
    } else {
      console.warn(prefix, message, ...args);
    }
  }

  error(message: string, context?: unknown, ...args: unknown[]): void {
    if (!this.shouldLog('error')) {
      return;
    }
    const prefix = this.formatPrefix('error');
    if (context !== undefined) {
      console.error(prefix, message, context, ...args);
    } else {
      console.error(prefix, message, ...args);
    }
  }
}

export const logger = new LoggerService();
