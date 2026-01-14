using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDBMS.Core.Parsing
{
    /// <summary>
    /// Base interface for all query types
    /// </summary>
    public interface IQuery
    {
        QueryType Type { get; }
    }

    /// <summary>
    /// Types of SQL queries supported
    /// </summary>
    public enum QueryType
    {
        SELECT,
        INSERT,
        UPDATE,
        DELETE,
        CREATE_TABLE,
        DROP_TABLE,
        CREATE_INDEX
    }

    #region SELECT Query

    /// <summary>
    /// Represents a SELECT query
    /// Example: SELECT name, age FROM users WHERE id > 10 ORDER BY name
    /// </summary>
    public class SelectQuery : IQuery
    {
        public QueryType Type => QueryType.SELECT;

        /// <summary>
        /// Columns to select. Empty list or ["*"] means all columns
        /// </summary>
        public List<SelectColumn> Columns { get; set; } = new List<SelectColumn>();

        /// <summary>
        /// Main table to select from
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// WHERE clause condition (optional)
        /// </summary>
        public WhereClause Where { get; set; }

        /// <summary>
        /// JOIN clauses (optional)
        /// </summary>
        public List<JoinClause> Joins { get; set; } = new List<JoinClause>();

        /// <summary>
        /// ORDER BY clauses (optional)
        /// </summary>
        public List<OrderByClause> OrderBy { get; set; } = new List<OrderByClause>();

        /// <summary>
        /// LIMIT clause (optional)
        /// </summary>
        public int? Limit { get; set; }
    }

    /// <summary>
    /// Represents a column in SELECT clause
    /// Can be: column_name, table.column, *, table.*, or expression
    /// </summary>
    public class SelectColumn
    {
        public string TableName { get; set; }  // Optional table qualifier
        public string ColumnName { get; set; }
        public string Alias { get; set; }      // Optional AS alias

        public bool IsWildcard => ColumnName == "*";

        public override string ToString()
        {
            var result = string.IsNullOrEmpty(TableName)
                ? ColumnName
                : $"{TableName}.{ColumnName}";

            if (!string.IsNullOrEmpty(Alias))
            {
                result += $" AS {Alias}";
            }

            return result;
        }
    }

    #endregion

    #region INSERT Query

    /// <summary>
    /// Represents an INSERT query
    /// Example: INSERT INTO users (name, age) VALUES ('John', 25)
    /// </summary>
    public class InsertQuery : IQuery
    {
        public QueryType Type => QueryType.INSERT;

        public string TableName { get; set; }

        /// <summary>
        /// Column names to insert into (optional, if not specified, all columns assumed)
        /// </summary>
        public List<string> Columns { get; set; } = new List<string>();

        /// <summary>
        /// Values to insert (one list per row for bulk inserts)
        /// </summary>
        public List<List<object>> Values { get; set; } = new List<List<object>>();
    }

    #endregion

    #region UPDATE Query

    /// <summary>
    /// Represents an UPDATE query
    /// Example: UPDATE users SET name = 'Jane', age = 26 WHERE id = 1
    /// </summary>
    public class UpdateQuery : IQuery
    {
        public QueryType Type => QueryType.UPDATE;

        public string TableName { get; set; }

        /// <summary>
        /// Assignments: column = value
        /// </summary>
        public List<Assignment> Assignments { get; set; } = new List<Assignment>();

        /// <summary>
        /// WHERE clause (optional but recommended!)
        /// </summary>
        public WhereClause Where { get; set; }
    }

    /// <summary>
    /// Represents a SET assignment: column = value
    /// </summary>
    public class Assignment
    {
        public string ColumnName { get; set; }
        public object Value { get; set; }

        public override string ToString() => $"{ColumnName} = {Value}";
    }

    #endregion

    #region DELETE Query

    /// <summary>
    /// Represents a DELETE query
    /// Example: DELETE FROM users WHERE id = 1
    /// </summary>
    public class DeleteQuery : IQuery
    {
        public QueryType Type => QueryType.DELETE;

        public string TableName { get; set; }

        /// <summary>
        /// WHERE clause (optional but VERY recommended!)
        /// </summary>
        public WhereClause Where { get; set; }
    }

    #endregion

    #region CREATE TABLE Query

    /// <summary>
    /// Represents a CREATE TABLE query
    /// Example: CREATE TABLE users (id INT PRIMARY KEY, name VARCHAR(100) NOT NULL)
    /// </summary>
    public class CreateTableQuery : IQuery
    {
        public QueryType Type => QueryType.CREATE_TABLE;

        public string TableName { get; set; }

        /// <summary>
        /// Column definitions
        /// </summary>
        public List<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();
    }

    /// <summary>
    /// Represents a column definition in CREATE TABLE
    /// </summary>
    public class ColumnDefinition
    {
        public string Name { get; set; }
        public string DataType { get; set; }      // INT, VARCHAR, BOOLEAN, etc.
        public int? MaxLength { get; set; }       // For VARCHAR(100)
        public bool IsPrimaryKey { get; set; }
        public bool IsUnique { get; set; }
        public bool IsNotNull { get; set; }

        public override string ToString()
        {
            var result = $"{Name} {DataType}";
            if (MaxLength.HasValue) result += $"({MaxLength})";
            if (IsPrimaryKey) result += " PRIMARY KEY";
            if (IsUnique) result += " UNIQUE";
            if (IsNotNull) result += " NOT NULL";
            return result;
        }
    }

    #endregion

    #region DROP TABLE Query

    /// <summary>
    /// Represents a DROP TABLE query
    /// Example: DROP TABLE users
    /// </summary>
    public class DropTableQuery : IQuery
    {
        public QueryType Type => QueryType.DROP_TABLE;

        public string TableName { get; set; }
    }

    #endregion

    #region CREATE INDEX Query

    /// <summary>
    /// Represents a CREATE INDEX query
    /// Example: CREATE INDEX idx_name ON users(name)
    /// </summary>
    public class CreateIndexQuery : IQuery
    {
        public QueryType Type => QueryType.CREATE_INDEX;

        public string IndexName { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
    }

    #endregion

    #region WHERE Clause

    /// <summary>
    /// Represents a WHERE clause condition
    /// Can be a simple condition (id = 1) or compound (id = 1 AND age > 18)
    /// </summary>
    public class WhereClause
    {
        public ConditionType Type { get; set; }

        // For simple conditions
        public string LeftOperand { get; set; }   // Column name or table.column
        public string Operator { get; set; }       // =, !=, >, <, >=, <=
        public object RightOperand { get; set; }   // Value or column name

        // For compound conditions (AND/OR)
        public WhereClause Left { get; set; }
        public WhereClause Right { get; set; }
        public LogicalOperator? LogicalOp { get; set; }

        public override string ToString()
        {
            if (Type == ConditionType.Compound)
            {
                return $"({Left} {LogicalOp} {Right})";
            }
            return $"{LeftOperand} {Operator} {RightOperand}";
        }
    }

    public enum ConditionType
    {
        Simple,     // id = 1
        Compound    // id = 1 AND age > 18
    }

    public enum LogicalOperator
    {
        AND,
        OR
    }

    #endregion

    #region JOIN Clause

    /// <summary>
    /// Represents a JOIN clause
    /// Example: INNER JOIN orders ON users.id = orders.user_id
    /// </summary>
    public class JoinClause
    {
        public JoinType Type { get; set; }
        public string TableName { get; set; }
        public JoinCondition Condition { get; set; }

        public override string ToString()
        {
            return $"{Type} JOIN {TableName} ON {Condition}";
        }
    }

    public enum JoinType
    {
        INNER,
        LEFT,
        RIGHT,
        OUTER
    }

    /// <summary>
    /// Represents a JOIN condition
    /// Example: users.id = orders.user_id
    /// </summary>
    public class JoinCondition
    {
        public string LeftTable { get; set; }
        public string LeftColumn { get; set; }
        public string Operator { get; set; }
        public string RightTable { get; set; }
        public string RightColumn { get; set; }

        public override string ToString()
        {
            return $"{LeftTable}.{LeftColumn} {Operator} {RightTable}.{RightColumn}";
        }
    }

    #endregion

    #region ORDER BY Clause

    /// <summary>
    /// Represents an ORDER BY clause
    /// Example: ORDER BY name ASC, age DESC
    /// </summary>
    public class OrderByClause
    {
        public string ColumnName { get; set; }
        public SortOrder Order { get; set; } = SortOrder.ASC;

        public override string ToString()
        {
            return $"{ColumnName} {Order}";
        }
    }

    public enum SortOrder
    {
        ASC,
        DESC
    }

    #endregion
}