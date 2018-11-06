using System.Threading.Tasks;

using NLog;

using Butterfly.Core.Database;
using Butterfly.Core.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Auth {
    public class ShareCodeAuthenticator : IAuthenticator {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public const string AUTH_TYPE = "Share-Code";

        protected readonly IDatabase database;

        protected readonly string accountTableName;
        protected readonly string accountTableIdFieldName;
        protected readonly string accountTableShareCodeFieldName;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="database"></param>
        /// <param name="accountTableName">Table name of the auth token table (default is "auth_token")</param>
        /// <param name="accountTableIdFieldName">Field name of the id field on the auth token table (default is "id")</param>
        /// <param name="accountTableShareCodeFieldName">Field name of the user id field on the auth token table (default is "user_id")</param>
        public ShareCodeAuthenticator(
            IDatabase database,
            string accountTableName = "account",
            string accountTableIdFieldName = "id",
            string accountTableShareCodeFieldName = "share_code"
        ) {
            this.database = database;
            this.accountTableName = accountTableName;
            this.accountTableIdFieldName = accountTableIdFieldName;
            this.accountTableShareCodeFieldName = accountTableShareCodeFieldName;
        }

        /// <summary>
        /// Validates the auth token id returning an <see cref="AuthToken"/> instance
        /// </summary>
        /// <param name="authType"></param>
        /// <param name="authValue"></param>
        /// <returns>An <see cref="AuthToken"/> instance</returns>
        public async Task<AuthToken> AuthenticateAsync(string authType, string authValue) {
            string accountId = await this.database.SelectValueAsync<string>($"SELECT {this.accountTableIdFieldName} FROM {this.accountTableName} WHERE {this.accountTableShareCodeFieldName}=@shareCode", new {
                shareCode = authValue
            });
            logger.Debug($"Authenticate():accountId={accountId}");
            if (string.IsNullOrEmpty(accountId)) throw new UnauthorizedException();
            return new ShareCodeToken(accountId, null);
        }
    }

    /// <summary>
    /// Represents the result of a successful <see cref="AuthManager.LoginAsync(Dict)"/> or <see cref="AuthManager.RegisterAsync(dynamic, Dict)"/>
    /// </summary>
    public class ShareCodeToken : AuthToken {
        public ShareCodeToken(string accountId, string role) : base(ShareCodeAuthenticator.AUTH_TYPE, accountId, role) {
        }
    }

}
