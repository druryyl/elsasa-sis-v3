/**
 * Currency utility helpers for Finance domain operations (GAP-006, TD-02).
 * Enforces exact integer representation in Indonesian Rupiah (IDR, zero decimal places).
 */

/**
 * Formats a monetary integer amount into Indonesian Rupiah display format (e.g., "Rp 750.000").
 * Safely accepts number, bigint, numeric string, or null/undefined.
 *
 * @param amount - The monetary integer amount in IDR.
 * @returns Formatted currency string, defaulting to "Rp 0" for empty/invalid inputs.
 */
export function formatIdr(amount: number | bigint | string | null | undefined): string {
  if (amount === null || amount === undefined || amount === '') {
    return 'Rp 0';
  }

  let numericVal: number;
  if (typeof amount === 'bigint') {
    numericVal = Number(amount);
  } else if (typeof amount === 'string') {
    const sanitized = amount.replace(/[^0-9-]/g, '');
    numericVal = sanitized ? Number.parseInt(sanitized, 10) : 0;
  } else {
    numericVal = Math.round(amount);
  }

  if (Number.isNaN(numericVal)) {
    return 'Rp 0';
  }

  // Format with Indonesian thousand-separator dot
  const formatted = new Intl.NumberFormat('id-ID', {
    style: 'currency',
    currency: 'IDR',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(numericVal);

  // Replace non-breaking spaces (\u00a0) with standard ASCII space
  return formatted.replace(/\u00a0/g, ' ');
}

/**
 * Parses a currency string (e.g. "Rp 750.000" or "750,000") into a raw integer IDR number.
 *
 * @param formatted - The formatted IDR currency string.
 * @returns Integer monetary value in IDR.
 */
export function parseIdr(formatted: string | null | undefined): number {
  if (!formatted) {
    return 0;
  }

  const isNegative = formatted.includes('-');
  const cleanDigits = formatted.replace(/\D/g, '');
  if (!cleanDigits) {
    return 0;
  }

  const parsed = Number.parseInt(cleanDigits, 10);
  return isNegative ? -parsed : parsed;
}
