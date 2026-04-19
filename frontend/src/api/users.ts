import { apiDelete, apiGet, apiPost, apiPut } from './client';
import type { PagedRequest, PagedResponse } from '../types';

export interface AppUser {
  id: number;
  name: string;
  surname: string;
  middleName: string | null;
  email: string;
  phoneNumber: string;
  position: string;
  createTime: string;
  changeTime: string | null;
  archiveLevel: number;
  account?: { id: number; login: string } | null;
}

export interface CreateUserDto {
  name: string;
  surname: string;
  middleName?: string | null;
  email: string;
  phoneNumber: string;
  position: string;
}

export type UpdateUserDto = Partial<CreateUserDto>;

export async function getUsers(request: PagedRequest): Promise<PagedResponse<AppUser>> {
  return apiGet<PagedResponse<AppUser>>('/user', request as Record<string, unknown>);
}

export async function getUser(id: number): Promise<AppUser> {
  return apiGet<AppUser>(`/user/${id}`);
}

export async function createUser(dto: CreateUserDto): Promise<AppUser> {
  return apiPost<AppUser>('/user', dto);
}

export async function updateUser(id: number, dto: UpdateUserDto): Promise<AppUser> {
  return apiPut<AppUser>(`/user/${id}`, dto);
}

export async function deleteUser(id: number): Promise<unknown> {
  return apiDelete(`/user/${id}`);
}
