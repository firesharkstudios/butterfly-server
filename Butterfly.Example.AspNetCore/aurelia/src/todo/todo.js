import { ArrayDataEventHandler, WebSocketChannelClient } from 'butterfly-client';

export class Todo {
  constructor() {
    this.todoList = [];
    this.channel = 'todos';
    this.channelClient = null;
    this.channelClientState = null;
    this.connect();
    this.subscribe();
  }
  
  connect() {
    let url = 'ws://localhost:8000/ws';
    this.channelClient = new WebSocketChannelClient({
      url,
      onStateChange(value) {
        this.channelClientState = value;
      }
    });

    this.channelClient.connect();
  }

  subscribe() {
    this.channelClient.subscribe({
      channel: this.channel,
      vars: {
        clientName: 'AureliaWebClient'
      },
      handler: new ArrayDataEventHandler({
        arrayMapping: {
          todo: this.todoList
        }
      })
    });
  }

  unsubscribe() {
    this.channelClient.unsubscribe(this.channel);
  }

  removeTodo(todo) {
    return fetch('http://localhost:8000/api/todo', {
      method: 'DELETE',
      headers:{
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(todo.id)
    });
  }
}
