// File: SimpleRDBMS.Core/Execution/DeleteExecutor.cs
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
    /// Executes DELETE queries
    /// </summary>
    public class DeleteExecutor
    {
        private readonly StorageEngine _storage;

        public DeleteExecutor(StorageEngine storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public QueryResult Execute(DeleteQuery query)
        {
            try
            {
                // Use StorageEngine's DeleteRows method - it handles everything!
                int rowsDeleted;

                if (query.Where != null)
                {
                    // Delete with WHERE clause
                    rowsDeleted = _storage.DeleteRows(query.TableName, row =>
                        EvaluateWhereClause(row.Data, query.Where));
                }
                else
                {
                    // Delete all rows
                    rowsDeleted = _storage.DeleteRows(query.TableName, _ => true);
                }

                return new QueryResult
                {
                    Success = true,
                    Message = $"Deleted {rowsDeleted} row(s)",
                    RowsAffected = rowsDeleted
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

        private bool EvaluateWhereClause(Dictionary<string, object?> row, WhereClause where)
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

        private bool EvaluateSimpleCondition(Dictionary<string, object?> row, WhereClause condition)
        {
            // Check if column exists
            if (!row.ContainsKey(condition.LeftOperand))
            {
                throw new InvalidOperationException($"Column '{condition.LeftOperand}' does not exist");
            }

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

        private int CompareValues(object? left, object? right)
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
    }
}