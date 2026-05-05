// ─── API wrapper ────────────────────────────────────────────────────────────

export interface ApiResponse<T> {
  statusCode: number;
  businessCode: string;
  message: string;
  data: T | null;
  errors: { field: string; message: string }[];
  timestamp: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// ─── Auth ────────────────────────────────────────────────────────────────────

export interface LoginDto {
  login: string;
  password: string;
}

export interface TokenResponseDto {
  accessToken: string;
  expiresAt: string;
  refreshToken: string;
  refreshExpiresAt: string;
  accountId: number;
  login: string;
  permissions: string[];
}

// ─── Enums ───────────────────────────────────────────────────────────────────

export type BusinessStatus = 0 | 1 | 15 | 16 | 30 | 32 | 88 | 120 | 320;

export const STATUS_LABELS: Record<number, string> = {
  0: 'Нет продукта',
  1: 'Нет прав',
  15: 'Группа — нет продукта',
  16: 'Группа — нет прав',
  30: 'Отложено (нет продукта)',
  32: 'Отложено (нет прав)',
  88: 'Прошло валидацию',
  120: 'Бэк-офис (нет продукта)',
  320: 'Бэк-офис (нет прав)',
};

export const STATUS_COLORS: Record<number, string> = {
  0: 'orange',
  1: 'red',
  15: 'blue',
  16: 'violet',
  30: 'yellow',
  32: 'grape',
  88: 'green',
  120: 'gray',
  320: 'dark',
};

// ─── SuspenseLine ────────────────────────────────────────────────────────────

export interface SuspenseLine {
  id: number;
  isrc: string | null;
  barcode: string | null;
  catalogNumber: string | null;
  productFormatCode: string | null;
  senderCompany: string | null;
  recipientCompany: string | null;
  operator: string | null;
  artist: string | null;
  trackTitle: string | null;
  genre: string | null;
  agreementType: string | null;
  agreementNumber: string | null;
  territoryCode: string | null;
  qty: number;
  ppd: number | null;
  /** Код/значение валюты отчёта (decimal с API) */
  exchangeCurrency: number;
  exchangeRate: number;
  businessStatus: BusinessStatus;
  /** Причина попадания в суспенс (из БД; приходит с GET по ID) */
  causeSuspense?: string | null;
  groupId: number | null;
  productId: number | null;
  createTime: string;
  changeTime: string | null;
  archiveLevel: number;
}

// ─── CatalogProductType ──────────────────────────────────────────────────────

export interface CatalogProductType {
  id: number;
  code: string;
  description: string;
}

// ─── CatalogProduct ──────────────────────────────────────────────────────────

export interface CatalogProduct {
  id: number;
  isrc: string | null;
  barcode: string | null;
  catalogNumber: string | null;
  productName: string | null;
  artist: string | null;
  genre: string | null;
  duration: string | null;
  releaseDate: string | null;
  productTypeId: number | null;
  createTime: string;
  archiveLevel: number;
}

// ─── Groups ──────────────────────────────────────────────────────────────────

export interface SuspenseGroup {
  id: number;
  businessStatus: BusinessStatus;
  accountId: number;
  catalogProductId: number | null;
  metaDataId: number | null;
  metaRightsId: number | null;
  createTime: string;
  changeTime: string | null;
  archiveLevel: number;
  postponeReason: string | null;
  suspenseCount?: number;
  revenueRub?: number;
  groupMetaData?: GroupMetadata | null;
  groupMetaRights?: GroupMetaRights | null;
  catalogProduct?: CatalogProduct | null;
}

export interface GroupMetadata {
  id: number;
  groupId: number;
  catalogNumber: string | null;
  barcode: string | null;
  isrc: string | null;
  artist: string | null;
  title: string | null;
  genre: string | null;
  description: string | null;
  productTypeCode: string | null;
  productTypeDesc: string | null;
  duration: string | null;
  releaseDate: string | null;
  productTypeId: number | null;
  catalogProductId: number | null;
}

export interface UpdateGroupMetadataDto {
  catalogNumber?: string | null;
  barcode?: string | null;
  isrc?: string | null;
  artist?: string | null;
  title?: string | null;
  genre?: string | null;
  description?: string | null;
  productTypeCode?: string | null;
  productTypeDesc?: string | null;
  duration?: string | null;
  releaseDate?: string | null;
  productTypeId?: number | null;
  catalogProductId?: number | null;
}

export interface GroupMetaRights {
  id: number;
  groupId: number;
  docNumber: string | null;
  docType: string | null;
  docDate: string | null;
  docStart: string | null;
  docEnd: string | null;
  territoryId: number | null;
  territoryCode: string | null;
  territoryDesc: string | null;
  senderCompanyId: number | null;
  receiverCompanyId: number | null;
  share: number | null;
  senderCompany?: Company | null;
  receiverCompany?: Company | null;
  territory?: Territory | null;
}

export interface UpdateGroupMetaRightsDto {
  docNumber?: string | null;
  docType?: string | null;
  docDate?: string | null;
  docStart?: string | null;
  docEnd?: string | null;
  territoryId?: number | null;
  territoryCode?: string | null;
  territoryDesc?: string | null;
  senderCompanyId?: number | null;
  receiverCompanyId?: number | null;
  share?: number | null;
}

// ─── CatalogProductRights ─────────────────────────────────────────────────────

export interface CatalogProductRights {
  id: number;
  catalogProductId: number;
  catalogProduct?: CatalogProduct | null;
  docNumber: string | null;
  companySender: string;
  companyReceiver: string;
  companySenderId: number;
  companyReceiverId: number;
  share: number;
  territoryCode: string;
  territoryDesc: string;
  territoryId: number;
  docStart: string;
  docEnd: string;
  createTime: string;
  archiveLevel: number;
}

// ─── Grouping ────────────────────────────────────────────────────────────────

export interface GroupingPreviewItem {
  key: Record<string, string>;
  count: number;
  revenueRub: number;
}

export interface GroupingCommitRequest {
  businessStatus: number;
  groupByColumns: string[];
  keyValues: Record<string, string>;
  accountId: number;
}

export interface SuspenseLinePreviewDto {
  id: number;
  isrc?: string;
  barcode?: string;
  catalogNumber?: string;
  artist?: string;
  trackTitle?: string;
  genre?: string;
  operator?: string;
  senderCompany?: string;
  recipientCompany?: string;
  territoryCode?: string;
  agreementType?: string;
  agreementNumber?: string;
  qty: number;
  ppd?: number;
  exchangeCurrency: number;
  exchangeRate: number;
}

// ─── Upload ──────────────────────────────────────────────────────────────────

export interface RowFormatError {
  rowNumber: number;
  isrc: string | null;
  errors: string[];
}

export interface ValidationResultDto {
  totalRows: number;
  validatedCount: number;
  noProductCount: number;
  noRightsCount: number;
  lines: ValidationLineResultDto[];
  rowFormatErrors?: RowFormatError[];
}

export interface ValidationLineResultDto {
  suspenseLineId: number;
  businessStatus: BusinessStatus;
  causeSuspense: string | null;
  productId: number | null;
}

// ─── Reference data ──────────────────────────────────────────────────────────

export interface Company {
  id: number;
  legalName: string;
  shortName: string;
  companyCode: string | null;
  createTime: string;
}

export interface UserProfile {
  id: number;
  name: string;
  surname: string;
  middleName: string | null;
  email: string;
  phoneNumber: string;
  position: string;
  archiveLevel: number;
  createTime: string;
  changeTime: string | null;
}

export interface Right {
  id: number;
  rightCode: string;
  rightDescription: string | null;
}

export interface AccountProfile {
  id: number;
  login: string;
  description: string | null;
  userId: number | null;
  user: UserProfile | null;
  rights: Right[];
  archiveLevel: number;
  createTime: string;
  changeTime: string | null;
}

/** Настройки UI в Account.UiPreferencesJson (GET/PUT /api/me/settings). Универсальные для всех ролей. */
export type ColorSchemePreference = 'light' | 'dark' | 'auto';

export type NotificationsPositionPreference =
  | 'top-right'
  | 'top-left'
  | 'bottom-right'
  | 'bottom-left'
  | 'top-center'
  | 'bottom-center';

export interface UserUiPreferences {
  defaultTablePageSize: number;
  filtersExpandedByDefault: boolean;
  colorScheme: ColorSchemePreference;
  notificationsPosition: NotificationsPositionPreference;
}

export interface MeUserProfile {
  id: number;
  name: string;
  surname: string;
  middleName: string | null;
  email: string;
  phoneNumber: string;
  position: string;
}

export interface MeSettingsResponse {
  accountId: number;
  login: string;
  description: string | null;
  preferences: UserUiPreferences;
  user: MeUserProfile | null;
}

export interface UpdateMeSettingsDto {
  description?: string | null;
  preferences?: UserUiPreferences;
  user?: Partial<Pick<MeUserProfile, 'name' | 'surname' | 'middleName' | 'email' | 'phoneNumber' | 'position'>>;
  currentPassword?: string;
  newPassword?: string;
}

export interface Territory {
  id: number;
  territoryCode: string;
  territoryName: string | null;
}

// ─── Paging ──────────────────────────────────────────────────────────────────

export interface PagedRequest {
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  Filters?: Record<string, string>;
  [key: string]: unknown;
}

// ─── Back Office ─────────────────────────────────────────────────────────────

export interface BackOfficeTaskDto {
  id: number;
  groupId: number;
  groupStatus: number;
  suspenseCount: number;
  problemDescription: string;
  taskStatus: number; // 0=Open 1=ReturnedToOperator 2=GroupDeleted 3=Completed
  createTime: string;
  createdByAccountId: number;
  createdByLogin: string;
  createdByName: string | null;
  artist: string | null;
  title: string | null;
  isrc: string | null;
  barcode: string | null;
}

export interface BackOfficeTask {
  id: number;
  groupId: number;
  group: SuspenseGroup;
  createdByAccountId: number;
  problemDescription: string;
  taskStatus: number;
  createTime: string;
  changeTime: string | null;
  archiveLevel: number;
}

// ─── Catalog create/update DTOs ──────────────────────────────────────────────

export interface CreateCatalogProductDto {
  productName?: string | null;
  artist?: string | null;
  isrc?: string | null;
  barcode?: string | null;
  catalogNumber?: string | null;
  productFormatCode?: string | null;
  genre?: string | null;
  albumName?: string | null;
  composer?: string | null;
  description?: string | null;
  releaseDate?: string | null;
  productTypeId?: number | null;
}

export interface UpdateCatalogProductDto extends CreateCatalogProductDto {}

export interface CreateCatalogProductRightsDto {
  catalogProductId: number;
  docNumber?: string | null;
  companySenderId: number;
  companyReceiverId: number;
  share: number;
  territoryId: number;
  docStart: string;
  docEnd: string;
}

export interface UpdateCatalogProductRightsDto {
  docNumber?: string | null;
  companySenderId?: number | null;
  companyReceiverId?: number | null;
  share?: number | null;
  territoryId?: number | null;
  docStart?: string | null;
  docEnd?: string | null;
}

export interface CreateCompanyDto {
  legalName: string;
  shortName: string;
  companyCode?: string | null;
  bankName?: string | null;
  phoneNumber?: string | null;
  country?: string | null;
  legalAddress?: string | null;
  actualAddress?: string | null;
  inn?: string | null;
  bic?: string | null;
}

export interface UpdateCompanyDto extends Partial<CreateCompanyDto> {}

export interface CreateTerritoryDto {
  territoryCode: string;
  territoryName?: string | null;
}

export interface UpdateTerritoryDto {
  territoryCode?: string | null;
  territoryName?: string | null;
}

// ─── Audit ───────────────────────────────────────────────────────────────────

export interface AuditGroupDto {
  id: number;
  businessStatus: number;
  statusName: string | null;
  createTime: string;
  createdByAccountId: number;
  createdByLogin: string;
  createdByName: string | null;
  lastChangeTime: string | null;
  lastStatusFrom: number | null;
  lastStatusFromName: string | null;
  lastStatusTo: number | null;
  lastStatusToName: string | null;
  lastChangedByLogin: string | null;
  lastChangedByName: string | null;
  catalogProductId: number | null;
  productName: string | null;
}

export interface AuditLineDto {
  id: number;
  businessStatus: number;
  statusName: string | null;
  groupId: number | null;
  isrc: string | null;
  barcode: string | null;
  artist: string | null;
  trackTitle: string | null;
  operator: string | null;
  createTime: string;
  groupCreatedByAccountId: number | null;
  groupCreatedByLogin: string | null;
  lastChangeTime: string | null;
  lastStatusFrom: number | null;
  lastStatusFromName: string | null;
  lastStatusTo: number | null;
  lastStatusToName: string | null;
  lastChangedByLogin: string | null;
  lastChangedByName: string | null;
}

export interface AuditLogEntryDto {
  id: number;
  statusFrom: number | null;
  statusFromName: string | null;
  statusTo: number;
  statusToName: string | null;
  accountId: number;
  accountLogin: string;
  accountName: string | null;
  operationTime: string;
}

export interface AuditGroupsRequest {
  pageNumber?: number;
  pageSize?: number;
  status?: number | null;
  onlyMine?: boolean;
  createdFrom?: string | null;
  createdTo?: string | null;
  lastChangedFrom?: string | null;
  lastChangedTo?: string | null;
}

export interface AuditLinesRequest {
  pageNumber?: number;
  pageSize?: number;
  status?: number | null;
  groupId?: number | null;
  onlyMine?: boolean;
  lastChangedFrom?: string | null;
  lastChangedTo?: string | null;
}
