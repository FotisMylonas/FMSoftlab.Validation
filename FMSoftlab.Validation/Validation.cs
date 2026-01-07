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

    public class StringIsDateRule<T> : ValidationRule<T>
    {
        private readonly string _dateFormat;

        public StringIsDateRule(string dateFormat)
        {
            _dateFormat=dateFormat;
        }
        public override ValidationResult Validate(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();

            if (IsValidDate(value))
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
            return $"{propertyName} value '{value}' is not a valid date";
        }
        private bool IsValidDate(object? input)
        {
            if (input == null)
                return false;

#if NET6_0_OR_GREATER
        if (input is DateOnly)
            return true;
#endif
            if (input is DateTime)
                return true;

            if (input is string strInput)
            {
                strInput = strInput.Trim();
                if (string.IsNullOrWhiteSpace(strInput))
                    return false;

                // Try exact format first (if provided)
                if (!string.IsNullOrWhiteSpace(_dateFormat))
                {
                    if (DateTime.TryParseExact(strInput,
                        _dateFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out _))
                    {
                        return true;
                    }
#if NET6_0_OR_GREATER
                if (DateOnly.TryParseExact(strInput, _dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    return true;
                }
#endif
                }

                // Fallback to general parsing
                if (DateTime.TryParse(strInput, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    return true;

#if NET6_0_OR_GREATER
            if (DateOnly.TryParse(strInput, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return true;
#endif
            }
            return false;
        }
    }

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

    public class MaxLengthRule<T> : ValidationRule<T>
    {
        private readonly int _maxLength;

        public MaxLengthRule(int maxLength)
        {
            _maxLength = maxLength;
        }

        public override ValidationResult Validate(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();

            if (value is string str && str.Length > _maxLength)
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
            return $"{propertyName} must not exceed {_maxLength} characters.";
        }
    }

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

    public class MaxValueRule<T> : ValidationRule<T>
    {
        private readonly IComparable _maxValue;

        public MaxValueRule(IComparable maxValue)
        {
            _maxValue = maxValue;
        }

        public override ValidationResult Validate(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();

            if (value is IComparable comparable && comparable.CompareTo(_maxValue) > 0)
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
            return $"{propertyName} must not exceed {_maxValue}.";
        }
    }

    public class MinValueRule<T> : ValidationRule<T>
    {
        private readonly IComparable _minValue;

        public MinValueRule(IComparable minValue)
        {
            _minValue = minValue;
        }

        public override ValidationResult Validate(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();

            if (value is IComparable comparable && comparable.CompareTo(_minValue) < 0)
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
            return $"{propertyName} must be at least {_minValue}.";
        }
    }

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

    // Collection validation rule
    public class CollectionValidationRule<T, TItem> : ValidationRule<T>
    {
        private readonly IValidator<TItem> _itemValidator;

        public CollectionValidationRule(IValidator<TItem> itemValidator)
        {
            _itemValidator = itemValidator;
        }

        public override ValidationResult Validate(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();

            if (value is IEnumerable<TItem> collection)
            {
                var index = 0;
                foreach (var item in collection)
                {
                    var itemResult = _itemValidator.Validate(item);

                    // Prefix item property names with collection property name and index
                    foreach (var message in itemResult.Messages)
                    {
                        message.PropertyName = $"{propertyName}[{index}].{message.PropertyName}";
                        result.Messages.Add(message);
                    }

                    index++;
                }
            }
            return result;
        }

        public override async Task<ValidationResult> ValidateAsync(T instance, string propertyName, object? value)
        {
            var result = new ValidationResult();

            if (value is IEnumerable<TItem> collection)
            {
                var index = 0;
                foreach (var item in collection)
                {
                    var itemResult = await _itemValidator.ValidateAsync(item);

                    // Prefix item property names with collection property name and index
                    foreach (var message in itemResult.Messages)
                    {
                        message.PropertyName = $"{propertyName}[{index}].{message.PropertyName}";
                        result.Messages.Add(message);
                    }

                    index++;
                }
            }
            return result;
        }

        protected override string GetDefaultMessage(T instance, string propertyName, object? value)
        {
            return $"Collection validation failed for {propertyName}.";
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

    // Validator interface and implementation
    public interface IValidator<T>
    {
        ValidationResult Validate(T instance);
        Task<ValidationResult> ValidateAsync(T instance);
    }

    public class Validator<T> : IValidator<T>
    {
        private readonly Dictionary<string, PropertyValidationRules<T>> _propertyRules =
            new Dictionary<string, PropertyValidationRules<T>>();
        private readonly List<IModelValidationRule<T>> _modelRules = new List<IModelValidationRule<T>>();

        public PropertyValidationRules<T> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            var propertyName = GetPropertyName(expression);

            if (!_propertyRules.ContainsKey(propertyName))
            {
                _propertyRules[propertyName] = new PropertyValidationRules<T>(propertyName);
            }

            return _propertyRules[propertyName];
        }

        public ValidationResult Validate(T instance)
        {
            var result = new ValidationResult();

            foreach (var kvp in _propertyRules)
            {
                var propertyName = kvp.Key;
                var rules = kvp.Value;

                object? propertyValue = GetPropertyValue(instance, propertyName);
                //if (propertyValue != null)
                //{
                var propertyResult = rules.Validate(instance, propertyValue);
                result.Messages.AddRange(propertyResult.Messages);
                //}
            }

            foreach (var rule in _modelRules)
            {
                var ruleResult = rule.Validate(instance);
                result.Messages.AddRange(ruleResult.Messages);
            }
            return result;
        }
        public async Task<ValidationResult> ValidateAsync(T instance)
        {
            var result = new ValidationResult();

            foreach (var kvp in _propertyRules)
            {
                var propertyName = kvp.Key;
                var rules = kvp.Value;

                var propertyValue = GetPropertyValue(instance, propertyName);
                //if (propertyValue != null)
                //{
                var propertyResult = await rules.ValidateAsync(instance, propertyValue);
                result.Messages.AddRange(propertyResult.Messages);
                //}
            }

            foreach (var rule in _modelRules)
            {
                var ruleResult = await rule.ValidateAsync(instance);
                result.Messages.AddRange(ruleResult.Messages);
            }

            return result;
        }

        private string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }

            throw new ArgumentException("Expression must be a property access expression.");
        }

        private object? GetPropertyValue(T instance, string propertyName)
        {
            var property = typeof(T).GetProperty(propertyName);
            return property?.GetValue(instance);
        }

        public ModelRuleBuilder<T> Rule()
        {
            return new ModelRuleBuilder<T>(_modelRules);
        }
        public AtLeastOneRule<T> AtLeastOneOf()
        {
            var rule = new AtLeastOneRule<T>();
            _modelRules.Add(rule);
            return rule;
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


