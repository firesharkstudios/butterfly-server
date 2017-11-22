/*
 * Copyright 2017 Fireshark Studios, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

function WebSocketChannelClient(options) {

    let private = this;

    let testConnectionEveryMillis = options.testConnectionEveryMillis || 3000;
    let userId = options.userId;
    let dataEventHandler = options.dataEventHandler;
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
                private.webSocket = new WebSocket("ws://localhost:8080/channel/" + userId);
                private.webSocket.onmessage = function (event) {
                    let channelEvent = JSON.parse(event.data);
                    let dataEventTransaction = channelEvent.value;
                    for (let i = 0; i < dataEventTransaction.dataEvents.length; i++) {
                        dataEventHandler.handle(dataEventTransaction.dataEvents[i]);
                    }
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
            private.testConnection();
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