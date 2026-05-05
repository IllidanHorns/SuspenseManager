export const PASSWORD_MIN_LENGTH = 8;
export const PASSWORD_ALLOWED_SPECIALS = '!@#%&*_-+=./|\\';

export function getPasswordPolicyError(password: string | null | undefined, fieldLabel = 'Пароль'): string | null {
  const value = password ?? '';

  if (!value) {
    return `${fieldLabel} обязателен`;
  }

  if (value.length < PASSWORD_MIN_LENGTH) {
    return `${fieldLabel}: минимум ${PASSWORD_MIN_LENGTH} символов`;
  }

  let hasLower = false;
  let hasUpper = false;
  let hasDigit = false;
  let hasSpecial = false;

  for (const ch of value) {
    if (ch >= 'a' && ch <= 'z') {
      hasLower = true;
      continue;
    }
    if (ch >= 'A' && ch <= 'Z') {
      hasUpper = true;
      continue;
    }
    if (ch >= '0' && ch <= '9') {
      hasDigit = true;
      continue;
    }
    if (PASSWORD_ALLOWED_SPECIALS.includes(ch)) {
      hasSpecial = true;
      continue;
    }

    return `${fieldLabel}: только латинские буквы, цифры и спецсимволы ${PASSWORD_ALLOWED_SPECIALS}`;
  }

  if (!hasLower || !hasUpper || !hasDigit || !hasSpecial) {
    return `${fieldLabel}: нужны строчная буква, заглавная буква, цифра и спецсимвол ${PASSWORD_ALLOWED_SPECIALS}`;
  }

  return null;
}

export function requiredPassword(fieldLabel = 'Пароль') {
  return (value: string | null | undefined) => getPasswordPolicyError(value, fieldLabel);
}

export function optionalPassword(fieldLabel = 'Пароль') {
  return (value: string | null | undefined) => {
    if (!value) return null;
    return getPasswordPolicyError(value, fieldLabel);
  };
}
