/** Ошибка API с телом ApiResponse (в т.ч. список ошибок валидации по полям). */
export class ApiRequestError extends Error {
  readonly statusCode: number;
  readonly businessCode?: string;
  /** Имена полей в PascalCase, как приходит от FluentValidation / ASP.NET */
  readonly fieldErrors: { field: string; message: string }[];

  constructor(
    message: string,
    options: {
      statusCode?: number;
      businessCode?: string;
      fieldErrors?: { field: string; message: string }[];
    } = {}
  ) {
    super(message);
    this.name = 'ApiRequestError';
    this.statusCode = options.statusCode ?? 400;
    this.businessCode = options.businessCode;
    this.fieldErrors = options.fieldErrors ?? [];
  }
}

export function isApiRequestError(e: unknown): e is ApiRequestError {
  return e instanceof ApiRequestError;
}

/** PascalCase → camelCase для ключей Mantine form (Isrc → isrc, TrackTitle → trackTitle). */
export function propertyNameToFormKey(propertyName: string): string {
  if (!propertyName) return propertyName;
  return propertyName.charAt(0).toLowerCase() + propertyName.slice(1);
}
