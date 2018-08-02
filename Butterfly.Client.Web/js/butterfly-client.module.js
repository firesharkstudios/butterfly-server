module.exports = {
    WebSocketChannelClient: function (options) {
        let private = this;

        let url = options.url;
        if (url.indexOf('://') == -1) {
            url = (window.location.protocol == 'https:' ? 'wss:' : 'ws:') + '//' + window.location.host + url;
        }
        console.debug('WebSocketChannelClient():url=' + url);

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
                    private.setStatus('Starting');
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
                            private.setStatus('Started');
                            private.markSubscriptionSent(false);
                            private.sendSubscriptions();
                        }
                        else if (message.messageType == 'UNAUTHENTICATED') {
                            public.stop();
                        }
                    };
                    private.webSocket.onopen = function () {
                        private.sendAuthorization();
                    };
                    private.webSocket.onerror = function (error) {
                        private.webSocket = null;
                    }
                    private.webSocket.onclose = function () {
                        private.webSocket = null;
                    }
                }
                catch (e) {
                    console.debug(e);
                    private.webSocket = null;
                }
            }
            else if (private.webSocket.readyState == 1) {
                //console.debug('testConnection():private.webSocket.readyState=' + private.webSocket.readyState);
                try {
                    private.webSocket.send('!');
                    console.debug('WebSocketChannelClient.testConnection():heartbeat success');
                }
                catch (e) {
                    private.webSocket = null;
                }
            }
            else {
                private.webSocket = null;
            }

            if (public.status != 'Stopped') {
                private.testConnectionTimeout = setTimeout(function () {
                    private.testConnection();
                }, options.testConnectionEveryMillis || 3000);
            }
        }

        private.sendAuthorization = function () {
            if (private.webSocket && private.webSocket.readyState == 1) {
                private.webSocket.send('Authorization:' + (private.auth || ''));
            }
        }

        private.sendSubscriptions = function () {
            if (private.webSocket && private.webSocket.readyState == 1 && public.status == 'Started') {
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
                    console.debug('WebSocketChannelClient.sendSubscriptions():text=' + text);
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
            start: function (auth) {
                console.debug('WebSocketChannelClient.start()');
                private.auth = auth;
                private.markSubscriptionSent(false);
                private.sendAuthorization();
                private.testConnection();
            },
            subscribe: function (handler, channelKey, vars) {
                console.debug(`WebSocketChannelClient.subscribe():channelKey=${channelKey}`);
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
                console.debug(`WebSocketChannelClient.unsubscribe():channelKey=${channelKey}`);
                if (!channelKey) channelKey = 'default';
                private.removeSubscription(channelKey);
                private.sendUnsubscribe(channelKey);
                if (options.onSubscriptionsUpdated) options.onSubscriptionsUpdated();
            },
            stop: function () {
                console.debug('WebSocketChannelClient.stop()')
                private.setStatus('Stopped');
                private.webSocket.close();
                private.webSocket = null;
                clearTimeout(private.testConnectionTimeout);
                for (let channelKey in private.subscriptionByChannelKey) {
                    let subscription = private.subscriptionByChannelKey[channelKey];
                    if (subscription.handlers) {
                        for (let i = 0; i < subscription.handlers.length; i++) {
                            subscription.handlers[i]('RESET');
                        }
                    }
                }
            }
        };

        return public;
    },

    VuexArrayGetters: function (arrayName) {
        let result = {};
        result[`${arrayName}Length`] = state => state[arrayName].length;
        result[`${arrayName}FindIndex`] = state => callback => state.myUsers.findIndex(callback);
        return result;
    },

    VueXArrayMutations: function (arrayName) {
        let result = {};
        result[`${arrayName}Splice`] = (state, options) => {
            if (options.item) state.myUsers.splice(options.start, options.deleteCount, options.item);
            else state.myUsers.splice(options.start, options.deleteCount);
        };
        return result;
    },

    VueXArrayHandler: function (store, arrayName) {
        return {
            get length() { return store.getters[`${arrayName}Length`] },
            findIndex(callback) { return store.getters[`${arrayName}FindIndex`](callback) },
            splice(start, deleteCount, item) {
                return store.commit(`${arrayName}Splice`, { start, deleteCount, item });
            },
        };
    },

    ArrayDataEventHandler: function (config) {
        let private = this;

        let keyFieldNamesByName = {};

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
            if (messageType == 'RESET') {
                for (let arrayKey in config.arrayMapping) {
                    let array = config.arrayMapping[arrayKey];
                    if (array) array.splice(0, array.length);
                }
            }
            else if (messageType == 'DATA-EVENT-TRANSACTION') {
                let dataEventTransaction = data;
                for (let i = 0; i < dataEventTransaction.dataEvents.length; i++) {
                    let dataEvent = dataEventTransaction.dataEvents[i];
                    //console.debug('ArrayDataEventHandler.handle():dataEvent.type=' + dataEvent.dataEventType + ',name=', dataEvent.name + ',keyValue=' + dataEvent.keyValue);
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
                            let index = array.findIndex(x => x._keyValue == keyValue);
                            if (index >= 0) {
                                console.error('Duplicate key \'' + keyValue + '\' in table \'' + dataEvent.name + '\'');
                            }
                            else {
                                dataEvent.record['_keyValue'] = keyValue;
                                array.splice(array.length, 0, dataEvent.record);
                            }
                        }
                        else if (dataEvent.dataEventType == 'Update') {
                            let keyValue = private.getKeyValue(dataEvent.name, dataEvent.record);
                            let index = array.findIndex(x => x._keyValue == keyValue);
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
                            let index = array.findIndex(x => x._keyValue == keyValue);
                            array.splice(index, 1);
                        }
                    }
                }
            }
            else if (config.onChannelMessage) {
                config.onChannelMessage(messageType, data);
            }
        }
    },

}
