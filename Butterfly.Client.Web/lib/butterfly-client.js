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
exports.default = _default;

function _default(options) {
  var _private = this;

  var url = options.url;

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
  };

  _private.testConnection = function () {
    if (!_private.webSocket) {
      try {
        _private.setStatus('Starting');

        _private.webSocket = new WebSocket(url);

        _private.webSocket.onmessage = function (event) {
          var message = JSON.parse(event.data);

          if (message.channelKey) {
            var subscription = _private.subscriptionByChannelKey[message.channelKey];

            if (subscription.handlers) {
              for (var i = 0; i < subscription.handlers.length; i++) {
                subscription.handlers[i](message.messageType, message.data);
              }
            }
          } else if (message.messageType == 'AUTHENTICATED') {
            _private.setStatus('Started');

            _private.markSubscriptionSent(false);

            _private.sendSubscriptions();
          } else if (message.messageType == 'UNAUTHENTICATED') {
            _public.stop();
          }
        };

        _private.webSocket.onopen = function () {
          _private.sendAuthorization();
        };

        _private.webSocket.onerror = function (error) {
          _private.webSocket = null;
        };

        _private.webSocket.onclose = function () {
          _private.webSocket = null;
        };
      } catch (e) {
        console.debug(e);
        _private.webSocket = null;
      }
    } else if (_private.webSocket.readyState == 1) {
      //console.debug('testConnection():_private.webSocket.readyState=' + _private.webSocket.readyState);
      try {
        _private.webSocket.send('!');

        console.debug('WebSocketChannelClient.testConnection():heartbeat success');
      } catch (e) {
        _private.webSocket = null;
      }
    } else {
      _private.webSocket = null;
    }

    if (_public.status != 'Stopped') {
      _private.testConnectionTimeout = setTimeout(function () {
        _private.testConnection();
      }, options.testConnectionEveryMillis || 3000);
    }
  };

  _private.sendAuthorization = function () {
    if (_private.webSocket && _private.webSocket.readyState == 1) {
      _private.webSocket.send('Authorization:' + (_private.auth || ''));
    }
  };

  _private.sendSubscriptions = function () {
    if (_private.webSocket && _private.webSocket.readyState == 1 && _public.status == 'Started') {
      var data = [];

      for (var key in _private.subscriptionByChannelKey) {
        var subscription = _private.subscriptionByChannelKey[key];

        if (!subscription.sent) {
          data.push({
            channelKey: key,
            vars: subscription.vars
          });
        }
      }

      if (data.length > 0) {
        var text = 'Subscribe:' + JSON.stringify(data);
        console.debug('WebSocketChannelClient.sendSubscriptions():text=' + text);

        _private.webSocket.send(text);

        _private.markSubscriptionSent(true);
      }
    }
  };

  _private.addSubscription = function (channelKey, subscription) {
    _private.subscriptionByChannelKey[channelKey] = subscription;

    _private.sendSubscriptions();
  };

  _private.sendUnsubscribe = function (channelKey) {
    if (_private.webSocket && _private.webSocket.readyState == 1) {
      var text = 'Unsubscribe:' + JSON.stringify(channelKey);

      _private.webSocket.send(text);
    }
  };

  _private.markSubscriptionSent = function (value) {
    for (var key in _private.subscriptionByChannelKey) {
      var subscription = _private.subscriptionByChannelKey[key];
      subscription.sent = value;
    }
  };

  _private.removeSubscription = function (channelKey) {
    delete _private.subscriptionByChannelKey[channelKey];
  };

  var _public = {
    status: null,
    start: function start(auth) {
      console.debug('WebSocketChannelClient.start()');
      _private.auth = auth;

      _private.markSubscriptionSent(false);

      _private.sendAuthorization();

      _private.testConnection();
    },
    subscribe: function subscribe(handler, channelKey, vars) {
      console.debug("WebSocketChannelClient.subscribe():channelKey=".concat(channelKey));
      if (!channelKey) channelKey = 'default';

      _private.removeSubscription(channelKey);

      _private.addSubscription(channelKey, {
        vars: vars,
        handlers: Array.isArray(handler) ? handler : [handler],
        sent: false
      });

      if (options.onSubscriptionsUpdated) options.onSubscriptionsUpdated();
    },
    unsubscribe: function unsubscribe(channelKey) {
      console.debug("WebSocketChannelClient.unsubscribe():channelKey=".concat(channelKey));
      if (!channelKey) channelKey = 'default';

      _private.removeSubscription(channelKey);

      _private.sendUnsubscribe(channelKey);

      if (options.onSubscriptionsUpdated) options.onSubscriptionsUpdated();
    },
    stop: function stop() {
      console.debug('WebSocketChannelClient.stop()');

      _private.setStatus('Stopped');

      _private.webSocket.close();

      _private.webSocket = null;
      clearTimeout(_private.testConnectionTimeout);

      for (var channelKey in _private.subscriptionByChannelKey) {
        var subscription = _private.subscriptionByChannelKey[channelKey];

        if (subscription.handlers) {
          for (var i = 0; i < subscription.handlers.length; i++) {
            subscription.handlers[i]('RESET');
          }
        }
      }
    }
  };
  return _public;
}

module.exports = exports["default"];

/***/ })

/******/ });
});
//# sourceMappingURL=butterfly-client.js.map