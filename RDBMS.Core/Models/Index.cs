using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDBMS.Core.Models;

/// <summary>
/// Represents an index on a table column for fast lookups
/// Uses a hash-based approach: O(1) lookup for equality checks
/// </summary>
public class Index
{
    public string Name {  get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ColumnName {  get; set; } = string.Empty;

    /// <summary>
    /// Hash map: column value -> list of row indices
    /// Example: { "John" -> [0, 5, 10], "Jane" -> [1, 3] }
    /// </summary>
    public Dictionary<object, List<int>> Entries { get; set; }

    public Index()
    {
        Entries = new Dictionary<object, List<int>>();
    }

    public Index(string name, string tableName, string columnName)
    {
        Name = name;
        TableName = tableName;
        ColumnName = columnName;
        Entries = new Dictionary<object, List<int>>();
    }

    /// <summary>
    /// Adds a row index to the index for a given value
    /// </summary>
    public void AddEntry(object value, int rowIndex)
    {
        // Handle NULL values
        var key = value ?? "NULL";

        if (!Entries.ContainsKey(key))
        {
            Entries[key] = new List<int>();
        }

        Entries[key].Add(rowIndex);
    }

    /// <summary>
    /// Removes a row index from the index for a given value
    /// </summary>
    public void RemoveEntry(object value, int rowIndex)
    {
        var key = value ?? "NULL";

        if (Entries.ContainsKey(key))
        {
            Entries[key].Remove(rowIndex);

            // Clean up empty lists
            if (Entries[key].Count == 0)
            {
                Entries.Remove(key);
            }
        }
    }

    /// <summary>
    /// Looks up row indices for a given value - O(1) operation
    /// </summary>
    public List<int> Lookup(object value)
    {
        var key = value ?? "NULL";
        return Entries.ContainsKey(key) ? Entries[key] : new List<int>();
    }

    /// <summary>
    /// Clears all index entries
    /// </summary>
    public void Clear()
    {
        Entries.Clear();
    }

    /// <summary>
    /// Rebuilds the entire index from scratch
    /// </summary>
    public void Rebuild(List<Row> rows)
    {
        Clear();

        for (int i = 0; i < rows.Count; i++)
        {
            var value = rows[i][ColumnName];
            AddEntry(value, i);
        }
    }

    public override string ToString()
    {
        return $"Index '{Name}' on {TableName}.{ColumnName} ({Entries.Count} unique values)";
    }
}
