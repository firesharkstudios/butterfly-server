export class App {
  constructor() {
    this.message = 'Butterfly Server .NET Aurelia Todo Example';
    this.todoName = 'A new todo item';
  }

  addTodo() {
    return fetch('http://localhost:8000/api/todo', {
      method: 'POST',
      headers:{
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ name: this.todoName })
    });
  }
}
