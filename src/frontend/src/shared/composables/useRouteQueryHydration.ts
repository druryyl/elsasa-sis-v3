import { onMounted, watch } from 'vue';
import { useRoute, type LocationQuery, type LocationQueryValue } from 'vue-router';

/**
 * Extracts a trimmed, non-empty string value from a Vue Router LocationQuery map.
 */
export function getQueryString(
  query: LocationQuery | Record<string, LocationQueryValue | LocationQueryValue[]> | undefined | null,
  key: string,
): string | null {
  if (!query) return null;
  const raw = query[key];
  if (typeof raw === 'string') {
    const trimmed = raw.trim();
    return trimmed.length > 0 ? trimmed : null;
  }
  if (Array.isArray(raw)) {
    for (const item of raw) {
      if (typeof item === 'string') {
        const trimmed = item.trim();
        if (trimmed.length > 0) return trimmed;
      }
    }
  }
  return null;
}

/**
 * Hydrates workbench filters, search terms, and active tabs from `route.query`
 * immediately, on mount, and whenever `route.query` changes.
 *
 * Safely guards against missing router injection in isolated component unit tests.
 */
export function useRouteQueryHydration(
  hydrate: (query: LocationQuery) => void,
): void {
  const route = useRoute();

  const applyQuery = () => {
    if (!route || !route.query) return;
    hydrate(route.query);
  };

  watch(
    () => route?.query,
    () => {
      applyQuery();
    },
    { immediate: true, deep: true },
  );

  onMounted(() => {
    applyQuery();
  });
}
