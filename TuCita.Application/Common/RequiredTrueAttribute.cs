using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Common;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class RequiredTrueAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is true;
    }
}
