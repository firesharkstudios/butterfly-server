if (!butterfly) var butterfly = {};
if (!butterfly.channel) butterfly.channel = {};

butterfly.channel.WebSocketChannelClient = function(options) {

    let private = this;

    let heartbeatEveryMillis = options.heartbeatEveryMillis || 3000;
    let sendSubscriptionsCheckEveryMillis = options.sendSubscriptionsCheckEveryMillis || 100;
    let url = options.url;
    let auth = options.auth;
    let onSubscriptionsUpdated = options.onSubscriptionsUpdated;
    let onStatusChange = options.onStatusChange;

    if (url.indexOf('://') == -1) {
        url = (window.location.protocol=='https:' ? 'wss:' : 'ws:') + '//' + window.location.host + url;
    }
    console.log('WebSocketChannelClient():url=' + url);

    private.subscriptions = [];
    private.handlersByKey = {};
    private.sendSubscriptions = true;

    private.setStatus = function (value) {
        if (public.status != value) {
            public.status = value;
            if (options.onStatusChange) onStatusChange(value);
        }
    }

    private.testConnection = function (firstAttempt) {
        //console.log('testConnection():firstAttempt=' + firstAttempt);
        if (!private.webSocket) {
            try {
                private.setStatus(firstAttempt ? 'Connecting...' : 'Reconnecting...');
                private.webSocket = new WebSocket(url);
                private.webSocket.onmessage = function (event) {
                    let pos = event.data.indexOf(':');
                    let channelKey = event.data.substring(0, pos);
                    let handlers = private.handlersByKey[channelKey];
                    if (handlers) {
                        let json = event.data.substring(pos + 1);
                        let dataEventTransaction = JSON.parse(json);
                        for (let i = 0; i < handlers.length; i++) {
                            handlers[i](dataEventTransaction);
                        }
                    }
                };
                private.webSocket.onopen = function () {
                    private.setStatus('Connected');
                    private.webSocket.send('Authorization:' + auth);
                };
                private.webSocket.onerror = function (error) {
                    private.webSocket = null;
                }
            }
            catch (e) {
                console.log(e);
                private.webSocket = null;
            }
        }
        else if (private.webSocket.readyState == 1) { // Open
            //console.log('testConnection():private.webSocket.readyState=' + private.webSocket.readyState);
            try {
                private.webSocket.send('!');
                console.log('testConnection():heartbeat success');
                private.setStatus('Connected');
            }
            catch (e) {
                private.webSocket = null;
            }
        }
        else {
            private.webSocket = null;
        }

        private.heartbeatTimeout = setTimeout(function () {
            private.testConnection(false);
        }, heartbeatEveryMillis);

        private.sendSubscriptionstimeout = setTimeout(function () {
            if (private.sendSubscriptions && private.webSocket.readyState==1) {
                let text = 'Subscriptions:' + JSON.stringify(private.subscriptions);
                private.webSocket.send(text);
                if (onSubscriptionsUpdated) onSubscriptionsUpdated();
                private.sendSubscriptions = false;
            }
        }, sendSubscriptionsCheckEveryMillis);
    }

    let public = {
        status: 'Connecting...',
        start: function () {
            private.testConnection(true);
        },
        subscribe: function (handler, channelKey, vars) {
            if (!channelKey) channelKey = 'default';
            public.unsubscribe(channelKey);
            private.subscriptions.push({
                channelKey: channelKey,
                vars: vars
            });
            private.handlersByKey[channelKey] = Array.isArray(handler) ? handler : [handler];
            private.sendSubscriptions = true;
        },
        unsubscribe: function (channelKey) {
            let index = private.subscriptions.indexOf(x => x.channelKey == channelKey);
            if (index >= 0) private.subscriptions.removeAt(index);
            delete private.handlersByKey[channelKey];
        },
        stop: function () {
            clearTimeout(private.heartbeatTimeout);
            private.setStatus('Disconnected');
        }
    };

    return public;
}