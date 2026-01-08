using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.Validation.Rules
{


    // Custom validation rule base
    public class CustomRule<T> : ValidationRule<T>
    {
        private readonly Func<T, object?, bool> _predicate;
        private readonly string _defaultMessage;

        public CustomRule(Func<T, object?, bool> predicate, string? defaultMessage = null)
        {
            _predicate = predicate;
            _defaultMessage = defaultMessage ?? "Custom validation failed.";
        }

        public override ValidationResult Validate(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();

            if (!_predicate(instance, value))
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
            return _defaultMessage;
        }
    }

    // Async custom validation rule
    public class AsyncCustomRule<T> : ValidationRule<T>
    {
        private readonly Func<T, object?, Task<bool>> _asyncPredicate;
        private readonly string _defaultMessage;

        public AsyncCustomRule(Func<T, object?, Task<bool>> asyncPredicate, string? defaultMessage = null)
        {
            _asyncPredicate = asyncPredicate;
            _defaultMessage = defaultMessage ?? "Async validation failed.";
        }

        public override ValidationResult Validate(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();
            return result;
        }
        public override async Task<ValidationResult> ValidateAsync(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();

            var isValid = await _asyncPredicate(instance, value); // Note: In production, consider async validation pattern

            if (!isValid)
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

        protected override string GetDefaultMessage(T instance, string propertyName, object? value)
        {
            return _defaultMessage;
        }
    }

}
