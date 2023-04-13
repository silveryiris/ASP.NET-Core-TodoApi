using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var todoItems = app.MapGroup("/todoitems");

app.MapGet("/", () => "Hello World!").WithSummary("health check").WithOpenApi();

todoItems.MapGet("/", GetAllTodos).WithSummary("Get all todo items").WithOpenApi();

todoItems.MapGet("/complete", GetCompleteTodos).WithSummary("Get all completed todo items").WithOpenApi();

todoItems.MapGet("/{id}", GetTodo).WithSummary("Get todo item by id").WithOpenApi();

todoItems.MapPost("/", CreateTodo).WithSummary("Create new todo item").WithOpenApi();

todoItems.MapPut("/{id}", UpdateTodo).WithSummary("Update todo item by id").WithOpenApi();

todoItems.MapDelete("/{id}", DeleteTodo).WithSummary("Delete todo item by id").WithOpenApi();


app.Run();

static async Task<IResult> GetAllTodos(TodoDb db)
{
    return TypedResults.Ok(await db.Todos.Select(x => new TodoItemDTO(x)).ToArrayAsync());
}

static async Task<IResult> GetCompleteTodos(TodoDb db)
{
    return TypedResults.Ok(await db.Todos.Where(t => t.IsComplete).Select(x => new TodoItemDTO(x)).ToArrayAsync());
}

static async Task<IResult> GetTodo(int id, TodoDb db)
{
    return await db.Todos.FindAsync(id)
        is Todo todo
            ? TypedResults.Ok(new TodoItemDTO(todo))
            : TypedResults.NotFound();
}

static async Task<IResult> CreateTodo(TodoItemDTO todoItemDTO, TodoDb db)
{
    var todo = new Todo
    {
        IsComplete = todoItemDTO.IsComplete,
        Name = todoItemDTO.Name,
    };

    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    todoItemDTO = new TodoItemDTO(todo);

    return TypedResults.Created($"todoitems/{todoItemDTO.Id}", todoItemDTO);
}

static async Task<IResult> UpdateTodo(int id, Todo todo, TodoDb db)
{
    var savedTodo = await db.Todos.FindAsync(id);

    if (savedTodo is null) return TypedResults.NotFound();

    savedTodo.Name = todo.Name;
    savedTodo.IsComplete = todo.IsComplete;

    await db.SaveChangesAsync();

    return TypedResults.NoContent();
}

static async Task<IResult> DeleteTodo(int id, TodoDb db)
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return TypedResults.Ok(todo);
    }

    return TypedResults.NotFound();
}