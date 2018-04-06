function WebSocketChannelClient(options) {
    let private = this;

    let heartbeatEveryMillis = options.heartbeatEveryMillis || 3000;

    let url = options.url;
    if (url.indexOf('://') == -1) {
        url = (window.location.protocol == 'https:' ? 'wss:' : 'ws:') + '//' + window.location.host + url;
    }
    console.log('WebSocketChannelClient():url=' + url);

    private.auth = null;
    private.subscriptionByChannelKey = {};

    private.setStatus = function (value) {
        if (public.status != value) {
            public.status = value;
            if (options.onStatusChange) options.onStatusChange(value);
        }
    }

    private.testConnection = function () {
        if (!private.webSocket) {
            try {
                private.setStatus('Connecting...');
                private.webSocket = new WebSocket(url);
                private.webSocket.onmessage = function (event) {
                    let message = JSON.parse(event.data);
                    if (message.channelKey) {
                        let subscription = private.subscriptionByChannelKey[message.channelKey];
                        if (subscription.handlers) {
                            for (let i = 0; i < subscription.handlers.length; i++) {
                                subscription.handlers[i](message.messageType, message.data);
                            }
                        }
                    }
                    else if (message.messageType == 'AUTHENTICATED') {
                        private.setStatus('Authenticated');
                        private.markSubscriptionSent(false);
                        private.sendSubscriptions();
                    }
                };
                private.webSocket.onopen = function () {
                    private.sendAuthorization();
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
                console.log('WebSocketChannelClient.testConnection():heartbeat success');
            }
            catch (e) {
                private.webSocket = null;
            }
        }
        else {
            private.webSocket = null;
        }

        private.heartbeatTimeout = setTimeout(function () {
            private.testConnection();
        }, heartbeatEveryMillis);

    }

    private.sendAuthorization = function () {
        if (private.webSocket && private.webSocket.readyState == 1) {
            private.webSocket.send('Authorization:' + (private.auth || ''));
        }
    }

    private.sendSubscriptions = function () {
        if (private.webSocket && private.webSocket.readyState == 1 && public.status == 'Authenticated') {
            let data = [];
            for (let key in private.subscriptionByChannelKey) {
                let subscription = private.subscriptionByChannelKey[key];
                if (!subscription.sent) {
                    data.push({
                        channelKey: key,
                        vars: subscription.vars,
                    });
                }
            }
            if (data.length > 0) {
                let text = 'Subscribe:' + JSON.stringify(data);
                console.log('WebSocketChannelClient.sendSubscriptions():text=' + text);
                private.webSocket.send(text);
                private.markSubscriptionSent(true);
            }
        }
    }

    private.addSubscription = function (channelKey, subscription) {
        private.subscriptionByChannelKey[channelKey] = subscription;
        private.sendSubscriptions();
    }

    private.sendUnsubscribe = function (channelKey) {
        if (private.webSocket && private.webSocket.readyState == 1) {
            let text = 'Unsubscribe:' + JSON.stringify(channelKey);
            private.webSocket.send(text);
        }
    }

    private.markSubscriptionSent = function (value) {
        for (let key in private.subscriptionByChannelKey) {
            let subscription = private.subscriptionByChannelKey[key];
            subscription.sent = value;
        }
    }

    private.removeSubscription = function (channelKey) {
        delete private.subscriptionByChannelKey[channelKey];
    }

    let public = {
        status: null,
        start: function () {
            private.testConnection();
        },
        authorize: function (newValue) {
            console.log('WebSocketChannelClient.authorize()');
            if (private.auth != newValue) {
                private.auth = newValue;
                private.markSubscriptionSent(false);
                private.sendAuthorization();
            }
        },
        subscribe: function (handler, channelKey, vars) {
            console.log('WebSocketChannelClient.subscribe()');
            if (!channelKey) channelKey = 'default';
            private.removeSubscription(channelKey);
            private.addSubscription(channelKey, {
                vars: vars,
                handlers: Array.isArray(handler) ? handler : [handler],
                sent: false,
            });
            if (options.onSubscriptionsUpdated) options.onSubscriptionsUpdated();
        },
        unsubscribe: function (channelKey) {
            if (!channelKey) channelKey = 'default';
            private.removeSubscription(channelKey);
            private.sendUnsubscribe(channelKey);
            if (options.onSubscriptionsUpdated) options.onSubscriptionsUpdated();
        },
        stop: function () {
            clearTimeout(private.heartbeatTimeout);
            private.setStatus('Disconnected');
        }
    };

    return public;
}

function ArrayDataEventHandler(config) {
    let private = this;

    let keyFieldNamesByName = {};

    private.findIndex = function (array, keyValue) {
        return array.findIndex(x => x._keyValue == keyValue);
    }

    private.getKeyValue = function (name, record) {
        let result = '';
        let keyFieldNames = keyFieldNamesByName[name];
        for (let i = 0; i < keyFieldNames.length; i++) {
            let value = record[keyFieldNames[i]];
            if (!result && result.length > 0) result += ';';
            result += '' + value;
        }
        return result;
    }

    return function (messageType, data) {
        if (messageType == 'DATA-EVENT-TRANSACTION') {
            let dataEventTransaction = data;
            for (let i = 0; i < dataEventTransaction.dataEvents.length; i++) {
                let dataEvent = dataEventTransaction.dataEvents[i];
                console.log('ArrayDataEventHandler.handle():dataEvent.type=' + dataEvent.dataEventType + ',name=', dataEvent.name + ',keyValue=' + dataEvent.keyValue);
                if (dataEvent.dataEventType == 'InitialEnd') {
                    if (config.onInitialEnd) config.onInitialEnd();
                }
                else {
                    let array = config.arrayMapping[dataEvent.name];
                    if (!array) {
                        console.error('No mapping for data event \'' + dataEvent.name + '\'');
                    }
                    else if (dataEvent.dataEventType == 'InitialBegin') {
                        array.splice(0, array.length);
                        keyFieldNamesByName[dataEvent.name] = dataEvent.keyFieldNames;
                    }
                    else if (dataEvent.dataEventType == 'Insert' || dataEvent.dataEventType == 'Initial') {
                        let keyValue = private.getKeyValue(dataEvent.name, dataEvent.record);
                        let index = private.findIndex(array, keyValue);
                        if (index >= 0) {
                            console.error('Duplicate key \'' + keyValue + '\' in table \'' + dataEvent.name + '\'');
                        }
                        else {
                            dataEvent.record['_keyValue'] = keyValue;
                            array.push(dataEvent.record);
                        }
                    }
                    else if (dataEvent.dataEventType == 'Update') {
                        let keyValue = private.getKeyValue(dataEvent.name, dataEvent.record);
                        let index = private.findIndex(array, keyValue);
                        if (index == -1) {
                            console.error('Could not find key \'' + keyValue + '\' in table \'' + dataEvent.name + '\'');
                        }
                        else {
                            dataEvent.record['_keyValue'] = keyValue;
                            array.splice(index, 1, dataEvent.record);
                        }
                    }
                    else if (dataEvent.dataEventType == 'Delete') {
                        let keyValue = private.getKeyValue(dataEvent.name, dataEvent.record);
                        let index = private.findIndex(array, keyValue);
                        array.splice(index, 1);
                    }
                }
            }
        }
        else if (config.onChannelMessage) {
            config.onChannelMessage(messageType, data);
        }
    }
}
