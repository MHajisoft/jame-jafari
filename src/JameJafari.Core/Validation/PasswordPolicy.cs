namespace JameJafari.Core.Validation;

public static class PasswordPolicy
{
    public const int MinLength = 6;
    public const int MaxLength = 100;

    public static string? Validate(string? password, bool allowEmpty = false)
    {
        if (string.IsNullOrEmpty(password))
            return allowEmpty ? null : "رمز عبور الزامی است";

        if (password.Length < MinLength)
            return "رمز عبور حداقل ۶ کاراکتر";

        if (password.Length > MaxLength)
            return "رمز عبور حداکثر ۱۰۰ کاراکتر";

        if (!password.Any(char.IsLetter))
            return "رمز عبور باید حداقل یک حرف داشته باشد";

        if (!password.Any(char.IsDigit))
            return "رمز عبور باید حداقل یک عدد داشته باشد";

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            return "رمز عبور باید حداقل یک نماد داشته باشد";

        return null;
    }
}
