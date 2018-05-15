using NLog;
using System;
using System.Text.RegularExpressions;

namespace Butterfly.Core.Util.Field {
    public class EmailFieldValidator : IFieldValidator {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly string name;
        protected readonly bool allowNull;
        protected readonly bool stripName;

        protected readonly static Regex REGEX = new Regex(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$");

        public EmailFieldValidator(string name, bool allowNull = true, bool stripName = false) {
            this.name = name;
            this.allowNull = allowNull;
            this.stripName = stripName;
        }

        public string Validate(string value) {
            logger.Debug($"Validate():value={value}");

            if (string.IsNullOrEmpty(value)) {
                if (this.allowNull) return value;
                throw new Exception($"Field {this.name} cannot be null");
            }

            int leftPos = value.IndexOf('<');
            int rightPos = value.LastIndexOf('>');
            if (leftPos > 0 && rightPos > 0 && leftPos < rightPos) {
                string emailName = value.Substring(0, leftPos).Trim();
                string emailAddress = value.Substring(leftPos + 1, rightPos - leftPos - 1).Trim().ToLower();
                if (!REGEX.IsMatch(emailAddress)) throw new Exception($"Invalid email address '{emailAddress}'");

                if (!emailAddress.Contains("@")) {
                    throw new Exception("Email address must contain @");
                }
                else if (stripName) {
                    return emailAddress;
                }
                else {
                    return $"{emailName} <{emailAddress}>";
                }
            }
            else if (!value.Contains("@")) {
                throw new Exception("Email address must contain @");
            }
            else {
                string emailAddress = value.Trim().ToLower();
                if (!REGEX.IsMatch(emailAddress)) throw new Exception($"Invalid email address '{emailAddress}'");
                return emailAddress;
            }
        }
    }
}
