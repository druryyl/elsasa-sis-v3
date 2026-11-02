// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import CreateCalendarEntryModal from '../CreateCalendarEntryModal.vue';
import * as calendarApi from '../../../api/calendar.api';

const mockToastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: mockToastAdd }),
}));

describe('CreateCalendarEntryModal.vue', () => {
  let queryClient: QueryClient;

  beforeAll(() => {
    if (typeof window !== 'undefined') {
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: vi.fn().mockImplementation((query) => ({
          matches: false,
          media: query,
          onchange: null,
          addListener: vi.fn(),
          removeListener: vi.fn(),
          addEventListener: vi.fn(),
          removeEventListener: vi.fn(),
          dispatchEvent: vi.fn(),
        })),
      });

      global.ResizeObserver = class ResizeObserver {
        observe() {}
        unobserve() {}
        disconnect() {}
      };
    }
  });

  beforeEach(() => {
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  function createWrapper(props: {
    visible: boolean;
    calendarId: string;
    isPublished?: boolean;
    initialDate?: string | null;
  }) {
    return mount(CreateCalendarEntryModal, {
      props,
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
      },
    });
  }

  it('renders modal when visible is true', () => {
    const wrapper = createWrapper({
      visible: true,
      calendarId: 'cal-1',
      isPublished: false,
    });

    expect(wrapper.find('[data-testid="create-calendar-entry-modal"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="entry-date-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="entry-name-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="entry-designation-select"]').exists()).toBe(true);
  });

  it('displays published warning banner when isPublished is true', () => {
    const wrapper = createWrapper({
      visible: true,
      calendarId: 'cal-1',
      isPublished: true,
    });

    const banner = wrapper.find('[data-testid="published-calendar-warning-banner"]');
    expect(banner.exists()).toBe(true);
    expect(banner.text()).toContain(
      'Adding or editing an entry on a published calendar will generate an auditable Calendar Amendment and bump version.',
    );
    expect(wrapper.find('[data-testid="entry-amendment-reason-input"]').exists()).toBe(true);
  });

  it('does not display published warning banner when isPublished is false', () => {
    const wrapper = createWrapper({
      visible: true,
      calendarId: 'cal-1',
      isPublished: false,
    });

    expect(wrapper.find('[data-testid="published-calendar-warning-banner"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="entry-amendment-reason-input"]').exists()).toBe(false);
  });

  it('prefills entry date with initialDate prop', async () => {
    const wrapper = createWrapper({
      visible: true,
      calendarId: 'cal-1',
      initialDate: '2026-11-15',
    });

    await flushPromises();
    const dateInput = wrapper.find('[data-testid="entry-date-input"]').element as HTMLInputElement;
    expect(dateInput.value).toBe('2026-11-15');
  });

  it('submits entry payload and invokes createCalendarEntry API', async () => {
    const mockCreatedEntry: calendarApi.CalendarEntryDto = {
      id: 'entry-123',
      calendarId: 'cal-1',
      entryDate: '2026-11-20',
      designation: 'holiday',
      name: 'Thanksgiving Holiday',
      isInstructional: false,
      appliesTo: 'All',
    };

    vi.spyOn(calendarApi, 'createCalendarEntry').mockResolvedValue(mockCreatedEntry);

    const wrapper = createWrapper({
      visible: true,
      calendarId: 'cal-1',
      initialDate: '2026-11-20',
    });

    await flushPromises();

    // Fill form
    const nameInput = wrapper.find('[data-testid="entry-name-input"]');
    await nameInput.setValue('Thanksgiving Holiday');

    const form = wrapper.find('form');
    await form.trigger('submit');
    await flushPromises();

    expect(calendarApi.createCalendarEntry).toHaveBeenCalledWith(
      'cal-1',
      expect.objectContaining({
        entryDate: '2026-11-20',
        name: 'Thanksgiving Holiday',
        designation: 'holiday',
        isInstructional: false,
      }),
    );

    expect(wrapper.emitted('created')).toBeTruthy();
    expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
  });
});
