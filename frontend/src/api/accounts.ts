import { apiGet } from './client';
import type { AccountProfile } from '../types';

export async function getAccountById(id: number): Promise<AccountProfile> {
  return apiGet<AccountProfile>(`/account/${id}`);
}
