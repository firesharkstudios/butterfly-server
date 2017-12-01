using System;
using System.Threading.Tasks;

namespace Butterfly.Channel {
    /// <summary>
    /// Allows clients to create new channels to the server and allows the server to push messages to a connected client.
    /// 
    /// <code>
    ///     var channelServer = new SomeChannelServer();
    ///     channelServer.OnNewChannel("/chat-message", channel => {
    ///         // Do stuff here to initialize the channel and return any object that should be disposed with the channel
    ///     });
    ///     channelServer.Start();
    /// </code>
    /// </summary>
    public interface IChannelServer : IDisposable {
        /// <summary>
        /// Add a listener when a new channel is created (return an IDisposable to dispose any objects when the channel is disposed)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="listener"></param>
        /// <returns></returns>
        IDisposable OnNewChannel(string path, Func<IChannel, IDisposable> listener);

        /// <summary>
        /// Add an async listener when a new channel is created (return an IDisposable to dispose any objects when the channel is disposed)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="listener"></param>
        /// <returns></returns>
        IDisposable OnNewChannelAsync(string path, Func<IChannel, Task<IDisposable>> listener);

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
