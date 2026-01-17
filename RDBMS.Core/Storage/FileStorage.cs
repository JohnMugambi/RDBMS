using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RDBMS.Core.Storage;

/// <summary>
/// Converts object? types during JSON deserialization to prevent JsonElement
/// </summary>
public class ObjectToInferredTypesConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String when reader.TryGetDateTime(out DateTime datetime) => datetime,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Null => null,
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}

/// <summary>
/// Handles low-level file I/O operations
/// </summary>
public class FileStorage
{
    private readonly string _dataDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileStorage(string dataDirectory)
    {
        _dataDirectory = dataDirectory;

        // Ensure dir exists
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            // Add the converter to handle object? types properly
            Converters = { new ObjectToInferredTypesConverter() }
        };
    }

    /// <summary>
    /// Gets the file path for a table's schema
    /// </summary>
    public string GetSchemaPath(string tableName)
    {
        return Path.Combine(_dataDirectory, $"{tableName}_schema.json");
    }

    /// <summary>
    /// Gets the file path for a table's data
    /// </summary>
    public string GetDataPath(string tableName)
    {
        return Path.Combine(_dataDirectory, $"{tableName}_data.json");
    }

    /// <summary>
    /// Gets the file path for an index
    /// </summary>
    public string GetIndexPath(string tableName, string indexName)
    {
        return Path.Combine(_dataDirectory, $"{tableName}_{indexName}.json");
    }

    /// <summary>
    /// Saves an object to a JSON file
    /// </summary>
    public void SaveJson<T>(string filePath, T data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            throw new StorageException($"Failed to save file: {filePath}", ex);
        }
    }

    /// <summary>
    /// Loads an object from a JSON file
    /// </summary>
    public T? LoadJson<T>(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return default;
            }

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            throw new StorageException($"Failed to load file: {filePath}", ex);
        }
    }

    /// <summary>
    /// Checks if a table exists (by checking if schema file exists)
    /// </summary>
    public bool TableExists(string tableName)
    {
        return File.Exists(GetSchemaPath(tableName));
    }

    /// <summary>
    /// Deletes all files related to a table
    /// </summary>
    public void DeleteTable(string tableName)
    {
        try
        {
            var schemaPath = GetSchemaPath(tableName);
            var dataPath = GetDataPath(tableName);

            if (File.Exists(schemaPath))
                File.Delete(schemaPath);

            if (File.Exists(dataPath))
                File.Delete(dataPath);

            // Delete index files (pattern: tableName_*.json, excluding schema and data)
            var indexFiles = Directory.GetFiles(_dataDirectory, $"{tableName}_*.json")
                .Where(f => !f.EndsWith("_schema.json") && !f.EndsWith("_data.json"));

            foreach (var indexFile in indexFiles)
            {
                File.Delete(indexFile);
            }
        }
        catch (Exception ex)
        {
            throw new StorageException($"Failed to delete table: {tableName}", ex);
        }
    }

    /// <summary>
    /// Lists all table names in the data directory
    /// </summary>
    public List<string> ListTables()
    {
        try
        {
            var schemaFiles = Directory.GetFiles(_dataDirectory, "*_schema.json");
            return schemaFiles
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Select(f => f.Replace("_schema", ""))
                .ToList();
        }
        catch (Exception ex)
        {
            throw new StorageException("Failed to list tables", ex);
        }
    }
}