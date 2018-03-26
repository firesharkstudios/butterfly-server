using NLog;
using System;
using System.Text.RegularExpressions;

namespace Butterfly.Util {
    public class PhoneFieldValidator : IFieldValidator {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly string name;
        protected readonly bool allowNull;

        protected static readonly Regex NON_PHONE_CHARS = new Regex(@"[^\+0-9]");

        public PhoneFieldValidator(string name, bool allowNull = true) {
            this.name = name;
            this.allowNull = allowNull;
        }

        public string Validate(string value) {
            logger.Debug($"Validate():value={value}");

            if (!this.allowNull && string.IsNullOrEmpty(value)) throw new Exception($"Field {this.name} cannot be null");

            string newPhone = NON_PHONE_CHARS.Replace(value, "");
            if (!newPhone.StartsWith("+") && newPhone.Length == 10) {
                return $"+1{newPhone}";
            }
            else {
                return newPhone;
            }
        }
    }
}
