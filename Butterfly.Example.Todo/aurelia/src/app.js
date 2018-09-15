export class App {
  constructor() {
    this.message = 'Butterfly Server .NET Aurelia Todo Example';
    this.todoName = "A new todo item"
  }

  addTodo() {
    return fetch("http://localhost:8000/api/todo/insert", {
      method: "POST",
      body: JSON.stringify({ name: this.todoName }),
      mode: "no-cors"
    });
  }
}
