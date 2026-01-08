using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.Validation.Rules
{
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


}
