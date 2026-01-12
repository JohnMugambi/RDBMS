using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDBMS.Core.Models;

/// <summary>
/// Represents a row of data in a table
/// Key: column name, Value: column value
/// </summary>
/// 

public class Row
{
    public Dictionary<string, object?> Data { get; set; }

    public Row()
    {
        Data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    public Row(Dictionary<string, object?> data)
    {
        Data = new Dictionary<string, object?>(data, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a value from the row by column name (case-insensitive)
    /// </summary>
    /// 
    public object? this[string columnName]
    {
        get => Data.ContainsKey(columnName) ? Data[columnName] : null;
        set => Data[columnName] = value;
    }

    /// <summary>
    /// Creates a copy of this row 
    /// important for returning query results, in joins, avoid mutation fo stored data during reads
    /// </summary>
    /// 
    public Row Clone()
    {
        return new Row(new Dictionary<string, object?>(Data));
    }

    /// <summary>
    /// Returns a string that represents the current object, listing all key-value pairs in the collection.
    /// </summary>
    /// <returns>A string containing each key and its associated value in the format "{ key1=value1, key2=value2, ... }". If a
    /// value is null, it is represented as "NULL".</returns>
    public override string ToString()
    {
        var values = Data.Select(kvp => $"{kvp.Key}={kvp.Value ?? "NULL"}");
        return $"{{ {string.Join(", ", values)} }}";
    }
}