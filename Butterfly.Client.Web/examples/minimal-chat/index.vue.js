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
            $.ajax('/api/minimal-chat/chat/message', {
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
            return this.chatMessages.sort(butterfly.util.FieldComparer('created_at'));
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
            $('#notConnectedModal').modal(value != 'Authenticated' ? 'show' : 'hide');
        }
    },
    mounted: function () {
        let self = this;

        // Create user id
        self.myUserId = butterfly.util.getOrCreateLocalStorageItem('userId', function () {
            return Math.floor(Math.random() * 999999);
        });

        // Create user name
        self.myUserName = butterfly.util.getOrCreateLocalStorageItem('userName', function () {
            return butterfly.clever.generateCleverName();
        });

        // Create channel to server and handle data events
        let channelClient = new WebSocketChannelClient({
            url: '/minimal-chat',
            onStatusChange: function (value) {
                self.connectionStatus = value;
            },
        });
        channelClient.subscribe(new ArrayDataEventHandler({
            arrayMapping: {
                chat_message: self.chatMessages,
            }
        }));
        channelClient.authorize('Custom ' + self.myUserId);
        channelClient.start();
    }
});