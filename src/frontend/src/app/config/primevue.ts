import Aura from '@primevue/themes/aura';
import type { PrimeVueConfiguration } from 'primevue/config';

/**
 * PrimeVue v4 design system configuration for Elsasa SIS V3.
 * Configured with the Aura preset and CSS layer ordering to allow
 * Tailwind CSS utilities to cleanly override component styles without !important.
 */
export const primevueConfig: PrimeVueConfiguration = {
  ripple: true,
  theme: {
    preset: Aura,
    options: {
      prefix: 'p',
      darkModeSelector: '.dark',
      cssLayer: {
        name: 'primevue',
        order: 'tailwind-base, primevue, tailwind-utilities',
      },
    },
  },
  pt: {
    // Global PassThrough unstyled/tailored defaults can be defined here
  },
};

export default primevueConfig;
