import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import { getStudentProgressionRecommendations } from '../api/progressionApi';
import type { ProgressionRecommendationDto } from '../types';

/**
 * Reactive TanStack Query hook fetching student academic progression recommendations (SCR-STU-03, P8-S24).
 * Query key: `['academics', 'students', studentId, 'promotion-recommendations']`
 */
export function useStudentProgressionRecommendation(
  studentId: MaybeRefOrGetter<string | null | undefined>,
) {
  const query = useQuery<ProgressionRecommendationDto[]>({
    queryKey: computed(() => [
      'academics',
      'students',
      toValue(studentId),
      'promotion-recommendations',
    ]),
    queryFn: () => {
      const id = toValue(studentId);
      if (!id) return [];
      return getStudentProgressionRecommendations(id);
    },
    enabled: computed(() => Boolean(toValue(studentId))),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
  });

  const latestRecommendation = computed<ProgressionRecommendationDto | null>(() => {
    const list = query.data.value;
    if (!list || list.length === 0) return null;
    return (
      [...list].sort(
        (a, b) => new Date(b.issuedAt).getTime() - new Date(a.issuedAt).getTime(),
      )[0] ?? null
    );
  });

  return {
    ...query,
    recommendations: query.data,
    latestRecommendation,
  };
}
