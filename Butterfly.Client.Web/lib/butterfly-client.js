(function webpackUniversalModuleDefinition(root, factory) {
	if(typeof exports === 'object' && typeof module === 'object')
		module.exports = factory();
	else if(typeof define === 'function' && define.amd)
		define("butterfly-client", [], factory);
	else if(typeof exports === 'object')
		exports["butterfly-client"] = factory();
	else
		root["butterfly-client"] = factory();
})(window, function() {
return /******/ (function(modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if(installedModules[moduleId]) {
/******/ 			return installedModules[moduleId].exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			i: moduleId,
/******/ 			l: false,
/******/ 			exports: {}
/******/ 		};
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);
/******/
/******/ 		// Flag the module as loaded
/******/ 		module.l = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// define getter function for harmony exports
/******/ 	__webpack_require__.d = function(exports, name, getter) {
/******/ 		if(!__webpack_require__.o(exports, name)) {
/******/ 			Object.defineProperty(exports, name, { enumerable: true, get: getter });
/******/ 		}
/******/ 	};
/******/
/******/ 	// define __esModule on exports
/******/ 	__webpack_require__.r = function(exports) {
/******/ 		if(typeof Symbol !== 'undefined' && Symbol.toStringTag) {
/******/ 			Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
/******/ 		}
/******/ 		Object.defineProperty(exports, '__esModule', { value: true });
/******/ 	};
/******/
/******/ 	// create a fake namespace object
/******/ 	// mode & 1: value is a module id, require it
/******/ 	// mode & 2: merge all properties of value into the ns
/******/ 	// mode & 4: return value when already ns object
/******/ 	// mode & 8|1: behave like require
/******/ 	__webpack_require__.t = function(value, mode) {
/******/ 		if(mode & 1) value = __webpack_require__(value);
/******/ 		if(mode & 8) return value;
/******/ 		if((mode & 4) && typeof value === 'object' && value && value.__esModule) return value;
/******/ 		var ns = Object.create(null);
/******/ 		__webpack_require__.r(ns);
/******/ 		Object.defineProperty(ns, 'default', { enumerable: true, value: value });
/******/ 		if(mode & 2 && typeof value != 'string') for(var key in value) __webpack_require__.d(ns, key, function(key) { return value[key]; }.bind(null, key));
/******/ 		return ns;
/******/ 	};
/******/
/******/ 	// getDefaultExport function for compatibility with non-harmony modules
/******/ 	__webpack_require__.n = function(module) {
/******/ 		var getter = module && module.__esModule ?
/******/ 			function getDefault() { return module['default']; } :
/******/ 			function getModuleExports() { return module; };
/******/ 		__webpack_require__.d(getter, 'a', getter);
/******/ 		return getter;
/******/ 	};
/******/
/******/ 	// Object.prototype.hasOwnProperty.call
/******/ 	__webpack_require__.o = function(object, property) { return Object.prototype.hasOwnProperty.call(object, property); };
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";
/******/
/******/
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(__webpack_require__.s = "./src/index.js");
/******/ })
/************************************************************************/
/******/ ({

/***/ "./src/array-data-event-handler.js":
/*!*****************************************!*\
  !*** ./src/array-data-event-handler.js ***!
  \*****************************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.default = _default;

function _default(config) {
  var _private = this;

  var keyFieldNamesByName = {};
  var batchSize = config.batchSize || 250;
  var queue = [];
  var queueCurrentOffset = 0;
  var handleQueueTimeout = null;

  _private.getKeyValue = function (name, record) {
    var result = '';
    var keyFieldNames = keyFieldNamesByName[name];

    for (var i = 0; i < keyFieldNames.length; i++) {
      var value = record[keyFieldNames[i]];
      if (!result && result.length > 0) result += ';';
      result += '' + value;
    }

    return result;
  };

  _private.handleDataEvent = function (dataEvent) {
    //console.debug('ArrayDataEventHandler.handle():dataEvent.type=' + dataEvent.dataEventType + ',name=', dataEvent.name + ',keyValue=' + dataEvent.keyValue);
    if (dataEvent.dataEventType === 'InitialEnd') {
      if (config.onInitialEnd) config.onInitialEnd();
    } else {
      var array = config.arrayMapping[dataEvent.name];

      if (!array) {
        console.error('No mapping for data event \'' + dataEvent.name + '\'');
      } else if (dataEvent.dataEventType === 'InitialBegin') {
        array.splice(0, array.length);
        keyFieldNamesByName[dataEvent.name] = dataEvent.keyFieldNames;
      } else if (dataEvent.dataEventType === 'Insert' || dataEvent.dataEventType === 'Initial') {
        var keyValue = _private.getKeyValue(dataEvent.name, dataEvent.record);

        var index = array.findIndex(function (x) {
          return x._keyValue == keyValue;
        });

        if (index >= 0) {
          console.error('Duplicate key \'' + keyValue + '\' in table \'' + dataEvent.name + '\'');
        } else {
          dataEvent.record['_keyValue'] = keyValue;
          array.splice(array.length, 0, dataEvent.record);
        }
      } else if (dataEvent.dataEventType === 'Update') {
        var _keyValue = _private.getKeyValue(dataEvent.name, dataEvent.record);

        var _index = array.findIndex(function (x) {
          return x._keyValue == _keyValue;
        });

        if (_index == -1) {
          console.error('Could not find key \'' + _keyValue + '\' in table \'' + dataEvent.name + '\'');
        } else {
          dataEvent.record['_keyValue'] = _keyValue;
          array.splice(_index, 1, dataEvent.record);
        }
      } else if (dataEvent.dataEventType === 'Delete') {
        var _keyValue2 = _private.getKeyValue(dataEvent.name, dataEvent.record);

        var _index2 = array.findIndex(function (x) {
          return x._keyValue == _keyValue2;
        });

        array.splice(_index2, 1);
      }
    }
  };

  _private.handleQueue = function () {
    if (handleQueueTimeout) clearTimeout(handleQueueTimeout);

    if (queue.length > 0) {
      var begin = queueCurrentOffset;
      var end = Math.min(begin + batchSize, queue[0].length);

      for (var i = begin; i < end; i++) {
        _private.handleDataEvent(queue[0][i]);
      }

      if (end === queue[0].length) {
        queue.splice(0, 1);
        queueCurrentOffset = 0;
      } else {
        queueCurrentOffset += batchSize;
        handleQueueTimeout = setTimeout(_private.handleQueue, 0);
      }
    }
  };

  return function (messageType, data) {
    if (messageType === 'RESET') {
      for (var arrayKey in config.arrayMapping) {
        var array = config.arrayMapping[arrayKey];
        if (array) array.splice(0, array.length);
      }
    } else if (messageType === 'DATA-EVENT-TRANSACTION') {
      queue.push(data.dataEvents);

      _private.handleQueue();
    } else if (config.onChannelMessage) {
      config.onChannelMessage(messageType, data);
    }
  };
}

module.exports = exports["default"];

/***/ }),

/***/ "./src/index.js":
/*!**********************!*\
  !*** ./src/index.js ***!
  \**********************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
  value: true
});
Object.defineProperty(exports, "ArrayDataEventHandler", {
  enumerable: true,
  get: function get() {
    return _arrayDataEventHandler.default;
  }
});
Object.defineProperty(exports, "VuexArrayGetters", {
  enumerable: true,
  get: function get() {
    return _vuexArrayGetters.default;
  }
});
Object.defineProperty(exports, "VuexArrayHandler", {
  enumerable: true,
  get: function get() {
    return _vuexArrayHandler.default;
  }
});
Object.defineProperty(exports, "VuexArrayMutations", {
  enumerable: true,
  get: function get() {
    return _vuexArrayMutations.default;
  }
});
Object.defineProperty(exports, "WebSocketChannelClient", {
  enumerable: true,
  get: function get() {
    return _webSocketChannelClient.default;
  }
});

var _arrayDataEventHandler = _interopRequireDefault(__webpack_require__(/*! ./array-data-event-handler.js */ "./src/array-data-event-handler.js"));

var _vuexArrayGetters = _interopRequireDefault(__webpack_require__(/*! ./vuex-array-getters.js */ "./src/vuex-array-getters.js"));

var _vuexArrayHandler = _interopRequireDefault(__webpack_require__(/*! ./vuex-array-handler.js */ "./src/vuex-array-handler.js"));

var _vuexArrayMutations = _interopRequireDefault(__webpack_require__(/*! ./vuex-array-mutations.js */ "./src/vuex-array-mutations.js"));

var _webSocketChannelClient = _interopRequireDefault(__webpack_require__(/*! ./web-socket-channel-client.js */ "./src/web-socket-channel-client.js"));

function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { default: obj }; }

/***/ }),

/***/ "./src/vuex-array-getters.js":
/*!***********************************!*\
  !*** ./src/vuex-array-getters.js ***!
  \***********************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.default = _default;

function _default(arrayName) {
  var result = {};

  result["".concat(arrayName, "Length")] = function (state) {
    return state[arrayName].length;
  };

  result["".concat(arrayName, "FindIndex")] = function (state) {
    return function (callback) {
      return state[arrayName].findIndex(callback);
    };
  };

  return result;
}

module.exports = exports["default"];

/***/ }),

/***/ "./src/vuex-array-handler.js":
/*!***********************************!*\
  !*** ./src/vuex-array-handler.js ***!
  \***********************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.default = _default;

function _default(store, arrayName) {
  return {
    get length() {
      return store.getters["".concat(arrayName, "Length")];
    },

    findIndex: function findIndex(callback) {
      return store.getters["".concat(arrayName, "FindIndex")](callback);
    },
    splice: function splice(start, deleteCount, item) {
      return store.commit("".concat(arrayName, "Splice"), {
        start: start,
        deleteCount: deleteCount,
        item: item
      });
    }
  };
}

module.exports = exports["default"];

/***/ }),

/***/ "./src/vuex-array-mutations.js":
/*!*************************************!*\
  !*** ./src/vuex-array-mutations.js ***!
  \*************************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.default = _default;

function _default(arrayName) {
  var result = {};

  result["".concat(arrayName, "Splice")] = function (state, options) {
    if (options.item) state[arrayName].splice(options.start, options.deleteCount, options.item);else state[arrayName].splice(options.start, options.deleteCount);
  };

  return result;
}

module.exports = exports["default"];

/***/ }),

/***/ "./src/web-socket-channel-client.js":
/*!******************************************!*\
  !*** ./src/web-socket-channel-client.js ***!
  \******************************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.default = void 0;

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

function _defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } }

function _createClass(Constructor, protoProps, staticProps) { if (protoProps) _defineProperties(Constructor.prototype, protoProps); if (staticProps) _defineProperties(Constructor, staticProps); return Constructor; }

/*
 * States...
 *  Disconnected - No WebSocket, no loop running
 *  Connecting - Create WebSocket and wait for WebSocket.onopen()
 *  Authenticating - Send Authentication and wait for server to send AUTHENTICATED or UNAUTHENTICATED
 *  Subscribing - Send subscriptions and transition to Connected
 *  Connected - Send heartbeats to server
 */
var _class =
/*#__PURE__*/
function () {
  function _class(options) {
    _classCallCheck(this, _class);

    this._options = options;
    var url = this._options.url;

    if (url.indexOf('://') === -1) {
      this._url = (window.location.protocol === 'https:' ? 'wss:' : 'ws:') + '//' + window.location.host + url;
    } else {
      this._url = url;
    }

    this._state = 'Disconnected';
    this._stateTimeout = null;
    this._auth = null;
    this._subscriptionByChannelKey = {};
  }

  _createClass(_class, [{
    key: "_setState",
    value: function _setState(value) {
      if (this._state !== value) {
        console.debug("_setState():value=".concat(value));
        this._state = value;
        if (this._options.onStateChange) this._options.onStateChange(value);

        this._clearStateTimeout();
      }
    }
  }, {
    key: "connect",
    value: function connect(auth) {
      console.debug('WebSocketChannelClient.connect()');
      this._auth = auth;

      this._setState('Connecting');

      this._connecting();
    }
  }, {
    key: "_connecting",
    value: function _connecting() {
      var _this = this;

      if (this._state === 'Disconnected') return;

      this._setState('Connecting');

      var connectingStartMillis = new Date().getTime();

      if (this._webSocket) {
        try {
          this._webSocket.close();
        } catch (e) {}

        this._webSocket = null;
      }

      var hasReconnected = false;

      var reconnect = function reconnect(error) {
        if (hasReconnected) return;
        hasReconnected = true;
        console.debug("_connecting():reconnect():error=".concat(error));
        var elapsedMillis = new Date().getTime() - connectingStartMillis;
        var reconnectEveryMillis = _this._options.reconnectEveryMillis || 3000;

        if (elapsedMillis > reconnectEveryMillis) {
          _this._connecting();
        } else {
          var wait = reconnectEveryMillis - elapsedMillis;
          _this._stateTimeout = setTimeout(_this._connecting.bind(_this), wait);
        }
      };

      try {
        console.debug("_connecting():new WebSocket(".concat(this._url, ")"));
        this._webSocket = new WebSocket(this._url);
        this._webSocket.onmessage = this._onMessage.bind(this);
        this._webSocket.onopen = this._authenticating.bind(this);
        this._webSocket.onerror = reconnect.bind(this);
        this._webSocket.onclose = reconnect.bind(this);
      } catch (e) {
        reconnect(e);
      }
    }
  }, {
    key: "_authenticating",
    value: function _authenticating() {
      if (this._state === 'Disconnected') return;

      this._setState('Authenticating');

      var text = 'Authorization:' + (this._auth || '');

      var success = this._sendText(text);

      if (success) {
        var authenticateEveryMillis = this._options.authenticateEveryMillis || 3000;
        this._stateTimeout = setTimeout(this._authenticating.bind(this), authenticateEveryMillis);
      }
    }
  }, {
    key: "_subscribing",
    value: function _subscribing() {
      if (this._state === 'Disconnected') return;

      this._setState('Subscribing'); // Build data


      var data = [];

      for (var key in this._subscriptionByChannelKey) {
        var subscription = this._subscriptionByChannelKey[key];

        if (!subscription.sent) {
          data.push({
            channelKey: key,
            vars: subscription.vars
          });
        }
      } // Subscribe


      var success = true;

      if (data.length > 0) {
        success = this._sendText('Subscribe:' + JSON.stringify(data));
      }

      if (success) {
        this._markSubscriptionsSent(true);

        this._connected();
      }
    }
  }, {
    key: "_unsubscribing",
    value: function _unsubscribing(channelKey) {
      if (this._state === 'Disconnected') return;

      this._setState('Unsubscribing');

      var success = this._sendText('Unsubscribe:' + JSON.stringify(channelKey));

      if (success) {
        this._connected();
      }
    }
  }, {
    key: "_connected",
    value: function _connected() {
      this._setState('Connected');

      var elapsedMillis = new Date().getTime() - this._lastSendTextMillis;

      var heartbeatEveryMillis = this._options.heartbeatEveryMillis || 3000;

      if (elapsedMillis >= heartbeatEveryMillis) {
        this._sendText('!');

        this._connected();
      } else {
        var wait = Math.max(0, heartbeatEveryMillis - elapsedMillis);
        this._stateTimeout = setTimeout(this._connected.bind(this), wait);
      }
    }
  }, {
    key: "disconnect",
    value: function disconnect() {
      console.debug('WebSocketChannelClient.disconnect()');

      this._setState('Disconnected');

      if (this._webSocket != null) {
        try {
          this._webSocket.close();
        } catch (e) {}

        this._webSocket = null;
      }

      this._clearStateTimeout();

      for (var channelKey in this._subscriptionByChannelKey) {
        var subscription = this._subscriptionByChannelKey[channelKey];

        if (subscription.handlers) {
          for (var i = 0; i < subscription.handlers.length; i++) {
            subscription.handlers[i]('RESET');
          }
        }
      }
    }
  }, {
    key: "_clearStateTimeout",
    value: function _clearStateTimeout() {
      if (this._stateTimeout) {
        clearTimeout(this._stateTimeout);
        this._stateTimeout = null;
      }
    }
  }, {
    key: "_sendText",
    value: function _sendText(text) {
      console.debug("_sendText():text=".concat(text));

      try {
        this._webSocket.send(text);

        this._lastSendTextMillis = new Date().getTime();
        return true;
      } catch (e) {
        console.error(e);

        this._connecting();

        return false;
      }
    }
  }, {
    key: "_onMessage",
    value: function _onMessage(event) {
      var message = JSON.parse(event.data);
      console.debug("_onMessage():message.messageType=".concat(message.messageType));

      if (message.channelKey) {
        var subscription = this._subscriptionByChannelKey[message.channelKey];

        if (subscription.handlers) {
          for (var i = 0; i < subscription.handlers.length; i++) {
            subscription.handlers[i](message.messageType, message.data);
          }
        }
      } else if (message.messageType === 'AUTHENTICATED') {
        this._markSubscriptionsSent(false);

        this._subscribing();
      } else if (message.messageType === 'UNAUTHENTICATED') {
        if (this._options.onUnauthenticated) this._options.onUnauthenticated(message.data);
        this.disconnect();
      }
    }
  }, {
    key: "_markSubscriptionsSent",
    value: function _markSubscriptionsSent(value) {
      for (var key in this._subscriptionByChannelKey) {
        this._subscriptionByChannelKey[key].sent = value;
      }
    }
  }, {
    key: "_isVarsSame",
    value: function _isVarsSame(varsOld, varsNew) {
      if (!varsOld && !varsNew) return true;else if (!varsOld && varsNew) return false;else if (varsOld && !varsNew) return false;else if (Object.keys(varsOld).length !== Object.keys(varsNew).length) return false;else {
        for (var key in varsOld) {
          if (varsOld[key] !== varsNew[key]) return false;
        }

        return true;
      }
    }
  }, {
    key: "subscribe",
    value: function subscribe(options) {
      var channelKey = options.channel || 'default';
      var handlers = Array.isArray(options.handler) ? options.handler : [options.handler];
      var vars = options.vars;
      console.debug("WebSocketChannelClient.subscribe():channelKey=".concat(channelKey));
      var existingSubscription = this._subscriptionByChannelKey[channelKey];

      if (existingSubscription) {
        var isVarsSame = this._isVarsSame(existingSubscription.vars, vars);

        console.debug("WebSocketChannelClient.subscribe():isVarsSame=".concat(isVarsSame));
        if (isVarsSame) return;
      }

      this._removeSubscription(channelKey);

      this._addSubscription(channelKey, {
        vars: vars,
        handlers: handlers,
        sent: false
      });

      if (this._state === 'Connected') {
        this._subscribing();
      }

      if (this._options.onSubscriptionsUpdated) this._options.onSubscriptionsUpdated();
    }
  }, {
    key: "unsubscribe",
    value: function unsubscribe(channelKey) {
      console.debug("WebSocketChannelClient.unsubscribe():channelKey=".concat(channelKey));
      if (!channelKey) channelKey = 'default';

      this._removeSubscription(channelKey);

      this._unsubscribing(channelKey);

      if (this._options.onSubscriptionsUpdated) this._options.onSubscriptionsUpdated();
    }
  }, {
    key: "_addSubscription",
    value: function _addSubscription(channelKey, subscription) {
      this._subscriptionByChannelKey[channelKey] = subscription;
    }
  }, {
    key: "_removeSubscription",
    value: function _removeSubscription(channelKey) {
      delete this._subscriptionByChannelKey[channelKey];
    }
  }]);

  return _class;
}();

exports.default = _class;
module.exports = exports["default"];

/***/ })

/******/ });
});
//# sourceMappingURL=butterfly-client.js.map