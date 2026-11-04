// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import FamilyBillingTab from '@/features/portal/FamilyBillingTab.vue';
import * as billingApi from '../billing/api/billing.api';
import type { BillingDocumentDto } from '../billing/types/billing.types';

const mockBillingDocuments: BillingDocumentDto[] = [
  {
    id: 'doc-001',
    documentNumber: 'BIL-2026-00001',
    studentAccountId: 'acc-001',
    studentId: 'stu-001',
    documentType: 'bill',
    recipientName: 'Bapak Ahmad',
    recipientRelationship: 'Father',
    recipientContact: '081234567890',
    billingPeriodStart: null,
    billingPeriodEnd: null,
    issueDate: '2026-08-01',
    paymentDueDate: '2026-08-15',
    totalBilledAmount: 750000,
    currencyCode: 'IDR',
    deliveryChannel: 'portal',
    deliveryStatus: 'issued',
    sentAt: null,
    notes: 'Tagihan SPP Agustus 2026',
    createdAt: '2026-08-01T08:00:00Z',
    createdBy: 'FinanceOfficer',
    items: [
      {
        id: 'item-001',
        billingDocumentId: 'doc-001',
        lineSequence: 1,
        itemType: 'obligation',
        referenceEntityType: 'financial_obligations',
        referenceEntityId: 'obl-001',
        description: 'SPP Bulanan Agustus 2026',
        amount: 750000,
        currencyCode: 'IDR',
        createdAt: '2026-08-01T08:00:00Z',
      },
    ],
  },
  {
    id: 'doc-002',
    documentNumber: 'BIL-2026-00002',
    studentAccountId: 'acc-001',
    studentId: 'stu-001',
    documentType: 'bill',
    recipientName: 'Bapak Ahmad',
    recipientRelationship: 'Father',
    recipientContact: '081234567890',
    billingPeriodStart: null,
    billingPeriodEnd: null,
    issueDate: '2026-07-01',
    paymentDueDate: '2026-07-15',
    totalBilledAmount: 500000,
    currencyCode: 'IDR',
    deliveryChannel: 'portal',
    deliveryStatus: 'paid',
    sentAt: null,
    notes: 'Biaya Kegiatan Juli 2026',
    createdAt: '2026-07-01T08:00:00Z',
    createdBy: 'FinanceOfficer',
    items: [
      {
        id: 'item-002',
        billingDocumentId: 'doc-002',
        lineSequence: 1,
        itemType: 'obligation',
        referenceEntityType: 'financial_obligations',
        referenceEntityId: 'obl-002',
        description: 'Biaya Seragam & Kegiatan',
        amount: 500000,
        currencyCode: 'IDR',
        createdAt: '2026-07-01T08:00:00Z',
      },
    ],
  },
];

describe('FamilyBillingTab.spec.ts (Family Portal Payer Workspace, SCR-SELF-02)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    if (typeof window !== 'undefined') {
      window.matchMedia = (query: string) =>
        ({
          matches: false,
          media: query,
          onchange: null,
          addListener: () => {},
          removeListener: () => {},
          addEventListener: () => {},
          removeEventListener: () => {},
          dispatchEvent: () => false,
        }) as unknown as MediaQueryList;

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

    vi.spyOn(billingApi, 'getBillingDocuments').mockResolvedValue({
      items: mockBillingDocuments,
      totalCount: 2,
      page: 1,
      pageSize: 50,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    vi.spyOn(billingApi, 'getBillingDocumentById').mockImplementation((id: string) => {
      const doc = mockBillingDocuments.find((d) => d.id === id) ?? mockBillingDocuments[0];
      return Promise.resolve(doc);
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
    document.body.innerHTML = '';
  });

  function mountTab(propsData: Record<string, any> = {}) {
    return mount(FamilyBillingTab, {
      attachTo: document.body,
      props: {
        studentId: 'stu-001',
        studentName: 'Ahmad Fauzi',
        payerName: 'Bapak Ahmad',
        ...propsData,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          RouterLink: {
            template: '<a><slot /></a>',
          },
        },
      },
    });
  }

  it('renders family billing tab with outstanding balance summary banner', async () => {
    const wrapper = mountTab();
    await flushPromises();

    expect(wrapper.find('[data-testid="family-billing-tab"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Total Outstanding Balance');
    // Only unpaid document doc-001 (750000) should be included in outstanding balance
    const outstandingEl = wrapper.find('[data-testid="family-outstanding-amount"]');
    expect(outstandingEl.exists()).toBe(true);
    expect(outstandingEl.text()).toBe('Rp 750.000');
  });

  it('lists issued bills with document numbers, amounts, and payment due dates for parents', async () => {
    const wrapper = mountTab();
    await flushPromises();

    const billsList = wrapper.find('[data-testid="family-bills-list"]');
    expect(billsList.exists()).toBe(true);

    const billItems = wrapper.findAll('[data-testid="family-bill-item"]');
    expect(billItems.length).toBe(2);

    // First bill: BIL-2026-00001
    expect(billItems[0].text()).toContain('BIL-2026-00001');
    expect(billItems[0].text()).toContain('Due: 2026-08-15');
    expect(billItems[0].text()).toContain('Issued: 2026-08-01');
    expect(billItems[0].text()).toContain('Recipient: Bapak Ahmad');
    expect(billItems[0].text()).toContain('Rp 750.000');

    // Second bill: BIL-2026-00002
    expect(billItems[1].text()).toContain('BIL-2026-00002');
    expect(billItems[1].text()).toContain('Due: 2026-07-15');
    expect(billItems[1].text()).toContain('Rp 500.000');
  });

  it('filters between All Documents, Open Bills, and Settled bills', async () => {
    const wrapper = mountTab();
    await flushPromises();

    // Default: 'all'
    expect(wrapper.findAll('[data-testid="family-bill-item"]').length).toBe(2);

    // Switch to Open Bills (unpaid)
    await wrapper.find('[data-testid="filter-unpaid-btn"]').trigger('click');
    await wrapper.vm.$nextTick();

    const unpaidItems = wrapper.findAll('[data-testid="family-bill-item"]');
    expect(unpaidItems.length).toBe(1);
    expect(unpaidItems[0].text()).toContain('BIL-2026-00001');
    expect(unpaidItems[0].text()).not.toContain('BIL-2026-00002');

    // Switch to Settled (paid)
    await wrapper.find('[data-testid="filter-paid-btn"]').trigger('click');
    await wrapper.vm.$nextTick();

    const paidItems = wrapper.findAll('[data-testid="family-bill-item"]');
    expect(paidItems.length).toBe(1);
    expect(paidItems[0].text()).toContain('BIL-2026-00002');
    expect(paidItems[0].text()).not.toContain('BIL-2026-00001');

    // Switch back to All
    await wrapper.find('[data-testid="filter-all-btn"]').trigger('click');
    await wrapper.vm.$nextTick();
    expect(wrapper.findAll('[data-testid="family-bill-item"]').length).toBe(2);
  });

  it('renders zero outstanding balance message when all obligations are settled', async () => {
    const paidOnlyDocuments = [mockBillingDocuments[1]]; // Only doc-002 with status 'paid'
    vi.spyOn(billingApi, 'getBillingDocuments').mockResolvedValueOnce({
      items: paidOnlyDocuments,
      totalCount: 1,
      page: 1,
      pageSize: 50,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    const wrapper = mountTab();
    await flushPromises();

    expect(wrapper.find('[data-testid="family-outstanding-amount"]').text()).toBe('Rp 0');
    expect(wrapper.text()).toContain('All tuition and fees are up to date!');
  });

  it('renders empty state when no billing documents exist', async () => {
    vi.spyOn(billingApi, 'getBillingDocuments').mockResolvedValueOnce({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    const wrapper = mountTab();
    await flushPromises();

    expect(wrapper.find('[data-testid="family-billing-empty-state"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('No Billing Statements Found');
  });

  it('opens InvoiceDetailModal and emits viewInvoice event when clicking View Details', async () => {
    const wrapper = mountTab();
    await flushPromises();

    const viewDetailBtn = wrapper.find('[data-testid="view-bill-details-btn"]');
    expect(viewDetailBtn.exists()).toBe(true);

    await viewDetailBtn.trigger('click');
    await flushPromises();

    expect(wrapper.emitted('viewInvoice')).toBeTruthy();
    expect(wrapper.emitted('viewInvoice')![0]).toEqual(['doc-001']);

    // Modal dialog is mounted to document.body
    expect(billingApi.getBillingDocumentById).toHaveBeenCalledWith('doc-001');
    expect(document.querySelector('[data-testid="invoice-detail-modal"]')).not.toBeNull();
  });
});
