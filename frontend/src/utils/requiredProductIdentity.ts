import type { CatalogProduct, GroupMetadata } from '../types';

const LABELS = {
  title: 'название',
  artist: 'исполнитель',
  isrc: 'ISRC',
  barcode: 'баркод',
  catalogNumber: 'каталожный номер',
} as const;

/** Обязательные поля данных продукта в каталоге (совпадают с проверкой на сервере). */
export const CATALOG_PRODUCT_MANDATORY_FIELDS_TEXT =
  'название, исполнитель, ISRC, баркод, каталожный номер';

/** Карточка в каталоге — те же поля, что на сервере (CatalogProductIdentity). */
export function getMissingCatalogProductIdentityFields(p: CatalogProduct): string[] {
  const missing: string[] = [];
  if (!p.productName?.trim()) missing.push(LABELS.title);
  if (!p.artist?.trim()) missing.push(LABELS.artist);
  if (!p.isrc?.trim()) missing.push(LABELS.isrc);
  if (!p.barcode?.trim()) missing.push(LABELS.barcode);
  if (!p.catalogNumber?.trim()) missing.push(LABELS.catalogNumber);
  return missing;
}

/** Для быстрой каталогизации — только вкладка «Метаданные». */
export function getMissingProductIdentityForQuickCatalog(metadata: GroupMetadata | null | undefined): string[] {
  if (!metadata) {
    return ['создайте и заполните метаданные'];
  }
  const missing: string[] = [];
  if (!metadata.title?.trim()) missing.push(LABELS.title);
  if (!metadata.artist?.trim()) missing.push(LABELS.artist);
  if (!metadata.isrc?.trim()) missing.push(LABELS.isrc);
  if (!metadata.barcode?.trim()) missing.push(LABELS.barcode);
  if (!metadata.catalogNumber?.trim()) missing.push(LABELS.catalogNumber);
  return missing;
}
