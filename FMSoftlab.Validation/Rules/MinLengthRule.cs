using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.Validation.Rules
{
    public class MinLengthRule<T> : ValidationRule<T>
    {
        private readonly int _minLength;

        public MinLengthRule(int minLength)
        {
            _minLength = minLength;
        }

        public override ValidationResult Validate(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();

            if (value is string str && str.Length < _minLength)
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
            return $"{propertyName} must be at least {_minLength} characters long.";
        }
    }

}
