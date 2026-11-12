// @vitest-environment jsdom
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import Select from 'primevue/select';
import PlanActivityModal from '../components/PlanActivityModal.vue';
import * as sessionsApi from '../api/sessions.api';
import type { AvailableLearningUnit } from '../types';

const availableUnits: AvailableLearningUnit[] = [
  {
    id: 'unit-1',
    learningStructureId: 'structure-1',
    code: 'MATH-101',
    name: 'Algebra I',
    unitType: 'course',
    level: 'Grade 10',
    status: 'active',
  },
  {
    id: 'unit-2',
    learningStructureId: 'structure-1',
    code: 'SCI-101',
    name: 'Integrated Science',
    unitType: 'course',
    level: 'Grade 10',
    status: 'active',
  },
];

describe('PlanActivityModal.spec.ts (SCR-ACD-01)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    if (typeof window !== 'undefined') {
      Object.defineProperty(window, 'matchMedia', {
        configurable: true,
        writable: true,
        value: (query: string) => ({
          matches: false,
          media: query,
          onchange: null,
          addListener: () => {},
          removeListener: () => {},
          addEventListener: () => {},
          removeEventListener: () => {},
          dispatchEvent: () => false,
        }),
      });

      global.ResizeObserver = class ResizeObserver {
        observe() {}
        unobserve() {}
        disconnect() {}
      };
    }

    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
    queryClient.clear();
  });

  function createWrapper() {
    return mount(PlanActivityModal, {
      props: { visible: true },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          Dialog: {
            props: ['visible'],
            template: '<div><slot /></div>',
          },
        },
      },
    });
  }

  it('populates the learning unit selector from the available-units lookup', async () => {
    const lookupSpy = vi
      .spyOn(sessionsApi, 'getAvailableLearningUnits')
      .mockResolvedValueOnce(availableUnits);

    const wrapper = createWrapper();
    await flushPromises();

    expect(lookupSpy).toHaveBeenCalledTimes(1);

    const unitSelect = wrapper.findAllComponents(Select)[0];
    expect(unitSelect.props('options')).toEqual([
      { label: 'MATH-101 — Algebra I', value: 'unit-1' },
      { label: 'SCI-101 — Integrated Science', value: 'unit-2' },
    ]);
  });

  it('auto-selects the first available unit once the lookup resolves', async () => {
    vi.spyOn(sessionsApi, 'getAvailableLearningUnits').mockResolvedValueOnce(availableUnits);

    const wrapper = createWrapper();
    await flushPromises();

    const unitSelect = wrapper.findAllComponents(Select)[0];
    expect(unitSelect.props('modelValue')).toBe('unit-1');
  });

  it('keeps the selector empty when no active units exist', async () => {
    vi.spyOn(sessionsApi, 'getAvailableLearningUnits').mockResolvedValueOnce([]);

    const wrapper = createWrapper();
    await flushPromises();

    const unitSelect = wrapper.findAllComponents(Select)[0];
    expect(unitSelect.props('options')).toEqual([]);
    expect(unitSelect.props('modelValue')).toBe('');
  });
});
