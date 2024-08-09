using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var todos = new List<Todo>();

// Get request to get all todos list
app.MapGet("/todos", () => todos);

// Get request to get specific todos list data
app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) => {
    var targetTodo = todos.SingleOrDefault(t => id == t.Id);
    return targetTodo is null ? TypedResults.NotFound() : TypedResults.Ok(targetTodo);
});

// Post request to add todos data in list
app.MapPost("/todos", (Todo task) => {
    todos.Add(task);
    return TypedResults.Created("/todos/{id}", task);
});

// Delete requests to remove the specific data
app.MapDelete("/todos/{id}", (int id) => {
    todos.RemoveAll(t => id == t.Id);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);

