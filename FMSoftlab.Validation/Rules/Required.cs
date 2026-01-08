using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.Validation.Rules
{
    // Built-in validation rules
    public class RequiredRule<T> : ValidationRule<T>
    {
        public override ValidationResult Validate(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();

            if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                result.Messages.Add(new ValidationMessage
                {
                    PropertyName = propertyName,
                    Message = GetMessage(instance, propertyName, value),
                    Type = _messageType,
                    AttemptedValue = value
                });
            }
            return result;
        }
        public override Task<ValidationResult> ValidateAsync(T instance, string propertyName, object? value)
        {
            return Task.FromResult(Validate(instance, propertyName, value));
        }

        protected override string GetDefaultMessage(T instance, string propertyName, object? value)
        {
            return $"{propertyName} is required.";
        }
    }

}
