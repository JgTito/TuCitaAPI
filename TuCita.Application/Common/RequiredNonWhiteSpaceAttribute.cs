using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Common;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class RequiredNonWhiteSpaceAttribute : ValidationAttribute
{
    public RequiredNonWhiteSpaceAttribute()
        : base("El campo {0} es obligatorio.")
    {
    }

    public override bool IsValid(object? value)
    {
        return value is string text && !string.IsNullOrWhiteSpace(text);
    }
}
