using System;
using System.Text.RegularExpressions;

namespace Butterfly.Util {
    public interface IFieldValidator {

        string Validate(string value);

    }
}
