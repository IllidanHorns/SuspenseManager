import axios from 'axios';
import type { ApiResponse, LoginDto, TokenResponseDto } from '../types';

export async function login(dto: LoginDto): Promise<TokenResponseDto> {
  const res = await axios.post<ApiResponse<TokenResponseDto>>('/api/auth/login', dto);
  if (!res.data.data) throw new Error(res.data.message || 'Неверный логин или пароль');
  return res.data.data;
}

export async function revoke(refreshToken: string): Promise<void> {
  await axios.post('/api/auth/revoke', { refreshToken });
}
