import { useEffect } from 'react';
import { Notifications } from '@mantine/notifications';
import { useAuth } from '../hooks/useAuth';
import { useMeSettingsQuery } from '../hooks/useMeSettings';
import type { ColorSchemePreference, NotificationsPositionPreference } from '../types';
import { useThemePreference } from '../theme/ThemePreferenceContext';

let syncedThemeAccountId: number | null = null;

function isColorScheme(v: string | undefined): v is ColorSchemePreference {
  return v === 'light' || v === 'dark' || v === 'auto';
}

function isNotificationsPosition(v: string | undefined): v is NotificationsPositionPreference {
  return (
    v === 'top-right' ||
    v === 'top-left' ||
    v === 'bottom-right' ||
    v === 'bottom-left' ||
    v === 'top-center' ||
    v === 'bottom-center'
  );
}

/**
 * Подтягивает тему с сервера ОДИН РАЗ за сессию — сразу после логина.
 * После этого ColorSchemeSync не вмешивается: все изменения темы (AppLayout,
 * SettingsPage) идут через прямой вызов setColorScheme и сами сохраняют на сервер.
 * Без этого ограничения фоновые рефетчи query перезаписывали бы тему обратно.
 */
export function ColorSchemeSync() {
  const { isLoggedIn, accountId } = useAuth();
  const { data } = useMeSettingsQuery({ enabled: isLoggedIn });
  const { preference, setPreference } = useThemePreference();

  useEffect(() => {
    if (!isLoggedIn) syncedThemeAccountId = null;
  }, [isLoggedIn]);

  useEffect(() => {
    if (!isLoggedIn || accountId <= 0) return;
    if (syncedThemeAccountId === accountId) return;
    const c = data?.preferences?.colorScheme;
    if (!isColorScheme(c)) return;
    syncedThemeAccountId = accountId;
    if (c !== preference) setPreference(c);
  }, [accountId, data?.preferences?.colorScheme, isLoggedIn, preference, setPreference]);

  return null;
}

/** Позиция уведомлений из настроек аккаунта. */
export function DynamicNotifications() {
  const { isLoggedIn } = useAuth();
  const { data } = useMeSettingsQuery({ enabled: isLoggedIn });
  const raw = data?.preferences?.notificationsPosition;
  const position = isNotificationsPosition(raw) ? raw : 'top-right';

  return <Notifications key={position} position={position} zIndex={1000} />;
}
