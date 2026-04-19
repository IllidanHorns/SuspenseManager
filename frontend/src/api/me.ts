import { apiGet, apiPut } from './client';
import type { MeSettingsResponse, UpdateMeSettingsDto } from '../types';

export async function getMeSettings(): Promise<MeSettingsResponse> {
  return apiGet<MeSettingsResponse>('/me/settings');
}

export async function updateMeSettings(dto: UpdateMeSettingsDto): Promise<MeSettingsResponse> {
  return apiPut<MeSettingsResponse>('/me/settings', dto);
}
