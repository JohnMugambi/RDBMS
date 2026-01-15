// File: SimpleRDBMS.Core/Execution/UpdateExecutor.cs
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
    /// Executes UPDATE queries
    /// </summary>
    public class UpdateExecutor
    {
        private readonly StorageEngine _storage;

        public UpdateExecutor(StorageEngine storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public QueryResult Execute(UpdateQuery query)
        {
            try
            {
                // Use StorageEngine's UpdateRows method
                int rowsUpdated = _storage.UpdateRows(
                    query.TableName,
                    // Predicate: which rows to update
                    row => query.Where == null || EvaluateWhereClause(row.Data, query.Where),
                    // Action: how to update them
                    row =>
                    {
                        var table = _storage.GetTable(query.TableName);

                        foreach (var assignment in query.Assignments)
                        {
                            // Validate column exists
                            var column = table.GetColumn(assignment.ColumnName);
                            if (column == null)
                            {
                                throw new InvalidOperationException(
                                    $"Column '{assignment.ColumnName}' does not exist in table '{query.TableName}'"
                                );
                            }

                            // Check if trying to update PRIMARY KEY
                            if (column.IsPrimaryKey)
                            {
                                throw new InvalidOperationException(
                                    $"Cannot update PRIMARY KEY column '{assignment.ColumnName}'"
                                );
                            }

                            // Validate and convert value
                            var validatedValue = ValidateAndConvertValue(column, assignment.Value);

                            // Update the value
                            row[assignment.ColumnName] = validatedValue;
                        }
                    }
                );

                return new QueryResult
                {
                    Success = true,
                    Message = $"Updated {rowsUpdated} row(s)",
                    RowsAffected = rowsUpdated
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

        private List<int> FindMatchingRows(Table table, WhereClause where)
        {
            var matchingIndices = new List<int>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                if (EvaluateWhereClause(table.Rows[i].Data, where))
                {
                    matchingIndices.Add(i);
                }
            }
            return matchingIndices;
        }


        private bool EvaluateWhereClause(Dictionary<string, object> row, WhereClause where)
        {
            if (where.Type == ConditionType.Simple)
            {
                return EvaluateSimpleCondition(row, where);
            }
            else // Compound
            {
                bool leftResult = EvaluateWhereClause(row, where.Left);
                bool rightResult = EvaluateWhereClause(row, where.Right);

                return where.LogicalOp switch
                {
                    LogicalOperator.AND => leftResult && rightResult,
                    LogicalOperator.OR => leftResult || rightResult,
                    _ => throw new NotSupportedException($"Logical operator {where.LogicalOp} is not supported")
                };
            }
        }

        private bool EvaluateSimpleCondition(Dictionary<string, object> row, WhereClause condition)
        {
            object leftValue = row[condition.LeftOperand];
            object rightValue = condition.RightOperand;

            // Handle NULL comparisons
            if (leftValue == null || rightValue == null)
            {
                return condition.Operator switch
                {
                    "=" => leftValue == null && rightValue == null,
                    "!=" => !(leftValue == null && rightValue == null),
                    _ => false
                };
            }

            // Compare values
            return condition.Operator switch
            {
                "=" => CompareValues(leftValue, rightValue) == 0,
                "!=" => CompareValues(leftValue, rightValue) != 0,
                ">" => CompareValues(leftValue, rightValue) > 0,
                "<" => CompareValues(leftValue, rightValue) < 0,
                ">=" => CompareValues(leftValue, rightValue) >= 0,
                "<=" => CompareValues(leftValue, rightValue) <= 0,
                _ => throw new NotSupportedException($"Operator '{condition.Operator}' is not supported")
            };
        }

        private int CompareValues(object left, object right)
        {
            if (left is IComparable leftComp && right is IComparable rightComp)
            {
                try
                {
                    var convertedRight = Convert.ChangeType(right, left.GetType());
                    return leftComp.CompareTo(convertedRight);
                }
                catch
                {
                    return string.Compare(left.ToString(), right.ToString(), StringComparison.Ordinal);
                }
            }

            return string.Compare(left?.ToString(), right?.ToString(), StringComparison.Ordinal);
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

        private void ValidateUniqueConstraint(Table table, Column column, object value, int excludeRowIndex)
        {
            for (int i = 0; i < table.Rows.Count; i++)
            {
                if (i == excludeRowIndex) continue; // Skip the row being updated

                if (Equals(table.Rows[i][column.Name], value))
                {
                    throw new InvalidOperationException(
                        $"UNIQUE constraint violation: value '{value}' already exists in column '{column.Name}'"
                    );
                }
            }
        }
    }
}