import type { CatalogProductRights } from '../types';

/** Текст подсказки — совпадает по смыслу с сервером (CatalogProductRightsIdentity). */
export const CATALOG_RIGHTS_MANDATORY_FIELDS_TEXT =
  'компания-отправитель, компания-получатель, территория, код территории, даты начала и окончания договора, доля от 0 до 100%';

function isBadRightsDate(s: string | null | undefined): boolean {
  if (!s?.trim()) return true;
  if (s.startsWith('0001-01-01')) return true;
  const t = Date.parse(s);
  return Number.isNaN(t);
}

/** Список подписей незаполненных обязательных полей для строки таблицы прав каталога. */
export function getMissingCatalogRightsIdentityFields(r: CatalogProductRights): string[] {
  const missing: string[] = [];
  if (!r.companySenderId || r.companySenderId <= 0) missing.push('компания-отправитель');
  if (!r.companyReceiverId || r.companyReceiverId <= 0) missing.push('компания-получатель');
  if (!r.territoryId || r.territoryId <= 0) missing.push('территория');
  if (!r.territoryCode?.trim()) missing.push('код территории');
  if (isBadRightsDate(r.docStart)) missing.push('дата начала договора');
  if (isBadRightsDate(r.docEnd)) missing.push('дата окончания договора');
  const sh = r.share;
  if (typeof sh !== 'number' || Number.isNaN(sh) || sh < 0 || sh > 100) missing.push('доля (%)');
  return missing;
}
