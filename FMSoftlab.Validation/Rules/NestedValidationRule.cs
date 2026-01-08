using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.Validation.Rules
{
    // Nested validation rule
    public class NestedValidationRule<T, TProperty> : ValidationRule<T>
    {
        private readonly IValidator<TProperty> _nestedValidator;

        public NestedValidationRule(IValidator<TProperty> nestedValidator)
        {
            _nestedValidator = nestedValidator;
        }

        public override ValidationResult Validate(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();

            if (value is TProperty nestedObject)
            {
                var nestedResult = _nestedValidator.Validate(nestedObject);

                // Prefix nested property names with parent property name
                foreach (var message in nestedResult.Messages)
                {
                    message.PropertyName = $"{propertyName}.{message.PropertyName}";
                    result.Messages.Add(message);
                }
            }

            return result;
        }

        public override async Task<ValidationResult> ValidateAsync(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();

            if (value is TProperty nestedObject)
            {
                var nestedResult = await _nestedValidator.ValidateAsync(nestedObject);

                // Prefix nested property names with parent property name
                foreach (var message in nestedResult.Messages)
                {
                    message.PropertyName = $"{propertyName}.{message.PropertyName}";
                    result.Messages.Add(message);
                }
            }
            return result;
        }
        protected override string GetDefaultMessage(T instance, string propertyName, object? value)
        {
            return $"Nested validation failed for {propertyName}.";
        }
    }


}
