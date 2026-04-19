import { apiDelete, apiGet, apiPost, apiPut } from './client';
import type { PagedRequest, PagedResponse } from '../types';

export interface AccountRow {
  id: number;
  login: string;
  description: string | null;
  userId: number | null;
  user: {
    id: number;
    name: string;
    surname: string;
    email: string;
  } | null;
  createTime: string;
  changeTime: string | null;
  archiveLevel: number;
}

export interface AccountRight {
  id: number;
  code: string;
  name: string;
  module: string | null;
}

export interface CreateAccountDto {
  login: string;
  password: string;
  description?: string | null;
  userId?: number | null;
}

export interface UpdateAccountDto {
  login?: string | null;
  password?: string | null;
  description?: string | null;
  userId?: number | null;
  unlinkUser?: boolean;
}

export type AccountListRequest = PagedRequest & {
  /** Явный фильтр привязки к карточке пользователя (сервер мапит в Filters.UserProfile). */
  userLink?: 'linked' | 'unlinked';
};

export async function getAccounts(request: AccountListRequest): Promise<PagedResponse<AccountRow>> {
  const { userLink, ...paged } = request;
  return apiGet<PagedResponse<AccountRow>>('/account', {
    ...paged,
    ...(userLink === 'linked' || userLink === 'unlinked' ? { userLink } : {}),
  } as Record<string, unknown>);
}

export async function getAccount(id: number): Promise<AccountRow> {
  return apiGet<AccountRow>(`/account/${id}`);
}

export async function createAccount(dto: CreateAccountDto): Promise<AccountRow> {
  return apiPost<AccountRow>('/account', dto);
}

export async function updateAccount(id: number, dto: UpdateAccountDto): Promise<AccountRow> {
  return apiPut<AccountRow>(`/account/${id}`, dto);
}

export async function deleteAccount(id: number): Promise<unknown> {
  return apiDelete(`/account/${id}`);
}

export async function getAccountRights(id: number): Promise<AccountRight[]> {
  return apiGet<AccountRight[]>(`/account/${id}/rights`);
}

export async function replaceAccountRights(accountId: number, rightIds: number[]): Promise<unknown> {
  return apiPut(`/account/${accountId}/rights`, { rightIds });
}
