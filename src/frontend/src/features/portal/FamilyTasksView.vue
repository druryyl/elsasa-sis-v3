<script setup lang="ts">
import { ref, computed } from 'vue';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import {
  CreditCard,
  CheckSquare,
  GraduationCap,
  User,
  HeartHandshake,
} from 'lucide-vue-next';
import { PageHeader } from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { useAuthStore } from '@/app/stores/auth.store';
import FamilyBillingTab from './FamilyBillingTab.vue';

const authStore = useAuthStore();

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Home', to: '/dashboard' },
  { label: 'Family Portal', current: true },
];

const activeTab = ref('billing');

// Current user profile
const userName = computed(() => authStore.user?.fullName ?? 'Parent / Guardian');
</script>

<template>
  <div class="space-y-6" data-testid="family-tasks-view">
    <!-- Header -->
    <PageHeader
      title="Family Tasks &amp; Requests Portal"
      subtitle="View issued tuition bills, check payment deadlines, and manage family student records."
      :breadcrumbs="breadcrumbs"
    />

    <!-- Portal Welcome Card -->
    <div class="bg-linear-to-r from-indigo-900 via-indigo-800 to-indigo-950 rounded-2xl p-6 text-white shadow-md flex flex-wrap items-center justify-between gap-4">
      <div>
        <div class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-indigo-700/60 text-indigo-200 text-xs font-medium mb-2 border border-indigo-600">
          <HeartHandshake class="w-3.5 h-3.5" />
          <span>Elsasa Family Self-Service Portal</span>
        </div>
        <h2 class="text-xl font-bold">
          Welcome, {{ userName }}
        </h2>
        <p class="text-sm text-indigo-200 mt-1 max-w-xl">
          Track upcoming school fee deadlines, download billing invoices, and review payment history.
        </p>
      </div>
    </div>

    <!-- Portal Tabs -->
    <div class="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
      <Tabs v-model:value="activeTab">
        <TabList class="px-6 border-b border-slate-200">
          <Tab value="billing" class="flex items-center gap-2 py-3">
            <CreditCard class="w-4 h-4" />
            <span>Billing &amp; Statements</span>
          </Tab>
          <Tab value="tasks" class="flex items-center gap-2 py-3">
            <CheckSquare class="w-4 h-4" />
            <span>Tasks &amp; Requests</span>
          </Tab>
        </TabList>

        <TabPanels class="p-6">
          <TabPanel value="billing" class="p-0">
            <FamilyBillingTab />
          </TabPanel>

          <TabPanel value="tasks" class="p-0">
            <div class="py-12 text-center text-slate-500" data-testid="family-tasks-placeholder">
              <CheckSquare class="w-12 h-12 text-slate-300 mx-auto mb-3" />
              <h3 class="text-base font-semibold text-slate-700">All Tasks Completed</h3>
              <p class="text-xs text-slate-500 mt-1 max-w-sm mx-auto">
                You have no pending consent forms or documentation requests at this time.
              </p>
            </div>
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>
  </div>
</template>
