import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { MantineProvider, createTheme } from '@mantine/core';
import { ModalsProvider } from '@mantine/modals';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './hooks/useAuth';
import { App } from './App';
import { ColorSchemeSync, DynamicNotifications } from './components/MePreferencesBootstrap';
import { ThemePreferenceProvider, useThemePreference } from './theme/ThemePreferenceContext';

import '@mantine/core/styles.css';
import '@mantine/notifications/styles.css';
import '@mantine/dates/styles.css';
import '@mantine/charts/styles.css';
import 'mantine-datatable/styles.css';

const theme = createTheme({
  primaryColor: 'indigo',
  defaultRadius: 'md',
  fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif',
  headings: {
    fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif',
    fontWeight: '600',
  },
  components: {
    Button: {
      defaultProps: { radius: 'md' },
    },
    Input: {
      defaultProps: { radius: 'md' },
    },
    Paper: {
      defaultProps: { radius: 'md' },
    },
    Card: {
      defaultProps: { radius: 'md' },
    },
  },
});

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
});

function RootProviders() {
  const { resolvedColorScheme } = useThemePreference();

  return (
    <MantineProvider theme={theme} forceColorScheme={resolvedColorScheme}>
      <ModalsProvider>
        <QueryClientProvider client={queryClient}>
          <BrowserRouter>
            <AuthProvider>
              <ColorSchemeSync />
              <DynamicNotifications />
              <App />
            </AuthProvider>
          </BrowserRouter>
        </QueryClientProvider>
      </ModalsProvider>
    </MantineProvider>
  );
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ThemePreferenceProvider>
      <RootProviders />
    </ThemePreferenceProvider>
  </React.StrictMode>
);
