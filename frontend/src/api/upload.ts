import { apiPost, apiPostForm } from './client';
import type { ValidationResultDto } from '../types';

export async function uploadFile(file: File): Promise<ValidationResultDto> {
  const form = new FormData();
  form.append('file', file);
  return apiPostForm<ValidationResultDto>('/upload', form);
}

export interface ManualSuspenseLineInput {
  isrc?: string;
  barcode?: string;
  catalogNumber?: string;
  productFormatCode?: string;
  senderCompany?: string;
  recipientCompany?: string;
  operator?: string;
  artist?: string;
  trackTitle?: string;
  agreementType?: string;
  agreementNumber?: string;
  territoryCode?: string;
  qty: number;
  ppd?: number;
  exchangeCurrency?: number;
  exchangeRate?: number;
  genre?: string;
}

export async function uploadManual(data: ManualSuspenseLineInput): Promise<ValidationResultDto> {
  return apiPost<ValidationResultDto>('/upload/manual', data);
}
