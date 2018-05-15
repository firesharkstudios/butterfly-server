using System;
using System.Text.RegularExpressions;

namespace Butterfly.Core.Util.Field {
    public interface IFieldValidator {

        string Validate(string value);

    }
}
