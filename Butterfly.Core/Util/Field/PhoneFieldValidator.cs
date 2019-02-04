/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Text.RegularExpressions;

using NLog;
using PhoneNumbers;

namespace Butterfly.Core.Util.Field {
    public class PhoneFieldValidator : IFieldValidator {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly string name;
        protected readonly bool allowNull;

        protected static readonly Regex NON_PHONE_CHARS = new Regex(@"[^\+0-9]");

        protected static readonly PhoneNumberUtil PHONE_NUMBER_UTIL = PhoneNumbers.PhoneNumberUtil.GetInstance();

        public PhoneFieldValidator(string name, bool allowNull = true) {
            this.name = name;
            this.allowNull = allowNull;
        }

        public string Validate(string value) {
            logger.Debug($"Validate():value={value}");

            if (string.IsNullOrEmpty(value)) {
                if (this.allowNull) return value;
                throw new Exception($"Field {this.name} cannot be null");
            }

            var phoneNumber = PHONE_NUMBER_UTIL.Parse(value, "US");
            if (!PHONE_NUMBER_UTIL.IsValidNumber(phoneNumber)) throw new Exception($"Invalid {this.name} number");
            var formattedPhoneNumber = PHONE_NUMBER_UTIL.Format(phoneNumber, PhoneNumberFormat.E164);
            var result = NON_PHONE_CHARS.Replace(formattedPhoneNumber, "").Trim();
            logger.Debug($"Validate():result={result}");
            return result;
        }

        public static string Format(string value) {
            var phoneNumber = PHONE_NUMBER_UTIL.Parse(value, "US");
            return PHONE_NUMBER_UTIL.Format(phoneNumber, PhoneNumberFormat.NATIONAL);
        }
    }
}
