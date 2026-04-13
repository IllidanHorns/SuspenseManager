import type { TokenResponseDto } from '../types';

const ACCESS_KEY = 'sm_access_token';
const REFRESH_KEY = 'sm_refresh_token';
const ACCOUNT_KEY = 'sm_account_id';
const LOGIN_KEY = 'sm_login';
const PERMS_KEY = 'sm_permissions';

export function saveTokens(data: TokenResponseDto) {
  localStorage.setItem(ACCESS_KEY, data.accessToken);
  localStorage.setItem(REFRESH_KEY, data.refreshToken);
  localStorage.setItem(ACCOUNT_KEY, String(data.accountId));
  localStorage.setItem(LOGIN_KEY, data.login);
  localStorage.setItem(PERMS_KEY, JSON.stringify(data.permissions));
}

export function getTokens() {
  return {
    accessToken: localStorage.getItem(ACCESS_KEY) ?? '',
    refreshToken: localStorage.getItem(REFRESH_KEY) ?? '',
  };
}

export function getSession() {
  return {
    accountId: Number(localStorage.getItem(ACCOUNT_KEY) ?? 0),
    login: localStorage.getItem(LOGIN_KEY) ?? '',
    permissions: JSON.parse(localStorage.getItem(PERMS_KEY) ?? '[]') as string[],
  };
}

export function clearTokens() {
  localStorage.removeItem(ACCESS_KEY);
  localStorage.removeItem(REFRESH_KEY);
  localStorage.removeItem(ACCOUNT_KEY);
  localStorage.removeItem(LOGIN_KEY);
  localStorage.removeItem(PERMS_KEY);
}

export function isAuthenticated() {
  return !!localStorage.getItem(ACCESS_KEY);
}
