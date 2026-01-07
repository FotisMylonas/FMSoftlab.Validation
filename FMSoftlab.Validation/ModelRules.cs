using System;
using System.Collections.Generic;
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
        protected string? _message;
        protected MessageType _messageType = MessageType.Error;
        protected string? _propertyName;

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
            _message = message;
            return this;
        }

        public ModelValidationRule<T> ForProperty(string propertyName)
        {
            _propertyName = propertyName;
            return this;
        }

        public ModelValidationRule<T> AsWarning()
        {
            _messageType = MessageType.Warning;
            return this;
        }

        protected bool ShouldExecute(T instance)
            => _when == null || _when(instance);

        public abstract ValidationResult Validate(T instance);
        public abstract Task<ValidationResult> ValidateAsync(T instance);
    }

    public class ModelCustomRule<T> : ModelValidationRule<T>
    {
        private readonly Func<T, bool> _predicate;

        public ModelCustomRule(Func<T, bool> predicate)
        {
            _predicate = predicate;
        }

        public override ValidationResult Validate(T instance)
        {
            var result = new ValidationResult();

            if (!ShouldExecute(instance))
                return result;

            if (!_predicate(instance))
            {
                result.Messages.Add(new ValidationMessage
                {
                    PropertyName = _propertyName ?? string.Empty,
                    Message = _message ?? "Model validation failed.",
                    Type = _messageType
                });
            }

            return result;
        }

        public override Task<ValidationResult> ValidateAsync(T instance)
            => Task.FromResult(Validate(instance));
    }

    public class AsyncModelCustomRule<T> : ModelValidationRule<T>
    {
        private readonly Func<T, Task<bool>> _predicate;

        public AsyncModelCustomRule(Func<T, Task<bool>> predicate)
        {
            _predicate = predicate;
        }

        public override ValidationResult Validate(T instance)
            => new ValidationResult();

        public override async Task<ValidationResult> ValidateAsync(T instance)
        {
            var result = new ValidationResult();

            if (!ShouldExecute(instance))
                return result;

            if (!await _predicate(instance))
            {
                result.Messages.Add(new ValidationMessage
                {
                    PropertyName = _propertyName ?? string.Empty,
                    Message = _message ?? "Async model validation failed.",
                    Type = _messageType
                });
            }

            return result;
        }
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
