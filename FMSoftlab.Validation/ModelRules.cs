using FMSoftlab.Validation.Rules.Model;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.Validation
{
    public interface IModelValidationRule<T>
    {
        ValidationResult Validate(T instance);
        Task<ValidationResult> ValidateAsync(T instance);
    }

    public abstract class ModelValidationRule<T> : IModelValidationRule<T>
    {
        protected Func<T, bool>? _when;
        protected string? _customMessage;
        protected Func<T, string>? _messageFactory;
        protected MessageType _messageType = MessageType.Error;
        //protected string? _propertyName;

        public ModelValidationRule<T> When(Func<T, bool> condition)
        {
            _when = condition;
            return this;
        }

        public ModelValidationRule<T> Unless(Func<T, bool> condition)
        {
            _when = x => !condition(x);
            return this;
        }

        public ModelValidationRule<T> WithMessage(string message)
        {
            _customMessage = message;
            return this;
        }
        public ModelValidationRule<T> WithMessage(Func<T, string> messageFactory)
        {
            _messageFactory = messageFactory;
            return this;
        }

        /*public ModelValidationRule<T> ForProperty(string propertyName)
        {
            _propertyName = propertyName;
            return this;
        }*/

        public ModelValidationRule<T> AsWarning()
        {
            _messageType = MessageType.Warning;
            return this;
        }

        protected bool ShouldExecute(T instance)
            => _when == null || _when(instance);

        protected string GetOutputMessage(T instance)
        {
            if (_messageFactory != null)
                return _messageFactory(instance);

            if (!string.IsNullOrWhiteSpace(_customMessage))
                return _customMessage!;

            return GetDefaultMessage(instance);
        }
        protected abstract string GetDefaultMessage(T instance);

        protected string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }

            throw new ArgumentException("Expression must be a property access expression.");
        }

        public abstract ValidationResult Validate(T instance);
        public abstract Task<ValidationResult> ValidateAsync(T instance);
    }


    public class ModelRuleBuilder<T>
    {
        private readonly List<IModelValidationRule<T>> _rules;

        public ModelRuleBuilder(List<IModelValidationRule<T>> rules)
        {
            _rules = rules;
        }

        public ModelValidationRule<T> Must(Func<T, bool> predicate)
        {
            var rule = new ModelCustomRule<T>(predicate);
            _rules.Add(rule);
            return rule;
        }

        public ModelValidationRule<T> MustAsync(Func<T, Task<bool>> predicate)
        {
            var rule = new AsyncModelCustomRule<T>(predicate);
            _rules.Add(rule);
            return rule;
        }
    }
}
