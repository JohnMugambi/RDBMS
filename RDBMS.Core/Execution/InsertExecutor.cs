using RDBMS.Core.Execution;
using RDBMS.Core.Models;
using RDBMS.Core.Parsing;
using RDBMS.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RDBMS.Core.Execution
{
    /// <summary>
    /// Executes INSERT queries
    /// </summary>
    public class InsertExecutor
    {
        private readonly StorageEngine _storage;

        public InsertExecutor(StorageEngine storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public QueryResult Execute(InsertQuery query)
        {
            try
            {
                var table = _storage.GetTable(query.TableName);
                int rowsInserted = 0;

                // Determine column order
                List<Column> targetColumns;
                if (query.Columns.Count == 0)
                {
                    // No columns specified, use all columns in table order
                    targetColumns = table.Columns;
                }
                else
                {
                    // Use specified columns
                    targetColumns = new List<Column>();
                    foreach (var colName in query.Columns)
                    {
                        var column = table.GetColumn(colName);
                        if (column == null)
                        {
                            throw new InvalidOperationException($"Column '{colName}' does not exist in table '{query.TableName}'");
                        }
                        targetColumns.Add(column);
                    }
                }

                // Insert each row
                foreach (var valueList in query.Values)
                {
                    if (valueList.Count != targetColumns.Count)
                    {
                        throw new InvalidOperationException(
                            $"Value count ({valueList.Count}) does not match column count ({targetColumns.Count})"
                        );
                    }

                    // Create row dictionary
                    var row = new Dictionary<string, object>();

                    for (int i = 0; i < targetColumns.Count; i++)
                    {
                        var column = targetColumns[i];
                        var value = valueList[i];

                        // Validate and convert value
                        var validatedValue = ValidateAndConvertValue(column, value);
                        row[column.Name] = validatedValue;
                    }

                    // Fill in missing columns with NULL (if not specified)
                    foreach (var column in table.Columns)
                    {
                        if (!row.ContainsKey(column.Name))
                        {
                            if (column.IsNotNull && !column.IsPrimaryKey)
                            {
                                throw new InvalidOperationException($"Column '{column.Name}' cannot be NULL");
                            }
                            row[column.Name] = null;
                        }
                    }

                    // Validate constraints
                    ValidateConstraints(table, row);

                    // Insert row - convert Dictionary to Row object
                    var rowObject = new Row(row.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value));
                    _storage.InsertRow(query.TableName, rowObject);
                    rowsInserted++;
                }

                return new QueryResult
                {
                    Success = true,
                    Message = $"Inserted {rowsInserted} row(s)",
                    RowsAffected = rowsInserted
                };
            }
            catch (Exception ex)
            {
                return new QueryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private object ValidateAndConvertValue(Column column, object value)
        {
            // Handle NULL
            if (value == null)
            {
                if (column.IsNotNull)
                {
                    throw new InvalidOperationException($"Column '{column.Name}' cannot be NULL");
                }
                return null;
            }

            // Convert based on data type
            try
            {
                return column.Type switch
                {
                    DataType.INT => Convert.ToInt32(value),
                    DataType.VARCHAR => ValidateVarchar(value.ToString(), column.MaxLength),
                    DataType.BOOLEAN => Convert.ToBoolean(value),
                    DataType.DATETIME => value is DateTime dt ? dt : DateTime.Parse(value.ToString()),
                    DataType.DECIMAL => Convert.ToDecimal(value),
                    _ => throw new NotSupportedException($"Data type {column.Type} is not supported")
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Cannot convert value '{value}' to type {column.Type} for column '{column.Name}': {ex.Message}"
                );
            }
        }

        private string ValidateVarchar(string value, int? maxLength)
        {
            if (maxLength.HasValue && value.Length > maxLength.Value)
            {
                throw new InvalidOperationException(
                    $"String value exceeds maximum length of {maxLength.Value}"
                );
            }
            return value;
        }

        private void ValidateConstraints(Table table, Dictionary<string, object> row)
        {
            // Check PRIMARY KEY uniqueness
            var primaryKey = table.GetPrimaryKey();
            if (primaryKey != null)
            {
                var pkValue = row[primaryKey.Name];
                if (pkValue == null)
                {
                    throw new InvalidOperationException($"PRIMARY KEY column '{primaryKey.Name}' cannot be NULL");
                }

                // Check if value already exists
                foreach (var existingRow in table.Rows)
                {
                    if (Equals(existingRow[primaryKey.Name], pkValue))
                    {
                        throw new InvalidOperationException(
                            $"PRIMARY KEY violation: value '{pkValue}' already exists in column '{primaryKey.Name}'"
                        );
                    }
                }
            }

            // Check UNIQUE constraints
            foreach (var column in table.Columns.Where(c => c.IsUnique))
            {
                var value = row[column.Name];
                if (value == null) continue; // NULL values are allowed for UNIQUE (unless NOT NULL is also set)

                foreach (var existingRow in table.Rows)
                {
                    if (Equals(existingRow[column.Name], value))
                    {
                        throw new InvalidOperationException(
                            $"UNIQUE constraint violation: value '{value}' already exists in column '{column.Name}'"
                        );
                    }
                }
            }
        }
    }
}