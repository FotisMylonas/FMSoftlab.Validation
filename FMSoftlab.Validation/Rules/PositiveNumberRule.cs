using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.Validation.Rules
{
    public class PositiveNumberRule<T> : ValidationRule<T>
    {
        public override ValidationResult Validate(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();

            if (IsPositiveNumber(value))
            {
                return result;
            }

            result.Messages.Add(new ValidationMessage
            {
                AttemptedValue = value,
                Message = GetMessage(instance, propertyName, value),
                Type = _messageType,
                PropertyName = propertyName
            });

            return result;
        }
        public override Task<ValidationResult> ValidateAsync(T instance, string propertyName, object? value)
        {
            return Task.FromResult(Validate(instance, propertyName, value));
        }
        protected override string GetDefaultMessage(T instance, string propertyName, object? value)
        {
            return $"{propertyName} value '{value}' is not a positive number.";
        }

        private static bool IsPositiveNumber(object? value)
        {
            return value switch
            {
                null => false,
                sbyte sb => sb > 0,
                byte b => b > 0,
                short s => s > 0,
                ushort us => us > 0,
                int i => i > 0,
                uint ui => ui > 0,
                long l => l > 0,
                ulong ul => ul > 0,
                float f => f > 0,
                double d => d > 0,
                decimal m => m > 0,
                _ => false
            };
        }
    }


}
