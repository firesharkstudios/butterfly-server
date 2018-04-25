using System;
using System.Text.RegularExpressions;

namespace Butterfly.Util {
    public class GenericFieldValidator : IFieldValidator {
        protected readonly string fieldName;
        protected readonly Regex regex;
        protected readonly bool allowNull;
        protected readonly bool forceLowerCase;
        protected readonly bool trim;
        protected readonly bool includeValueInError;

        public GenericFieldValidator(string fieldName, string regex, bool allowNull = true, bool forceLowerCase = false, bool trim = true, bool includeValueInError = true) {
            this.fieldName = fieldName;
            this.regex = new Regex(regex);
            this.allowNull = allowNull;
            this.forceLowerCase = forceLowerCase;
            this.trim = trim;
            this.includeValueInError = includeValueInError;
        }

        public string Validate(string value) {
            if (!this.allowNull && string.IsNullOrEmpty(value)) throw new Exception($"Field {this.fieldName} cannot be null");

            if (!regex.Match(value).Success) {
                if (this.includeValueInError) {
                    throw new Exception($"Invalid {this.fieldName} '{value}'");
                }
                else {
                    throw new Exception($"Invalid {this.fieldName}");
                }
            }

            string newValue = value;
            if (!string.IsNullOrEmpty(value)) {
                if (this.forceLowerCase) newValue = newValue.ToLower();
                if (this.trim) newValue = newValue.Trim();
            }
            return newValue;
        }
    }
}
