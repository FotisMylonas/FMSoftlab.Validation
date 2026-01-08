using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;


namespace FMSoftlab.Validation.Rules.Model
{
    /// <summary>
    /// Validates that at least one of the specified properties has a value
    /// </summary>
    public class AtLeastOneValueExistsRule<T> : ModelValidationRule<T>, IModelValidationRule<T>
    {
        private readonly List<string> _propertyNames = new List<string>();

        public AtLeastOneValueExistsRule<T> Property<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            var propertyName = GetPropertyName(expression);
            _propertyNames.Add(propertyName);
            return this;
        }

        public override ValidationResult Validate(T instance)
        {
            var result = new ValidationResult();

            if (!ShouldExecute(instance))
                return result;

            if (!HasAtLeastOneValue(instance))
            {
                result.Messages.Add(new ValidationMessage
                {
                    PropertyName = string.Empty,
                    Message = GetOutputMessage(instance),
                    Type = _messageType
                });
            }
            return result;
        }

        public override Task<ValidationResult> ValidateAsync(T instance)
        {
            return Task.FromResult(Validate(instance));
        }

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

        protected override string GetDefaultMessage(T instance)
        {
            var propertyList = string.Join(", ", _propertyNames);
            return $"At least one of the following properties must have a value: {propertyList}";
        }
    }

    // Extension method for Validator to make it easier to use
    public static class AtLeastOneRuleExtensions
    {
        public static AtLeastOneValueExistsRule<T> AtLeastOne<T>(this Validator<T> validator)
        {
            var rule = new AtLeastOneValueExistsRule<T>();
            // We need access to the model rules list, so this needs to be added via Rule()
            return rule;
        }
    }
}