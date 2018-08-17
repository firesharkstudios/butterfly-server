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

  return function (messageType, data) {
    if (messageType == 'RESET') {
      for (var arrayKey in config.arrayMapping) {
        var array = config.arrayMapping[arrayKey];
        if (array) array.splice(0, array.length);
      }
    } else if (messageType == 'DATA-EVENT-TRANSACTION') {
      var dataEventTransaction = data;

      for (var i = 0; i < dataEventTransaction.dataEvents.length; i++) {
        var dataEvent = dataEventTransaction.dataEvents[i]; //console.debug('ArrayDataEventHandler.handle():dataEvent.type=' + dataEvent.dataEventType + ',name=', dataEvent.name + ',keyValue=' + dataEvent.keyValue);

        if (dataEvent.dataEventType == 'InitialEnd') {
          if (config.onInitialEnd) config.onInitialEnd();
        } else {
          var _array = config.arrayMapping[dataEvent.name];

          if (!_array) {
            console.error('No mapping for data event \'' + dataEvent.name + '\'');
          } else if (dataEvent.dataEventType == 'InitialBegin') {
            _array.splice(0, _array.length);

            keyFieldNamesByName[dataEvent.name] = dataEvent.keyFieldNames;
          } else if (dataEvent.dataEventType == 'Insert' || dataEvent.dataEventType == 'Initial') {
            (function () {
              var keyValue = _private.getKeyValue(dataEvent.name, dataEvent.record);

              var index = _array.findIndex(function (x) {
                return x._keyValue == keyValue;
              });

              if (index >= 0) {
                console.error('Duplicate key \'' + keyValue + '\' in table \'' + dataEvent.name + '\'');
              } else {
                dataEvent.record['_keyValue'] = keyValue;

                _array.splice(_array.length, 0, dataEvent.record);
              }
            })();
          } else if (dataEvent.dataEventType == 'Update') {
            (function () {
              var keyValue = _private.getKeyValue(dataEvent.name, dataEvent.record);

              var index = _array.findIndex(function (x) {
                return x._keyValue == keyValue;
              });

              if (index == -1) {
                console.error('Could not find key \'' + keyValue + '\' in table \'' + dataEvent.name + '\'');
              } else {
                dataEvent.record['_keyValue'] = keyValue;

                _array.splice(index, 1, dataEvent.record);
              }
            })();
          } else if (dataEvent.dataEventType == 'Delete') {
            (function () {
              var keyValue = _private.getKeyValue(dataEvent.name, dataEvent.record);

              var index = _array.findIndex(function (x) {
                return x._keyValue == keyValue;
              });

              _array.splice(index, 1);
            })();
          }
        }
      }
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
 * Usage...
 *  start() must be called with a valid authorization before calling subscribe()/unsubscribe()
 *  subscribe()/unsubscribe() may be called any time after start()
 */
var _class =
/*#__PURE__*/
function () {
  function _class(options) {
    _classCallCheck(this, _class);

    this._options = options;
    var url = this._options.url;

    if (url.indexOf('://') == -1) {
      this._url = (window.location.protocol == 'https:' ? 'wss:' : 'ws:') + '//' + window.location.host + url;
    } else {
      this._url = url;
    }

    console.debug('WebSocketChannelClient():url=' + url);
    this._status = null;
    this._auth = null;
    this._subscriptionByChannelKey = {};
    this._queuedMessages = [];
  }

  _createClass(_class, [{
    key: "_setStatus",
    value: function _setStatus(value) {
      if (this._status != value) {
        this._status = value;
        if (this._options.onStatusChange) this._options.onStatusChange(value);
      }
    }
  }, {
    key: "_queue",
    value: function _queue(text) {
      console.debug("_queue():text=".concat(text));

      this._queuedMessages.push(text);

      this._sendQueue();
    }
  }, {
    key: "_sendQueue",
    // Called every 3 seconds while status!='Stopped'
    value: function _sendQueue() {
      console.debug("_sendQueue():_webSocketReady=".concat(this._webSocketReady));
      if (this._sendQueueTimeout) clearTimeout(this._sendQueueTimeout);

      if (this._webSocketReady) {
        do {
          var hasMessage = this._queuedMessages.length > 0;
          var text = hasMessage ? this._queuedMessages[0] : '!';

          try {
            console.debug("_sendQueue():text=".concat(text));

            this._webSocket.send(text);
          } catch (e) {
            this._webSocket = null;
          }

          if (this._webSocket && hasMessage) {
            this._queuedMessages.shift();
          }
        } while (this._queuedMessages.length > 0);
      }

      if (this.status != 'Stopped') {
        this._sendQueueTimeout = setTimeout(this._sendQueue.bind(this), this._options.sendQueueEveryMillis || 3000);
      }
    }
  }, {
    key: "_onMessage",
    value: function _onMessage(event) {
      console.debug("_onMessage():event.data=".concat(event.data));
      var message = JSON.parse(event.data);

      if (message.channelKey) {
        var subscription = this._subscriptionByChannelKey[message.channelKey];

        if (subscription.handlers) {
          for (var i = 0; i < subscription.handlers.length; i++) {
            subscription.handlers[i](message.messageType, message.data);
          }
        }
      } else if (message.messageType == 'AUTHENTICATED') {
        this._setStatus('Started');
      } else if (message.messageType == 'UNAUTHENTICATED') {
        this.stop();
      }
    } // Called every 3 seconds until webSocket!=null

  }, {
    key: "_setupConnection",
    value: function _setupConnection() {
      var _this = this;

      this._setStatus('Starting');

      if (!this._webSocket) {
        if (this._sendQueueTimeout) clearTimeout(this._sendQueueTimeout);

        try {
          console.debug("_setupConnection():new WebSocket(".concat(this._url, ")"));
          this._webSocket = new WebSocket(this._url);
          this._webSocket.onmessage = this._onMessage.bind(this);

          this._webSocket.onopen = function () {
            console.debug('_webSocket.onopen()');

            _this._sendQueue();
          };

          this._webSocket.onerror = function (error) {
            return _this._webSocket = null;
          };

          this._webSocket.onclose = function () {
            return _this._webSocket = null;
          };

          console.debug("_setupConnection():success");
        } catch (e) {
          console.debug(e);
          this._webSocket = null;
          this._setupConnectionTimeout = setTimeout(this._setupConnection.bind(this), this._options.setupConnectionEveryMillis || 3000);
        }

        if (this._webSocket) {
          this._sendQueue();
        }
      }
    }
  }, {
    key: "_queueAuthorization",
    value: function _queueAuthorization(auth) {
      this._auth = auth;

      this._queue('Authorization:' + (this._auth || ''));
    }
  }, {
    key: "_queueSubscribe",
    value: function _queueSubscribe() {
      var data = [];

      for (var key in this._subscriptionByChannelKey) {
        var subscription = this._subscriptionByChannelKey[key];

        if (!subscription.sent) {
          data.push({
            channelKey: key,
            vars: subscription.vars
          });
        }
      }

      if (data.length > 0) {
        this._queue('Subscribe:' + JSON.stringify(data));

        this._markSubscriptionsQueued(true);
      }
    }
  }, {
    key: "_addSubscription",
    value: function _addSubscription(channelKey, subscription) {
      this._subscriptionByChannelKey[channelKey] = subscription;

      this._queueSubscribe();
    }
  }, {
    key: "_queueUnsubscribe",
    value: function _queueUnsubscribe(channelKey) {
      this._queue('Unsubscribe:' + JSON.stringify(channelKey));
    }
  }, {
    key: "_markSubscriptionsQueued",
    value: function _markSubscriptionsQueued(value) {
      for (var key in this._subscriptionByChannelKey) {
        var subscription = this._subscriptionByChannelKey[key];
        subscription.sent = value;
      }
    }
  }, {
    key: "_removeSubscription",
    value: function _removeSubscription(channelKey) {
      delete this._subscriptionByChannelKey[channelKey];
    }
  }, {
    key: "start",
    value: function start(auth) {
      console.debug('WebSocketChannelClient.start()');
      this._queuedMessages = [];
      this._subscriptionByChannelKey = {};

      this._setupConnection();

      this._queueAuthorization(auth);
    }
  }, {
    key: "subscribe",
    value: function subscribe(handler, channelKey, vars) {
      console.debug("WebSocketChannelClient.subscribe():channelKey=".concat(channelKey));
      if (!channelKey) channelKey = 'default';

      this._removeSubscription(channelKey);

      this._addSubscription(channelKey, {
        vars: vars,
        handlers: Array.isArray(handler) ? handler : [handler],
        sent: false
      });

      if (this._options.onSubscriptionsUpdated) this._options.onSubscriptionsUpdated();
    }
  }, {
    key: "unsubscribe",
    value: function unsubscribe(channelKey) {
      console.debug("WebSocketChannelClient.unsubscribe():channelKey=".concat(channelKey));
      if (!channelKey) channelKey = 'default';

      this._removeSubscription(channelKey);

      this._queueUnsubscribe(channelKey);

      if (this._options.onSubscriptionsUpdated) this._options.onSubscriptionsUpdated();
    }
  }, {
    key: "stop",
    value: function stop() {
      console.debug('WebSocketChannelClient.stop()');

      this._setStatus('Stopped');

      if (this._webSocket != null) {
        this._webSocket.close();

        this._webSocket = null;
      }

      if (this._sendQueueTimeout) clearTimeout(this._sendQueueTimeout);
      if (this._setupConnectionTimeout) clearTimeout(this._setupConnectionTimeout);

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
    key: "status",
    get: function get() {
      return this._status;
    }
  }, {
    key: "_webSocketReady",
    get: function get() {
      return this._webSocket && this._webSocket.readyState == 1;
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