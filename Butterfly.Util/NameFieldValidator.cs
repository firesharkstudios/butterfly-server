using NLog;
using System;
using System.Text.RegularExpressions;

namespace Butterfly.Util {
    public class NameFieldValidator : IFieldValidator {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly string name;
        protected readonly bool allowNull;
        protected readonly int maxLength;

        public NameFieldValidator(string name, bool allowNull = true, int maxLength = 25) {
            this.name = name;
            this.allowNull = allowNull;
            this.maxLength = maxLength;
        }

        public string Validate(string value) {
            logger.Debug($"Validate():value={value}");

            if (string.IsNullOrEmpty(value) && !this.allowNull) throw new Exception($"Field {this.name} cannot be null");
            if (!string.IsNullOrEmpty(value) && value.Length > this.maxLength) throw new Exception($"{this.name} too long");
            if (!string.IsNullOrEmpty(value) && value.Contains("\"")) throw new Exception($"{this.name} cannot contain double quotes");

            return value;
        }
    }
}
