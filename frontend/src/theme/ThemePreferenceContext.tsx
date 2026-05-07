import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ColorSchemePreference } from '../types';

type ResolvedColorScheme = 'light' | 'dark';

const STORAGE_KEY = 'mantine-color-scheme-value';
const MEDIA_QUERY = '(prefers-color-scheme: dark)';

interface ThemePreferenceContextValue {
  preference: ColorSchemePreference;
  resolvedColorScheme: ResolvedColorScheme;
  setPreference: (next: ColorSchemePreference) => void;
}

const ThemePreferenceContext = createContext<ThemePreferenceContextValue | null>(null);

function isColorSchemePreference(value: string | null): value is ColorSchemePreference {
  return value === 'light' || value === 'dark' || value === 'auto';
}

function getStoredPreference(): ColorSchemePreference {
  if (typeof window === 'undefined') return 'light';
  try {
    const stored = window.localStorage.getItem(STORAGE_KEY);
    return isColorSchemePreference(stored) ? stored : 'light';
  } catch {
    return 'light';
  }
}

function getSystemColorScheme(): ResolvedColorScheme {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return 'light';
  return window.matchMedia(MEDIA_QUERY).matches ? 'dark' : 'light';
}

function resolveColorScheme(preference: ColorSchemePreference): ResolvedColorScheme {
  return preference === 'auto' ? getSystemColorScheme() : preference;
}

function applyDocumentColorScheme(colorScheme: ResolvedColorScheme) {
  if (typeof document === 'undefined') return;
  document.documentElement.setAttribute('data-mantine-color-scheme', colorScheme);
}

export function ThemePreferenceProvider({ children }: { children: React.ReactNode }) {
  const [preference, setPreferenceState] = useState<ColorSchemePreference>(() => getStoredPreference());
  const [resolvedColorScheme, setResolvedColorScheme] = useState<ResolvedColorScheme>(() =>
    resolveColorScheme(getStoredPreference()));

  const setPreference = useCallback((next: ColorSchemePreference) => {
    setPreferenceState(next);
  }, []);

  useEffect(() => {
    const resolved = resolveColorScheme(preference);
    setResolvedColorScheme(resolved);
    applyDocumentColorScheme(resolved);

    try {
      window.localStorage.setItem(STORAGE_KEY, preference);
    } catch {
      // Ignore localStorage access issues, theme still applies in-memory.
    }
  }, [preference]);

  useEffect(() => {
    if (preference !== 'auto' || typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      return undefined;
    }

    const mediaQuery = window.matchMedia(MEDIA_QUERY);
    const handleChange = () => {
      const resolved = mediaQuery.matches ? 'dark' : 'light';
      setResolvedColorScheme(resolved);
      applyDocumentColorScheme(resolved);
    };

    handleChange();

    if (typeof mediaQuery.addEventListener === 'function') {
      mediaQuery.addEventListener('change', handleChange);
      return () => mediaQuery.removeEventListener('change', handleChange);
    }

    mediaQuery.addListener(handleChange);
    return () => mediaQuery.removeListener(handleChange);
  }, [preference]);

  const value = useMemo(
    () => ({ preference, resolvedColorScheme, setPreference }),
    [preference, resolvedColorScheme, setPreference],
  );

  return (
    <ThemePreferenceContext.Provider value={value}>
      {children}
    </ThemePreferenceContext.Provider>
  );
}

export function useThemePreference() {
  const context = useContext(ThemePreferenceContext);
  if (!context) throw new Error('useThemePreference must be used within ThemePreferenceProvider');
  return context;
}
