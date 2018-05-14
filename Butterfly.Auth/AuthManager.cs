using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using NLog;

using Butterfly.Database;
using Butterfly.Util;
using Butterfly.Util.Field;
using Butterfly.WebApi;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Auth {
    /// <summary>
    /// Provides an API to register and login users, handle forgot password and reset password requests, and validate auth tokens. 
    /// </summary>
    /// <remarks>
    /// Can be initialized like this...
    /// <code>
    ///     var database = (initialize an IDatabase instance here)
    ///     var notifyManager = (initialize NotifyManager here)
    ///     var welcomeEmailNotifyMessage = (load welcome email here)
    ///     var resetEmailNotifyMessage = (load reset email here)
    ///     var authManager = new AuthManager(
    ///         database,
    ///         defaultRole: "full-access",
    ///         onEmailVerify: notifyManager.VerifyAsync,
    ///         onPhoneVerify: notifyManager.VerifyAsync,
    ///         onRegister: user => {
    ///             notifyManager.Queue(welcomeEmailNotifyMessage.Evaluate(user));
    ///         },
    ///         onForgotPassword: user => {
    ///             notifyManager.Queue(resetEmailNotifyMessage.Evaluate(user));
    ///         }
    ///     );
    /// </code>
    /// </remarks>
    public class AuthManager {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly IDatabase database;

        protected readonly int authTokenDurationDays;
        protected readonly int resetCodeLength;
        protected readonly int resetTokenDurationMinutes;

        protected readonly string accountTableName;

        protected readonly string userTableName;
        protected readonly string userTableIdFieldName;
        protected readonly string userTableUsernameFieldName;
        protected readonly string userTableEmailFieldName;
        protected readonly string userTableEmailVerifiedAtFieldName;
        protected readonly string userTablePhoneFieldName;
        protected readonly string userTablePhoneVerifiedAtFieldName;
        protected readonly string userTableSaltFieldName;
        protected readonly string userTablePasswordHashFieldName;
        protected readonly string userTableFirstNameFieldName;
        protected readonly string userTableLastNameFieldName;
        protected readonly string userTableResetCodeFieldName;
        protected readonly string userTableResetCodeExpiresAtFieldName;
        protected readonly string userTableAccountIdFieldName;
        protected readonly string userTableRoleFieldName;

        protected readonly string authTokenTableName;
        protected readonly string authTokenIdFieldName;
        protected readonly string authTokenTableUserIdFieldName;
        protected readonly string authTokenTableExpiresAtFieldName;

        protected readonly string defaultRole;

        protected readonly Func<string, int, Task> onEmailVerify;
        protected readonly Func<string, int, Task> onPhoneVerify;

        protected readonly Action<Dict> onRegister;
        protected readonly Action<Dict> onForgotPassword;

        protected readonly IFieldValidator usernameFieldValidator;
        protected readonly IFieldValidator passwordFieldValidator;
        protected readonly IFieldValidator nameFieldValidator;
        protected readonly IFieldValidator emailFieldValidator;
        protected readonly IFieldValidator phoneFieldValidator;

        /// <summary>
        /// Create an instance of AuthManager
        /// </summary>
        /// <param name="database"></param>
        /// <param name="authTokenDurationDays">How long new <see cref="AuthToken"/> instances are valid for</param>
        /// <param name="resetCodeLength">How many digits should a reset code be</param>
        /// <param name="resetTokenDurationMinutes">How long new reset codes are valid for</param>
        /// <param name="accountTableName">Table name of the account table (default is "account")</param>
        /// <param name="userTableName">Table name of the user table (default is "user")</param>
        /// <param name="userTableIdFieldName">Field name of the id field on the user table (default is "id")</param>
        /// <param name="userTableUsernameFieldName">Field name of the username field on the user table (default is "username")</param>
        /// <param name="userTableEmailFieldName">Field name of the email field on the user table (default is "email")</param>
        /// <param name="userTableEmailVerifiedAtFieldName">Field name of the email verified at field on the user table (default is "email_verified_at")</param>
        /// <param name="userTablePhoneFieldName">Field name of the phone field on the user table (default is "phone")</param>
        /// <param name="userTablePhoneVerifiedAtFieldName">Field name of the phone verified at field on the user table (default is "phone_verified_at")</param>
        /// <param name="userTableSaltFieldName">Field name of the salt field on the user table (default is "salt")</param>
        /// <param name="userTablePasswordHashFieldName">Field name of the password hash field on the user table (default is "password_hash")</param>
        /// <param name="userTableFirstNameFieldName">Field name of the first name field on the user table (default is "first_name")</param>
        /// <param name="userTableLastNameFieldName">Field name of the last name field on the user table (default is "last_name")</param>
        /// <param name="userTableResetCodeFieldName">Field name of the reset code field on the user table (default is "reset_code")</param>
        /// <param name="userTableResetCodeExpiresAtFieldName">Field name of the reset code expires at field on the user table (default is "reset_code_expires_at")</param>
        /// <param name="userTableAccountIdFieldName">Field name of the account id field on the user table (default is "account_id")</param>
        /// <param name="userTableRoleFieldName">Field name of the role field on the user table (default is "role")</param>
        /// <param name="authTokenTableName">Table name of the auth token table (default is "auth_token")</param>
        /// <param name="authTokenIdFieldName">Field name of the id field on the auth token table (default is "id")</param>
        /// <param name="authTokenTableUserIdFieldName">Field name of the user id field on the auth token table (default is "user_id")</param>
        /// <param name="authTokenTableExpiresAtFieldName">Field name of the expires at field on the auth token table (default is "expires_at")</param>
        /// <param name="defaultRole">Default value for the role field on a new user</param>
        /// <param name="onEmailVerify">Callback when <see cref="AuthManager.VerifyAsync(Dict, string, string, Func{string, int, Task})"/> is called with an email address</param>
        /// <param name="onPhoneVerify">Callback when <see cref="AuthManager.VerifyAsync(Dict, string, string, Func{string, int, Task})"/> is called with a phone number</param>
        /// <param name="onRegister">Callback when <see cref="AuthManager.RegisterAsync(dynamic, Dict)"/> is called</param>
        /// <param name="onForgotPassword">Callback when <see cref="AuthManager.ForgotPasswordAsync(string)"/> is called</param>
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
            string userTableEmailVerifiedAtFieldName = "email_verified_at",
            string userTablePhoneFieldName = "phone",
            string userTablePhoneVerifiedAtFieldName = "phone_verified_at",
            string userTableSaltFieldName = "salt",
            string userTablePasswordHashFieldName = "password_hash",
            string userTableFirstNameFieldName = "first_name",
            string userTableLastNameFieldName = "last_name",
            string userTableResetCodeFieldName = "reset_code",
            string userTableResetCodeExpiresAtFieldName = "reset_code_expires_at",
            string userTableAccountIdFieldName = "account_id",
            string userTableRoleFieldName = "role",
            string authTokenTableName = "auth_token",
            string authTokenIdFieldName = "id",
            string authTokenTableUserIdFieldName = "user_id",
            string authTokenTableExpiresAtFieldName = "expires_at",
            string defaultRole = "full-access",
            Func<string, int, Task> onEmailVerify = null,
            Func<string, int, Task> onPhoneVerify = null,
            Action<Dict> onRegister = null,
            Action<Dict> onForgotPassword = null
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
            this.userTableEmailVerifiedAtFieldName = userTableEmailVerifiedAtFieldName;
            this.userTablePhoneFieldName = userTablePhoneFieldName;
            this.userTablePhoneVerifiedAtFieldName = userTablePhoneVerifiedAtFieldName;
            this.userTableSaltFieldName = userTableSaltFieldName;
            this.userTablePasswordHashFieldName = userTablePasswordHashFieldName;
            this.userTableFirstNameFieldName = userTableFirstNameFieldName;
            this.userTableLastNameFieldName = userTableLastNameFieldName;
            this.userTableResetCodeFieldName = userTableResetCodeFieldName;
            this.userTableResetCodeExpiresAtFieldName = userTableResetCodeExpiresAtFieldName;
            this.userTableAccountIdFieldName = userTableAccountIdFieldName;
            this.userTableRoleFieldName = userTableRoleFieldName;

            this.authTokenTableName = authTokenTableName;
            this.authTokenIdFieldName = authTokenIdFieldName;
            this.authTokenTableUserIdFieldName = authTokenTableUserIdFieldName;
            this.authTokenTableExpiresAtFieldName = authTokenTableExpiresAtFieldName;

            this.defaultRole = defaultRole;

            this.onEmailVerify = onEmailVerify;
            this.onPhoneVerify = onPhoneVerify;
            this.onRegister = onRegister;
            this.onForgotPassword = onForgotPassword;

            this.usernameFieldValidator = new GenericFieldValidator(this.userTableUsernameFieldName, @"^[_A-z0-9\-\.]{3,25}$", allowNull: false, forceLowerCase: true, includeValueInError: true);
            this.passwordFieldValidator = new GenericFieldValidator("password", "^.{6,255}$", allowNull: false, forceLowerCase: false, includeValueInError: false);
            this.nameFieldValidator = new NameFieldValidator(this.userTableLastNameFieldName, allowNull: false, maxLength: 25);
            this.emailFieldValidator = new EmailFieldValidator(this.userTableEmailFieldName, allowNull: true);
            this.phoneFieldValidator = new PhoneFieldValidator(this.userTableEmailFieldName, allowNull: false);
        }

        /// <summary>
        /// Call to setup a Web API with the specified <paramref name="webApiServer"/> with the following URLs...
        ///     GET /api/auth/check-username/{username}
        ///     GET /api/auth/check-auth-token/{id}
        ///     POST /api/auth/create-anonymous
        ///     POST /api/auth/register
        ///     POST /api/auth/login
        ///     POST /api/auth/forgot-password
        ///     POST /api/auth/reset-password
        ///     POST /api/auth/verify-email
        ///     POST /api/auth/verify-phone
        /// </summary>
        /// <param name="webApiServer"></param>
        /// <param name="pathPrefix">Defaults to /api/auth</param>
        public void SetupWebApi(IWebApiServer webApiServer, string pathPrefix = "/api/auth") {
            webApiServer.OnGet($"{pathPrefix}/check-username/{{username}}", async(req, res) => {
                string username = req.PathParams.GetAs("username", (string)null);
                logger.Debug($"/check-username/{username}");
                Dict user = await this.LookupUsernameAsync(username, this.userTableIdFieldName);
                bool available = user == null;
                await res.WriteAsJsonAsync(available);
            });

            webApiServer.OnGet($"{pathPrefix}/check-auth-token/{{id}}", async (req, res) => {
                string id = req.PathParams.GetAs("id", (string)null);
                logger.Debug($"/check-auth-token/{id}"); //?join_code={joinCode}");
                AuthToken authToken = await this.AuthenticateAsync(id);
                await res.WriteAsJsonAsync(authToken);
            });

            webApiServer.OnPost($"{pathPrefix}/create-anonymous", async(req, res) => {
                Dict data = await req.ParseAsJsonAsync<Dict>();
                AuthToken authToken = await this.CreateAnonymousUserAsync();
                await res.WriteAsJsonAsync(authToken);
            });

            webApiServer.OnPost($"{pathPrefix}/register", async(req, res) => {
                Dict registration = await req.ParseAsJsonAsync<Dict>();
                AuthToken authToken = await this.RegisterAsync(registration, new Dict {
                    { "host_name", Dns.GetHostName() },
                    { "user_agent", req.UserAgent },
                    { "user_host_address", req.UserHostAddress },
                    { "user_host_name", req.UserHostName },
                });
                await res.WriteAsJsonAsync(authToken);
            });

            webApiServer.OnPost($"{pathPrefix}/login", async(req, res) => {
                Dict login = await req.ParseAsJsonAsync<Dict>();
                AuthToken authToken = await this.LoginAsync(login);
                await res.WriteAsJsonAsync(authToken);
            });

            webApiServer.OnPost($"{pathPrefix}/forgot-password", async(req, res) => {
                Dict data = await req.ParseAsJsonAsync<Dict>();
                string username = data.GetAs("username", (string)null);
                await this.ForgotPasswordAsync(username);
            });

            webApiServer.OnPost($"{pathPrefix}/reset-password", async(req, res) => {
                Dict resetPassword = await req.ParseAsJsonAsync<Dict>();
                AuthToken authToken = await this.ResetPasswordAsync(resetPassword);
                await res.WriteAsJsonAsync(authToken);
            });

            webApiServer.OnPost($"{pathPrefix}/verify-email", async (req, res) => {
                Dict data = await req.ParseAsJsonAsync<Dict>();
                await this.VerifyAsync(data, "email", "email_verified_at", this.onEmailVerify);
            });

            webApiServer.OnPost($"{pathPrefix}/verify-phone", async (req, res) => {
                Dict data = await req.ParseAsJsonAsync<Dict>();
                await this.VerifyAsync(data, "phone", "phone_verified_at", this.onPhoneVerify);
            });
        }

        /// <summary>
        /// Call to verify a user's email or phone
        /// </summary>
        /// <param name="data"></param>
        /// <param name="fieldName"></param>
        /// <param name="verifiedAtFieldName"></param>
        /// <param name="onVerify"></param>
        /// <returns></returns>
        public async Task VerifyAsync(Dict data, string fieldName, string verifiedAtFieldName, Func<string, int, Task> onVerify) {
            string contact = data.GetAs("contact", (string)null);
            if (string.IsNullOrEmpty(contact)) throw new Exception($"Must specify contact to verify {fieldName}");

            int code = data.GetAs("verify_code", -1);
            if (code == -1) throw new Exception($"Must specify code to verify {fieldName}");

            string id = data.GetAs("id", (string)null);
            if (string.IsNullOrEmpty(id)) throw new Exception($"Must specify id to verify {fieldName}");

            string storedContact = await this.database.SelectValueAsync<string>($"SELECT {fieldName} FROM {this.userTableName}", new {
                id
            });
            if (storedContact != contact) throw new Exception("Verified contact does not match stored contact");

            await this.onEmailVerify(contact, code);

            var values = new Dict {
                { "id", id },
                { verifiedAtFieldName, DateTime.Now }
            };
            await this.database.UpdateAndCommitAsync("user", values);
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="input"></param>
        /// <param name="notifyData"></param>
        /// <returns></returns>
        public async Task<AuthToken> RegisterAsync(dynamic input, Dict notifyData = null) {
            Dict registration = this.ConvertInputToDict(input);

            // Handle registering an anonymous user
            string userId = registration.GetAs(this.authTokenTableUserIdFieldName, (string)null);
            string accountId = null;
            logger.Trace($"RegisterAsync():userId={userId}");
            if (!string.IsNullOrEmpty(userId)) {
                accountId = await this.database.SelectValueAsync<string>($"SELECT account_id FROM {this.userTableName}", userId);
            }
            if (string.IsNullOrEmpty(accountId)) {
                userId = null;
            }

            // Check if username is available
            string username = this.usernameFieldValidator.Validate(registration?.GetAs(this.userTableUsernameFieldName, (string)null));
            logger.Trace($"RegisterAsync():username={username}");
            Dict existingUserByUsername = await this.database.SelectRowAsync(this.userTableName, new Dict {
                { this.userTableUsernameFieldName, username }
            });
            if (existingUserByUsername != null) throw new Exception("Username '" + username + "' is unavailable");

            string password = this.passwordFieldValidator.Validate(registration?.GetAs("password", (string)null));

            string email = this.emailFieldValidator.Validate(registration?.GetAs(this.userTableEmailFieldName, (string)null));
            string phone = this.phoneFieldValidator.Validate(registration?.GetAs(this.userTablePhoneFieldName, (string)null));

            string firstName = this.nameFieldValidator.Validate(registration?.GetAs(this.userTableFirstNameFieldName, (string)null));
            string lastName = this.nameFieldValidator.Validate(registration?.GetAs(this.userTableLastNameFieldName, (string)null));

            string salt = Guid.NewGuid().ToString();
            string passwordHash = $"{salt} {password}".Hash();

            if (string.IsNullOrEmpty(accountId)) {
                accountId = await this.database.InsertAndCommitAsync<string>(this.accountTableName);
            }

            Dict user = new Dict {
                { this.userTableAccountIdFieldName, accountId },
                { this.userTableUsernameFieldName, username },
                { this.userTableSaltFieldName, salt },
                { this.userTablePasswordHashFieldName, passwordHash },
                { this.userTableEmailFieldName, email },
                { this.userTablePhoneFieldName, phone },
                { this.userTableFirstNameFieldName, firstName  },
                { this.userTableLastNameFieldName, lastName },
                { this.userTableRoleFieldName, this.defaultRole },
            };
            if (string.IsNullOrEmpty(userId)) {
                userId = await this.database.InsertAndCommitAsync<string>(this.userTableName, user);
            }
            else {
                user[this.userTableIdFieldName] = userId;
                await this.database.UpdateAndCommitAsync(this.userTableName, user);
            }

            if (this.onRegister != null) {
                var registerData = new Dict {
                    { this.userTableUsernameFieldName, username },
                    { this.userTableEmailFieldName, email },
                    { this.userTablePhoneFieldName, phone },
                    { this.userTableFirstNameFieldName, firstName  },
                    { this.userTableLastNameFieldName, lastName },
                };
                if (notifyData != null) registerData.UpdateFrom(notifyData);
                this.onRegister(registerData);
            }

            return await this.CreateAuthTokenAsync(userId);
        }

        /// <summary>
        /// Validates the auth token id returning an <see cref="AuthToken"/> instance
        /// </summary>
        /// <param name="authTokenId"></param>
        /// <returns>An <see cref="AuthToken"/> instance</returns>
        public async Task<AuthToken> AuthenticateAsync(string authTokenId) {
            Dict authTokenDict = await this.database.SelectRowAsync($"SELECT at.{this.authTokenIdFieldName}, at.{this.authTokenTableUserIdFieldName}, u.{this.userTableAccountIdFieldName}, u.{this.userTableUsernameFieldName}, u.{this.userTableRoleFieldName}, at.{this.authTokenTableExpiresAtFieldName} FROM {this.authTokenTableName} at INNER JOIN {this.userTableName} u ON at.user_id=u.id WHERE at.id=@authTokenId", new {
                authTokenId
            });
            logger.Debug($"Authenticate():authTokenDict={authTokenDict}");
            if (authTokenDict == null) throw new UnauthorizedException();

            var authToken = AuthToken.FromDict(authTokenDict, this.authTokenIdFieldName, this.authTokenTableUserIdFieldName, this.userTableUsernameFieldName, this.userTableRoleFieldName, this.userTableAccountIdFieldName, this.authTokenTableExpiresAtFieldName);
            logger.Debug($"Authenticate():authToken.expiresAt={authToken.expiresAt}");
            if (authToken.expiresAt == DateTime.MinValue || authToken.expiresAt < DateTime.Now) throw new UnauthorizedException();

            return authToken;
        }

        /// <summary>
        /// Lookup user record from username
        /// </summary>
        /// <param name="username"></param>
        /// <param name="fieldNames"></param>
        /// <returns></returns>
        public async Task<Dict> LookupUsernameAsync(string username, string fieldNames = "*") {
            username = this.usernameFieldValidator.Validate(username);
            string sql = $"SELECT {fieldNames} FROM {this.userTableName} WHERE {this.userTableUsernameFieldName}=@username";
            logger.Debug($"LookupUsername():sql={sql}");
            return await this.database.SelectRowAsync(sql, new {
                username
            });
        }

        /// <summary>
        /// Creates an anonymous user and returns a valid <see cref="AuthToken"/>
        /// </summary>
        /// <returns>An  <see cref="AuthToken"/> instance created</returns>
        public async Task<AuthToken> CreateAnonymousUserAsync() {
            Random random = new Random();
            using (ITransaction transaction = await this.database.BeginTransactionAsync()) {
                string accountId = await transaction.InsertAsync<string>(this.accountTableName);
                string role = this.defaultRole;

                var firstName = CleverNameX.COLORS[random.Next(0, CleverNameX.COLORS.Length)];
                var lastName = CleverNameX.ANIMALS[random.Next(0, CleverNameX.ANIMALS.Length)];
                string userId = await transaction.InsertAsync<string>(this.userTableName, new Dict {
                    { this.userTableFirstNameFieldName, firstName },
                    { this.userTableLastNameFieldName, lastName },
                    { this.userTableAccountIdFieldName, accountId },
                    { this.userTableRoleFieldName, role }
                });

                DateTime expiresAt = DateTime.Now.AddDays(this.authTokenDurationDays);
                string id = await transaction.InsertAsync<string>(this.authTokenTableName, new Dict {
                    { this.authTokenTableUserIdFieldName, userId },
                    { this.authTokenTableExpiresAtFieldName, expiresAt },
                });

                await transaction.CommitAsync();

                return new AuthToken(id, userId, null, role, accountId, expiresAt);
            }
        }

        /// <summary>
        /// Create an auth token
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>The AuthToken instance created</returns>
        public async Task<AuthToken> CreateAuthTokenAsync(string userId) {
            Dict user = await this.database.SelectRowAsync($"SELECT {this.userTableUsernameFieldName}, {this.userTableRoleFieldName}, {this.userTableAccountIdFieldName} FROM user WHERE id=@userId", new {
                userId
            });
            if (user==null) throw new Exception("Invalid user");

            DateTime expiresAt = DateTime.Now.AddDays(this.authTokenDurationDays);
            string id = await this.database.InsertAndCommitAsync<string>(this.authTokenTableName, new Dict {
                { this.authTokenTableUserIdFieldName, userId },
                { this.authTokenTableExpiresAtFieldName, expiresAt },
            });

            return new AuthToken(id, userId, user.GetAs(this.userTableUsernameFieldName, (string)null), user.GetAs(this.userTableRoleFieldName, (string)null), user.GetAs(this.userTableAccountIdFieldName, (string)null), expiresAt);
        }

        /// <summary>
        /// Logs in the user creating a valid <see cref="AuthToken"/>
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public async Task<AuthToken> LoginAsync(Dict login) {
            string username = login?.GetAs(this.userTableUsernameFieldName, (string)null);
            Dict user = await this.LookupUsernameAsync(username, string.Join(",", new string[] { this.userTableIdFieldName, this.userTableSaltFieldName, this.userTablePasswordHashFieldName }));
            if (user == null) throw new Exception("Invalid username '" + username + "'");

            string passwordHash = user.GetAs(this.userTablePasswordHashFieldName, (string)null);

            string salt = user.GetAs(this.userTableSaltFieldName, (string)null);
            string password = this.passwordFieldValidator.Validate(login?.GetAs("password", (string)null));
            string newPasswordHash = $"{salt} {password}".Hash();

            if (newPasswordHash != passwordHash) throw new Exception("Incorrect password");

            string userId = user.GetAs(this.userTableIdFieldName, (string)null);
            return await this.CreateAuthTokenAsync(userId);
        }

        /// <summary>
        /// Creates a reset code and invokes <see cref="AuthManager.onForgotPassword"/> to send the reset code to the user
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public async Task ForgotPasswordAsync(string username) {
            Dict user = await this.LookupUsernameAsync(username, string.Join(",", new string[] { this.userTableIdFieldName, this.userTableFirstNameFieldName, this.userTableLastNameFieldName, this.userTableEmailFieldName }));
            if (user == null) throw new Exception("Invalid username '" + username + "'");

            string userId = user.GetAs(this.userTableIdFieldName, (string)null);
            string resetCode = await this.CreateResetCodeAsync(userId);
            user[this.userTableResetCodeFieldName] = resetCode;

            if (this.onForgotPassword != null) this.onForgotPassword(user);
        }

        /// <summary>
        /// Resets the user's password if a valid reset code is included
        /// </summary>
        /// <param name="resetPassword"></param>
        /// <returns></returns>
        public async Task<AuthToken> ResetPasswordAsync(Dict resetPassword) {
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

            return await this.CreateAuthTokenAsync(userId);
        }

        protected async Task<string> CreateResetCodeAsync(string userId) {
            Random random = new Random();
            int resetCodeMax = (int)Math.Pow(10, this.resetCodeLength);
            long randomResetCode = random.Next(resetCodeMax);
            var resetCode = randomResetCode.ToString(String.Concat(Enumerable.Repeat("0", this.resetCodeLength)));

            DateTime expiresAt = DateTime.Now.AddMinutes(this.resetTokenDurationMinutes);
            await this.database.UpdateAndCommitAsync(this.userTableName, new Dict {
                { this.userTableIdFieldName, userId },
                { this.userTableResetCodeFieldName, resetCode },
                { this.userTableResetCodeExpiresAtFieldName, expiresAt },
            });

            return resetCode;
        }

        protected Dict ConvertInputToDict(dynamic input) {
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

    /// <summary>
    /// Represents the result of a successful <see cref="AuthManager.LoginAsync(Dict)"/> or <see cref="AuthManager.RegisterAsync(dynamic, Dict)"/>
    /// </summary>
    public class AuthToken {
        public readonly string id;
        public readonly string userId;
        public readonly string username;
        public readonly string role;
        public readonly string accountId;
        public readonly DateTime expiresAt;

        public AuthToken(string id, string userId, string username, string role, string accountId, DateTime expiresAt) {
            this.id = id;
            this.userId = userId;
            this.username = username;
            this.role = role;
            this.accountId = accountId;
            this.expiresAt = expiresAt;
        }

        public static AuthToken FromDict(Dict dict, string idFieldName, string userIdFieldName, string usernameFieldName, string roleFieldName, string accountIdFieldName, string expiresAtFieldName) {
            string id = dict.GetAs(idFieldName, (string)null);
            string userId = dict.GetAs(userIdFieldName, (string)null);
            string username = dict.GetAs(usernameFieldName, (string)null);
            string role = dict.GetAs(roleFieldName, (string)null);
            string accountId = dict.GetAs(accountIdFieldName, (string)null);
            DateTime expiresAt = dict.GetAs(expiresAtFieldName, DateTime.MinValue);
            return new AuthToken(id, userId, username, role, accountId, expiresAt);
        }
    }

}
