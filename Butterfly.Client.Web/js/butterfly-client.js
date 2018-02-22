function WebSocketChannelClient(options) {
    let private = this;

    let heartbeatEveryMillis = options.heartbeatEveryMillis || 3000;
    let url = options.url;
    let onSubscriptionsUpdated = options.onSubscriptionsUpdated;

    if (url.indexOf('://') == -1) {
        url = (window.location.protocol == 'https:' ? 'wss:' : 'ws:') + '//' + window.location.host + url;
    }
    console.log('WebSocketChannelClient():url=' + url);

    private.auth = null;
    private.subscriptions = [];
    private.handlersByKey = {};

    private.setStatus = function (value) {
        if (public.status != value) {
            public.status = value;
            if (private.onStatusChange) private.onStatusChange(value);
        }
    }

    private.testConnection = function () {
        //console.log('testConnection():firstAttempt=' + firstAttempt);
        if (!private.webSocket) {
            try {
                private.setStatus('Connecting...');
                private.webSocket = new WebSocket(url);
                private.webSocket.onmessage = function (event) {
                    if (event.data == '$AUTHENTICATED') {
                        private.setStatus('Connected');
                    }
                    else {
                        let pos = event.data.indexOf(':');
                        let channelKey = event.data.substring(0, pos);
                        let handlers = private.handlersByKey[channelKey];
                        if (handlers) {
                            let json = event.data.substring(pos + 1);
                            let message = JSON.parse(json);
                            for (let i = 0; i < handlers.length; i++) {
                                handlers[i](message);
                            }
                        }
                    }
                };
                private.webSocket.onopen = function () {
                    private.sendAuthorization();
                    private.sendSubscribe(private.subscriptions);
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
            private.testConnection();
        }, heartbeatEveryMillis);

    }

    private.sendAuthorization = function () {
        if (private.webSocket && private.webSocket.readyState == 1) {
            private.webSocket.send('Authorization:' + (private.auth || ''));
        }
    }

    private.sendSubscribe = function (subscriptions) {
        if (private.webSocket && private.webSocket.readyState == 1) {
            let newSubscriptions = Array.isArray(subscriptions) ? subscriptions : [subscriptions];
            let text = 'Subscribe:' + JSON.stringify(newSubscriptions);
            private.webSocket.send(text);
        }
    }

    private.addSubscription = function (handler, channelKey, subscription) {
        private.subscriptions.push(subscription);
        private.handlersByKey[channelKey] = Array.isArray(handler) ? handler : [handler];
    }

    private.sendUnsubscribe = function (channelKey) {
        if (private.webSocket && private.webSocket.readyState == 1) {
            let text = 'Unsubscribe:' + JSON.stringify(channelKey);
            private.webSocket.send(text);
        }
    }

    private.removeSubscription = function (channelKey) {
        let index = private.subscriptions.findIndex(x => x.channelKey == channelKey);
        if (index >= 0) {
            private.subscriptions.splice(index, 1);
            delete private.handlersByKey[channelKey];
        }
    }

    let public = {
        status: 'Connecting...',
        onStatusChange: function (callback) {
            private.onStatusChange = callback;
        },
        start: function () {
            private.testConnection();
        },
        authorize: function (newValue) {
            private.auth = newValue;
            private.sendAuthorization();
            private.sendSubscribe(private.subscriptions);
        },
        subscribe: function (handler, channelKey, vars) {
            if (!channelKey) channelKey = 'default';
            private.removeSubscription(channelKey);
            let subscription = {
                channelKey: channelKey,
                vars: vars
            };
            private.addSubscription(handler, channelKey, subscription);
            private.sendSubscribe(subscription);
            if (onSubscriptionsUpdated) onSubscriptionsUpdated();
        },
        unsubscribe: function (channelKey) {
            if (!channelKey) channelKey = 'default';
            private.removeSubscription(channelKey);
            private.sendUnsubscribe(channelKey);
            if (onSubscriptionsUpdated) onSubscriptionsUpdated();
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

    return function (message) {
        if (typeof message == "string") {
            if (message.startsWith('!')) {
                let error = message.substring(1);
                if (config.onChannelError) {
                    config.onChannelError(error);
                }
            }
        }
        else {
            let dataEventTransaction = message;
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
    }

    return public;
}

function FieldComparer(fieldName) {
    return function (a, b) {
        let valueA = a[fieldName];
        let valueB = b[fieldName];
        if (valueA < valueB) return -1;
        if (valueA > valueB) return 1;
        return 0;
    }
}

// From https://stackoverflow.com/questions/105034/create-guid-uuid-in-javascript
function uuidv4() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

function getOrCreateLocalStorageItem(key, createFunc) {
    let value = window.localStorage.getItem(key);
    if (!value) {
        value = createFunc();
        window.localStorage.setItem(key, value);
    }
    return value;
}

/*
function authorizedAjax(method, uri, authorization, value) {
    return $.ajax(uri, {
        method: method,
        headers: {
            'Authorization': authorization,
        },
        data: JSON.stringify(value),
        processData: false,
    });
}
*/