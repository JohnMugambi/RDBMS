using RDBMS.Core.Models;
using RDBMS.Core.Storage;
using RDBMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDBMS.Core;

/// <summary>
/// Main database class - the entry point for all database operations
/// Coordinates storage and will coordinate query execution later
/// </summary>
/// 
public class Database
{
    private readonly StorageEngine _storageEngine;
    private readonly string _name;

    public string Name => _name;
    public string DataDirectory { get; }

    public Database(string name, string? dataDirectory = null)
    {
        _name = name;
        DataDirectory = dataDirectory ?? System.IO.Path.Combine(Directory.GetCurrentDirectory(),"data", name);

        //ensure dir is there
        if (!Directory.Exists(DataDirectory))
        {
            Directory.CreateDirectory(DataDirectory);
        }

        _storageEngine = new StorageEngine(DataDirectory);
    }

    #region Table management    

    /// <summary>
    /// Creates a new table
    /// </summary>
    /// 
    public void CreateTable(string tableName, List<Column> columns)
    {
        var table = new Table(tableName);
        table.Columns.AddRange(columns);

        _storageEngine.CreateTable(table);
    }

    /// <summary>
    /// Drops a table
    /// </summary>
    public void DropTable(string tableName)
    {
        _storageEngine.DropTable(tableName);
    }

    /// <summary>
    /// Gets a table by name
    /// </summary>
    public Table GetTable(string tableName)
    {
        return _storageEngine.GetTable(tableName);
    }

    /// <summary>
    /// Checks if a table exists
    /// </summary>
    public bool TableExists(string tableName)
    {
        return _storageEngine.TableExists(tableName);
    }

    /// <summary>
    /// Lists all tables in the database
    /// </summary>
    public List<string> ListTables()
    {
        return _storageEngine.ListTables();
    }

    #endregion

    #region Index Management


    /// <summary>
    /// Creates an index on a table column
    /// </summary>
    /// 
    public void CreateIndex(string tableName, string indexName, string columnName)
    {

        _storageEngine.CreateIndex(tableName, indexName, columnName);
    }

    /// <summary>
    /// Drops an index
    /// </summary>
    public void DropIndex(string tableName, string indexName)
    {
        _storageEngine.DropIndex(tableName, indexName);
    }

    #endregion

    #region Data Operations (Direct - will be replaced by SQL execution later)

    /// <summary>
    /// Inserts a row into a table
    /// </summary>
    public void InsertRow(string tableName, Row row)
    {
        _storageEngine.InsertRow(tableName, row);
    }

    /// <summary>
    /// Inserts a row using column-value pairs
    /// </summary>
    public void InsertRow(string tableName, Dictionary<string, object?> values)
    {
        var row = new Row(values);
        _storageEngine.InsertRow(tableName, row);
    }

    /// <summary>
    /// Selects all rows from a table
    /// </summary>
    public List<Row> SelectAll(string tableName)
    {
        var table = GetTable(tableName);
        return new List<Row>(table.Rows);
    }

    /// <summary>
    /// Selects rows matching a predicate
    /// </summary>
    public List<Row> Select(string tableName, Func<Row, bool> predicate)
    {
        var table = GetTable(tableName);
        return table.Rows.Where(predicate).ToList();
    }

    /// <summary>
    /// Updates rows matching a predicate
    /// </summary>
    public int Update(string tableName, Func<Row, bool> predicate, Action<Row> updateAction)
    {
        return _storageEngine.UpdateRows(tableName, predicate, updateAction);
    }

    /// <summary>
    /// Deletes rows matching a predicate
    /// </summary>
    public int Delete(string tableName, Func<Row, bool> predicate)
    {
        return _storageEngine.DeleteRows(tableName, predicate);
    }

    /// <summary>
    /// Deletes all rows from a table
    /// </summary>
    public int DeleteAll(string tableName)
    {
        return _storageEngine.DeleteRows(tableName, _ => true);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Prints table schema to console
    /// </summary>
    public void PrintTableSchema(string tableName)
    {
        var table = GetTable(tableName);

        Console.WriteLine($"\nTable: {table.Name}");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"{"Column",-20} {"Type",-15} {"Constraints",-30}");
        Console.WriteLine(new string('-', 80));

        foreach (var column in table.Columns)
        {
            var constraints = new List<string>();
            if (column.IsPrimaryKey) constraints.Add("PRIMARY KEY");
            if (column.IsUnique) constraints.Add("UNIQUE");
            if (column.IsNotNull) constraints.Add("NOT NULL");

            var typeStr = column.Type == DataType.VARCHAR && column.MaxLength.HasValue
                ? $"VARCHAR({column.MaxLength})"
                : column.Type.ToString();

            Console.WriteLine($"{column.Name,-20} {typeStr,-15} {string.Join(", ", constraints),-30}");
        }

        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"Total Rows: {table.RowCount}");
        Console.WriteLine($"Indexes: {table.Indexes.Count}");

        if (table.Indexes.Any())
        {
            Console.WriteLine("\nIndexes:");
            foreach (var index in table.Indexes)
            {
                Console.WriteLine($"  - {index.Name} on column '{index.ColumnName}'");
            }
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Prints table data to console
    /// </summary>
    public void PrintTableData(string tableName, int maxRows = 100)
    {
        var table = GetTable(tableName);

        if (table.Rows.Count == 0)
        {
            Console.WriteLine($"\nTable '{tableName}' is empty.\n");
            return;
        }

        Console.WriteLine($"\nTable: {tableName} (showing {Math.Min(maxRows, table.Rows.Count)} of {table.Rows.Count} rows)");
        Console.WriteLine(new string('-', 80));

        // Print header
        var columnNames = table.Columns.Select(c => c.Name).ToList();
        var header = string.Join(" | ", columnNames.Select(c => c.PadRight(15)));
        Console.WriteLine(header);
        Console.WriteLine(new string('-', 80));

        // Print rows
        var rowsToPrint = table.Rows.Take(maxRows);
        foreach (var row in rowsToPrint)
        {
            var values = columnNames.Select(col =>
            {
                var value = row[col];
                var valueStr = value?.ToString() ?? "NULL";
                return valueStr.Length > 15 ? valueStr.Substring(0, 12) + "..." : valueStr;
            });
            Console.WriteLine(string.Join(" | ", values.Select(v => v.PadRight(15))));
        }

        Console.WriteLine(new string('-', 80));
        Console.WriteLine();
    }

    /// <summary>
    /// Flushes all in-memory data to disk
    /// </summary>
    public void Flush()
    {
        _storageEngine.FlushAll();
    }

    /// <summary>
    /// Gets database statistics
    /// </summary>
    public void PrintDatabaseInfo()
    {
        var tables = ListTables();

        Console.WriteLine($"\nDatabase: {Name}");
        Console.WriteLine($"Location: {DataDirectory}");
        Console.WriteLine($"Total Tables: {tables.Count}");
        Console.WriteLine();

        if (tables.Any())
        {
            Console.WriteLine("Tables:");
            foreach (var tableName in tables)
            {
                var table = GetTable(tableName);
                Console.WriteLine($"  - {tableName}: {table.RowCount} rows, {table.Columns.Count} columns, {table.Indexes.Count} indexes");
            }
            Console.WriteLine();
        }
    }

    #endregion

}