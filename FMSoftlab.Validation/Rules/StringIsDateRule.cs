using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace FMSoftlab.Validation.Rules
{
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

}
