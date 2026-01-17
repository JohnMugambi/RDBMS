using RDBMS.Core;
using RDBMS.Core.Execution;
using RDBMS.Core.Parsing;
using RDBMS.Core.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

//Cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "RDBMS API",
        Version = "v1",
        Description = "REST API for RDBMS"
    });
});

// Register RDBMS services
var dataDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".rdbms"
);

builder.Services.AddSingleton<StorageEngine>(sp => new StorageEngine(dataDirectory));

var app = builder.Build();

// Initialize database - create tasks table if it doesn't exist
InitializeDatabase(app.Services);

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowNextJs");

app.UseAuthorization();

app.MapControllers();

Console.WriteLine($"RDBMS API Starting...");
Console.WriteLine($"Data Directory: {dataDirectory}");

app.Run();



// Helper method to initialize database
void InitializeDatabase(IServiceProvider services)
{
    try
    {
        var storage = services.GetRequiredService<StorageEngine>();
        var tables = storage.GetAllTableNames();

        if (!tables.Contains("tasks"))
        {
            Console.WriteLine("Creating tasks table...");

            string createTableSql = @"
                CREATE TABLE tasks (
                    id INT PRIMARY KEY,
                    title VARCHAR(200) NOT NULL,
                    description VARCHAR(500),
                    completed BOOLEAN,
                    priority VARCHAR(20),
                    created_at DATETIME
                )
            ";

            var tokenizer = new Tokenizer(createTableSql);
            var tokens = tokenizer.Tokenize();
            var parser = new Parser(tokens);
            var query = parser.Parse();
            var executor = new QueryExecutor(storage);
            var result = executor.Execute(query);

            if (result.Success)
            {
                Console.WriteLine("Tasks table created successfully");

                // Create index on completed column
                string createIndexSql = "CREATE INDEX idx_completed ON tasks(completed)";
                tokenizer = new Tokenizer(createIndexSql);
                tokens = tokenizer.Tokenize();
                parser = new Parser(tokens);
                query = parser.Parse();
                result = executor.Execute(query);

                if (result.Success)
                {
                    Console.WriteLine("Index on 'completed' column created");
                }
            }
            else
            {
                Console.WriteLine($"Failed to create tasks table: {result.ErrorMessage}");
            }
        }
        else
        {
            Console.WriteLine("Tasks table already exists");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error initializing database: {ex.Message}");
    }
}