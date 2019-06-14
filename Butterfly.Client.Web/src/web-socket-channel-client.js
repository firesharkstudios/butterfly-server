/*
 * States...
 *  Disconnected - No WebSocket, no loop running
 *  Connecting - Create WebSocket and wait for WebSocket.onopen()
 *  Authenticating - Send Authentication and wait for server to send AUTHENTICATED or UNAUTHENTICATED
 *  Subscribing - Send subscriptions and transition to Connected
 *  Connected - Send heartbeats to server
 */

export default class {
  constructor(options) {
    this._options = options;

    let url = this._options.url;

    if (url.indexOf('://') === -1) {
      this._url = (window.location.protocol === 'https:' ? 'wss:' : 'ws:') + '//' + window.location.host + url;
    }
    else {
      this._url = url;
    }

    this._state = 'Disconnected';
    this._stateTimeout = null;
    this._auth = null;
    this._subscriptionByChannelKey = {};
  }

  _setState(value) {
    if (this._state !== value) {
      console.debug(`_setState():value=${value}`);
      this._state = value;
      if (this._options.onStateChange) this._options.onStateChange(value);
      this._clearStateTimeout();
    }
  }

  connect(auth) {
    console.debug('WebSocketChannelClient.connect()');
    this._auth = auth;

    this._setState('Connecting');
    this._connecting();
  }

  _connecting() {
    if (this._state === 'Disconnected') return;

    this._setState('Connecting');
    let connectingStartMillis = new Date().getTime();

    if (this._webSocket) {
      try {
        this._webSocket.close();
      }
      catch (e) { }
      this._webSocket = null;
    }

    let hasReconnected = false;
    let reconnect = error => {
      if (hasReconnected) return;
      hasReconnected = true;

      console.debug(`_connecting():reconnect():error=${error}`);
      let elapsedMillis = new Date().getTime() - connectingStartMillis;
      let reconnectEveryMillis = this._options.reconnectEveryMillis || 3000;

      if (elapsedMillis > reconnectEveryMillis) {
        this._connecting();
      }
      else {
        let wait = reconnectEveryMillis - elapsedMillis;

        this._stateTimeout = setTimeout(this._connecting.bind(this), wait);
      }
    };

    try {
      console.debug(`_connecting():new WebSocket(${this._url})`);
      this._webSocket = new WebSocket(this._url);
      this._webSocket.onmessage = this._onMessage.bind(this);
      this._webSocket.onopen = this._authenticating.bind(this);
      this._webSocket.onerror = reconnect.bind(this);
      this._webSocket.onclose = reconnect.bind(this);
    }
    catch (e) {
      reconnect(e);
    }
  }

  _authenticating() {
    if (this._state === 'Disconnected') return;

    this._setState('Authenticating');
    let text = 'Authorization:' + (this._auth || '');
    let success = this._sendText(text);

    if (success) {
      let authenticateEveryMillis = this._options.authenticateEveryMillis || 3000;

      this._stateTimeout = setTimeout(this._authenticating.bind(this), authenticateEveryMillis);
    }
  }

  _subscribing() {
    if (this._state === 'Disconnected') return;

    this._setState('Subscribing');

    // Build data
    let data = [];

    for (let key in this._subscriptionByChannelKey) {
      let subscription = this._subscriptionByChannelKey[key];

      if (!subscription.sent) {
        data.push({
          channelKey: key,
          vars: subscription.vars,
        });
      }
    }

    // Subscribe
    let success = true;

    if (data.length > 0) {
      success = this._sendText('Subscribe:' + JSON.stringify(data));
    }

    if (success) {
      this._markSubscriptionsSent(true);
      this._connected();
    }
  }

  _unsubscribing(channelKey) {
    if (this._state === 'Disconnected') return;

    this._setState('Unsubscribing');
    let success = this._sendText('Unsubscribe:' + JSON.stringify(channelKey));

    if (success) {
      this._connected();
    }
  }

  _connected() {
    this._setState('Connected');

    let elapsedMillis = new Date().getTime() - this._lastSendTextMillis;
    let heartbeatEveryMillis = this._options.heartbeatEveryMillis || 3000;

    if (elapsedMillis >= heartbeatEveryMillis) {
      this._sendText('!');
      this._connected();
    }
    else {
      let wait = Math.max(0, heartbeatEveryMillis - elapsedMillis);

      this._stateTimeout = setTimeout(this._connected.bind(this), wait);
    }
  }

  disconnect() {
    console.debug('WebSocketChannelClient.disconnect()')
    this._setState('Disconnected');
    if (this._webSocket != null) {
      try {
        this._webSocket.close();
      }
      catch (e) { }
      this._webSocket = null;
    }
    this._clearStateTimeout();
    for (let channelKey in this._subscriptionByChannelKey) {
      let subscription = this._subscriptionByChannelKey[channelKey];

      if (subscription.handlers) {
        for (let i = 0; i < subscription.handlers.length; i++) {
          subscription.handlers[i]('RESET');
        }
      }
    }
  }

  _clearStateTimeout() {
    if (this._stateTimeout) {
      clearTimeout(this._stateTimeout);
      this._stateTimeout = null;
    }
  }

  _sendText(text) {
    console.debug(`_sendText():text=${text}`);
    try {
      this._webSocket.send(text);
      this._lastSendTextMillis = new Date().getTime();
      return true;
    }
    catch (e) {
      console.error(e);
      this._connecting();
      return false;
    }
  }

  _onMessage(event) {
    let message = JSON.parse(event.data);

    console.debug(`_onMessage():message.messageType=${message.messageType}`);
    if (message.channelKey) {
      let subscription = this._subscriptionByChannelKey[message.channelKey];

      if (subscription.handlers) {
        for (let i = 0; i < subscription.handlers.length; i++) {
          subscription.handlers[i](message.messageType, message.data);
        }
      }
    }
    else if (message.messageType === 'AUTHENTICATED') {
      this._markSubscriptionsSent(false);
      this._subscribing();
    }
    else if (message.messageType === 'UNAUTHENTICATED') {
      if (this._options.onUnauthenticated) this._options.onUnauthenticated(message.data);
      this.disconnect();
    }
  }

  _markSubscriptionsSent(value) {
    for (let key in this._subscriptionByChannelKey) {
      this._subscriptionByChannelKey[key].sent = value;
    }
  }

  _isVarsSame(varsOld, varsNew) {
    if (!varsOld && !varsNew) return true;
    else if (!varsOld && varsNew) return false;
    else if (varsOld && !varsNew) return false;
    else if (Object.keys(varsOld).length !== Object.keys(varsNew).length) return false;
    else {
      for (let key in varsOld) {
        if (varsOld[key] !== varsNew[key]) return false;
      }
      return true;
    }
  }

  subscribe(options) {
    let channelKey = options.channel || 'default';
    let handlers = Array.isArray(options.handler) ? options.handler : [options.handler];
    let vars = options.vars;

    console.debug(`WebSocketChannelClient.subscribe():channelKey=${channelKey}`);

    let existingSubscription = this._subscriptionByChannelKey[channelKey];
    if (existingSubscription) {
      let isVarsSame = this._isVarsSame(existingSubscription.vars, vars);
      console.debug(`WebSocketChannelClient.subscribe():isVarsSame=${isVarsSame}`);
      if (isVarsSame) return;
    }

    this._removeSubscription(channelKey);
    this._addSubscription(channelKey, {
      vars,
      handlers,
      sent: false
    });
    if (this._state === 'Connected') {
      this._subscribing();
    }
    if (this._options.onSubscriptionsUpdated) this._options.onSubscriptionsUpdated();
  }

  unsubscribe(channelKey) {
    console.debug(`WebSocketChannelClient.unsubscribe():channelKey=${channelKey}`);
    if (!channelKey) channelKey = 'default';

    this._removeSubscription(channelKey);
    this._unsubscribing(channelKey);
    if (this._options.onSubscriptionsUpdated) this._options.onSubscriptionsUpdated();
  }

  _addSubscription(channelKey, subscription) {
    this._subscriptionByChannelKey[channelKey] = subscription;
  }

  _removeSubscription(channelKey) {
    delete this._subscriptionByChannelKey[channelKey];
  }

}
