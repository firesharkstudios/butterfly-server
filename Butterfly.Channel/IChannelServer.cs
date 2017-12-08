using System;
using System.Threading.Tasks;

namespace Butterfly.Channel {
    /// <summary>
    /// Allows clients to create new channels to the server and allows the server to push messages to connected clients.<para/>
    /// </summary>
    /// <remarks>
    /// Initializing a channel server instance...<para/>
    /// <code>
    ///     var channelServer = new SomeChannelServer();
    ///     channelServer.OnNewChannel("/chat", channel => {
    ///         // Do stuff here to initialize the channel (send initial data, listen for specific data change events, etc)
    ///         // and return any object that should be disposed when the channel is disposed
    ///     });
    ///     channelServer.Start();
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

        //Func<string, string> Authenticate { get; }

        /// <summary>
        /// Add a listener when a new channel is created
        /// </summary>
        /// <param name="pathFilter">Filters new channel requests by path (pass '/chat' to only match channels created with path '/chat')</param>
        /// <param name="listener">Lambda to run to initialize the channel</param>
        /// <returns>Any object that should be disposed when the channel is disposed</returns>
        IDisposable OnNewChannel(string pathFilter, Func<IChannel, IDisposable> listener);

        /// <summary>
        /// Add an async listener when a new channel is created (return an IDisposable to dispose any objects when the channel is disposed)
        /// </summary>
        /// <param name="pathFilter">Filters new channel requests by path (pass '/chat' to only match channels created with path '/chat')</param>
        /// <param name="listener">Lambda to run to initialize the channel</param>
        /// <returns>Any object that should be disposed when the channel is disposed</returns>
        IDisposable OnNewChannelAsync(string pathFilter, Func<IChannel, Task<IDisposable>> listener);

        /// <summary>
        /// Number of channels
        /// </summary>
        int ChannelCount { get; }

        /// <summary>
        /// Retrieve a channel by id
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="throwExceptionIfMissing"></param>
        /// <returns></returns>
        IChannel GetChannel(string channelId, bool throwExceptionIfMissing = false);

        /// <summary>
        /// Queues a value to be sent to the specified channel (normally converted to JSON and transmitted as text)
        /// </summary>
        void Queue(string channelId, object value);

        /// <summary>
        /// Starts the channel server.  Must be called after adding new channel listeners (via <see cref="OnNewChannel(string, Func{IChannel, IDisposable})"/> and <see cref="OnNewChannelAsync(string, Func{IChannel, Task{IDisposable}})"/>)
        /// </summary>
        void Start();
    }
}
