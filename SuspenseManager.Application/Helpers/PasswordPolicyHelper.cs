using Common.Exceptions;

namespace Application.Helpers;

public static class PasswordPolicyHelper
{
    public const int MinLength = 8;
    public const string AllowedSpecials = "!@#%&*_-+=./|\\";

    public static void ValidateOrThrow(string password, string messagePrefix = "Пароль")
    {
        var error = GetValidationError(password, messagePrefix);
        if (error == null) return;

        throw new BusinessException(error, "PASSWORD_WEAK", 400);
    }

    public static string? GetValidationError(string? password, string messagePrefix = "Пароль")
    {
        if (string.IsNullOrEmpty(password))
        {
            return $"{messagePrefix} обязателен.";
        }

        if (password.Length < MinLength)
        {
            return $"{messagePrefix} должен быть не короче {MinLength} символов.";
        }

        var hasLower = false;
        var hasUpper = false;
        var hasDigit = false;
        var hasSpecial = false;

        foreach (var ch in password)
        {
            if (ch is >= 'a' and <= 'z')
            {
                hasLower = true;
                continue;
            }

            if (ch is >= 'A' and <= 'Z')
            {
                hasUpper = true;
                continue;
            }

            if (ch is >= '0' and <= '9')
            {
                hasDigit = true;
                continue;
            }

            if (AllowedSpecials.Contains(ch))
            {
                hasSpecial = true;
                continue;
            }

            return $"{messagePrefix} может содержать только латинские буквы, цифры и спецсимволы {AllowedSpecials}.";
        }

        if (!hasLower || !hasUpper || !hasDigit || !hasSpecial)
        {
            return $"{messagePrefix} должен содержать минимум одну строчную латинскую букву, одну заглавную латинскую букву, одну цифру и один спецсимвол из набора {AllowedSpecials}.";
        }

        return null;
    }
}
