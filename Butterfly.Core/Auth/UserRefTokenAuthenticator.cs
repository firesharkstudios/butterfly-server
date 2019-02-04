using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NLog;

using Butterfly.Core.Database;
using Butterfly.Core.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Auth {
    public class UserRefTokenAuthenticator : IAuthenticator {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public const string AUTH_TYPE = "User-Ref-Token";

        protected readonly IDatabase database;
        protected readonly string authTokenTableName;
        protected readonly string authTokenIdFieldName;
        protected readonly string authTokenTableUserIdFieldName;
        protected readonly string authTokenTableExpiresAtFieldName;

        protected readonly string userTableName;
        protected readonly string userTableUsernameFieldName;
        protected readonly string userTableAccountIdFieldName;
        protected readonly string userTableRoleFieldName;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="database"></param>
        /// <param name="authTokenTableName">Table name of the auth token table (default is "auth_token")</param>
        /// <param name="authTokenIdFieldName">Field name of the id field on the auth token table (default is "id")</param>
        /// <param name="authTokenTableUserIdFieldName">Field name of the user id field on the auth token table (default is "user_id")</param>
        /// <param name="authTokenTableExpiresAtFieldName">Field name of the expires at field on the auth token table (default is "expires_at")</param>
        /// <param name="userTableName">Table name of the user table (default is "user")</param>
        /// <param name="userTableUsernameFieldName">Field name of the username field on the user table (default is "username")</param>
        /// <param name="userTableAccountIdFieldName">Field name of the account id field on the user table (default is "account_id")</param>
        /// <param name="userTableRoleFieldName">Field name of the role field on the user table (default is "role")</param>
        public UserRefTokenAuthenticator(
            IDatabase database,
            string authTokenTableName = "auth_token",
            string authTokenIdFieldName = "id",
            string authTokenTableUserIdFieldName = "user_id",
            string authTokenTableExpiresAtFieldName = "expires_at",
            string userTableName = "user",
            string userTableUsernameFieldName = "username",
            string userTableAccountIdFieldName = "account_id",
            string userTableRoleFieldName = "role"
        ) {
            this.database = database;
            this.authTokenTableName = authTokenTableName;
            this.authTokenIdFieldName = authTokenIdFieldName;
            this.authTokenTableUserIdFieldName = authTokenTableUserIdFieldName;
            this.authTokenTableExpiresAtFieldName = authTokenTableExpiresAtFieldName;

            this.userTableName = userTableName;
            this.userTableUsernameFieldName = userTableUsernameFieldName;
            this.userTableAccountIdFieldName = userTableAccountIdFieldName;
            this.userTableRoleFieldName = userTableRoleFieldName;
        }

        /// <summary>
        /// Validates the auth token id returning an <see cref="AuthToken"/> instance
        /// </summary>
        /// <param name="authType"></param>
        /// <param name="authValue"></param>
        /// <returns>An <see cref="AuthToken"/> instance</returns>
        public async Task<AuthToken> AuthenticateAsync(string authType, string authValue) {
            List<string> fieldList = new List<string>();
            if (!string.IsNullOrEmpty(this.authTokenIdFieldName)) fieldList.Add($"at.{ this.authTokenIdFieldName}");
            if (!string.IsNullOrEmpty(this.authTokenTableUserIdFieldName)) fieldList.Add($"at.{ this.authTokenTableUserIdFieldName}");
            if (!string.IsNullOrEmpty(this.userTableAccountIdFieldName)) fieldList.Add($"u.{ this.userTableAccountIdFieldName}");
            if (!string.IsNullOrEmpty(this.userTableUsernameFieldName)) fieldList.Add($"u.{ this.userTableUsernameFieldName}");
            if (!string.IsNullOrEmpty(this.userTableRoleFieldName)) fieldList.Add($"u.{ this.userTableRoleFieldName}");
            if (!string.IsNullOrEmpty(this.authTokenTableExpiresAtFieldName)) fieldList.Add($"at.{ this.authTokenTableExpiresAtFieldName}");
            Dict authTokenDict = await this.database.SelectRowAsync($"SELECT {string.Join(",", fieldList)} FROM {this.authTokenTableName} at INNER JOIN {this.userTableName} u ON at.user_id=u.id WHERE at.id=@authTokenId", new {
                authTokenId = authValue
            });
            logger.Debug($"Authenticate():authTokenDict={authTokenDict}");
            if (authTokenDict == null) throw new UnauthorizedException();

            var authToken = UserRefToken.FromDict(authTokenDict, this.authTokenIdFieldName, this.authTokenTableUserIdFieldName, this.userTableUsernameFieldName, this.userTableRoleFieldName, this.userTableAccountIdFieldName, this.authTokenTableExpiresAtFieldName);
            logger.Debug($"Authenticate():authToken.expiresAt={authToken.expiresAt}");
            if (authToken.expiresAt == DateTime.MinValue || authToken.expiresAt < DateTime.Now) throw new UnauthorizedException();

            return authToken;
        }

        public Task<string> InsertAsync(ITransaction transaction, string userId, DateTime expiresAt) {
            return transaction.InsertAsync<string>(this.authTokenTableName, new Dict {
                { this.authTokenTableUserIdFieldName, userId },
                { this.authTokenTableExpiresAtFieldName, expiresAt },
            });
        }
    }

    /// <summary>
    /// Represents the result of a successful <see cref="AuthManager.LoginAsync(Dict)"/> or <see cref="AuthManager.RegisterAsync(dynamic, Dict)"/>
    /// </summary>
    public class UserRefToken : AuthToken {
        public readonly string id;
        public readonly string userId;
        public readonly string username;
        public readonly DateTime expiresAt;

        public UserRefToken(string id, string userId, string username, string role, string accountId, DateTime expiresAt) : base(UserRefTokenAuthenticator.AUTH_TYPE, accountId, role) {
            this.id = id;
            this.userId = userId;
            this.username = username;
            this.expiresAt = expiresAt;
        }

        public static UserRefToken FromDict(Dict dict, string idFieldName, string userIdFieldName, string usernameFieldName, string roleFieldName, string accountIdFieldName, string expiresAtFieldName) {
            string id = string.IsNullOrEmpty(idFieldName) ? null : dict.GetAs(idFieldName, (string)null);
            string userId = string.IsNullOrEmpty(userIdFieldName) ? null : dict.GetAs(userIdFieldName, (string)null);
            string username = string.IsNullOrEmpty(usernameFieldName) ? null : dict.GetAs(usernameFieldName, (string)null);
            string role = string.IsNullOrEmpty(roleFieldName) ? null : dict.GetAs(roleFieldName, (string)null);
            string accountId = string.IsNullOrEmpty(accountIdFieldName) ? null : dict.GetAs(accountIdFieldName, (string)null);
            DateTime expiresAt = string.IsNullOrEmpty(expiresAtFieldName) ? DateTime.MaxValue : dict.GetAs(expiresAtFieldName, DateTime.MinValue);
            return new UserRefToken(id, userId, username, role, accountId, expiresAt);
        }
    }

}
