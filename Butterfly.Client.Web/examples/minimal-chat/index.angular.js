angular.module('app', ['components'])
    .controller('SimpleChat', function ($scope, $locale) {
        $scope.connectionStatus = 'Connecting...';
        $scope.myUserId = getOrCreateLocalStorageItem('userId', function () {
            return uuidv4();
        });
        $scope.myUserName = getOrCreateLocalStorageItem('userName', function () {
            return generateCleverName();
        });
        $scope.chatMessages = [];

        this.$onInit = function () {
            // Create channel to server and handle data events
            let channelClient = new WebSocketChannelClient({
                url: '/minimal-chat',
                auth: 'User ' + $scope.myUserId,
                onDataEvent: new ArrayDataEventHandler({
                    arrayMapping: {
                        chat_message: $scope.chatMessages,
                    }
                }),
                onUpdated: function () {
                    $scope.$apply();
                },
                onStatusChange: function (value) {
                    self.connectionStatus = value;
                },
            });
            channelClient.start();
        };
    });

angular.module('components', [])
    .directive('chatMessagesComponent', function () {
        return {
            restrict: 'E',
            transclude: true,
            scope: {
                myUserName: '<',
                chatMessages: '<',
            },
            controller: function ($scope, $element) {
                let self = this;
                $scope.postMessage = function () {
                    $.ajax('/api/minimal-chat/chat/message', {
                        method: 'POST',
                        data: JSON.stringify({
                            userName: $scope.myUserName,
                            text: $scope.formMessage,
                        }),
                        processData: false,
                    });
                    this.formMessage = null;
                };
                $scope.checkPostMessageKey = function (e) {
                    if (e.keyCode == 13) {
                        $scope.postMessage();
                    }
                };
                $scope.sortedChatMessages = function () {
                    return $scope.chatMessages.sort(FieldComparer('created_at'));
                };
                $scope.$watchCollection($scope.sortedChatMessages(), function () {
                    let chatMessageHistory = $($element).find('.chat-message-history');
                    chatMessageHistory.animate({ scrollTop: chatMessageHistory.prop("scrollHeight") }, 1000);
                });
            },
            template:
            '<div class="d-flex flex-column">' +
            '    <div class="flex-grow chat-message-history">' +
            '        <div ng-repeat="chatMessage in sortedChatMessages() track by $index">' +
            '            <p><mark> {{ chatMessage.user_name }}: </mark> &nbsp; {{ chatMessage.text }}</p>' +
            '        </div>' +
            '    </div>' +
            '    <div>' +
            '        <br />' +
            '        <div class="d-flex">' +
            '            <div class="flex-grow">' +
            '                <input class="form-control" ng-model="formMessage" placeholder="Type your message" ng-keyup="checkPostMessageKey($event)" />' +
            '            </div>' +
            '            <div>&nbsp;</div>' +
            '            <div>' +
            '                <button class="btn btn-primary" ng-click="postMessage()">Send</button>' +
            '        </div>' +
            '    </div>' +
            '</div>',
            replace: true
        };
    });