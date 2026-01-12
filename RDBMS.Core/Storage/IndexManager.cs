using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RDBMS.Core.Models;


namespace RDBMS.Core.Storage;


/// <summary>
/// Manages index operations
/// </summary>
public class IndexManager
{
    private readonly TableManager _tableManager;

    public IndexManager(TableManager tableManager)
    {
        _tableManager = tableManager;
    }

    /// <summary>
    /// Creates an index on a table column
    /// </summary>
    public void CreateIndex(Table table, string indexName, string columnName)
    {
        // Validate column exists
        var column = table.GetColumn(columnName);
        if (column == null)
        {
            throw new StorageException($"Column '{columnName}' does not exist in table '{table.Name}'");
        }

        // Check if index already exists
        if (table.GetIndex(indexName) != null)
        {
            throw new StorageException($"Index '{indexName}' already exists on table '{table.Name}'");
        }

        // Create and build the index
        var index = new Models.Index(indexName, table.Name, columnName);
        index.Rebuild(table.Rows);

        // Add to table and save
        table.Indexes.Add(index);
        _tableManager.SaveSchema(table);
        _tableManager.SaveIndex(index);
    }

    /// <summary>
    /// Drops an index from a table
    /// </summary>
    public void DropIndex(Table table, string indexName)
    {
        var index = table.GetIndex(indexName);
        if (index == null)
        {
            throw new StorageException($"Index '{indexName}' does not exist on table '{table.Name}'");
        }

        // Remove from table
        table.Indexes.Remove(index);
        _tableManager.SaveSchema(table);

        // Note: We don't delete the index file here for simplicity
        // Could be cleaned up later
    }

    /// <summary>
    /// Updates all indexes after a row is inserted
    /// </summary>
    public void UpdateIndexesOnInsert(Table table, int rowIndex)
    {
        foreach (var index in table.Indexes)
        {
            var value = table.Rows[rowIndex][index.ColumnName];
            index.AddEntry(value, rowIndex);
        }
    }

    /// <summary>
    /// Updates all indexes after a row is deleted
    /// </summary>
    public void UpdateIndexesOnDelete(Table table, int rowIndex)
    {
        foreach (var index in table.Indexes)
        {
            var value = table.Rows[rowIndex][index.ColumnName];
            index.RemoveEntry(value, rowIndex);
        }
    }

    /// <summary>
    /// Updates all indexes after a row is updated
    /// </summary>
    public void UpdateIndexesOnUpdate(Table table, int rowIndex, Row oldRow)
    {
        foreach (var index in table.Indexes)
        {
            var oldValue = oldRow[index.ColumnName];
            var newValue = table.Rows[rowIndex][index.ColumnName];

            // If value changed, update the index
            if (!Equals(oldValue, newValue))
            {
                index.RemoveEntry(oldValue, rowIndex);
                index.AddEntry(newValue, rowIndex);
            }
        }
    }

    /// <summary>
    /// Rebuilds all indexes for a table
    /// </summary>
    public void RebuildAllIndexes(Table table)
    {
        foreach (var index in table.Indexes)
        {
            index.Rebuild(table.Rows);
            _tableManager.SaveIndex(index);
        }
    }
}
