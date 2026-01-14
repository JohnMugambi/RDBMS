using RDBMS.Core.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RDBMS.Core.Parsing.Tokenizer;

namespace RDBMS.Core.Tests
{
    public class ParserTests
    {
        #region CREATE TABLE Tests

        [Fact]
        public void Parse_CreateTable_Success()
        {
            // Arrange
            string sql = "CREATE TABLE users (id INT PRIMARY KEY, name VARCHAR(100) NOT NULL, age INT)";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as CreateTableQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal("users", query.TableName);
            Assert.Equal(3, query.Columns.Count);

            Assert.Equal("id", query.Columns[0].Name);
            Assert.Equal("INT", query.Columns[0].DataType);
            Assert.True(query.Columns[0].IsPrimaryKey);

            Assert.Equal("name", query.Columns[1].Name);
            Assert.Equal("VARCHAR", query.Columns[1].DataType);
            Assert.Equal(100, query.Columns[1].MaxLength);
            Assert.True(query.Columns[1].IsNotNull);

            Assert.Equal("age", query.Columns[2].Name);
            Assert.Equal("INT", query.Columns[2].DataType);
        }

        [Fact]
        public void Parse_CreateTable_WithMultipleConstraints_Success()
        {
            // Arrange
            string sql = "CREATE TABLE products (id INT PRIMARY KEY, code VARCHAR(50) UNIQUE NOT NULL)";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as CreateTableQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal("products", query.TableName);
            Assert.Equal(2, query.Columns.Count);

            Assert.Equal("code", query.Columns[1].Name);
            Assert.True(query.Columns[1].IsUnique);
            Assert.True(query.Columns[1].IsNotNull);
        }

        [Fact]
        public void Parse_CreateTable_WithAllDataTypes_Success()
        {
            // Arrange
            string sql = @"CREATE TABLE test_table (
                col_int INT,
                col_varchar VARCHAR(255),
                col_bool BOOLEAN,
                col_datetime DATETIME,
                col_decimal DECIMAL
            )";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as CreateTableQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal("test_table", query.TableName);
            Assert.Equal(5, query.Columns.Count);
            Assert.Equal("INT", query.Columns[0].DataType);
            Assert.Equal("VARCHAR", query.Columns[1].DataType);
            Assert.Equal("BOOLEAN", query.Columns[2].DataType);
            Assert.Equal("DATETIME", query.Columns[3].DataType);
            Assert.Equal("DECIMAL", query.Columns[4].DataType);
        }

        #endregion

        #region INSERT Tests

        [Fact]
        public void Parse_Insert_SingleRow_Success()
        {
            // Arrange
            string sql = "INSERT INTO users (name, age) VALUES ('John', 25)";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as InsertQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal("users", query.TableName);
            Assert.Equal(2, query.Columns.Count);
            Assert.Equal("name", query.Columns[0]);
            Assert.Equal("age", query.Columns[1]);
            Assert.Single(query.Values);
            Assert.Equal("John", query.Values[0][0]);
            Assert.Equal(25, query.Values[0][1]);
        }

        [Fact]
        public void Parse_Insert_MultipleRows_Success()
        {
            // Arrange
            string sql = "INSERT INTO users (name, age) VALUES ('John', 25), ('Jane', 30), ('Bob', 35)";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as InsertQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal("users", query.TableName);
            Assert.Equal(3, query.Values.Count);
            Assert.Equal("Jane", query.Values[1][0]);
            Assert.Equal(30, query.Values[1][1]);
        }

        [Fact]
        public void Parse_Insert_WithoutColumnList_Success()
        {
            // Arrange
            string sql = "INSERT INTO users VALUES ('John', 25, 'john@email.com')";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as InsertQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal("users", query.TableName);
            Assert.Empty(query.Columns); // No explicit columns
            Assert.Single(query.Values);
            Assert.Equal(3, query.Values[0].Count);
        }

        [Fact]
        public void Parse_Insert_WithNullValue_Success()
        {
            // Arrange
            string sql = "INSERT INTO users (name, age, email) VALUES ('John', 25, NULL)";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as InsertQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Null(query.Values[0][2]);
        }

        [Fact]
        public void Parse_Insert_WithBooleanValue_Success()
        {
            // Arrange
            string sql = "INSERT INTO users (name, is_active) VALUES ('John', TRUE)";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as InsertQuery;

            // Assert
            Assert.NotNull(query);
            Assert.True((bool)query.Values[0][1]);
        }

        #endregion

        #region SELECT Tests

        [Fact]
        public void Parse_Select_AllColumns_Success()
        {
            // Arrange
            string sql = "SELECT * FROM users";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as SelectQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal("users", query.TableName);
            Assert.Single(query.Columns);
            Assert.True(query.Columns[0].IsWildcard);
        }

        [Fact]
        public void Parse_Select_SpecificColumns_Success()
        {
            // Arrange
            string sql = "SELECT id, name, age FROM users";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as SelectQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal("users", query.TableName);
            Assert.Equal(3, query.Columns.Count);
            Assert.Equal("id", query.Columns[0].ColumnName);
            Assert.Equal("name", query.Columns[1].ColumnName);
            Assert.Equal("age", query.Columns[2].ColumnName);
        }

        [Fact]
        public void Parse_Select_WithQualifiedColumns_Success()
        {
            // Arrange
            string sql = "SELECT users.id, users.name FROM users";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as SelectQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal(2, query.Columns.Count);
            Assert.Equal("users", query.Columns[0].TableName);
            Assert.Equal("id", query.Columns[0].ColumnName);
        }

        [Fact]
        public void Parse_Select_WithSimpleWhere_Success()
        {
            // Arrange
            string sql = "SELECT * FROM users WHERE id = 1";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as SelectQuery;

            // Assert
            Assert.NotNull(query);
            Assert.NotNull(query.Where);
            Assert.Equal(ConditionType.Simple, query.Where.Type);
            Assert.Equal("id", query.Where.LeftOperand);
            Assert.Equal("=", query.Where.Operator);
            Assert.Equal(1, query.Where.RightOperand);
        }

        [Fact]
        public void Parse_Select_WithCompoundWhere_Success()
        {
            // Arrange
            string sql = "SELECT * FROM users WHERE age > 18 AND name = 'John'";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as SelectQuery;

            // Assert
            Assert.NotNull(query);
            Assert.NotNull(query.Where);
            Assert.Equal(ConditionType.Compound, query.Where.Type);
            Assert.Equal(LogicalOperator.AND, query.Where.LogicalOp);
            Assert.NotNull(query.Where.Left);
            Assert.NotNull(query.Where.Right);
            Assert.Equal("age", query.Where.Left.LeftOperand);
            Assert.Equal(">", query.Where.Left.Operator);
        }

        [Fact]
        public void Parse_Select_WithOrCondition_Success()
        {
            // Arrange
            string sql = "SELECT * FROM users WHERE age < 18 OR age > 65";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as SelectQuery;

            // Assert
            Assert.NotNull(query);
            Assert.NotNull(query.Where);
            Assert.Equal(ConditionType.Compound, query.Where.Type);
            Assert.Equal(LogicalOperator.OR, query.Where.LogicalOp);
        }

        [Fact]
        public void Parse_Select_WithAllOperators_Success()
        {
            // Test each operator
            var operators = new[] { "=", "!=", ">", "<", ">=", "<=" };

            foreach (var op in operators)
            {
                string sql = $"SELECT * FROM users WHERE age {op} 18";
                var tokens = new Tokenizer(sql).Tokenize();
                var parser = new Parser(tokens);
                var query = parser.Parse() as SelectQuery;

                Assert.NotNull(query);
                Assert.Equal(op, query.Where.Operator);
            }
        }

        #endregion

        #region JOIN Tests

        [Fact]
        public void Parse_Select_WithInnerJoin_Success()
        {
            // Arrange
            string sql = "SELECT users.name, orders.product FROM users INNER JOIN orders ON users.id = orders.user_id";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as SelectQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal("users", query.TableName);
            Assert.Single(query.Joins);
            Assert.Equal(JoinType.INNER, query.Joins[0].Type);
            Assert.Equal("orders", query.Joins[0].TableName);

            var condition = query.Joins[0].Condition;
            Assert.Equal("users", condition.LeftTable);
            Assert.Equal("id", condition.LeftColumn);
            Assert.Equal("=", condition.Operator);
            Assert.Equal("orders", condition.RightTable);
            Assert.Equal("user_id", condition.RightColumn);
        }

        [Fact]
        public void Parse_Select_WithLeftJoin_Success()
        {
            // Arrange
            string sql = "SELECT * FROM users LEFT JOIN orders ON users.id = orders.user_id";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as SelectQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Single(query.Joins);
            Assert.Equal(JoinType.LEFT, query.Joins[0].Type);
        }

        [Fact]
        public void Parse_Select_WithMultipleJoins_Success()
        {
            // Arrange
            string sql = @"SELECT * FROM users 
                          INNER JOIN orders ON users.id = orders.user_id 
                          LEFT JOIN products ON orders.product_id = products.id";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as SelectQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal(2, query.Joins.Count);
            Assert.Equal(JoinType.INNER, query.Joins[0].Type);
            Assert.Equal(JoinType.LEFT, query.Joins[1].Type);
        }

        [Fact]
        public void Parse_Select_WithJoinAndWhere_Success()
        {
            // Arrange
            string sql = "SELECT * FROM users INNER JOIN orders ON users.id = orders.user_id WHERE users.age > 18";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as SelectQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Single(query.Joins);
            Assert.NotNull(query.Where);
        }

        #endregion

        #region UPDATE Tests

        [Fact]
        public void Parse_Update_SingleColumn_Success()
        {
            // Arrange
            string sql = "UPDATE users SET name = 'Jane' WHERE id = 1";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as UpdateQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal("users", query.TableName);
            Assert.Single(query.Assignments);
            Assert.Equal("name", query.Assignments[0].ColumnName);
            Assert.Equal("Jane", query.Assignments[0].Value);
            Assert.NotNull(query.Where);
        }

        [Fact]
        public void Parse_Update_MultipleColumns_Success()
        {
            // Arrange
            string sql = "UPDATE users SET name = 'Jane', age = 26, is_active = TRUE WHERE id = 1";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as UpdateQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal(3, query.Assignments.Count);
            Assert.Equal("name", query.Assignments[0].ColumnName);
            Assert.Equal("age", query.Assignments[1].ColumnName);
            Assert.Equal("is_active", query.Assignments[2].ColumnName);
        }

        [Fact]
        public void Parse_Update_WithoutWhere_Success()
        {
            // Arrange
            string sql = "UPDATE users SET is_active = FALSE";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as UpdateQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Null(query.Where);
        }

        #endregion

        #region DELETE Tests

        [Fact]
        public void Parse_Delete_WithWhere_Success()
        {
            // Arrange
            string sql = "DELETE FROM users WHERE id = 1";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as DeleteQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal("users", query.TableName);
            Assert.NotNull(query.Where);
            Assert.Equal("id", query.Where.LeftOperand);
        }

        [Fact]
        public void Parse_Delete_WithoutWhere_Success()
        {
            // Arrange
            string sql = "DELETE FROM users";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as DeleteQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal("users", query.TableName);
            Assert.Null(query.Where);
        }

        [Fact]
        public void Parse_Delete_WithComplexWhere_Success()
        {
            // Arrange
            string sql = "DELETE FROM users WHERE age < 18 OR is_active = FALSE";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as DeleteQuery;

            // Assert
            Assert.NotNull(query);
            Assert.NotNull(query.Where);
            Assert.Equal(ConditionType.Compound, query.Where.Type);
        }

        #endregion

        #region CREATE INDEX Tests

        [Fact]
        public void Parse_CreateIndex_Success()
        {
            // Arrange
            string sql = "CREATE INDEX idx_name ON users(name)";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as CreateIndexQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal("idx_name", query.IndexName);
            Assert.Equal("users", query.TableName);
            Assert.Equal("name", query.ColumnName);
        }

        #endregion

        #region DROP TABLE Tests

        [Fact]
        public void Parse_DropTable_Success()
        {
            // Arrange
            string sql = "DROP TABLE users";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act
            var query = parser.Parse() as DropTableQuery;

            // Assert
            Assert.NotNull(query);
            Assert.Equal("users", query.TableName);
        }

        #endregion

        #region Error Tests

        [Fact]
        public void Parse_InvalidSyntax_ThrowsException()
        {
            // Arrange
            string sql = "INVALID SQL QUERY";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act & Assert
            Assert.Throws<SqlSyntaxException>(() => parser.Parse());
        }

        [Fact]
        public void Parse_MissingFromKeyword_ThrowsException()
        {
            // Arrange
            string sql = "SELECT * users";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act & Assert
            Assert.Throws<SqlSyntaxException>(() => parser.Parse());
        }

        [Fact]
        public void Parse_UnterminatedParenthesis_ThrowsException()
        {
            // Arrange
            string sql = "INSERT INTO users (name VALUES ('John')";
            var tokens = new Tokenizer(sql).Tokenize();
            var parser = new Parser(tokens);

            // Act & Assert
            Assert.Throws<SqlSyntaxException>(() => parser.Parse());
        }

        #endregion
    }
}