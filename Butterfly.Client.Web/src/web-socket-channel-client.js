export default function(options) {
	let _private = this;

	let url = options.url;
	if (url.indexOf('://') == -1) {
		url = (window.location.protocol == 'https:' ? 'wss:' : 'ws:') + '//' + window.location.host + url;
	}
	console.debug('WebSocketChannelClient():url=' + url);

	_private.auth = null;
	_private.subscriptionByChannelKey = {};

	_private.setStatus = function (value) {
		if (_public.status != value) {
			_public.status = value;
			if (options.onStatusChange) options.onStatusChange(value);
		}
	}

	_private.testConnection = function () {
		if (!_private.webSocket) {
			try {
				_private.setStatus('Starting');
				_private.webSocket = new WebSocket(url);
				_private.webSocket.onmessage = function (event) {
					let message = JSON.parse(event.data);
					if (message.channelKey) {
						let subscription = _private.subscriptionByChannelKey[message.channelKey];
						if (subscription.handlers) {
							for (let i = 0; i < subscription.handlers.length; i++) {
								subscription.handlers[i](message.messageType, message.data);
							}
						}
					}
					else if (message.messageType == 'AUTHENTICATED') {
						_private.setStatus('Started');
						_private.markSubscriptionSent(false);
						_private.sendSubscriptions();
					}
					else if (message.messageType == 'UNAUTHENTICATED') {
						_public.stop();
					}
				};
				_private.webSocket.onopen = function () {
					_private.sendAuthorization();
				};
				_private.webSocket.onerror = function (error) {
					_private.webSocket = null;
				}
				_private.webSocket.onclose = function () {
					_private.webSocket = null;
				}
			}
			catch (e) {
				console.debug(e);
				_private.webSocket = null;
			}
		}
		else if (_private.webSocket.readyState == 1) {
			//console.debug('testConnection():_private.webSocket.readyState=' + _private.webSocket.readyState);
			try {
				_private.webSocket.send('!');
				console.debug('WebSocketChannelClient.testConnection():heartbeat success');
			}
			catch (e) {
				_private.webSocket = null;
			}
		}
		else {
			_private.webSocket = null;
		}

		if (_public.status != 'Stopped') {
			_private.testConnectionTimeout = setTimeout(function () {
				_private.testConnection();
			}, options.testConnectionEveryMillis || 3000);
		}
	}

	_private.sendAuthorization = function () {
		if (_private.webSocket && _private.webSocket.readyState == 1) {
			_private.webSocket.send('Authorization:' + (_private.auth || ''));
		}
	}

	_private.sendSubscriptions = function () {
		if (_private.webSocket && _private.webSocket.readyState == 1 && _public.status == 'Started') {
			let data = [];
			for (let key in _private.subscriptionByChannelKey) {
				let subscription = _private.subscriptionByChannelKey[key];
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
				_private.webSocket.send(text);
				_private.markSubscriptionSent(true);
			}
		}
	}

	_private.addSubscription = function (channelKey, subscription) {
		_private.subscriptionByChannelKey[channelKey] = subscription;
		_private.sendSubscriptions();
	}

	_private.sendUnsubscribe = function (channelKey) {
		if (_private.webSocket && _private.webSocket.readyState == 1) {
			let text = 'Unsubscribe:' + JSON.stringify(channelKey);
			_private.webSocket.send(text);
		}
	}

	_private.markSubscriptionSent = function (value) {
		for (let key in _private.subscriptionByChannelKey) {
			let subscription = _private.subscriptionByChannelKey[key];
			subscription.sent = value;
		}
	}

	_private.removeSubscription = function (channelKey) {
		delete _private.subscriptionByChannelKey[channelKey];
	}

	let _public = {
		status: null,
		start: function (auth) {
			console.debug('WebSocketChannelClient.start()');
			_private.auth = auth;
			_private.markSubscriptionSent(false);
			_private.sendAuthorization();
			_private.testConnection();
		},
		subscribe: function (handler, channelKey, vars) {
			console.debug(`WebSocketChannelClient.subscribe():channelKey=${channelKey}`);
			if (!channelKey) channelKey = 'default';
			_private.removeSubscription(channelKey);
			_private.addSubscription(channelKey, {
				vars: vars,
				handlers: Array.isArray(handler) ? handler : [handler],
				sent: false,
			});
			if (options.onSubscriptionsUpdated) options.onSubscriptionsUpdated();
		},
		unsubscribe: function (channelKey) {
			console.debug(`WebSocketChannelClient.unsubscribe():channelKey=${channelKey}`);
			if (!channelKey) channelKey = 'default';
			_private.removeSubscription(channelKey);
			_private.sendUnsubscribe(channelKey);
			if (options.onSubscriptionsUpdated) options.onSubscriptionsUpdated();
		},
		stop: function () {
			console.debug('WebSocketChannelClient.stop()')
			_private.setStatus('Stopped');
			_private.webSocket.close();
			_private.webSocket = null;
			clearTimeout(_private.testConnectionTimeout);
			for (let channelKey in _private.subscriptionByChannelKey) {
				let subscription = _private.subscriptionByChannelKey[channelKey];
				if (subscription.handlers) {
					for (let i = 0; i < subscription.handlers.length; i++) {
						subscription.handlers[i]('RESET');
					}
				}
			}
		}
	};

	return _public;
}
