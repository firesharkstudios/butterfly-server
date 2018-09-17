// The Vue build version to load with the `import` command
// (runtime-only or standalone) has been set in webpack.base.conf with an alias.
import Vue from 'vue'
import Vuetify from 'vuetify'
import 'vuetify/dist/vuetify.css'
import VueCordova from 'vue-cordova'
import VueHead from 'vue-head'

import App from './App'
import router from './router'

import { ArrayDataEventHandler, WebSocketChannelClient } from 'butterfly-client'

Vue.use(Vuetify)
Vue.config.productionTip = false
Vue.use(VueCordova)
Vue.use(VueHead)

// add cordova.js only if serving the app through file://
if (window.location.protocol === 'file:' || window.location.port === '3000') {
  var cordovaScript = document.createElement('script')
  cordovaScript.setAttribute('type', 'text/javascript')
  cordovaScript.setAttribute('src', 'cordova.js')
  document.body.appendChild(cordovaScript)
}

/* eslint-disable no-new */
new Vue({
  el: '#app',
  router,
  template: '<App/>',
  components: { App },
  head: {
    meta: [
      {
        name: 'viewport',
        content: 'width=device-width, initial-scale=1, minimum-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover'
      }
    ]
  },
  data() {
    return {
      channelClient: null,
      channelClientState: null,
    }
  },
  methods: {
    // Replace 'localhost' with your DHCP assigned address
    // (localhost and 127.0.0.1 do not work in Android emulator)
    callApi(url, rawData) {
      let fullUrl = `http://localhost:8000${url}`;
      return fetch(fullUrl, {
        method: 'POST',
        body: JSON.stringify(rawData),
        mode: 'no-cors'
      });
    },
    subscribe(options) {
      let self = this;
      self.channelClient.subscribe({
        channel: options.key,
        vars: options.vars,
        handler: new ArrayDataEventHandler({
          arrayMapping: options.arrayMapping,
          onInitialEnd: options.onInitialEnd,
          onChannelMessage: options.onChannelMessage
        })
      });
    },
    unsubscribe(key) {
      let self = this;
      self.channelClient.unsubscribe(key);
    },
  },
  beforeMount() {
    let self = this;

    // Replace 'localhost' with your DHCP assigned address
    // (localhost and 127.0.0.1 do not work in Android emulator)
    let url = `ws://localhost:8000/ws`;
    self.channelClient = new WebSocketChannelClient({
      url,
      onStateChange(value) {
        self.channelClientState = value;
      }
    });
    self.channelClient.connect();
  },
})
