export default class {
  constructor(options) {
    this._options = options;

    let url = this._options.url;
    if (url.indexOf('://') == -1) {
      this._url = (window.location.protocol == 'https:' ? 'wss:' : 'ws:') + '//' + window.location.host + url;
    }
    else {
      this._url = url;
    }
    //console.debug('WebSocketChannelClient():url=' + url);

    this._status = null;
    this._auth = null;
    this._subscriptionByChannelKey = {};
    this._queuedMessages = [];
    this._webSocketOpened = false;
  }

  _setStatus(value) {
    if (this._status != value) {
      this._status = value;
      if (this._options.onStatusChange) this._options.onStatusChange(value);
    }
  }

  _queue(text) {
    console.debug(`_queue():text=${text}`);
    this._queuedMessages.push(text);
    this._sendQueue();
  }

  _sendQueue() {
    console.debug(`_sendQueue()`);
    if (this._sendQueueTimeout) clearTimeout(this._sendQueueTimeout);
    if (this._status == 'Stopped' || !this._webSocketOpened) return;

    let fail = false;
    do {
      if (this._webSocket == null) {
        fail = true;
        break;
      }

      let hasMessage = this._queuedMessages && this._queuedMessages.length > 0;
      let text = hasMessage ? this._queuedMessages[0] : '!';
      try {
        //console.debug(`_sendQueue():text=${text}`);
        this._webSocket.send(text);
        if (hasMessage) this._queuedMessages.shift();
      }
      catch (e) {
        fail = true;
        break;
      }
    } while (this._queuedMessages.length > 0);

    if (fail) {
      this._setupConnection();
    }
    else {
      this._sendQueueTimeout = setTimeout(this._sendQueue.bind(this), this._options.sendQueueEveryMillis || 3000);
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
    else if (message.messageType == 'AUTHENTICATED') {
      this._setStatus('Started');
    }
    else if (message.messageType == 'UNAUTHENTICATED') {
      this.stop();
    }
  }

  _scheduleSetupConnection() {
    this._setupConnectionTimeout = setTimeout(this._setupConnection.bind(this), this._options.setupConnectionEveryMillis || 3000);
  }

  // Called every 3 seconds until successful
  _setupConnection() {
    this._setStatus('Starting');
    this._webSocketOpened = false;
    if (this._webSocket) {
      this._webSocket.close();
      this._webSocket = null;
    }
    if (this._sendQueueTimeout) clearTimeout(this._sendQueueTimeout);

    try {
      console.debug(`_setupConnection():new WebSocket(${this._url})`);
      this._webSocket = new WebSocket(this._url);
      this._webSocket.onmessage = this._onMessage.bind(this);
      this._webSocket.onopen = () => {
        console.debug('_setupConnection():_webSocket.onopen()');
        this._webSocketOpened = true;
        this._queuedMessages = [];
        this._queue('Authorization:' + (this._auth || ''));
        this._markSubscriptionsSent(false);
        this._queueSubscribe();
      };
      this._webSocket.onerror = error => {
        console.debug('_setupConnection():_webSocket.onerror()');
        this._scheduleSetupConnection();
      };
      this._webSocket.onclose = () => {
        console.debug('_setupConnection():_webSocket():onclose()');
        this._webSocket = null;
      }
    }
    catch (e) {
      this._scheduleSetupConnection();
    }
  }

  _queueSubscribe() {
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
    if (data.length > 0) {
      this._queue('Subscribe:' + JSON.stringify(data));
      this._markSubscriptionsSent(true);
    }
  }

  _addSubscription(channelKey, subscription) {
    this._subscriptionByChannelKey[channelKey] = subscription;
    this._queueSubscribe();
  }

  _queueUnsubscribe(channelKey) {
    this._queue('Unsubscribe:' + JSON.stringify(channelKey));
  }

  _markSubscriptionsSent(value) {
    for (let key in this._subscriptionByChannelKey) {
      let subscription = this._subscriptionByChannelKey[key];
      subscription.sent = value;
    }
  }

  _removeSubscription(channelKey) {
    delete this._subscriptionByChannelKey[channelKey];
  }

  start(auth) {
    //console.debug('WebSocketChannelClient.start()');
    this._auth = auth;
    this._setupConnection();
  }

  subscribe(handler, channelKey, vars) {
    //console.debug(`WebSocketChannelClient.subscribe():channelKey=${channelKey}`);
    if (!channelKey) channelKey = 'default';
    this._removeSubscription(channelKey);
    this._addSubscription(channelKey, {
      vars: vars,
      handlers: Array.isArray(handler) ? handler : [handler],
      sent: false,
    });
    if (this._options.onSubscriptionsUpdated) this._options.onSubscriptionsUpdated();
  }

  unsubscribe(channelKey) {
    //console.debug(`WebSocketChannelClient.unsubscribe():channelKey=${channelKey}`);
    if (!channelKey) channelKey = 'default';
    this._removeSubscription(channelKey);
    this._queueUnsubscribe(channelKey);
    if (this._options.onSubscriptionsUpdated) this._options.onSubscriptionsUpdated();
  }

  stop() {
    //console.debug('WebSocketChannelClient.stop()')
    this._setStatus('Stopped');
    if (this._webSocket != null) {
      this._webSocket.close();
      this._webSocket = null;
    }
    if (this._sendQueueTimeout) clearTimeout(this._sendQueueTimeout);
    if (this._setupConnectionTimeout) clearTimeout(this._setupConnectionTimeout);
    for (let channelKey in this._subscriptionByChannelKey) {
      let subscription = this._subscriptionByChannelKey[channelKey];
      if (subscription.handlers) {
        for (let i = 0; i < subscription.handlers.length; i++) {
          subscription.handlers[i]('RESET');
        }
      }
    }
  }
}
