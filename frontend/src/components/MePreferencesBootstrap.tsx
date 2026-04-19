import { useEffect } from 'react';
import { useMantineColorScheme } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { useAuth } from '../hooks/useAuth';
import { useMeSettingsQuery } from '../hooks/useMeSettings';
import type { ColorSchemePreference, NotificationsPositionPreference } from '../types';

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

/** Подтягивает тему из GET /me/settings после входа (перекрывает локальный кэш Mantine). */
export function ColorSchemeSync() {
  const { isLoggedIn } = useAuth();
  const { data } = useMeSettingsQuery({ enabled: isLoggedIn });
  const { setColorScheme } = useMantineColorScheme();

  useEffect(() => {
    const c = data?.preferences?.colorScheme;
    if (isColorScheme(c)) setColorScheme(c);
  }, [data?.preferences?.colorScheme, setColorScheme]);

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
