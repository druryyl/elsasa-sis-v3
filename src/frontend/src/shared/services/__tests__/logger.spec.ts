import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { LoggerService } from '../logger';

describe('logger service', () => {
  let debugSpy: ReturnType<typeof vi.spyOn>;
  let infoSpy: ReturnType<typeof vi.spyOn>;
  let warnSpy: ReturnType<typeof vi.spyOn>;
  let errorSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    debugSpy = vi.spyOn(console, 'debug').mockImplementation(() => {});
    infoSpy = vi.spyOn(console, 'info').mockImplementation(() => {});
    warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
    errorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('respects debug log level and logs all messages', () => {
    const logger = new LoggerService('debug');
    logger.debug('debug message', { key: 'val' });
    logger.info('info message');
    logger.warn('warn message');
    logger.error('error message');

    expect(debugSpy).toHaveBeenCalledTimes(1);
    expect(infoSpy).toHaveBeenCalledTimes(1);
    expect(warnSpy).toHaveBeenCalledTimes(1);
    expect(errorSpy).toHaveBeenCalledTimes(1);
  });

  it('suppresses debug and info when level is warn', () => {
    const logger = new LoggerService('warn');
    logger.debug('debug message');
    logger.info('info message');
    logger.warn('warn message');
    logger.error('error message');

    expect(debugSpy).not.toHaveBeenCalled();
    expect(infoSpy).not.toHaveBeenCalled();
    expect(warnSpy).toHaveBeenCalledTimes(1);
    expect(errorSpy).toHaveBeenCalledTimes(1);
  });

  it('suppresses all logs when level is none', () => {
    const logger = new LoggerService('none');
    logger.debug('debug');
    logger.info('info');
    logger.warn('warn');
    logger.error('error');

    expect(debugSpy).not.toHaveBeenCalled();
    expect(infoSpy).not.toHaveBeenCalled();
    expect(warnSpy).not.toHaveBeenCalled();
    expect(errorSpy).not.toHaveBeenCalled();
  });

  it('updates level dynamically using setLevel', () => {
    const logger = new LoggerService('error');
    logger.info('initial suppressed info');
    expect(infoSpy).not.toHaveBeenCalled();

    logger.setLevel('info');
    expect(logger.getLevel()).toBe('info');

    logger.info('now permitted info');
    expect(infoSpy).toHaveBeenCalledTimes(1);
  });
});
