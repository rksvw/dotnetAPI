using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.FileSystemGlobbing;
using System;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());

var app = builder.Build();

var todos = new List<Todo>();

// MIDDLEWARE BUILD-IN
app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

// MIDDLEWARE CUSTOM
app.Use(async (context, next) => {
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] || Started");
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] || Finished");
});


// Get request to get all todos list
app.MapGet("/todos", (ITaskService service) => service.GetTodos());

// Get request to get specific todos list data
app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService service) => {
    var targetTodo = service.GetTodoById(id);
    return targetTodo is null ? TypedResults.NotFound() : TypedResults.Ok(targetTodo);
});

// Post request to add todos data in list
app.MapPost("/todos", (Todo task, ITaskService service) => {
    service.AddTodo(task);
    return TypedResults.Created("/todos/{id}", task);
}).AddEndpointFilter(async (context, next) => {
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();

    if (taskArgument.DueDate < DateTime.UtcNow) {
        errors.Add(nameof(Todo.DueDate), ["Cannot have due date in the past."]);
    }
    if (taskArgument.IsCompleted)
    {
        errors.Add(nameof(Todo.IsCompleted), ["Cannot add completed todo."]);
    }
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }
    return await next(context);
});

// Delete requests to remove the specific data
app.MapDelete("/todos/{id}", (int id, ITaskService service) => {
    service.DeleteTodoById(id);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);

interface ITaskService
{
    Todo? GetTodoById(int Id);
    List<Todo> GetTodos();
    void DeleteTodoById(int Id);
    Todo AddTodo(Todo task);
}

class InMemoryTaskService : ITaskService
{
    private readonly List<Todo> _todos = [];
    public Todo AddTodo(Todo task)
    {
        _todos.Add(task);
        return task;
    }

    public void DeleteTodoById(int Id)
    {
        _todos.RemoveAll(task => Id == task.Id);
    }

    public Todo? GetTodoById(int Id)
    {
        return _todos.SingleOrDefault(t => Id == t.Id);
    }

    public List<Todo> GetTodos()
    {
        return _todos;
    }
}