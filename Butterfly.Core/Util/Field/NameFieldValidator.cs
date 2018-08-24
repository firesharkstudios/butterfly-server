/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using NLog;
using System;
using System.Text.RegularExpressions;

namespace Butterfly.Core.Util.Field {
    public class NameFieldValidator : IFieldValidator {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly string name;
        protected readonly bool allowNull;
        protected readonly bool trim;
        protected readonly int maxLength;

        public NameFieldValidator(string name, bool allowNull = true, bool trim = true, int maxLength = 25) {
            this.name = name;
            this.allowNull = allowNull;
            this.trim = trim;
            this.maxLength = maxLength;
        }

        public string Validate(string value) {
            logger.Debug($"Validate():value={value}");

            if (string.IsNullOrEmpty(value) && !this.allowNull) throw new Exception($"Field {this.name} cannot be null");
            if (!string.IsNullOrEmpty(value) && value.Length > this.maxLength) throw new Exception($"{this.name} too long");
            if (!string.IsNullOrEmpty(value) && value.Contains("\"")) throw new Exception($"{this.name} cannot contain double quotes");

            string newValue = value;
            if (!string.IsNullOrEmpty(newValue)) {
                if (this.trim) newValue = value.Trim();
            }
            return newValue;
        }
    }
}
