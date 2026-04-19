import { useState, useEffect } from 'react';
import { useMeSettingsQuery } from './useMeSettings';

/**
 * Drop-in replacement for useState(20) for table page sizes.
 * Hydrates from user's saved preference on first mount.
 */
export function useDefaultPageSize(fallback = 20) {
  const { data: meSettings } = useMeSettingsQuery();
  const [pageSize, setPageSize] = useState(fallback);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    if (!meSettings?.preferences || hydrated) return;
    setPageSize(meSettings.preferences.defaultTablePageSize);
    setHydrated(true);
  }, [meSettings, hydrated]);

  return [pageSize, setPageSize] as const;
}
