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
    /// Executes SELECT queries with support for WHERE, JOIN, and ORDER BY
    /// </summary>
    public class SelectExecutor
    {
        private readonly StorageEngine _storage;

        public SelectExecutor(StorageEngine storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public QueryResult Execute(SelectQuery query)
        {
            try
            {
                var table = _storage.GetTable(query.TableName);

                // Start with all rows from main table - convert Row objects to dictionaries
                List<Dictionary<string, object?>> resultRows = table.Rows
                    .Select(r => r.Data.ToDictionary(
                        kvp => $"{query.TableName}.{kvp.Key}",  // Add table prefix!
                        kvp => kvp.Value
                    ))
                    .ToList();

                // Apply JOINs if any
                if (query.Joins.Count > 0)
                {
                    resultRows = ExecuteJoins(query, table, resultRows);
                }

                // Apply WHERE clause
                if (query.Where != null)
                {
                    resultRows = ApplyWhereClause(resultRows, query.Where);
                }

                // Apply ORDER BY
                if (query.OrderBy != null && query.OrderBy.Count > 0)
                {
                    resultRows = ApplyOrderBy(resultRows, query.OrderBy);
                }

                // Apply LIMIT
                if (query.Limit.HasValue)
                {
                    resultRows = resultRows.Take(query.Limit.Value).ToList();
                }

                // Project columns
                var projectedRows = ProjectColumns(resultRows, query.Columns, table);

                // Build result
                var result = new QueryResult
                {
                    Success = true,
                    Message = $"Selected {projectedRows.Count} row(s)",
                    Data = projectedRows,
                    ColumnNames = projectedRows.Count > 0 ? projectedRows[0].Keys.ToList() : new List<string>()
                };

                return result;
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

        private List<Dictionary<string, object?>> ExecuteJoins(
            SelectQuery query,
            Table mainTable,
            List<Dictionary<string, object?>> leftRows)
        {
            var result = leftRows;

            foreach (var joinClause in query.Joins)
            {
                var rightTable = _storage.GetTable(joinClause.TableName);
                result = ExecuteSingleJoin(result, rightTable, joinClause, mainTable.Name);
            }

            return result;
        }

        private List<Dictionary<string, object?>> ExecuteSingleJoin(
            List<Dictionary<string, object?>> leftRows,
            Table rightTable,
            JoinClause joinClause,
            string leftTableName)
        {
            var joinedRows = new List<Dictionary<string, object?>>();
            var condition = joinClause.Condition;

            foreach (var leftRow in leftRows)
            {
                bool foundMatch = false;

                foreach (var rightRow in rightTable.Rows)
                {
                    // Check join condition - pass rightRow.Data
                    if (EvaluateJoinCondition(leftRow, rightRow.Data, condition))
                    {
                        foundMatch = true;

                        // Combine rows with qualified column names
                        var combinedRow = new Dictionary<string, object?>();

                        // Add left row columns (may already be qualified from previous joins)
                        foreach (var kvp in leftRow)
                        {
                            combinedRow[kvp.Key] = kvp.Value;
                        }

                        // Add right row columns with table qualification
                        foreach (var kvp in rightRow.Data)
                        {
                            string qualifiedName = $"{rightTable.Name}.{kvp.Key}";
                            combinedRow[qualifiedName] = kvp.Value;
                        }

                        joinedRows.Add(combinedRow);
                    }
                }

                // For LEFT JOIN, include left row even if no match
                if (!foundMatch && joinClause.Type == JoinType.LEFT)
                {
                    var combinedRow = new Dictionary<string, object?>(leftRow);

                    // Add NULL for right table columns
                    foreach (var column in rightTable.Columns)
                    {
                        string qualifiedName = $"{rightTable.Name}.{column.Name}";
                        combinedRow[qualifiedName] = null;
                    }

                    joinedRows.Add(combinedRow);
                }
            }

            return joinedRows;
        }

        private bool EvaluateJoinCondition(
    Dictionary<string, object?> leftRow,
    Dictionary<string, object?> rightRow,
    JoinCondition condition)
        {
            // Build the qualified left column name
            var leftKey = $"{condition.LeftTable}.{condition.LeftColumn}";

            // Get left value (should be qualified now)
            object? leftValue = null;
            if (leftRow.ContainsKey(leftKey))
            {
                leftValue = leftRow[leftKey];
            }
            else if (leftRow.ContainsKey(condition.LeftColumn))
            {
                // Fallback to unqualified (shouldn't happen now)
                leftValue = leftRow[condition.LeftColumn];
            }
            else
            {
                throw new InvalidOperationException($"Column '{leftKey}' not found in left table");
            }

            // Get right value (unqualified in the Row.Data)
            object? rightValue = null;
            if (rightRow.ContainsKey(condition.RightColumn))
            {
                rightValue = rightRow[condition.RightColumn];
            }
            else
            {
                throw new InvalidOperationException($"Column '{condition.RightColumn}' not found in right table");
            }

            // NULL values don't match in JOINs
            if (leftValue == null || rightValue == null)
            {
                return false;
            }

            // Compare based on operator
            return condition.Operator switch
            {
                "=" => Equals(leftValue, rightValue),
                "!=" or "<>" => !Equals(leftValue, rightValue),
                _ => throw new NotSupportedException($"Join operator '{condition.Operator}' is not supported")
            };
        }

        private List<Dictionary<string, object?>> ApplyWhereClause(
            List<Dictionary<string, object?>> rows,
            WhereClause where)
        {
            return rows.Where(row => EvaluateWhereClause(row, where)).ToList();
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
            // Get column value (handle both qualified and unqualified names)
            object? leftValue = GetColumnValue(row, condition.LeftOperand);
            object? rightValue = condition.RightOperand;

            // If right operand is a string and looks like a column name, try to get its value
            if (rightValue is string rightStr && !rightStr.StartsWith("'") && row.ContainsKey(rightStr))
            {
                rightValue = row[rightStr];
            }

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

        private object? GetColumnValue(Dictionary<string, object?> row, string columnName)
        {
            // Try direct lookup first
            if (row.ContainsKey(columnName))
            {
                return row[columnName];
            }

            // Try to find with table qualification
            var matchingKey = row.Keys.FirstOrDefault(k => k.EndsWith($".{columnName}"));
            if (matchingKey != null)
            {
                return row[matchingKey];
            }

            throw new InvalidOperationException($"Column '{columnName}' not found in result set");
        }

        private int CompareValues(object left, object right)
        {
            // Convert to comparable types
            if (left is IComparable leftComp && right is IComparable rightComp)
            {
                // Try to convert right to same type as left
                try
                {
                    var convertedRight = Convert.ChangeType(right, left.GetType());
                    return leftComp.CompareTo(convertedRight);
                }
                catch
                {
                    // If conversion fails, compare as strings
                    return string.Compare(left.ToString(), right.ToString(), StringComparison.Ordinal);
                }
            }

            return string.Compare(left?.ToString(), right?.ToString(), StringComparison.Ordinal);
        }

        private List<Dictionary<string, object?>> ApplyOrderBy(
            List<Dictionary<string, object?>> rows,
            List<OrderByClause> orderByClauses)
        {
            IOrderedEnumerable<Dictionary<string, object?>>? orderedRows = null;

            for (int i = 0; i < orderByClauses.Count; i++)
            {
                var clause = orderByClauses[i];

                if (i == 0)
                {
                    orderedRows = clause.Order == SortOrder.ASC
                        ? rows.OrderBy(r => GetColumnValue(r, clause.ColumnName))
                        : rows.OrderByDescending(r => GetColumnValue(r, clause.ColumnName));
                }
                else
                {
                    orderedRows = clause.Order == SortOrder.ASC
                        ? orderedRows!.ThenBy(r => GetColumnValue(r, clause.ColumnName))
                        : orderedRows!.ThenByDescending(r => GetColumnValue(r, clause.ColumnName));
                }
            }

            return orderedRows?.ToList() ?? rows;
        }

        private List<Dictionary<string, object?>> ProjectColumns(
            List<Dictionary<string, object?>> rows,
            List<SelectColumn> selectColumns,
            Table mainTable)
        {
            // If no rows, return empty list
            if (rows.Count == 0)
            {
                return new List<Dictionary<string, object?>>();
            }

            // Check if selecting all columns (*)
            if (selectColumns.Count == 1 && selectColumns[0].IsWildcard)
            {
                return rows; // Return all columns as-is
            }

            // Project specific columns
            var projectedRows = new List<Dictionary<string, object?>>();

            foreach (var row in rows)
            {
                var projectedRow = new Dictionary<string, object?>();

                foreach (var selectCol in selectColumns)
                {
                    if (selectCol.IsWildcard)
                    {
                        // Add all columns
                        foreach (var kvp in row)
                        {
                            projectedRow[kvp.Key] = kvp.Value;
                        }
                    }
                    else
                    {
                        // Get specific column
                        string columnKey = !string.IsNullOrEmpty(selectCol.TableName)
                            ? $"{selectCol.TableName}.{selectCol.ColumnName}"
                            : selectCol.ColumnName;

                        object? value = GetColumnValue(row, columnKey);

                        // Use alias if provided, otherwise use column name
                        string outputKey = !string.IsNullOrEmpty(selectCol.Alias)
                            ? selectCol.Alias
                            : selectCol.ColumnName;

                        projectedRow[outputKey] = value;
                    }
                }

                projectedRows.Add(projectedRow);
            }

            return projectedRows;
        }
    }
}