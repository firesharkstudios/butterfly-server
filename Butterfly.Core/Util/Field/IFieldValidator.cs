using System;
using System.Text.RegularExpressions;

namespace Butterfly.Util.Field {
    public interface IFieldValidator {

        string Validate(string value);

    }
}
