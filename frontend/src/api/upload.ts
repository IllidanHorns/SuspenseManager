import { apiPostForm } from './client';
import type { ValidationResultDto } from '../types';

export async function uploadFile(file: File): Promise<ValidationResultDto> {
  const form = new FormData();
  form.append('file', file);
  return apiPostForm<ValidationResultDto>('/upload', form);
}
