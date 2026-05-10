using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TuCita.Application.Common;

public static class DataAnnotationsValidator
{
    public static IReadOnlyCollection<ValidationError> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(instance);

        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        ValidateConstructorParameters(instance, results);

        return results
            .SelectMany(ToValidationErrors)
            .ToArray();
    }

    private static void ValidateConstructorParameters(object instance, List<ValidationResult> results)
    {
        var type = instance.GetType();
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase);
        var constructor = type.GetConstructors()
            .OrderByDescending(item => item.GetParameters().Length)
            .FirstOrDefault(item => item.GetParameters().All(parameter => properties.ContainsKey(parameter.Name ?? string.Empty)));

        if (constructor is null)
        {
            return;
        }

        foreach (var parameter in constructor.GetParameters())
        {
            var attributes = parameter.GetCustomAttributes<ValidationAttribute>(inherit: true).ToArray();
            if (attributes.Length == 0 || string.IsNullOrWhiteSpace(parameter.Name))
            {
                continue;
            }

            var property = properties[parameter.Name];
            var value = property.GetValue(instance);
            var context = new ValidationContext(instance)
            {
                MemberName = property.Name
            };

            foreach (var attribute in attributes)
            {
                var result = attribute.GetValidationResult(value, context);
                if (result is not null)
                {
                    results.Add(result);
                }
            }
        }
    }

    private static IEnumerable<ValidationError> ToValidationErrors(ValidationResult result)
    {
        var message = result.ErrorMessage ?? "El campo no es válido.";
        var members = result.MemberNames.Any() ? result.MemberNames : [string.Empty];

        foreach (var member in members)
        {
            yield return new ValidationError(member, message);
        }
    }
}
