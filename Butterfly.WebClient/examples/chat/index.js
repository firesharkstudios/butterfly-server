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

// Chats Component
Vue.component('chats-component', Vue.extend({
    template: '#chats-component',
    props: ['myUserId', 'chats'],
    data: function () {
        return {
            selectedChatId: null,
        };
    },
    methods: {
        deleteChat: function (chatId) {
            bootbox.confirm("Delete this chat?", function (result) {
                authorizedAjax('POST', '/api/chat/delete', 'User ' + self.myUserId, {
                    id: chatId,
                });
            });
        }
    },
    watch: {
        chats: function (value) {
            if (value.length > 0) {
                if (this.selectedChatId && !this.chats.find(x => x.id == this.selectedChatId) || !this.selectedChatId) {
                    this.selectedChatId = value[0].id;
                }
            }
        },
        selectedChatId: function (value) {
            this.$emit('new-selection', value);
        }
    },
    computed: {
        sortedChats: function () {
            return this.chats.sort(FieldComparer('name'));
        },
    },
}));

// Chat Participants Component
Vue.component('chat-participants-component', Vue.extend({
    template: '#chat-participants-component',
    props: ['myUserId', 'selectedChatId', 'chatParticipants'],
    methods: {
        showUpdateProfilePrompt: function (name) {
            let self = this;
            bootbox.prompt({
                title: 'Update Profile: What name do you want to use?',
                value: name,
                callback: function (result) {
                    authorizedAjax('POST', '/api/profile/update', 'User ' + self.myUserId, {
                        name: result,
                    });
                }
            });
        },
    },
    computed: {
        selectedChatParticipants: function () {
            return this.chatParticipants && this.selectedChatId ? this.chatParticipants.filter(x => x.chat_id == this.selectedChatId).sort(FieldComparer('name')) : null;
        },
    },
}));

// Chat Messages Component
Vue.component('chat-messages-component', Vue.extend({
    template: '#chat-messages-component',
    props: ['myUserId', 'selectedChatId', 'chatMessages'],
    data: function () {
        return {
            formMessage: null
        }
    },
    methods: {
        postMessage: function () {
            authorizedAjax('POST', '/api/chat/message', 'User ' + this.myUserId, {
                chatId: this.selectedChatId,
                text: this.formMessage,
            });
            this.formMessage = null;
        }
    },
    computed: {
        selectedChatMessages: function () {
            return this.chatMessages && this.selectedChatId ? this.chatMessages.filter(x => x.chat_id == this.selectedChatId).sort(FieldComparer('created_at')) : null;
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

        mes: [],
        chats: [],
        chatUsers: [],
        chatParticipants: [],
        chatMessages: [],

        myUserId: null,
        selectedChatId: null,

        formProfileName: null,
    },
    methods: {
        setSelectedChatId: function (chatId) {
            console.log('App.setSelectedChatId():id=' + chatId);
            this.selectedChatId = chatId;
        },
        showAddChatPrompt: function () {
            let self = this;
            bootbox.prompt({
                title: 'Add Chat: What is the chat topic?',
                value: 'My Topic',
                callback: function (result) {
                    authorizedAjax('POST', '/api/chat/create', 'User ' + self.myUserId, {
                        name: result,
                    });
                }
            });
        },
        showAddParticipantDialog: function () {
            let self = this;
            bootbox.dialog({
                title: 'Add Participant',
                message: '<p class="text-center">Anyone opening this url with join the selected chat...</p><p class="text-center"><a href="#">' + self.joinUrl + '</a></p><p class="text-center"><b>Tip:</b> Open in a different browser to login as a different user</p>'
            });
        },
        showConnectionModal: function (value) {
            $('#notConnectedModal').modal(value ? 'show' : 'hide');
        },
    },
    computed: {
        me: function () {
            return this.mes.length > 0 ? this.mes[0] : null;
        },
        joinUrl: function () {
            let selectedChat = this.chats.find(x => x.id == this.selectedChatId);
            let pos = document.location.href.indexOf('?');
            let baseUrl = pos == -1 ? document.location.href : document.location.href.substring(0, pos);
            return selectedChat ? baseUrl + '?join=' + selectedChat.join_id : null;
        },
    },
    watch: {
        me: function (value) {
            this.formProfileName = this.me.name;
        },
    },
    mounted: function () {
        let self = this;

        // Create user id
        self.myUserId = getOrCreateLocalStorageItem('userId', function () {
            return uuidv4();
        });

        // Create channel to server and handle data events
        self.showConnectionModal(true);
        let channelClient = new WebSocketChannelClient({
            userId: self.myUserId,
            dataEventHandler: new VueDataEventHandler({
                vueArrayMapping: {
                    me: self.mes,
                    chat: self.chats,
                    chat_user: self.chatUsers,
                    chat_participant: self.chatParticipants,
                    chat_message: self.chatMessages,
                }
            }),
            onStatusChange: function (value) {
                self.connectionStatus = value;
                self.showConnectionModal(value != 'Connected');
            },
        });
        channelClient.start();

        // Join chat if url has a join query string parameter
        let match = /join=(.*)/.exec(window.location.href);
        if (match) {
            authorizedAjax('POST', '/api/chat/join', 'User ' + self.myUserId, {
                joinId: match[1],
            });
        }
    }
});