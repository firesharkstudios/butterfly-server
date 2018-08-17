// The Vue build version to load with the `import` command
// (runtime-only or standalone) has been set in webpack.base.conf with an alias.
import Vue from 'vue'
import App from './App'
import router from './router'
import Vuetify from 'vuetify'
import 'vuetify/dist/vuetify.min.css'

import { ArrayDataEventHandler, WebSocketChannelClient } from 'butterfly-client'

import reqwest from 'reqwest'

Vue.use(Vuetify)

Vue.config.productionTip = false

/* eslint-disable no-new */
new Vue({
  el: '#app',
  router,
  components: { App },
  template: '<App/>',
  data() {
    return {
      channelClient: null,
      status: null,
    }
  },
  methods: {
    callApi(url, rawData) {
      return reqwest({
        url,
        method: 'POST',
        data: JSON.stringify(rawData),
      });
    },
    subscribe(options) {
      let self = this;
      self.channelClient.subscribe(
        new ArrayDataEventHandler({
          arrayMapping: options.arrayMapping,
          onInitialEnd: options.onInitialEnd,
          onChannelMessage: options.onChannelMessage
        }),
        options.key,
        options.vars,
      );
    },
    unsubscribe(key) {
      let self = this;
      self.channelClient.unsubscribe(key);
    },
  },
  beforeMount() {
    let self = this;

    // Setup channel client
    let url = `ws://${window.location.host}/ws`;
    self.channelClient = new WebSocketChannelClient({
      url,
      onStatusChange(value) {
        self.status = value;
      }
    });
    self.channelClient.start('Custom My-User-Id');
  },
})
