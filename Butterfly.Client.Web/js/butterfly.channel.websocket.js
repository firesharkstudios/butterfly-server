function WebSocketChannelClient(options) {

    let private = this;

    let heartbeatEveryMillis = options.heartbeatEveryMillis || 3000;
    let url = options.url;
    let onDataEvent = options.onDataEvent;
    let onUpdated = options.onUpdated;
    let onStatusChange = options.onStatusChange;

    if (url.indexOf('://') == -1) {
        url = (window.location.protocol=='https:' ? 'wss:' : 'ws:') + '//' + window.location.host + url;
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
                };
                private.webSocket.onerror = function (error) {
                    private.webSocket = null;
                }
            }
            catch (e) {
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
}