import { apiGet } from './client';

export interface RightCatalogItem {
  id: number;
  code: string;
  name: string;
  module: string | null;
}

export interface RolePreset {
  id: string;
  name: string;
  description: string;
  rightIds: number[];
}

export async function getRightsCatalog(): Promise<RightCatalogItem[]> {
  return apiGet<RightCatalogItem[]>('/rights');
}

export async function getRolePresets(): Promise<RolePreset[]> {
  return apiGet<RolePreset[]>('/rights/presets');
}
