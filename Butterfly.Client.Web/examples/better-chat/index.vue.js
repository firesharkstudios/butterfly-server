// Note: This example uses 'User 123' as the authorization header just to keep the example simple
//       (in a real world app, you should use a more robust authorization mechanism)

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
            let self = this;
            bootbox.confirm("Delete this chat?", function (result) {
                $.ajax({
                    method: 'POST',
                    url: '/api/better-chat/chat/delete',
                    headers: {
                        Authorization: 'User ' + self.myUserId,
                    },
                    data: JSON.stringify({
                        id: chatId,
                    })
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
            return this.chats.sort(butterfly.util.FieldComparer('name'));
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
                    $.ajax({
                        method: 'POST',
                        url: '/api/better-chat/profile/update',
                        headers: {
                            Authorization: 'User ' + self.myUserId,
                        },
                        data: JSON.stringify({
                            name: result,
                        })
                    });
                }
            });
        },
    },
    computed: {
        selectedChatParticipants: function () {
            return this.chatParticipants && this.selectedChatId ? this.chatParticipants.filter(x => x.chat_id == this.selectedChatId).sort(butterfly.util.FieldComparer('name')) : null;
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
            $.ajax({
                method: 'POST',
                url: '/api/better-chat/chat/message',
                headers: {
                    Authorization: 'User ' + this.myUserId,
                },
                data: JSON.stringify({
                    chatId: this.selectedChatId,
                    text: this.formMessage,
                })
            });
            this.formMessage = null;
        }
    },
    computed: {
        selectedChatMessages: function () {
            return this.chatMessages && this.selectedChatId ? this.chatMessages.filter(x => x.chat_id == this.selectedChatId).sort(butterfly.util.FieldComparer('created_at')) : null;
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
                value: '',
                callback: function (result) {
                    $.ajax({
                        method: 'POST',
                        url: '/api/better-chat/chat/create',
                        headers: {
                            Authorization: 'User ' + self.myUserId,
                        },
                        data: JSON.stringify({
                            name: result,
                        })
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
            $('#notConnectedModal').modal(value != 'Authenticated' ? 'show' : 'hide');
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
        connectionStatus: function (value) {
            $('#notConnectedModal').modal(value != 'Authenticated' ? 'show' : 'hide');
        },
        me: function (value) {
            this.formProfileName = this.me.name;
        },
    },
    mounted: function () {
        let self = this;

        // Create user id
        self.myUserId = butterfly.util.getOrCreateLocalStorageItem('userId', function () {
            return Math.floor(Math.random() * 999999);
        });

        // Create channel to server and handle data events
        let channelClient = new WebSocketChannelClient({
            url: '/better-chat',
            onStatusChange: function(value) {
                self.connectionStatus = value;
            },
        });
        channelClient.subscribe(new ArrayDataEventHandler({
            arrayMapping: {
                me: self.mes,
                chat: self.chats,
                chat_user: self.chatUsers,
                chat_participant: self.chatParticipants,
                chat_message: self.chatMessages,
            }
        }));
        channelClient.authorize('Custom ' + self.myUserId);
        channelClient.start();

        // Join chat if url has a join query string parameter
        let match = /join=(.*)/.exec(window.location.href);
        if (match) {
            $.ajax({
                method: 'POST',
                url: '/api/better-chat/chat/join',
                headers: {
                    Authorization: 'User ' + self.myUserId,
                },
                data: JSON.stringify({
                    joinId: match[1],
                })
            });
        }
    }
});

