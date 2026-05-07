import axios, { isAxiosError } from 'axios';
import { getTokens, clearTokens, saveTokens } from '../utils/auth';
import { ApiRequestError } from './apiError';
import type { ApiResponse, TokenResponseDto } from '../types';

/**
 * Загрузка Excel: парсинг и пакетная валидация на сервере при большом файле могут идти минутами.
 * Глобальный timeout axios (15 с) для этого не подходит.
 */
export const UPLOAD_REQUEST_TIMEOUT_MS = 600_000;

/** Не показывать пользователю сырой текст axios вида «Request failed with status code 400». */
function rejectAsUserFriendlyError(error: unknown): Promise<never> {
  if (!isAxiosError(error)) {
    return Promise.reject(error instanceof Error ? error : new Error('Не удалось выполнить запрос'));
  }

  if (error.code === 'ECONNABORTED' || /timeout/i.test(error.message ?? '')) {
    return Promise.reject(
      new Error(
        'Истекло время ожидания ответа. Для большого отчёта обработка на сервере могла всё равно завершиться — проверьте список суспенсов. При необходимости загрузите файл ещё раз или разбейте отчёт на части.'
      )
    );
  }

  const status = error.response?.status;
  const raw = error.response?.data;

  if (raw && typeof raw === 'object' && !Array.isArray(raw)) {
    const o = raw as Record<string, unknown>;
    if (typeof o.message === 'string' && o.message.length > 0) {
      return Promise.reject(
        new ApiRequestError(o.message, {
          statusCode: status,
          businessCode: typeof o.businessCode === 'string' ? o.businessCode : undefined,
          fieldErrors: [],
        })
      );
    }
    const title = typeof o.title === 'string' ? o.title : '';
    const detail = typeof o.detail === 'string' ? o.detail : '';
    if (title || detail) {
      const msg = [title, detail].filter(Boolean).join(' ');
      return Promise.reject(new ApiRequestError(msg || 'Ошибка запроса', { statusCode: status, fieldErrors: [] }));
    }
  }

  if (status === 400) {
    return Promise.reject(
      new ApiRequestError('Проверьте введённые данные и попробуйте снова.', {
        statusCode: 400,
        fieldErrors: [],
      })
    );
  }
  if (status === 403) {
    return Promise.reject(
      new ApiRequestError('Недостаточно прав для выполнения этого действия.', {
        statusCode: 403,
        businessCode: 'FORBIDDEN',
        fieldErrors: [],
      })
    );
  }
  if (status === 404) {
    return Promise.reject(new ApiRequestError('Данные не найдены.', { statusCode: 404, fieldErrors: [] }));
  }
  if (status === 409) {
    return Promise.reject(
      new ApiRequestError('Конфликт данных: запись уже существует или нарушено ограничение.', {
        statusCode: 409,
        businessCode: 'DB_CONFLICT',
        fieldErrors: [],
      })
    );
  }
  if (status != null && status >= 500) {
    return Promise.reject(
      new ApiRequestError('Сервер временно недоступен. Попробуйте позже.', {
        statusCode: status,
        fieldErrors: [],
      })
    );
  }

  const m = error.message ?? '';
  if (/^Request failed with status code \d{3}$/i.test(m)) {
    return Promise.reject(new Error('Не удалось выполнить запрос. Попробуйте ещё раз.'));
  }
  return Promise.reject(new Error(m || 'Не удалось выполнить запрос'));
}

const client = axios.create({
  baseURL: '/api',
  timeout: 15000,
});

// Attach access token to every request
client.interceptors.request.use((config) => {
  const { accessToken } = getTokens();
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

let refreshing: Promise<string | null> | null = null;

type SessionUpdateCallback = (data: TokenResponseDto) => void;
let onSessionUpdated: SessionUpdateCallback | null = null;

export function registerSessionUpdateCallback(cb: SessionUpdateCallback) {
  onSessionUpdated = cb;
}

// Auto-refresh on 401
client.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config;
    if (error.response?.status === 401 && !original._retry) {
      original._retry = true;
      if (!refreshing) {
        refreshing = doRefresh().finally(() => { refreshing = null; });
      }
      const newToken = await refreshing;
      if (newToken) {
        original.headers.Authorization = `Bearer ${newToken}`;
        return client(original);
      }
      clearTokens();
      window.location.href = '/login';
    }
    // Разбор тела ApiResponse: детальные ошибки валидации по полям
    const data = error.response?.data as
      | {
          message?: string;
          statusCode?: number;
          businessCode?: string;
          errors?: { field?: string; message?: string }[];
        }
      | undefined;

    if (data && typeof data === 'object') {
      const raw = Array.isArray(data.errors) ? data.errors : [];
      const fieldErrors = raw
        .filter((e) => e && (e.field != null || e.message != null))
        .map((e) => ({
          field: e.field ?? '',
          message: e.message ?? '',
        }));

      const msg =
        typeof data.message === 'string' && data.message.length > 0
          ? data.message
          : 'Ошибка запроса';

      if (fieldErrors.length > 0 || (typeof data.message === 'string' && data.message.length > 0)) {
        return Promise.reject(
          new ApiRequestError(fieldErrors.length > 0 ? msg : data.message ?? msg, {
            statusCode: error.response?.status ?? data.statusCode,
            businessCode: data.businessCode,
            fieldErrors,
          })
        );
      }
    }

    return rejectAsUserFriendlyError(error);
  }
);

async function doRefresh(): Promise<string | null> {
  const { refreshToken } = getTokens();
  if (!refreshToken) return null;
  try {
    const res = await axios.post<ApiResponse<TokenResponseDto>>('/api/auth/refresh', {
      refreshToken,
    });
    const data = res.data.data;
    if (!data) return null;
    saveTokens(data);
    onSessionUpdated?.(data);
    return data.accessToken;
  } catch {
    return null;
  }
}

// Flatten nested Filters object into Filters[key]=value so ASP.NET Core
// Dictionary<string,string> model binding picks it up correctly.
function flattenParams(params?: Record<string, unknown>): Record<string, unknown> {
  if (!params) return {};
  const result: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(params)) {
    if (key === 'Filters' && value !== null && value !== undefined && typeof value === 'object') {
      for (const [fKey, fVal] of Object.entries(value as Record<string, string>)) {
        if (fVal !== '' && fVal !== null && fVal !== undefined) {
          result[`Filters[${fKey}]`] = fVal;
        }
      }
    } else {
      result[key] = value;
    }
  }
  return result;
}

// Unwrap ApiResponse<T> — throws on business error
export async function apiGet<T>(url: string, params?: Record<string, unknown>): Promise<T> {
  const res = await client.get<ApiResponse<T>>(url, { params: flattenParams(params) });
  if (!res.data.data && res.data.statusCode >= 400) {
    throw new Error(res.data.message || 'Ошибка запроса');
  }
  return res.data.data as T;
}

export async function apiPost<T>(url: string, body?: unknown): Promise<T> {
  const res = await client.post<ApiResponse<T>>(url, body);
  if (!res.data.data && res.data.statusCode >= 400) {
    throw new Error(res.data.message || 'Ошибка запроса');
  }
  return res.data.data as T;
}

export async function apiPut<T>(url: string, body?: unknown): Promise<T> {
  const res = await client.put<ApiResponse<T>>(url, body);
  if (!res.data.data && res.data.statusCode >= 400) {
    throw new Error(res.data.message || 'Ошибка запроса');
  }
  return res.data.data as T;
}

export async function apiDelete<T>(url: string): Promise<T> {
  const res = await client.delete<ApiResponse<T>>(url);
  return res.data.data as T;
}

export async function apiGetBlob(url: string, params?: Record<string, unknown>): Promise<Blob> {
  const res = await client.get(url, { params, responseType: 'blob' });
  return res.data as Blob;
}

export async function apiPostBlob(url: string, data?: unknown): Promise<Blob> {
  const res = await client.post(url, data, { responseType: 'blob' });
  return res.data as Blob;
}

export async function apiPostForm<T>(url: string, form: FormData): Promise<T> {
  const res = await client.post<ApiResponse<T>>(url, form, {
    headers: { 'Content-Type': 'multipart/form-data' },
    timeout: UPLOAD_REQUEST_TIMEOUT_MS,
  });
  if (!res.data.data && res.data.statusCode >= 400) {
    throw new Error(res.data.message || 'Ошибка загрузки');
  }
  return res.data.data as T;
}

export default client;
