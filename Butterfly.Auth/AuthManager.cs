using System;
using System.Linq;
using System.Threading.Tasks;

using Butterfly.Database;
using Butterfly.Util;
using Butterfly.WebApi;
using NLog;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Auth {

    public class AuthManager {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public const string UNAUTHORIZED = "Unauthorized";

        protected readonly IDatabase database;

        protected readonly int authTokenDurationDays;
        protected readonly int resetCodeLength;
        protected readonly int resetTokenDurationMinutes;

        protected readonly string accountTableName;

        protected readonly string userTableName;
        protected readonly string userTableIdFieldName;
        protected readonly string userTableUsernameFieldName;
        protected readonly string userTableEmailFieldName;
        protected readonly string userTableSaltFieldName;
        protected readonly string userTablePasswordHashFieldName;
        protected readonly string userTableFirstNameFieldName;
        protected readonly string userTableLastNameFieldName;
        protected readonly string userTableResetCodeFieldName;
        protected readonly string userTableResetCodeExpiresAtFieldName;
        protected readonly string userTableAccountIdFieldName;

        protected readonly string authTokenTableName;
        protected readonly string authTokenIdFieldName;
        protected readonly string authTokenTableUserIdFieldName;
        protected readonly string authTokenTableExpiresAtFieldName;

        protected readonly Action onRegister;
        protected readonly Action onForgotPassword;

        protected readonly FieldValidator usernameFieldValidator;
        protected readonly FieldValidator passwordFieldValidator;
        protected readonly FieldValidator firstNameFieldValidator;
        protected readonly FieldValidator lastNameFieldValidator;
        protected readonly FieldValidator emailFieldValidator;

        public AuthManager(
            IDatabase database, 
            int authTokenDurationDays = 90,
            int resetCodeLength = 6,
            int resetTokenDurationMinutes = 90,
            string accountTableName = "account",
            string userTableName = "user", 
            string userTableIdFieldName = "id",
            string userTableUsernameFieldName = "username",
            string userTableEmailFieldName = "email",
            string userTableSaltFieldName = "salt",
            string userTablePasswordHashFieldName = "password_hash",
            string userTableFirstNameFieldName = "first_name",
            string userTableLastNameFieldName = "last_name",
            string userTableResetCodeFieldName = "reset_code",
            string userTableResetCodeExpiresAtFieldName = "reset_code_expires_at",
            string userTableAccountIdFieldName = "account_id",
            string authTokenTableName = "auth_token",
            string authTokenIdFieldName = "id",
            string authTokenTableUserIdFieldName = "user_id",
            string authTokenTableExpiresAtFieldName = "expires_at",
            Action onRegister = null,
            Action onForgotPassword = null
        ) {
            this.database = database;

            this.authTokenDurationDays = authTokenDurationDays;
            this.resetCodeLength = resetCodeLength;
            this.resetTokenDurationMinutes = resetTokenDurationMinutes;

            this.accountTableName = accountTableName;

            this.userTableName = userTableName;
            this.userTableIdFieldName = userTableIdFieldName;
            this.userTableUsernameFieldName = userTableUsernameFieldName;
            this.userTableEmailFieldName = userTableEmailFieldName;
            this.userTableSaltFieldName = userTableSaltFieldName;
            this.userTablePasswordHashFieldName = userTablePasswordHashFieldName;
            this.userTableFirstNameFieldName = userTableFirstNameFieldName;
            this.userTableLastNameFieldName = userTableLastNameFieldName;
            this.userTableResetCodeFieldName = userTableResetCodeFieldName;
            this.userTableResetCodeExpiresAtFieldName = userTableResetCodeExpiresAtFieldName;
            this.userTableAccountIdFieldName = userTableAccountIdFieldName;

            this.authTokenTableName = authTokenTableName;
            this.authTokenIdFieldName = authTokenIdFieldName;
            this.authTokenTableUserIdFieldName = authTokenTableUserIdFieldName;
            this.authTokenTableExpiresAtFieldName = authTokenTableExpiresAtFieldName;

            this.onRegister = onRegister;
            this.onForgotPassword = onForgotPassword;

            this.usernameFieldValidator = new FieldValidator(this.userTableUsernameFieldName, @"^[_A-z0-9\-\.]{3,25}$", allowNull: false, forceLowerCase: true, includeValueInError: true);
            this.passwordFieldValidator = new FieldValidator("password", "^.{6,255}$", allowNull: false, forceLowerCase: false, includeValueInError: false);
            this.firstNameFieldValidator = new FieldValidator(this.userTableFirstNameFieldName, "^[^\\\'\\\"]{ 1, 25 }$", allowNull: false, forceLowerCase: false, includeValueInError: true);
            this.lastNameFieldValidator = new FieldValidator(this.userTableLastNameFieldName, "^[^\\\'\\\"]{ 1, 25 }$", allowNull: false, forceLowerCase: false, includeValueInError: true);
            this.emailFieldValidator = new FieldValidator(this.userTableEmailFieldName, @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$", allowNull: false, forceLowerCase: false, includeValueInError: true);
        }

        public void SetupWebApi(IWebApiServer webApiServer, string pathPrefix) {
            webApiServer.OnGet($"{pathPrefix}/check-username/{{username}}", async(req, res) => {
                string username = req.QueryParams.GetAs("username", (string)null);
                Dict user = await this.LookupUsername(username, this.userTableIdFieldName);
                bool available = user == null;
                await res.WriteAsJsonAsync(available);
            });

            webApiServer.OnPost($"{pathPrefix}/auth-token", async(req, res) => {
                AuthToken authToken = await this.CreateAnonymousUser();
                await res.WriteAsJsonAsync(authToken);
            });

            webApiServer.OnPost($"{pathPrefix}/register", async(req, res) => {
                Dict registration = await req.ParseAsJsonAsync<Dict>();
                AuthToken authToken = await this.RegisterAsync(registration);
                await res.WriteAsJsonAsync(authToken);
            });

            webApiServer.OnPost($"{pathPrefix}/login", async(req, res) => {
                Dict login = await req.ParseAsJsonAsync<Dict>();
                AuthToken authToken = await this.Login(login);
                await res.WriteAsJsonAsync(authToken);
            });

            webApiServer.OnPost($"{pathPrefix}/forgot-password/{{username}}", async(req, res) => {
                string username = req.QueryParams.GetAs("username", (string)null);
                await this.ForgotPassword(username);
            });

            webApiServer.OnPost($"{pathPrefix}/reset-password", async(req, res) => {
                Dict resetPassword = await req.ParseAsJsonAsync<Dict>();
                AuthToken authToken = await this.ResetPassword(resetPassword);
                await res.WriteAsJsonAsync(authToken);
            });
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public async Task<AuthToken> RegisterAsync(dynamic input) {
            Dict registration = this.ConvertInputToDict(input);

            // Handle registering an anonymous user
            string userId = registration.GetAs(this.userTableIdFieldName, (string)null);
            Dict existingUserByUserId = await this.database.SelectRowAsync(this.userTableName, userId);
            if (existingUserByUserId == null) {
                userId = null;
            }
            else {
                string userName = existingUserByUserId.GetAs(this.userTableUsernameFieldName, (string)null);
                if (!string.IsNullOrEmpty(userName)) throw new Exception("User '" + userId + "' already registered");
            }

            // Check if username is available
            string username = this.usernameFieldValidator.Validate(registration?.GetAs(this.userTableUsernameFieldName, (string)null));
            Dict existingUserByUsername = await this.database.SelectRowAsync(this.userTableName, new Dict {
                { this.userTableUsernameFieldName, username }
            });
            if (existingUserByUsername != null) throw new Exception("Username '" + username + "' is unavailable");

            string password = this.passwordFieldValidator.Validate(registration?.GetAs("password", (string)null));
            string email = this.emailFieldValidator.Validate(registration?.GetAs(this.userTableEmailFieldName, (string)null));
            string firstName = this.firstNameFieldValidator.Validate(registration?.GetAs(this.userTableFirstNameFieldName, (string)null));
            string lastName = this.lastNameFieldValidator.Validate(registration?.GetAs(this.userTableLastNameFieldName, (string)null));

            string salt = Guid.NewGuid().ToString();
            string passwordHash = $"{salt} {password}".Hash();

            Dict user = new Dict {
                { this.userTableUsernameFieldName, username },
                { this.userTableSaltFieldName, salt },
                { this.userTablePasswordHashFieldName, passwordHash },
                { this.userTableEmailFieldName, email },
                { this.userTableFirstNameFieldName, firstName  },
                { this.userTableLastNameFieldName, lastName },
            };
            if (string.IsNullOrEmpty(userId)) {
                userId = await this.database.InsertAndCommitAsync<string>(this.userTableName, user);
            }
            else {
                user[this.userTableIdFieldName] = userId;
                await this.database.UpdateAndCommitAsync(this.userTableName, user);
            }

            if (this.onRegister != null) this.onRegister();

            return await this.CreateAuthToken(userId);
        }

        /// <summary>
        /// Validates the auth token id returning an <see cref="AuthToken"/> instance
        /// </summary>
        /// <param name="authTokenId"></param>
        /// <returns>An <see cref="AuthToken"/> instance</returns>
        public async Task<AuthToken> Authenticate(string authTokenId) {
            Dict authTokenDict = await this.database.SelectRowAsync($"SELECT at.{this.authTokenIdFieldName}, at.{this.authTokenTableUserIdFieldName}, u.{this.userTableAccountIdFieldName}, at.{this.authTokenTableExpiresAtFieldName} FROM {this.authTokenTableName} at INNER JOIN {this.userTableName} u ON at.user_id=u.id", authTokenId);
            if (authTokenDict == null) throw new Exception(AuthManager.UNAUTHORIZED);
            var authToken = AuthToken.FromDict(authTokenDict, this.authTokenIdFieldName, this.authTokenTableUserIdFieldName, this.userTableAccountIdFieldName, this.authTokenTableExpiresAtFieldName);

            if (authToken.expiresAt == DateTime.MinValue || authToken.expiresAt < DateTime.Now) throw new Exception(AuthManager.UNAUTHORIZED);

            return authToken;
        }

        public async Task<Dict> LookupUsername(string username, string fieldNames = "*") {
            username = this.usernameFieldValidator.Validate(username);
            return await this.database.SelectRowAsync($"SELECT {fieldNames} FROM {this.userTableName} WHERE {this.userTableUsernameFieldName}=@username", new {
                username
            });
        }

        /// <summary>
        /// Creates an anonymous user and returns a valid <see cref="AuthToken"/>
        /// </summary>
        /// <returns>An  <see cref="AuthToken"/> instance created</returns>
        public async Task<AuthToken> CreateAnonymousUser() {
            Random random = new Random();
            using (ITransaction transaction = await this.database.BeginTransactionAsync()) {
                string accountId = await transaction.InsertAsync<string>(this.accountTableName, new Dict {
                });

                string userId = await transaction.InsertAsync<string>(this.userTableName, new Dict {
                    { this.userTableFirstNameFieldName, CleverNameX.COLORS[random.Next(0, CleverNameX.COLORS.Length)] },
                    { this.userTableLastNameFieldName, CleverNameX.ANIMALS[random.Next(0, CleverNameX.ANIMALS.Length)] },
                    { this.userTableAccountIdFieldName, accountId }
                });

                DateTime expiresAt = DateTime.Now.AddDays(this.authTokenDurationDays);
                string id = await transaction.InsertAsync<string>(this.authTokenTableName, new Dict {
                    { this.authTokenTableUserIdFieldName, userId },
                    { this.authTokenTableExpiresAtFieldName, expiresAt },
                });

                return new AuthToken(id, userId, accountId, expiresAt);
            }
        }

        /// <summary>
        /// Create an auth token
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>The AuthToken instance created</returns>
        public async Task<AuthToken> CreateAuthToken(string userId) {
            string accountId = await this.database.SelectValueAsync<string>("SELECT account_id FROM user WHERE id=@userId", new {
                userId
            });
            if (string.IsNullOrEmpty(accountId)) throw new Exception("Invalid user");

            DateTime expiresAt = DateTime.Now.AddDays(this.authTokenDurationDays);
            string id = await this.database.InsertAndCommitAsync<string>(this.authTokenTableName, new Dict {
                { this.authTokenTableUserIdFieldName, userId },
                { this.authTokenTableExpiresAtFieldName, expiresAt },
            });

            return new AuthToken(id, userId, accountId, expiresAt);
        }

        public async Task<AuthToken> Login(Dict login) {
            string username = login?.GetAs(this.userTableUsernameFieldName, (string)null);
            Dict user = await this.LookupUsername(username, string.Join(",", new string[] { this.userTableIdFieldName, this.userTableSaltFieldName, this.userTablePasswordHashFieldName }));
            if (user == null) throw new Exception("Invalid username '" + username + "'");

            string passwordHash = user.GetAs(this.userTablePasswordHashFieldName, (string)null);

            string salt = user.GetAs(this.userTableSaltFieldName, (string)null);
            string password = this.passwordFieldValidator.Validate(login?.GetAs("password", (string)null));
            string newPasswordHash = $"{salt} {password}".Hash();

            if (newPasswordHash != passwordHash) throw new Exception("Incorrect password");

            string userId = user.GetAs(this.userTableIdFieldName, (string)null);
            return await this.CreateAuthToken(userId);
        }

        public async Task ForgotPassword(string username) {
            Dict user = await this.LookupUsername(username, string.Join(",", new string[] { this.userTableIdFieldName, this.userTableFirstNameFieldName, this.userTableLastNameFieldName, this.userTableEmailFieldName }));
            if (user == null) throw new Exception("Invalid username '" + username + "'");

            string userId = user.GetAs(this.userTableIdFieldName, (string)null);
            string resetCode = await this.CreateResetCode(userId, username);

            user["resetCode"] = resetCode;

            /*
            string resetEmailTo = user.GetAs(this.userTableFirstNameFieldName, (string)null) + " " + user.GetAs(this.userTableLastNameFieldName, (string)null) + " <" + user.GetAs(this.userTableEmailFieldName, (string)null) + ">";
            await this.helpFireContext.SendMessageManager.QueueEmail(NotifyMessagePriority.High, user, this.resetEmailMessage.From, resetEmailTo, this.resetEmailMessage.Subject, this.resetEmailMessage.BodyText);

            var emailVars = new Dict {
                { "user", user },
                { "hostName", System.Net.Dns.GetHostName() },
            };
            await this.helpFireContext.SendMessageManager.QueueEmail(NotifyMessagePriority.Low, emailVars, this.notifyForgotPasswordEmailMessage.From, this.notifyForgotPasswordEmailMessage.To, this.notifyForgotPasswordEmailMessage.Subject, this.notifyForgotPasswordEmailMessage.BodyText);
            */
            if (this.onForgotPassword != null) this.onForgotPassword();
        }

        public async Task<AuthToken> ResetPassword(Dict resetPassword) {
            string username = resetPassword.GetAs(this.userTableUsernameFieldName, (string)null);
            Dict user = await this.database.SelectRowAsync(this.userTableName, new Dict {
                { this.userTableUsernameFieldName, username }
            });
            if (user == null) throw new Exception("Invalid username '" + username + "'");

            string userResetCode = user.GetAs(this.userTableResetCodeFieldName, (string)null);
            string resetCode = resetPassword.GetAs(this.userTableResetCodeFieldName, (string)null);
            if (!userResetCode.Equals(resetCode, StringComparison.OrdinalIgnoreCase)) throw new Exception("Invalid reset code '" + resetCode + "'");

            DateTime expiresAt = user.GetAs(this.userTableResetCodeExpiresAtFieldName, DateTime.MinValue);
            if (expiresAt==DateTime.MinValue || expiresAt < DateTime.Now) throw new Exception("Reset code has expired");

            string userId = user.GetAs(this.userTableIdFieldName, (string)null);
            string salt = user.GetAs(this.userTableSaltFieldName, (string)null);
            string password = this.passwordFieldValidator.Validate(resetPassword?.GetAs("password", (string)null));
            string passwordHash = $"{salt} {password}".Hash();

            Dict update = new Dict {
                { this.userTableIdFieldName, userId },
                { this.userTablePasswordHashFieldName, passwordHash },
            };
            await this.database.UpdateAndCommitAsync(this.userTableName, update);

            return await this.CreateAuthToken(userId);
        }

        protected async Task<string> CreateResetCode(string userId, string username) {
            Random random = new Random();
            var resetCode = random.Next(10 ^ this.resetCodeLength).ToString(String.Concat(Enumerable.Repeat("0", this.resetCodeLength)));

            DateTime expiresAt = DateTime.Now.AddMinutes(this.resetTokenDurationMinutes);
            await this.database.UpdateAndCommitAsync(this.userTableName, new Dict {
                { this.userTableResetCodeFieldName, resetCode },
                { this.userTableResetCodeExpiresAtFieldName, expiresAt },
            });

            return resetCode;
        }


        public Dict ConvertInputToDict(dynamic input) {
            // If is null, return empty dictionary
            if (input == null) {
                return new Dict();
            }

            // If is already a dictionary, return the dictionary
            else if (input is Dict d) {
                return new Dict(d);
            }

            // Otherwise, convert input to a dictionary
            else {
                return DynamicX.ToDictionary(input);
            }
        }

    }

    public class AuthToken {
        public readonly string id;
        public readonly string userId;
        public readonly string accountId;
        public readonly DateTime expiresAt;

        public AuthToken(string id, string userId, string accountId, DateTime expiresAt) {
            this.id = id;
            this.userId = userId;
            this.accountId = accountId;
            this.expiresAt = expiresAt;
        }

        public static AuthToken FromDict(Dict dict, string idFieldName, string userIdFieldName, string accountIdFieldName, string expiresAtFieldName) {
            string id = dict.GetAs(idFieldName, (string)null);
            string userId = dict.GetAs(userIdFieldName, (string)null);
            string accountId = dict.GetAs(accountIdFieldName, (string)null);
            DateTime expiresAt = dict.GetAs(expiresAtFieldName, DateTime.MinValue);
            return new AuthToken(id, userId, accountId, expiresAt);
        }
    }
}
