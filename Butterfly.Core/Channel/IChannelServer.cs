using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Channel {
    /// <summary>
    /// Allows clients to create new channels to the server and allows the server to push messages to connected clients.<para/>
    /// </summary>
    /// <remarks>
    /// Initialize a channel server instance with a single route and single default channel...<para/>
    /// <code>
    ///     var channelServer = new SomeChannelServer();
    ///     var route = channelServer.RegisterRoute("/chat");
    ///     route.RegisterChannel(handlerAsync: async(vars, channel) => {
    ///         // Do stuff here to initialize the channel (send initial data, listen for specific data change events, etc)
    ///         // and return any object that should be disposed when the channel is disposed
    ///     });
    ///     channelServer.Start();
    ///     
    /// Register a default channel that creates a DynamicView on the chat_message table sending all data to the channel
    /// </code>
    /// 
    /// If a client has now created a channel at /chat?id=123, the server can now push data to the client via...<para/>
    /// <code>
    ///     channelServer.Queue("123", "Hello");
    /// </code>
    /// 
    /// If you no longer need a channel server instance, call Dispose() on the channel server...<para/>
    /// <code>
    ///     channelServer.Dispose();
    /// </code>
    /// </remarks>
    public interface IChannelServer : IDisposable {
        /// <summary>
        /// Registeres a route
        /// </summary>
        /// <param name="routePath"></param>
        /// <param name="getAuthToken"></param>
        /// <param name="getAuthTokenAsync"></param>
        /// <param name="getConnectionId"></param>
        /// <param name="getConnectionIdAsync"></param>
        /// <returns></returns>
        RegisteredRoute RegisterRoute(string routePath, Func<string, string, object> getAuthToken = null, Func<string, string, Task<object>> getAuthTokenAsync = null, Func<object, string> getConnectionId = null, Func<object, Task<string>> getConnectionIdAsync = null);

        /// <summary>
        /// Number of connections
        /// </summary>
        ICollection<IChannelServerConnection> UnauthenticatedConnections { get; }

        ICollection<IChannelServerConnection> AuthenticatedConnections { get; }

        /// <summary>
        /// Retrieve a channel by id
        /// </summary>
        /// <param name="authId"></param>
        /// <param name="throwExceptionIfMissing"></param>
        /// <returns></returns>
        IChannelServerConnection GetConnection(string authId, bool throwExceptionIfMissing = false);

        /// <summary>
        /// Starts the channel server
        /// </summary>
        void Start();
    }
}
