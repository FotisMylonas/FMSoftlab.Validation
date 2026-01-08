using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.Validation.Rules.Model
{
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
                    PropertyName = string.Empty,
                    Message = GetOutputMessage(instance),
                    Type = _messageType
                });
            }
            return result;
        }

        public override Task<ValidationResult> ValidateAsync(T instance)
            => Task.FromResult(Validate(instance));

        protected override string GetDefaultMessage(T instance)
        {
            return "Custom rule failed";
        }
    }

    public class AsyncModelCustomRule<T> : ModelValidationRule<T>
    {
        private readonly Func<T, Task<bool>> _predicate;

        public AsyncModelCustomRule(Func<T, Task<bool>> predicate)
        {
            _predicate = predicate;
        }

        public override ValidationResult Validate(T instance) => new ValidationResult();

        public override async Task<ValidationResult> ValidateAsync(T instance)
        {
            var result = new ValidationResult();

            if (!ShouldExecute(instance))
                return result;

            if (!await _predicate(instance))
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

        protected override string GetDefaultMessage(T instance)
        {
            return "Custom async rule failed";
        }
    }

}
