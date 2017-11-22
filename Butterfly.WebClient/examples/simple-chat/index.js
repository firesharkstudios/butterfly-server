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

// Chat Messages Component
Vue.component('chat-messages-component', Vue.extend({
    template: '#chat-messages-component',
    props: ['myUserName', 'chatMessages'],
    data: function () {
        return {
            formMessage: null
        }
    },
    methods: {
        postMessage: function () {
            $.ajax('/api/chat/message', {
                method: 'POST',
                data: JSON.stringify({
                    userName: this.myUserName,
                    text: this.formMessage,
                }),
                processData: false,
            });
            this.formMessage = null;
        }
    },
    computed: {
        selectedChatMessages: function () {
            return this.chatMessages.sort(FieldComparer('created_at'));
        },
    },
    watch: {
        selectedChatMessages: function () {
            let self = this;
            Vue.nextTick(function () {
                let chatMessageHistory = $(self.$el).find('.chat-message-history');
                chatMessageHistory.animate({ scrollTop: chatMessageHistory.prop("scrollHeight") }, 1000);
            });
        }
    }
}));

// App
let app = new Vue({
    el: '#app',
    data: {
        connectionStatus: 'Connecting...', 
        myUserId: null,
        myUserName: null,
        chatMessages: [],
    },
    watch: {
        connectionStatus: function (value) {
            $('#notConnectedModal').modal(value != 'Connected' ? 'show' : 'hide');
        }
    },
    mounted: function () {
        let self = this;

        // Create user id
        self.myUserId = getOrCreateLocalStorageItem('userId', function () {
            return uuidv4();
        });

        // Create user name
        self.myUserName = getOrCreateLocalStorageItem('userName', function () {
            return generateCleverName();
        });

        // Create channel to server and handle data events
        let channelClient = new WebSocketChannelClient({
            userId: self.myUserId,
            dataEventHandler: new VueDataEventHandler({
                vueArrayMapping: {
                    chat_message: self.chatMessages,
                }
            }),
            onStatusChange: function (value) {
                self.connectionStatus = value;
            },
        });
        channelClient.start();
    }
});