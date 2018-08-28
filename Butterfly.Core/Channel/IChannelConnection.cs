/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace Butterfly.Core.Channel {
    /// <summary>
    /// Internal interface representing a communications channel from the server to the client (might be implemented via WebSockets, HTTP long polling, etc)
    /// </summary>
    public interface IChannelConnection : IDisposable {
        object AuthToken { get; }
        string Id { get; }
        DateTime Created { get;  }
        DateTime LastHeartbeat { get; }
        void Start(object authToken, string id);
        void QueueMessage(string channelKey = null, string messageType = null, object data = null);
    }
}
