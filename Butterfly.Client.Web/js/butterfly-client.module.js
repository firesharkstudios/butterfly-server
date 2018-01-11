module.exports = {
    WebSocketChannelClient: function (options) {

        let private = this;

        let heartbeatEveryMillis = options.heartbeatEveryMillis || 3000;
        let url = options.url;
        let auth = options.auth;
        let onDataEvent = options.onDataEvent;
        let onUpdated = options.onUpdated;
        let onStatusChange = options.onStatusChange;

        if (url.indexOf('://') == -1) {
            url = (window.location.protocol == 'https:' ? 'wss:' : 'ws:') + '//' + window.location.host + url;
        }
        console.log('WebSocketChannelClient():url=' + url);

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
                        let dataEventTransaction = JSON.parse(event.data);
                        for (let i = 0; i < dataEventTransaction.dataEvents.length; i++) {
                            onDataEvent(dataEventTransaction.dataEvents[i]);
                        }
                        if (onUpdated) onUpdated();
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

            private.timeout = setTimeout(function () {
                private.testConnection(false);
            }, heartbeatEveryMillis);
        }

        let public = {
            status: 'Connecting...',
            start: function () {
                private.testConnection(true);
            },
            stop: function () {
                clearTimeout(private.timeout);
                private.setStatus('Disconnected');
            }
        };

        return public;
    },
    ArrayDataEventHandler: function (config) {

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

        return function (dataEvent) {
            console.log('ArrayDataEventHandler.handle():dataEvent.type=' + dataEvent.dataEventType + ',name=', dataEvent.name + ',keyValue=' + dataEvent.keyValue);
            let array = config.arrayMapping[dataEvent.name];
            if (dataEvent.dataEventType == 'InitialBegin') {
                array.splice(0, array.length);
                keyFieldNamesByName[dataEvent.name] = dataEvent.keyFieldNames;
            }
            else if (dataEvent.dataEventType == 'Insert' || dataEvent.dataEventType == 'Initial') {
                let keyValue = private.getKeyValue(dataEvent.name, dataEvent.record);
                dataEvent.record['_keyValue'] = keyValue;
                array.push(dataEvent.record);
            }
            else if (dataEvent.dataEventType == 'Update') {
                let keyValue = private.getKeyValue(dataEvent.name, dataEvent.record);
                let index = private.findIndex(array, keyValue);
                array.splice(index, 1, dataEvent.record);
            }
            else if (dataEvent.dataEventType == 'Delete') {
                let keyValue = private.getKeyValue(dataEvent.name, dataEvent.record);
                let index = private.findIndex(array, keyValue);
                array.splice(index, 1);
            }
            else if (dataEvent.dataEventType == 'InitialEnd') {
                if (config.loaded) config.loaded();
            }
        }

        return public;
    },

    FieldComparer: function (fieldName) {
        return function (a, b) {
            let valueA = a[fieldName];
            let valueB = b[fieldName];
            if (valueA < valueB) return -1;
            if (valueA > valueB) return 1;
            return 0;
        }
    },

    // From https://stackoverflow.com/questions/105034/create-guid-uuid-in-javascript
    uuidv4: function () {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    },

    getOrCreateLocalStorageItem: function (key, createFunc) {
        let value = window.localStorage.getItem(key);
        if (!value) {
            value = createFunc();
            window.localStorage.setItem(key, value);
        }
        return value;
    },

    authorizedAjax: function (method, uri, authorization, value) {
        return $.ajax(uri, {
            method: method,
            headers: {
                'Authorization': authorization,
            },
            data: JSON.stringify(value),
            processData: false,
        });
    }
}