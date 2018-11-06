using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Butterfly.Core.Auth {
    public interface IAuthenticator {
        Task<AuthToken> AuthenticateAsync(string authType, string authValue);
    }
}
