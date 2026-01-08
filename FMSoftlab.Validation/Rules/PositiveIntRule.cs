using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.Validation.Rules
{
    public class PositiveIntRule<T> : ValidationRule<T>
    {
        public override ValidationResult Validate(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();
            if (value is int intValue && intValue>0)
            {
                return result;
            }
            result.Messages.Add(new ValidationMessage
            {
                AttemptedValue=value,
                Message = GetMessage(instance, propertyName, value),
                Type = _messageType,
                PropertyName=propertyName
            });
            return result;
        }
        public override Task<ValidationResult> ValidateAsync(T instance, string propertyName, object? value)
        {
            return Task.FromResult(Validate(instance, propertyName, value));
        }
        protected override string GetDefaultMessage(T instance, string propertyName, object? value)
        {
            return $"{propertyName} {value} is not a positive int";
        }
    }

}
