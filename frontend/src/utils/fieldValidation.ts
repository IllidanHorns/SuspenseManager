/**
 * Ограничения длины полей как в EF Core (SuspenseManager.Data/Configurations).
 * Сообщения — для подсказок пользователю до ответа сервера.
 */

export const DbMax = {
  groupMetadata: {
    title: 255,
    artist: 255,
    isrc: 15,
    barcode: 20,
    catalogNumber: 100,
    genre: 100,
    description: 1000,
    productTypeCode: 50,
    productTypeDesc: 500,
  },
  groupMetaRights: {
    docNumber: 100,
    docType: 100,
    territoryCode: 10,
    territoryDesc: 255,
  },
  catalogProduct: {
    productName: 255,
    artist: 255,
    isrc: 15,
    barcode: 20,
    catalogNumber: 100,
    genre: 100,
    description: 1000,
    productFormatCode: 50,
    productTypeDesc: 500,
    albumName: 255,
    composer: 255,
  },
  catalogRights: {
    docNumber: 100,
  },
  company: {
    legalName: 255,
    shortName: 100,
    companyCode: 50,
    bankName: 255,
    phoneNumber: 20,
    country: 10,
    legalAddress: 500,
    actualAddress: 500,
    inn: 12,
    bic: 20,
  },
  territory: {
    code: 10,
    name: 255,
  },
  user: {
    name: 100,
    surname: 100,
    middleName: 100,
    email: 255,
    phoneNumber: 20,
    position: 255,
  },
  account: {
    login: 100,
    password: 500,
    description: 1000,
  },
  /** SuspenseLine — ручной ввод на UploadPage */
  suspenseLine: {
    isrc: 15,
    barcode: 20,
    catalogNumber: 100,
    productFormatCode: 50,
    artist: 255,
    trackTitle: 255,
    genre: 100,
    senderCompany: 255,
    recipientCompany: 255,
    operator: 255,
    agreementType: 100,
    agreementNumber: 100,
    territoryCode: 10,
  },
} as const;

export type StringValidator = (value: string | null | undefined) => string | null;

/** Не длиннее `max` символов (пустая строка допустима). */
export function maxLen(max: number, fieldLabel: string): StringValidator {
  return (value) => {
    const s = value ?? '';
    if (s.length > max) {
      return `${fieldLabel}: не более ${max} символов (сейчас ${s.length})`;
    }
    return null;
  };
}

/** Пусто или не длиннее max. */
export function optMaxLen(max: number, fieldLabel: string): StringValidator {
  return (value) => {
    const s = value ?? '';
    if (s.length === 0) return null;
    return maxLen(max, fieldLabel)(value);
  };
}

export function required(msg = 'Обязательное поле'): StringValidator {
  return (value) => {
    const s = value ?? '';
    if (!s.trim()) return msg;
    return null;
  };
}

export function combine(...validators: StringValidator[]): StringValidator {
  return (value) => {
    for (const v of validators) {
      const err = v(value);
      if (err) return err;
    }
    return null;
  };
}

const emailRe = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function emailField(label = 'Email'): StringValidator {
  return combine(
    required(`Укажите ${label.toLowerCase()}`),
    maxLen(DbMax.user.email, label),
    (value) => {
      const s = (value ?? '').trim();
      if (!emailRe.test(s)) return 'Введите корректный email';
      return null;
    }
  );
}

/** Дата в формате YYYY-MM-DD или пусто (для полей type="date" и текстовых). */
export function dateIsoOptional(fieldLabel: string): StringValidator {
  return (value) => {
    const s = (value ?? '').trim();
    if (!s) return null;
    if (!/^\d{4}-\d{2}-\d{2}$/.test(s)) {
      return `${fieldLabel}: укажите дату в формате ГГГГ-ММ-ДД`;
    }
    return null;
  };
}

export function dateIsoRequired(fieldLabel: string): StringValidator {
  return combine(required(`Укажите ${fieldLabel}`), dateIsoOptional(fieldLabel));
}

export function sharePercent(fieldLabel = 'Доля'): (v: number | null | undefined) => string | null {
  return (v) => {
    if (v === null || v === undefined || Number.isNaN(Number(v))) {
      return `Укажите ${fieldLabel.toLowerCase()} (%)`;
    }
    const n = Number(v);
    if (n < 0 || n > 100) return `${fieldLabel}: от 0 до 100`;
    return null;
  };
}

// +7 (900) 000-00-00, +79000000000, 8-800-555-35-35, etc.
const phoneRe = /^\+?[\d\s()\-]{7,20}$/;

export function phoneField(label = 'Телефон'): StringValidator {
  return combine(
    required(`Укажите ${label.toLowerCase()}`),
    maxLen(DbMax.user.phoneNumber, label),
    (value) => {
      const s = (value ?? '').trim();
      if (!phoneRe.test(s)) return 'Введите корректный номер телефона (например: +7 (900) 000-00-00)';
      return null;
    }
  );
}

/** Телефон необязателен, но если указан — должен соответствовать формату. */
export function phoneOptional(label = 'Телефон'): StringValidator {
  return (value) => {
    const s = (value ?? '').trim();
    if (!s) return null;
    if (!phoneRe.test(s)) return 'Введите корректный номер телефона (например: +7 (900) 000-00-00)';
    if (s.length > DbMax.user.phoneNumber) return maxLen(DbMax.user.phoneNumber, label)(value);
    return null;
  };
}

/** ИНН: ровно 10 цифр (юр. лицо) или 12 цифр (физ. лицо). */
export function innOptional(): StringValidator {
  return (value) => {
    const s = (value ?? '').trim();
    if (!s) return null;
    if (!/^\d+$/.test(s)) return 'ИНН: только цифры';
    if (s.length !== 10 && s.length !== 12) return 'ИНН должен содержать ровно 10 цифр (юр. лицо) или 12 цифр (физ. лицо)';
    return null;
  };
}

/** БИК: ровно 9 цифр. */
export function bicOptional(): StringValidator {
  return (value) => {
    const s = (value ?? '').trim();
    if (!s) return null;
    if (!/^\d{9}$/.test(s)) return 'БИК должен содержать ровно 9 цифр';
    return null;
  };
}
