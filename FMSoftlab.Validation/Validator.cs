using FMSoftlab.Validation.Rules.Model;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.Validation
{
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
        public AtLeastOneValueExistsRule<T> AtLeastOneOf()
        {
            var rule = new AtLeastOneValueExistsRule<T>();
            _modelRules.Add(rule);
            return rule;
        }
    }
}