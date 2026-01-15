using RDBMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDBMS.Core.Models;

/// <summary>
/// Represents a table in the database
/// Contains schema (columns) and data (rows)
/// </summary>
public class Table
{
    public string Name { get; set; } = string.Empty;
    public List<Column> Columns { get; set; }
    public List<Row> Rows { get; set; }
    public List<Index> Indexes { get; set; }

    public Table()
    {
        Columns = new List<Column>();
        Rows = new List<Row>();
        Indexes = new List<Index>();
    }

    public Table(string name)
    {
        Name = name;
        Columns = new List<Column>();
        Rows = new List<Row>();
        Indexes = new List<Index>();
    }

    public Table(string name, List<Column> columns)
    {
        Name = name;
        Columns = columns ?? new List<Column>();
        Rows = new List<Row>();
        Indexes = new List<Index>();
    }

    /// <summary>
    /// Gets a column by name (case-insensitive)
    /// </summary>
    public Column? GetColumn(string columnName)
    {
        return Columns.FirstOrDefault(c =>
            c.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the primary key column, if any
    /// </summary>
    public Column? GetPrimaryKey()
    {
        return Columns.FirstOrDefault(c => c.IsPrimaryKey);
    }

    /// <summary>
    /// Gets an index by name
    /// </summary>
    public Index? GetIndex(string indexName)
    {
        return Indexes.FirstOrDefault(i =>
            i.Name.Equals(indexName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets an index for a specific column, if one exists
    /// </summary>
    public Index? GetIndexForColumn(string columnName)
    {
        return Indexes.FirstOrDefault(i =>
            i.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a column exists
    /// </summary>
    public bool HasColumn(string columnName)
    {
        return GetColumn(columnName) != null;
    }

    /// <summary>
    /// Validates that all columns in the row exist in the table
    /// </summary>
    public bool ValidateRowColumns(Row row)
    {
        foreach (var columnName in row.Data.Keys)
        {
            if (!HasColumn(columnName))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Validates that a row's values match column types and constraints
    /// </summary>
    /// <param name="row">The row to validate</param>
    /// <param name="excludeRowIndex">Row index to exclude from constraint checks (for updates)</param>
    public (bool isValid, string? errorMessage) ValidateRow(Row row, int? excludeRowIndex = null)
    {
        foreach (var column in Columns)
        {
            var value = row[column.Name];

            // Check NOT NULL constraint
            if (value == null && column.IsNotNull)
            {
                return (false, $"Column '{column.Name}' cannot be NULL");
            }

            // Check data type
            if (value != null && !column.IsValidValue(value))
            {
                return (false, $"Invalid value for column '{column.Name}': {value}");
            }

            // Check UNIQUE constraint
            if (column.IsUnique && value != null)
            {
                var existingRows = new List<Row>();
                for (int i = 0; i < Rows.Count; i++)
                {
                    // Skip the row being updated
                    if (excludeRowIndex.HasValue && i == excludeRowIndex.Value)
                        continue;

                    if (Equals(Rows[i][column.Name], value))
                    {
                        existingRows.Add(Rows[i]);
                    }
                }

                if (existingRows.Any())
                {
                    return (false, $"UNIQUE constraint violation on column '{column.Name}': value '{value}' already exists");
                }
            }

            // Check PRIMARY KEY constraint (must be unique and not null)
            if (column.IsPrimaryKey)
            {
                if (value == null)
                {
                    return (false, $"PRIMARY KEY column '{column.Name}' cannot be NULL");
                }

                var existingRows = new List<Row>();
                for (int i = 0; i < Rows.Count; i++)
                {
                    // Skip the row being updated
                    if (excludeRowIndex.HasValue && i == excludeRowIndex.Value)
                        continue;

                    if (Equals(Rows[i][column.Name], value))
                    {
                        existingRows.Add(Rows[i]);
                    }
                }

                if (existingRows.Any())
                {
                    return (false, $"PRIMARY KEY constraint violation on column '{column.Name}': value '{value}' already exists");
                }
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Gets the total number of rows
    /// </summary>
    public int RowCount => Rows.Count;

    public override string ToString()
    {
        var columnNames = string.Join(", ", Columns.Select(c => c.Name));
        return $"Table '{Name}' ({Columns.Count} columns, {Rows.Count} rows): {columnNames}";
    }
}