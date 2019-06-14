/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Text.RegularExpressions;

using NLog;

namespace Butterfly.Core.Util.Field {
    public class TextFieldValidator : IFieldValidator {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly string name;
        protected readonly bool allowNull;
        protected readonly bool trim;
        protected readonly bool forceLower;
        protected readonly bool forceUpper;
        protected readonly int maxLength;
        protected readonly Regex validRegex;
        protected readonly Regex removeRegex;

        public TextFieldValidator(string name, bool allowNull = true, bool trim = true, bool forceLower = false, bool forceUpper = false, int maxLength = 25, string validRegex = null, string removeRegex = null) {
            this.name = name;
            this.allowNull = allowNull;
            this.trim = trim;
            this.forceLower = forceLower;
            this.forceUpper = forceUpper;
            this.maxLength = maxLength;
            this.validRegex = string.IsNullOrEmpty(validRegex) ? null : new Regex(validRegex);
            this.removeRegex = string.IsNullOrEmpty(removeRegex) ? null : new Regex(removeRegex);
        }

        public string Validate(string value) {
            logger.Debug($"Validate():value={value}");

            if (string.IsNullOrEmpty(value)) {
                if (!this.allowNull) throw new Exception($"Field {this.name} cannot be null");
                return null;
            }

            string newValue = value;
            if (!string.IsNullOrEmpty(newValue)) {
                if (this.trim) newValue = value.Trim();
                if (this.forceLower) newValue = value.ToLower();
                if (this.forceUpper) newValue = value.ToUpper();
            }

            if (!string.IsNullOrEmpty(newValue) && newValue.Length > this.maxLength) throw new Exception($"{this.name} too long");
            if (this.validRegex != null && !this.validRegex.IsMatch(newValue)) throw new Exception($"{this.name} is invalid");
            if (this.removeRegex != null) newValue = removeRegex.Replace(newValue, "");

            return newValue;
        }
    }
}
