import Vue from 'vue'
import Vuetify from 'vuetify'
import 'vuetify/dist/vuetify.css'

import App from './App'
import router from './router'

import { ArrayDataEventHandler, WebSocketChannelClient } from 'butterfly-client'

Vue.use(Vuetify)
if (!process.env.IS_WEB) Vue.use(require('vue-electron'))
Vue.config.productionTip = false

/* eslint-disable no-new */
new Vue({
  components: { App },
  router,
  template: '<App/>',
    data() {
        return {
            channelClient: null,
            channelClientState: null,
        }
    },
    methods: {
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
