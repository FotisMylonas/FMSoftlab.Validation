using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FMSoftlab.Validation.Rules
{
    public class EmailRule<T> : ValidationRule<T>
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled);

        public override ValidationResult Validate(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();

            if (value is string email && !string.IsNullOrEmpty(email) && !EmailRegex.IsMatch(email))
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
            return $"{propertyName} is not a valid email address.";
        }
    }
}
