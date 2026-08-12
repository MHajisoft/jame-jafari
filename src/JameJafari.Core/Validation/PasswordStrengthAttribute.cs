using System.ComponentModel.DataAnnotations;

namespace JameJafari.Core.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PasswordStrengthAttribute : ValidationAttribute
{
    public bool AllowEmpty { get; init; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var error = PasswordPolicy.Validate(value as string, AllowEmpty);
        return error is null ? ValidationResult.Success : new ValidationResult(error);
    }
}
