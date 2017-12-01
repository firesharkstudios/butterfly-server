using System;
using System.Threading.Tasks;

namespace Butterfly.WebApi {
    /// <summary>
    /// Allows receiving API requests via HTTP.<para/>
    /// 
    /// Initializing a web api server instance...<para/>
    /// <code>
    ///     var webApiServer = new SomeWebApiServer();
    ///     webApiServer.OnGet("/chat-messages", async(req, res) => {
    ///         // Handle the request and return any objects as needed (will be JSON encoded)
    ///     });
    ///     webApiServer.OnPost("/login", async(req, res) => {
    ///         // Handle the request and return any objects as needed (will be JSON encoded)
    ///     });
    ///     webApiServer.Start();
    /// </code>
    /// </summary>
    public interface IWebApiServer : IDisposable {
        /// <summary>
        /// Add a listener responding to GET requests
        /// </summary>
        /// <param name="path"></param>
        /// <param name="listener"></param>
        void OnGet(string path, Func<IWebRequest, IWebResponse, Task> listener);

        /// <summary>
        /// Add a listener responding to POST requests
        /// </summary>
        /// <param name="path"></param>
        /// <param name="listener"></param>
        void OnPost(string path, Func<IWebRequest, IWebResponse, Task> listener);

        /// <summary>
        /// Start the web api server
        /// </summary>
        /// <param name="path"></param>
        /// <param name="listener"></param>
        void Start();
    }
}
