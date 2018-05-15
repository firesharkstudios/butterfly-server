using NLog;
using System;
using System.Collections.Generic;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Channel {
    public class Channel : IDisposable {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly IChannelServerConnection connection;
        protected readonly string channelKey;
        protected readonly Dict vars;

        public Channel(IChannelServerConnection connection, string channelKey, Dict vars) {
            this.connection = connection;
            this.channelKey = channelKey;
            this.vars = vars;
        }

        public IChannelServerConnection Connection => this.connection;

        public string ChannelKey => this.channelKey;

        public Dict Vars => this.vars;

        protected readonly List<IDisposable> disposables = new List<IDisposable>();

        public void Attach(IDisposable disposable) {
            this.disposables.Add(disposable);
        }

        public void Queue(string messageType, object data) {
            this.connection.QueueMessage(channelKey: this.channelKey, messageType: messageType, data: data);
        }

        public void Dispose() {
            logger.Debug($"Dispose():ChannelKey={this.ChannelKey}");
            foreach (var disposable in this.disposables) {
                try {
                    disposable.Dispose();
                }
                catch (Exception e) {
                    logger.Error(e);
                }
            }
            this.disposables.Clear();
        }

    }
}
