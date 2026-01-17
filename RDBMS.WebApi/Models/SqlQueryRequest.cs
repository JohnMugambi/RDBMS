namespace RDBMS.WebApi.Models;

/// <summary>
/// Request model for executing custom SQL queries
/// </summary>
public class SqlQueryRequest
{
    public string Sql { get; set; } = string.Empty;
}