using System;
using System.Text.RegularExpressions;

namespace Butterfly.Database {
    public class FieldValidator {
        protected readonly string fieldName;
        protected readonly Regex regex;
        protected readonly bool allowNull;
        protected readonly bool forceLowerCase;
        protected readonly bool includeValueInError;

        public FieldValidator(string fieldName, string regex, bool allowNull = true, bool forceLowerCase = false, bool includeValueInError = true) {
            this.fieldName = fieldName;
            this.regex = new Regex(regex);
            this.allowNull = allowNull;
            this.forceLowerCase = forceLowerCase;
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
            if (forceLowerCase && !string.IsNullOrEmpty(value)) {
                return value.ToLower();
            }
            else {
                return value;
            }
        }
    }
}
