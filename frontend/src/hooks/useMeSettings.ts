import { useQuery, type UseQueryOptions } from '@tanstack/react-query';
import { getMeSettings } from '../api/me';
import type { MeSettingsResponse } from '../types';

export const ME_SETTINGS_QUERY_KEY = ['me-settings'] as const;

type MeSettingsQueryOptions = Pick<UseQueryOptions<MeSettingsResponse>, 'enabled'>;

export function useMeSettingsQuery(options?: MeSettingsQueryOptions) {
  return useQuery({
    queryKey: ME_SETTINGS_QUERY_KEY,
    queryFn: () => getMeSettings(),
    staleTime: 60_000,
    enabled: options?.enabled ?? true,
  });
}
