import axios from 'axios';
import { getTokens, clearTokens, saveTokens } from '../utils/auth';
import type { ApiResponse, TokenResponseDto } from '../types';

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
    // Extract business error message from API response body if available
    const apiMessage = error.response?.data?.message;
    if (apiMessage) {
      return Promise.reject(new Error(apiMessage));
    }
    return Promise.reject(error);
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

export async function apiPostForm<T>(url: string, form: FormData): Promise<T> {
  const res = await client.post<ApiResponse<T>>(url, form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  if (!res.data.data && res.data.statusCode >= 400) {
    throw new Error(res.data.message || 'Ошибка загрузки');
  }
  return res.data.data as T;
}

export default client;
