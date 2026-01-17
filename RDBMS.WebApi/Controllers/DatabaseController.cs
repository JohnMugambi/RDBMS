using Microsoft.AspNetCore.Mvc;
using RDBMS.Core.Execution;
using RDBMS.Core.Models;
using RDBMS.Core.Parsing;
using RDBMS.Core.Storage;
using RDBMS.WebApi.Models;
using System.Collections.Generic;
using System.Xml.Linq;

namespace RDBMS.WebApi.Controllers;

[ApiController]
[Route("api")]
public class DatabaseController : ControllerBase
{
    private readonly StorageEngine _storage;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(StorageEngine storage, ILogger<DatabaseController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <summary>
    /// Create a new task
    /// </summary>
    [HttpPost("tasks")]
    public IActionResult CreateTask([FromBody] TaskDto task)
    {
        try
        {
            if (task.Id == 0)
            {
                task.Id = GenerateNextId();
            }

            // Build INSERT SQL
            string sql = $@"
                INSERT INTO tasks (id, title, description, completed, priority, created_at)
                VALUES ({task.Id}, '{EscapeSql(task.Title)}', '{EscapeSql(task.Description ?? "")}', 
                        {(task.Completed ? "TRUE" : "FALSE")}, '{EscapeSql(task.Priority ?? "")}', 
                        '{task.CreatedAt:yyyy-MM-dd HH:mm:ss}')
            ";

            var result = ExecuteSql(sql);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage ?? "Failed to create task"));
            }

            return CreatedAtAction(
                nameof(GetTask),
                new { id = task.Id },
                ApiResponse<TaskDto>.SuccessResponse(task, "Task created successfully", 1)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get all tasks
    /// </summary>
    [HttpGet("tasks")]
    public IActionResult GetTasks()
    {
        try
        {
            var result = ExecuteSql("SELECT * FROM tasks");

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage ?? "Failed to get tasks"));
            }

            var tasks = result.Data?.Select(row => new TaskDto
            {
                Id = Convert.ToInt32(row["id"]),
                Title = row["title"]?.ToString() ?? "",
                Description = row["description"]?.ToString(),
                Completed = Convert.ToBoolean(row["completed"]),
                Priority = row["priority"]?.ToString(),
                CreatedAt = Convert.ToDateTime(row["created_at"])
            }).ToList() ?? new List<TaskDto>();

            return Ok(ApiResponse<List<TaskDto>>.SuccessResponse(tasks, null, tasks.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tasks");
            return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get a single task by ID
    /// </summary>
    [HttpGet("tasks/{id}")]
    public IActionResult GetTask(int id)
    {
        try
        {
            var result = ExecuteSql($"SELECT * FROM tasks WHERE id = {id}");

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage ?? "Failed to get task"));
            }

            if (result.Data == null || result.Data.Count == 0)
            {
                return NotFound(ApiResponse<object>.ErrorResponse($"Task with ID {id} not found"));
            }

            var row = result.Data[0];
            var task = new TaskDto
            {
                Id = Convert.ToInt32(row["id"]),
                Title = row["title"]?.ToString() ?? "",
                Description = row["description"]?.ToString(),
                Completed = Convert.ToBoolean(row["completed"]),
                Priority = row["priority"]?.ToString(),
                CreatedAt = Convert.ToDateTime(row["created_at"])
            };

            return Ok(ApiResponse<TaskDto>.SuccessResponse(task));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task {TaskId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Update a task
    /// </summary>
    [HttpPut("tasks/{id}")]
    public IActionResult UpdateTask(int id, [FromBody] TaskDto task)
    {
        try
        {
            // Build UPDATE SQL
            string sql = $@"
                UPDATE tasks 
                SET title = '{EscapeSql(task.Title)}',
                    description = '{EscapeSql(task.Description ?? "")}',
                    completed = {(task.Completed ? "TRUE" : "FALSE")},
                    priority = '{EscapeSql(task.Priority ?? "")}'
                WHERE id = {id}
            ";

            var result = ExecuteSql(sql);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage ?? "Failed to update task"));
            }

            if (result.RowsAffected == 0)
            {
                return NotFound(ApiResponse<object>.ErrorResponse($"Task with ID {id} not found"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                new { id, updated = true },
                "Task updated successfully",
                result.RowsAffected
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task {TaskId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Delete a task
    /// </summary>
    [HttpDelete("tasks/{id}")]
    public IActionResult DeleteTask(int id)
    {
        try
        {
            var result = ExecuteSql($"DELETE FROM tasks WHERE id = {id}");

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage ?? "Failed to delete task"));
            }

            if (result.RowsAffected == 0)
            {
                return NotFound(ApiResponse<object>.ErrorResponse($"Task with ID {id} not found"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                new { id, deleted = true },
                "Task deleted successfully",
                result.RowsAffected
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task {TaskId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Execute custom SQL query
    /// </summary>
    [HttpPost("execute")]
    public IActionResult ExecuteCustomSql([FromBody] SqlQueryRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Sql))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("SQL query is required"));
            }

            var result = ExecuteSql(request.Sql);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(result.ErrorMessage ?? "Query execution failed"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                new
                {
                    data = result.Data,
                    columns = result.ColumnNames
                },
                result.Message,
                result.RowsAffected
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing SQL: {Sql}", request.Sql);
            return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// List all tables in the database
    /// </summary>
    [HttpGet("tables")]
    public IActionResult GetTables()
    {
        try
        {
            var tables = _storage.GetAllTableNames();
            return Ok(ApiResponse<object>.SuccessResponse(
                new { tables },
                null,
                tables.Count
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tables");
            return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get schema for a specific table
    /// </summary>
    [HttpGet("tables/{tableName}/schema")]
    public IActionResult GetTableSchema(string tableName)
    {
        try
        {
            var table = _storage.GetTable(tableName);

            if (table == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse($"Table '{tableName}' not found"));
            }

            var schema = new
            {
                name = table.Name,
                columns = table.Columns.Select(c => new
                {
                    name = c.Name,
                    type = c.Type.ToString(),
                    maxLength = c.MaxLength,
                    isPrimaryKey = c.IsPrimaryKey,
                    isUnique = c.IsUnique,
                    isNotNull = c.IsNotNull
                }),
                indexes = table.Indexes.Select(i => new
                {
                    name = i.Name,
                    columnName = i.ColumnName
                }),
                rowCount = table.Rows.Count
            };

            return Ok(ApiResponse<object>.SuccessResponse(schema));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schema for table {TableName}", tableName);
            return StatusCode(500, ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    // Helper method to execute SQL
    private QueryResult ExecuteSql(string sql)
    {
        var tokenizer = new Tokenizer(sql);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var query = parser.Parse();
        var executor = new QueryExecutor(_storage);
        return executor.Execute(query);
    }

    // Helper method to escape SQL strings
    private string EscapeSql(string? value)
    {
        if (value == null) return "";
        return value.Replace("'", "''");
    }

    //dictionary keys are prefixed with the table name.
    private object? GetColumnValue(Dictionary<string, object?> row, string columnName, string tableName = "tasks")
    {
        var value = row.ContainsKey(columnName) ? row[columnName] : row[$"{tableName}.{columnName}"];

        // Handle JsonElement
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => jsonElement.GetString(),
                System.Text.Json.JsonValueKind.Number => jsonElement.GetInt32(),
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                System.Text.Json.JsonValueKind.Null => null,
                _ => jsonElement.ToString()
            };
        }

        return value;
    }

    private int GenerateNextId()
    {
        var result = ExecuteSql("SELECT * FROM tasks");
        if (result.Data == null || result.Data.Count == 0)
            return 1;

        var maxId = result.Data
            .Select(row => Convert.ToInt32(row["id"]))
            .Max();

        return maxId + 1;
    }
}