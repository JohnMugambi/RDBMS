using RDBMS.Core.Models;
using SimpleRDBMS.Core.Models;

namespace RDBMS.Core.Storage;

/// <summary>
/// Manages table operations: CRUD on tables and their data
/// </summary>
public class TableManager
{
    private readonly FileStorage _fileStorage;

    public TableManager(FileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }

    /// <summary>
    /// Creates a new table and saves it to disk
    /// </summary>
    public void CreateTable(Table table)
    {
        if (_fileStorage.TableExists(table.Name))
        {
            throw new TableAlreadyExistsException(table.Name);
        }

        // Save schema
        SaveSchema(table);

        // Save empty data
        SaveData(table);

        // Create indexes for primary keys automatically
        var primaryKey = table.GetPrimaryKey();
        if (primaryKey != null)
        {
            var index = new Models.Index($"pk_{table.Name}_{primaryKey.Name}", table.Name, primaryKey.Name);
            table.Indexes.Add(index);
            SaveIndex(index);
        }
    }

    /// <summary>
    /// Loads a table from disk (schema + data + indexes)
    /// </summary>
    public Table LoadTable(string tableName)
    {
        if (!_fileStorage.TableExists(tableName))
        {
            throw new TableNotFoundException(tableName);
        }

        // Load schema
        var schemaDto = _fileStorage.LoadJson<TableSchemaDto>(_fileStorage.GetSchemaPath(tableName));
        if (schemaDto == null)
        {
            throw new StorageException($"Failed to load schema for table: {tableName}");
        }

        // Convert DTO to Table
        var table = new Table(schemaDto.Name);

        // Convert columns
        foreach (var colDto in schemaDto.Columns)
        {
            var column = new Column
            {
                Name = colDto.Name,
                Type = Enum.Parse<DataType>(colDto.Type),
                MaxLength = colDto.MaxLength,
                IsPrimaryKey = colDto.IsPrimaryKey,
                IsUnique = colDto.IsUnique,
                IsNotNull = colDto.IsNotNull
            };
            table.Columns.Add(column);
        }

        // Load data
        var dataDto = _fileStorage.LoadJson<TableDataDto>(_fileStorage.GetDataPath(tableName));
        if (dataDto != null)
        {
            foreach (var rowData in dataDto.Rows)
            {
                // Convert values to correct types
                var typedRow = new Dictionary<string, object?>();
                foreach (var kvp in rowData)
                {
                    var column = table.GetColumn(kvp.Key);
                    if (column != null)
                    {
                        typedRow[kvp.Key] = ConvertValue(kvp.Value, column.Type);
                    }
                }
                table.Rows.Add(new Row(typedRow));
            }
        }

        // Load indexes
        foreach (var indexDto in schemaDto.Indexes)
        {
            var index = new Models.Index(indexDto.Name, indexDto.TableName, indexDto.ColumnName);
            // Rebuild index from current data
            index.Rebuild(table.Rows);
            table.Indexes.Add(index);
        }

        return table;
    }

    /// <summary>
    /// Saves table schema to disk
    /// </summary>
    public void SaveSchema(Table table)
    {
        var schemaDto = new TableSchemaDto
        {
            Name = table.Name,
            Columns = table.Columns.Select(c => new ColumnDto
            {
                Name = c.Name,
                Type = c.Type.ToString(),
                MaxLength = c.MaxLength,
                IsPrimaryKey = c.IsPrimaryKey,
                IsUnique = c.IsUnique,
                IsNotNull = c.IsNotNull
            }).ToList(),
            Indexes = table.Indexes.Select(i => new IndexDto
            {
                Name = i.Name,
                TableName = i.TableName,
                ColumnName = i.ColumnName
            }).ToList()
        };

        _fileStorage.SaveJson(_fileStorage.GetSchemaPath(table.Name), schemaDto);
    }

    /// <summary>
    /// Saves table data to disk
    /// </summary>
    public void SaveData(Table table)
    {
        var dataDto = new TableDataDto
        {
            Rows = table.Rows.Select(r => new Dictionary<string, object?>(r.Data)).ToList()
        };

        _fileStorage.SaveJson(_fileStorage.GetDataPath(table.Name), dataDto);
    }

    /// <summary>
    /// Saves an index to disk
    /// </summary>
    public void SaveIndex(Models.Index index)
    {
        var indexPath = _fileStorage.GetIndexPath(index.TableName, index.Name);

        // Convert index entries to serializable format
        var indexData = new Dictionary<string, List<int>>();
        foreach (var entry in index.Entries)
        {
            var key = entry.Key?.ToString() ?? "NULL";
            indexData[key] = entry.Value;
        }

        _fileStorage.SaveJson(indexPath, indexData);
    }

    /// <summary>
    /// Deletes a table from disk
    /// </summary>
    public void DeleteTable(string tableName)
    {
        if (!_fileStorage.TableExists(tableName))
        {
            throw new TableNotFoundException(tableName);
        }

        _fileStorage.DeleteTable(tableName);
    }

    /// <summary>
    /// Checks if a table exists
    /// </summary>
    public bool TableExists(string tableName)
    {
        return _fileStorage.TableExists(tableName);
    }

    /// <summary>
    /// Lists all table names
    /// </summary>
    public List<string> ListTables()
    {
        return _fileStorage.ListTables();
    }

    /// <summary>
    /// Converts a JSON value to the appropriate C# type
    /// </summary>
    private object? ConvertValue(object? value, DataType type)
    {
        if (value == null) return null;

        try
        {
            return type switch
            {
                DataType.INT => Convert.ToInt32(value),
                DataType.VARCHAR => value.ToString(),
                DataType.BOOLEAN => Convert.ToBoolean(value),
                DataType.DATETIME => value is DateTime dt ? dt : DateTime.Parse(value.ToString()!),
                DataType.DECIMAL => Convert.ToDecimal(value),
                _ => value
            };
        }
        catch
        {
            return value; // Return as-is if conversion fails
        }
    }
}