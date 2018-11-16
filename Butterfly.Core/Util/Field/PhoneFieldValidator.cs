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

        protected readonly PhoneNumberUtil phoneNumberUtil;

        public PhoneFieldValidator(string name, bool allowNull = true) {
            this.name = name;
            this.allowNull = allowNull;
            this.phoneNumberUtil = PhoneNumbers.PhoneNumberUtil.GetInstance();
        }

        public string Validate(string value) {
            logger.Debug($"Validate():value={value}");

            if (string.IsNullOrEmpty(value)) {
                if (this.allowNull) return value;
                throw new Exception($"Field {this.name} cannot be null");
            }

            var phoneNumber = this.phoneNumberUtil.Parse(value, "US");
            if (!this.phoneNumberUtil.IsValidNumber(phoneNumber)) throw new Exception($"Invalid {this.name} number");
            var formattedPhoneNumber = this.phoneNumberUtil.Format(phoneNumber, PhoneNumberFormat.E164);
            var result = NON_PHONE_CHARS.Replace(formattedPhoneNumber, "").Trim();
            logger.Debug($"Validate():result={result}");
            return result;

            /*
            string newPhone = NON_PHONE_CHARS.Replace(value, "").Trim();
            if (!newPhone.StartsWith("+") && newPhone.Length == 10) {
                return $"+1{newPhone}";
            }
            else {
                return newPhone;
            }
            */
        }
    }
}
