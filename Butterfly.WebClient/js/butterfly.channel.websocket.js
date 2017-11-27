function WebSocketChannelClient(options) {

    let private = this;

    let testConnectionEveryMillis = options.testConnectionEveryMillis || 3000;
    let url = options.url;
    let dataEventHandler = options.dataEventHandler;
    let onUpdated = options.onUpdated;
    let onStatusChange = options.onStatusChange;

    private.setStatus = function (value) {
        if (public.status != value) {
            public.status = value;
            if (options.onStatusChange) onStatusChange(value);
        }
    }

    private.testConnection = function (firstAttempt) {
        console.log('testConnection():firstAttempt=' + firstAttempt);
        if (!private.webSocket) {
            try {
                private.setStatus(firstAttempt ? 'Connecting...' : 'Reconnecting...');
                private.webSocket = new WebSocket(url);
                private.webSocket.onmessage = function (event) {
                    let channelEvent = JSON.parse(event.data);
                    let dataEventTransaction = channelEvent.value;
                    for (let i = 0; i < dataEventTransaction.dataEvents.length; i++) {
                        dataEventHandler.handle(dataEventTransaction.dataEvents[i]);
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
            console.log('testConnection():private.webSocket.readyState=' + private.webSocket.readyState);
            try {
                private.webSocket.send('__heartbeat__');
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
        }, testConnectionEveryMillis);
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