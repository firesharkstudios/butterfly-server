import Vue from 'vue'
import App from './App.vue'
import router from './router'
import vuetify from './plugins/vuetify';

Vue.config.productionTip = false

import { ArrayDataEventHandler, WebSocketChannelClient } from 'butterfly-client'

new Vue({
  router,
  vuetify,
  render: h => h(App),
  data() {
    return {
      channelClient: null,
      channelClientState: null,
    }
  },
  methods: {
    callApi(url, rawData) {
      return fetch(url, {
        method: 'POST',
        body: JSON.stringify(rawData),
        mode: 'no-cors'
      });
    },
    subscribe(options) {
      let self = this;
      self.channelClient.subscribe({
        channel: options.channel,
        vars: options.vars,
        handler: new ArrayDataEventHandler({
          arrayMapping: options.arrayMapping,
          onInitialEnd: options.onInitialEnd,
          onChannelMessage: options.onChannelMessage
        }),
      });
    },
    unsubscribe(key) {
      let self = this;
      self.channelClient.unsubscribe(key);
    },
  },
  beforeMount() {
    let self = this;
    let url = `ws://localhost:8000/ws`;
    self.channelClient = new WebSocketChannelClient({
      url,
      onStateChange(value) {
        self.channelClientState = value;
      }
    });
    self.channelClient.connect();
  },
}).$mount('#app')
