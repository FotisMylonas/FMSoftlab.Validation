using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;


namespace FMSoftlab.Validation
{
    /// <summary>
    /// Validates that at least one of the specified properties has a value
    /// </summary>
    public class AtLeastOneRule<T> : IModelValidationRule<T>
    {
        private readonly List<string> _propertyNames = new List<string>();
        private Func<T, bool>? _when;
        private string? _message;
        private MessageType _messageType = MessageType.Error;
        private string? _propertyName;

        public AtLeastOneRule<T> Property<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            var propertyName = GetPropertyName(expression);
            _propertyNames.Add(propertyName);
            return this;
        }

        public AtLeastOneRule<T> When(Func<T, bool> condition)
        {
            _when = condition;
            return this;
        }

        public AtLeastOneRule<T> Unless(Func<T, bool> condition)
        {
            _when = x => !condition(x);
            return this;
        }

        public AtLeastOneRule<T> WithMessage(string message)
        {
            _message = message;
            return this;
        }

        public AtLeastOneRule<T> ForProperty(string propertyName)
        {
            _propertyName = propertyName;
            return this;
        }

        public AtLeastOneRule<T> AsWarning()
        {
            _messageType = MessageType.Warning;
            return this;
        }

        public AtLeastOneRule<T> AsError()
        {
            _messageType = MessageType.Error;
            return this;
        }

        public ValidationResult Validate(T instance)
        {
            var result = new ValidationResult();

            if (!ShouldExecute(instance))
                return result;

            if (!HasAtLeastOneValue(instance))
            {
                result.Messages.Add(new ValidationMessage
                {
                    PropertyName = _propertyName ?? string.Empty,
                    Message = _message ?? GetDefaultMessage(),
                    Type = _messageType
                });
            }
            return result;
        }

        public Task<ValidationResult> ValidateAsync(T instance)
        {
            return Task.FromResult(Validate(instance));
        }

        private bool ShouldExecute(T instance)
            => _when == null || _when(instance);

        private bool HasAtLeastOneValue(T instance)
        {
            foreach (var propertyName in _propertyNames)
            {
                var property = typeof(T).GetProperty(propertyName);
                if (property == null)
                    continue;

                var value = property.GetValue(instance);

                // Check if value is not null and not an empty string
                if (value != null && !(value is string str && string.IsNullOrWhiteSpace(str)))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetDefaultMessage()
        {
            var propertyList = string.Join(", ", _propertyNames);
            return $"At least one of the following properties must have a value: {propertyList}";
        }

        private string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }

            throw new ArgumentException("Expression must be a property access expression.");
        }
    }

    // Extension method for Validator to make it easier to use
    public static class AtLeastOneRuleExtensions
    {
        public static AtLeastOneRule<T> AtLeastOne<T>(this Validator<T> validator)
        {
            var rule = new AtLeastOneRule<T>();
            // We need access to the model rules list, so this needs to be added via Rule()
            return rule;
        }
    }




}