using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RDBMS.Core.Models;


namespace RDBMS.Core.Storage;


/// <summary>
/// Main storage engine - coordinates all storage operations
/// This is the primary interface for the database to interact with storage
/// </summary>
public class StorageEngine
{
    private readonly FileStorage _fileStorage;
    private readonly TableManager _tableManager;
    private readonly IndexManager _indexManager;
    private readonly Dictionary<string, Table> _loadedTables;

    public StorageEngine(string dataDirectory)
    {
        _fileStorage = new FileStorage(dataDirectory);
        _tableManager = new TableManager(_fileStorage);
        _indexManager = new IndexManager(_tableManager);
        _loadedTables = new Dictionary<string, Table>(StringComparer.OrdinalIgnoreCase);
    }

    #region Table Operations

    /// <summary>
    /// Creates a new table
    /// </summary>
    public void CreateTable(Table table)
    {
        _tableManager.CreateTable(table);
        _loadedTables[table.Name] = table;
    }

    /// <summary>
    /// Gets a table (loads from disk if not in memory)
    /// </summary>
    public Table GetTable(string tableName)
    {
        // Check if already loaded
        if (_loadedTables.TryGetValue(tableName, out var cachedTable))
        {
            return cachedTable;
        }

        // Load from disk
        var table = _tableManager.LoadTable(tableName);
        _loadedTables[tableName] = table;
        return table;
    }

    /// <summary>
    /// Drops a table
    /// </summary>
    public void DropTable(string tableName)
    {
        _tableManager.DeleteTable(tableName);
        _loadedTables.Remove(tableName);
    }

    /// <summary>
    /// Checks if a table exists
    /// </summary>
    public bool TableExists(string tableName)
    {
        return _tableManager.TableExists(tableName);
    }

    /// <summary>
    /// Lists all tables
    /// </summary>
    public List<string> ListTables()
    {
        return _tableManager.ListTables();
    }

    /// <summary>
    /// Gets all table names (alias for ListTables for consistency)
    /// </summary>
    public List<string> GetAllTableNames()
    {
        return _tableManager.ListTables();
    }

    #endregion

    #region Row Operations

    /// <summary>
    /// Inserts a row into a table
    /// </summary>
    public void InsertRow(string tableName, Row row)
    {
        var table = GetTable(tableName);

        // Validate row (no exclusion for new rows)
        var (isValid, errorMessage) = table.ValidateRow(row, excludeRowIndex: null);
        if (!isValid)
        {
            throw new StorageException(errorMessage!);
        }

        // Add row
        int rowIndex = table.Rows.Count;
        table.Rows.Add(row);

        // Update indexes
        _indexManager.UpdateIndexesOnInsert(table, rowIndex);

        // Save to disk
        _tableManager.SaveData(table);
        SaveAllIndexes(table);
    }

    /// <summary>
    /// Updates rows in a table
    /// </summary>
    public int UpdateRows(string tableName, Func<Row, bool> predicate, Action<Row> updateAction)
    {
        var table = GetTable(tableName);
        int updatedCount = 0;

        for (int i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            if (predicate(row))
            {
                var oldRow = row.Clone();
                updateAction(row);

                // Validate updated row - pass the current index to exclude it from constraint checks
                var (isValid, errorMessage) = table.ValidateRow(row, excludeRowIndex: i);
                if (!isValid)
                {
                    throw new StorageException(errorMessage!);
                }

                // Update indexes
                _indexManager.UpdateIndexesOnUpdate(table, i, oldRow);
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            _tableManager.SaveData(table);
            SaveAllIndexes(table);
        }

        return updatedCount;
    }

    /// <summary>
    /// Deletes rows from a table
    /// </summary>
    public int DeleteRows(string tableName, Func<Row, bool> predicate)
    {
        var table = GetTable(tableName);
        var rowsToDelete = new List<int>();

        // Find rows to delete
        for (int i = 0; i < table.Rows.Count; i++)
        {
            if (predicate(table.Rows[i]))
            {
                rowsToDelete.Add(i);
            }
        }

        // Delete in reverse order to maintain indices
        rowsToDelete.Reverse();
        foreach (var index in rowsToDelete)
        {
            _indexManager.UpdateIndexesOnDelete(table, index);
            table.Rows.RemoveAt(index);
        }

        if (rowsToDelete.Count > 0)
        {
            // Rebuild all indexes after deletions
            _indexManager.RebuildAllIndexes(table);
            _tableManager.SaveData(table);
        }

        return rowsToDelete.Count;
    }

    #endregion

    #region Index Operations

    /// <summary>
    /// Creates an index on a table column
    /// </summary>
    public void CreateIndex(string tableName, string indexName, string columnName)
    {
        var table = GetTable(tableName);
        _indexManager.CreateIndex(table, indexName, columnName);
    }

    /// <summary>
    /// Drops an index
    /// </summary>
    public void DropIndex(string tableName, string indexName)
    {
        var table = GetTable(tableName);
        _indexManager.DropIndex(table, indexName);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Saves all indexes for a table
    /// </summary>
    private void SaveAllIndexes(Table table)
    {
        foreach (var index in table.Indexes)
        {
            _tableManager.SaveIndex(index);
        }
    }

    /// <summary>
    /// Flushes all loaded tables to disk
    /// </summary>
    public void FlushAll()
    {
        foreach (var table in _loadedTables.Values)
        {
            _tableManager.SaveData(table);
            SaveAllIndexes(table);
        }
    }

    #endregion
}
