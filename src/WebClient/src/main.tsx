import '@mantine/core/styles.css';
import '@mantine/notifications/styles.css';
import './styles.css';
import { MantineProvider } from '@mantine/core';
import { ModalsProvider } from '@mantine/modals';
import { Notifications } from '@mantine/notifications';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { statusOf } from './lib/errors';
import { theme } from './theme';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      // No reintenta errores del cliente (401, 403, 404…)
      retry: (failures, error) => {
        const status = statusOf(error);
        return (status === undefined || status >= 500) && failures < 2;
      }
    }
  }
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <MantineProvider theme={theme}>
      <Notifications position="top-right" />
      <QueryClientProvider client={queryClient}>
        <ModalsProvider>
          <App />
        </ModalsProvider>
      </QueryClientProvider>
    </MantineProvider>
  </StrictMode>
);
