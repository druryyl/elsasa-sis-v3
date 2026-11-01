/**
 * Formats a numeric amount as standard Indonesian Rupiah (e.g., "Rp 1.500.000").
 * Converts non-breaking space characters from Intl into standard ASCII space.
 */
export function formatIDR(amount: number): string {
  if (typeof amount !== 'number' || Number.isNaN(amount)) {
    return 'Rp 0';
  }

  const formatted = new Intl.NumberFormat('id-ID', {
    style: 'currency',
    currency: 'IDR',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);

  // Replace non-breaking space (\u00a0) with standard ASCII space
  return formatted.replace(/\u00a0/g, ' ');
}
