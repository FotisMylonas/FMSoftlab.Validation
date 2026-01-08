using FMSoftlab.Validation.Rules;
using FMSoftlab.Validation.Rules.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FMSoftlab.Validation
{
    public enum MessageType
    {
        Error,
        Warning
    }

    public class ValidationMessage
    {
        public string PropertyName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public MessageType Type { get; set; }
        public object? AttemptedValue { get; set; }
    }

    public class ValidationResult
    {
        public List<ValidationMessage> Messages { get; set; } = new List<ValidationMessage>();

        public bool IsValid => !Messages.Any(m => m.Type == MessageType.Error);
        public bool HasErrors => Messages.Any(m => m.Type == MessageType.Error);
        public bool HasWarnings => Messages.Any(m => m.Type == MessageType.Warning);

        public List<ValidationMessage> Errors => Messages.Where(m => m.Type == MessageType.Error).ToList();
        public List<ValidationMessage> Warnings => Messages.Where(m => m.Type == MessageType.Warning).ToList();
    }

    // Base validation rule interface
    public interface IValidationRule<T>
    {
        ValidationResult Validate(T instance, string propertyName, object? value);
        Task<ValidationResult> ValidateAsync(T instance, string propertyName, object? value);
    }

    // Core validation rule base class
    public abstract class ValidationRule<T> : IValidationRule<T>
    {
        protected string _customMessage;
        protected Func<T, string> _messageFactory;
        protected MessageType _messageType = MessageType.Error;

        public ValidationRule<T> WithMessage(string message)
        {
            _customMessage = message;
            return this;
        }

        public ValidationRule<T> WithMessage(Func<T, string> messageFactory)
        {
            _messageFactory = messageFactory;
            return this;
        }

        public ValidationRule<T> AsWarning()
        {
            _messageType = MessageType.Warning;
            return this;
        }

        public ValidationRule<T> AsError()
        {
            _messageType = MessageType.Error;
            return this;
        }

        public abstract ValidationResult Validate(T instance, string propertyName, object? value);
        public abstract Task<ValidationResult> ValidateAsync(T instance, string propertyName, object? value);

        protected string GetMessage(T instance, string propertyName, object? value)
        {
            if (_messageFactory != null)
                return _messageFactory(instance);

            if (!string.IsNullOrEmpty(_customMessage))
                return _customMessage;

            return GetDefaultMessage(instance, propertyName, value);
        }

        protected abstract string GetDefaultMessage(T instance, string propertyName, object? value);

        protected Func<T, bool>? _when;

        public ValidationRule<T> When(Func<T, bool> condition)
        {
            _when = condition;
            return this;
        }

        public ValidationRule<T> Unless(Func<T, bool> condition)
        {
            _when = instance => !condition(instance);
            return this;
        }
        public bool ShouldExecute(T instance)
        {
            return _when == null || _when(instance);
        }
    }




    // Property validation configuration
    public class PropertyValidationRules<T>
    {
        private readonly List<IValidationRule<T>> _rules = new List<IValidationRule<T>>();
        private readonly string _propertyName;

        public PropertyValidationRules(string propertyName)
        {
            _propertyName = propertyName;
        }

        public PropertyValidationRules<T> When(Func<T, bool> condition)
        {
            if (_rules.Count > 0 && _rules.Last() is ValidationRule<T> lastRule)
            {
                lastRule.When(condition);
            }
            return this;
        }

        public PropertyValidationRules<T> Unless(Func<T, bool> condition)
        {
            if (_rules.Count > 0 && _rules.Last() is ValidationRule<T> lastRule)
            {
                lastRule.Unless(condition);
            }
            return this;
        }

        public PropertyValidationRules<T> IsRequired()
        {
            _rules.Add(new RequiredRule<T>());
            return this;
        }
        public PropertyValidationRules<T> PositiveInt()
        {
            _rules.Add(new PositiveIntRule<T>());
            return this;
        }
        public PropertyValidationRules<T> PositiveNumber()
        {
            _rules.Add(new PositiveNumberRule<T>());
            return this;
        }
        public PropertyValidationRules<T> StringIsDate(string dateFormat)
        {
            _rules.Add(new StringIsDateRule<T>(dateFormat));
            return this;
        }
        public PropertyValidationRules<T> MaxLength(int maxLength)
        {
            _rules.Add(new MaxLengthRule<T>(maxLength));
            return this;
        }

        public PropertyValidationRules<T> MinLength(int minLength)
        {
            _rules.Add(new MinLengthRule<T>(minLength));
            return this;
        }

        public PropertyValidationRules<T> MaxValue(IComparable maxValue)
        {
            _rules.Add(new MaxValueRule<T>(maxValue));
            return this;
        }

        public PropertyValidationRules<T> MinValue(IComparable minValue)
        {
            _rules.Add(new MinValueRule<T>(minValue));
            return this;
        }

        public PropertyValidationRules<T> IsEmail()
        {
            _rules.Add(new EmailRule<T>());
            return this;
        }

        public PropertyValidationRules<T> Must(Func<T, object?, bool> predicate, string? message = null)
        {
            _rules.Add(new CustomRule<T>(predicate, message));
            return this;
        }

        public PropertyValidationRules<T> MustAsync(Func<T, object?, Task<bool>> asyncPredicate, string? message = null)
        {
            _rules.Add(new AsyncCustomRule<T>(asyncPredicate, message));
            return this;
        }

        public PropertyValidationRules<T> ValidateNested<TProperty>(IValidator<TProperty> nestedValidator)
        {
            _rules.Add(new NestedValidationRule<T, TProperty>(nestedValidator));
            return this;
        }

        public PropertyValidationRules<T> ValidateCollection<TItem>(IValidator<TItem> itemValidator)
        {
            _rules.Add(new CollectionValidationRule<T, TItem>(itemValidator));
            return this;
        }

        public PropertyValidationRules<T> WithMessage(string message)
        {
            if (_rules.Count > 0 && _rules.Last() is ValidationRule<T> lastRule)
            {
                lastRule.WithMessage(message);
            }
            return this;
        }

        public PropertyValidationRules<T> WithMessage(Func<T, string> messageFactory)
        {
            if (_rules.Count > 0 && _rules.Last() is ValidationRule<T> lastRule)
            {
                lastRule.WithMessage(messageFactory);
            }
            return this;
        }

        public PropertyValidationRules<T> AsWarning()
        {
            if (_rules.Count > 0 && _rules.Last() is ValidationRule<T> lastRule)
            {
                lastRule.AsWarning();
            }
            return this;
        }

        public PropertyValidationRules<T> AsError()
        {
            if (_rules.Count > 0 && _rules.Last() is ValidationRule<T> lastRule)
            {
                lastRule.AsError();
            }
            return this;
        }

        internal ValidationResult Validate(T instance, object? value)
        {
            var result = new ValidationResult();

            foreach (var rule in _rules)
            {
                if (rule is ValidationRule<T> vr && !vr.ShouldExecute(instance))
                    continue;

                var ruleResult = rule.Validate(instance, _propertyName, value);
                result.Messages.AddRange(ruleResult.Messages);
            }

            return result;
        }
        internal async Task<ValidationResult> ValidateAsync(T instance, object? value)
        {
            var result = new ValidationResult();

            foreach (var rule in _rules)
            {
                if (rule is ValidationRule<T> vr && !vr.ShouldExecute(instance))
                    continue;

                var ruleResult = await rule.ValidateAsync(instance, _propertyName, value);
                result.Messages.AddRange(ruleResult.Messages);
            }

            return result;
        }
    }



    // Validation registry for managing validators
    public static class ValidationRegistry
    {
        private static readonly ConcurrentDictionary<Type, object> _validators = new ConcurrentDictionary<Type, object>();

        public static void RegisterValidator<T>(IValidator<T> validator)
        {
            _validators[typeof(T)] = validator;
        }

        public static IValidator<T> GetValidator<T>()
        {
            if (_validators.TryGetValue(typeof(T), out var validator))
            {
                return (IValidator<T>)validator;
            }

            throw new InvalidOperationException($"No validator registered for type {typeof(T).Name}");
        }

        public static bool HasValidator<T>()
        {
            return _validators.ContainsKey(typeof(T));
        }
    }
}


