using RDBMS.Core.Execution;
using RDBMS.Core.Models;
using RDBMS.Core.Parsing;
using RDBMS.Core.Storage;

namespace RDBMS.Tests.Execution
{
    public class QueryExecutorTests : IDisposable
    {
        private readonly string _testDataDirectory;
        private readonly StorageEngine _storage;
        private readonly QueryExecutor _executor;

        public QueryExecutorTests()
        {
            // Create temporary test directory
            _testDataDirectory = Path.Combine(Path.GetTempPath(), $"RDBMS_Test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDataDirectory);

            _storage = new StorageEngine(_testDataDirectory);
            _executor = new QueryExecutor(_storage);
        }

        public void Dispose()
        {
            // Clean up test directory
            if (Directory.Exists(_testDataDirectory))
            {
                Directory.Delete(_testDataDirectory, recursive: true);
            }
        }

        #region CREATE TABLE Tests

        [Fact]
        public void Execute_CreateTable_Success()
        {
            // Arrange
            string sql = "CREATE TABLE users (id INT PRIMARY KEY, name VARCHAR(100) NOT NULL, age INT)";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("created successfully", result.Message);
            Assert.Equal(0, result.RowsAffected);

            // Verify table exists
            var table = _storage.GetTable("users");
            Assert.NotNull(table);
            Assert.Equal(3, table.Columns.Count);
        }

        [Fact]
        public void Execute_CreateTable_WithAllDataTypes_Success()
        {
            // Arrange
            string sql = @"CREATE TABLE test_types (
                id INT PRIMARY KEY,
                name VARCHAR(50),
                active BOOLEAN,
                created DATETIME,
                price DECIMAL
            )";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            var table = _storage.GetTable("test_types");
            Assert.Equal(5, table.Columns.Count);
            Assert.Equal(DataType.INT, table.Columns[0].Type);
            Assert.Equal(DataType.VARCHAR, table.Columns[1].Type);
            Assert.Equal(DataType.BOOLEAN, table.Columns[2].Type);
            Assert.Equal(DataType.DATETIME, table.Columns[3].Type);
            Assert.Equal(DataType.DECIMAL, table.Columns[4].Type);
        }

        [Fact]
        public void Execute_CreateTable_AlreadyExists_Fails()
        {
            // Arrange
            string sql = "CREATE TABLE users (id INT PRIMARY KEY)";
            var query = ParseQuery(sql);
            _executor.Execute(query); // Create first time

            // Act
            var result = _executor.Execute(query); // Try to create again

            // Assert
            Assert.False(result.Success);
            Assert.Contains("already exists", result.ErrorMessage);
        }

        #endregion

        #region INSERT Tests

        [Fact]
        public void Execute_Insert_SingleRow_Success()
        {
            // Arrange
            CreateTestTable();
            string sql = "INSERT INTO users (id, name, age) VALUES (1, 'John', 25)";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.RowsAffected);

            // Verify data
            var table = _storage.GetTable("users");
            Assert.Single(table.Rows);
            Assert.Equal(1, table.Rows[0]["id"]);
            Assert.Equal("John", table.Rows[0]["name"]);
            Assert.Equal(25, table.Rows[0]["age"]);
        }

        [Fact]
        public void Execute_Insert_MultipleRows_Success()
        {
            // Arrange
            CreateTestTable();
            string sql = "INSERT INTO users (id, name, age) VALUES (1, 'John', 25), (2, 'Jane', 30)";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.RowsAffected);

            var table = _storage.GetTable("users");
            Assert.Equal(2, table.Rows.Count);
        }

        [Fact]
        public void Execute_Insert_WithoutColumnList_Success()
        {
            // Arrange
            CreateTestTable();
            string sql = "INSERT INTO users VALUES (1, 'John', 25)";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.RowsAffected);
        }

        [Fact]
        public void Execute_Insert_DuplicatePrimaryKey_Fails()
        {
            // Arrange
            CreateTestTable();
            string sql1 = "INSERT INTO users (id, name, age) VALUES (1, 'John', 25)";
            string sql2 = "INSERT INTO users (id, name, age) VALUES (1, 'Jane', 30)";

            _executor.Execute(ParseQuery(sql1));

            // Act
            var result = _executor.Execute(ParseQuery(sql2));

            // Assert
            Assert.False(result.Success);
            Assert.Contains("PRIMARY KEY violation", result.ErrorMessage);
        }

        [Fact]
        public void Execute_Insert_NullInNotNullColumn_Fails()
        {
            // Arrange
            CreateTestTable();
            string sql = "INSERT INTO users (id, name, age) VALUES (1, NULL, 25)";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("cannot be NULL", result.ErrorMessage);
        }

        [Fact]
        public void Execute_Insert_VarcharTooLong_Fails()
        {
            // Arrange
            CreateTestTable();
            string longName = new string('A', 101); // Exceeds VARCHAR(100)
            string sql = $"INSERT INTO users (id, name, age) VALUES (1, '{longName}', 25)";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("exceeds maximum length", result.ErrorMessage);
        }

        #endregion

        #region SELECT Tests

        [Fact]
        public void Execute_Select_AllColumns_Success()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "SELECT * FROM users";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, result.Data.Count);
            Assert.Contains("id", result.ColumnNames);
            Assert.Contains("name", result.ColumnNames);
            Assert.Contains("age", result.ColumnNames);
        }

        [Fact]
        public void Execute_Select_SpecificColumns_Success()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "SELECT name, age FROM users";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, result.Data.Count);
            Assert.Equal(2, result.ColumnNames.Count);
            Assert.Contains("name", result.ColumnNames);
            Assert.Contains("age", result.ColumnNames);
            Assert.DoesNotContain("id", result.ColumnNames);
        }

        [Fact]
        public void Execute_Select_WithWhereEquals_Success()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "SELECT * FROM users WHERE id = 2";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Single(result.Data);
            Assert.Equal("Jane", result.Data[0]["name"]);
        }

        [Fact]
        public void Execute_Select_WithWhereGreaterThan_Success()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "SELECT * FROM users WHERE age > 25";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.Data.Count); // Jane (30) and Bob (35)
        }

        [Fact]
        public void Execute_Select_WithCompoundWhere_Success()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "SELECT * FROM users WHERE age > 20 AND age < 30";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Single(result.Data); // Only John (25)
            Assert.Equal("John", result.Data[0]["name"]);
        }

        [Fact]
        public void Execute_Select_WithOrCondition_Success()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "SELECT * FROM users WHERE age = 25 OR age = 35";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.Data.Count); // John and Bob
        }

        [Fact]
        public void Execute_Select_NoMatchingRows_ReturnsEmpty()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "SELECT * FROM users WHERE age > 100";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Empty(result.Data);
        }

        #endregion

        #region JOIN Tests

        [Fact]
        public void Execute_Select_InnerJoin_Success()
        {
            CreateTestTableWithData();
            // Arrange
            CreateTestTablesWithJoinData();
            string sql = "SELECT users.name, orders.product FROM users INNER JOIN orders ON users.id = orders.user_id";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, result.Data.Count); // 3 orders
            Assert.Equal("John", result.Data[0]["name"]);
            Assert.Equal("Laptop", result.Data[0]["product"]);
        }

        [Fact]
        public void Execute_Select_LeftJoin_Success()
        {
            // Arrange
            CreateTestTablesWithJoinData();
            string sql = "SELECT users.name, orders.product FROM users LEFT JOIN orders ON users.id = orders.user_id";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(4, result.Data.Count); // 3 orders + 1 user with no orders

            // Find Bob who has no orders
            var bobRow = result.Data.FirstOrDefault(r => r["name"].ToString() == "Bob");
            Assert.NotNull(bobRow);
            Assert.Null(bobRow["product"]); // NULL for no matching order
        }

        #endregion

        #region UPDATE Tests

        [Fact]
        public void Execute_Update_SingleRow_Success()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "UPDATE users SET name = 'Johnny', age = 26 WHERE id = 1";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.RowsAffected);

            // Verify update
            var table = _storage.GetTable("users");
            var updatedRow = table.Rows.First(r => (int)r["id"] == 1);
            Assert.Equal("Johnny", updatedRow["name"]);
            Assert.Equal(26, updatedRow["age"]);
        }

        [Fact]
        public void Execute_Update_MultipleRows_Success()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "UPDATE users SET age = 40 WHERE age > 25";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.RowsAffected); // Jane and Bob

            // Verify updates
            var table = _storage.GetTable("users");
            Assert.Equal(2, table.Rows.Count(r => (int)r["age"] == 40));
        }

        [Fact]
        public void Execute_Update_WithoutWhere_UpdatesAll()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "UPDATE users SET age = 50";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, result.RowsAffected);

            // Verify all updated
            var table = _storage.GetTable("users");
            Assert.All(table.Rows, r => Assert.Equal(50, r["age"]));
        }

        [Fact]
        public void Execute_Update_PrimaryKey_Fails()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "UPDATE users SET id = 999 WHERE id = 1";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("PRIMARY KEY", result.ErrorMessage);
        }

        [Fact]
        public void Execute_Update_NoMatchingRows_Success()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "UPDATE users SET age = 100 WHERE id = 999";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, result.RowsAffected);
        }

        #endregion

        #region CREATE INDEX Tests

        [Fact]
        public void Execute_CreateIndex_Success()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "CREATE INDEX idx_age ON users(age)";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("created successfully", result.Message);

            // Verify index exists
            var table = _storage.GetTable("users");
            Assert.Contains(table.Indexes, idx => idx.Name == "idx_age");
        }

        #endregion

        #region DELETE Tests

        [Fact]
        public void Execute_Delete_SingleRow_Success()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "DELETE FROM users WHERE id = 2";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.RowsAffected);

            // Verify deletion
            var table = _storage.GetTable("users");
            Assert.Equal(2, table.Rows.Count);
            Assert.DoesNotContain(table.Rows, r => (int)r["id"] == 2);
        }

        [Fact]
        public void Execute_Delete_MultipleRows_Success()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "DELETE FROM users WHERE age > 25";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.RowsAffected); // Jane and Bob

            // Verify only John remains
            var table = _storage.GetTable("users");
            Assert.Single(table.Rows);
            Assert.Equal("John", table.Rows[0]["name"]);
        }

        [Fact]
        public void Execute_Delete_WithoutWhere_DeletesAll()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "DELETE FROM users";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, result.RowsAffected);

            // Verify all deleted
            var table = _storage.GetTable("users");
            Assert.Empty(table.Rows);
        }

        [Fact]
        public void Execute_Delete_NoMatchingRows_Success()
        {
            // Arrange
            CreateTestTableWithData();
            string sql = "DELETE FROM users WHERE id = 999";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, result.RowsAffected);

            // Verify nothing deleted
            var table = _storage.GetTable("users");
            Assert.Equal(3, table.Rows.Count);
        }

        #endregion

        #region DROP TABLE Tests

        [Fact]
        public void Execute_DropTable_Success()
        {
            // Arrange
            CreateTestTable();
            string sql = "DROP TABLE users";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("dropped successfully", result.Message);

            // Verify table no longer exists
            Assert.Throws<TableNotFoundException>(() => _storage.GetTable("users"));
        }

        [Fact]
        public void Execute_DropTable_NotExists_Fails()
        {
            // Arrange
            string sql = "DROP TABLE nonexistent";
            var query = ParseQuery(sql);

            // Act
            var result = _executor.Execute(query);

            // Assert
            Assert.False(result.Success);
            Assert.Contains(" not found", result.ErrorMessage);
        }

        #endregion

        #region Helper Methods

        private void CreateTestTable()
        {
            string sql = "CREATE TABLE users (id INT PRIMARY KEY, name VARCHAR(100) NOT NULL, age INT)";
            var query = ParseQuery(sql);
            _executor.Execute(query);
        }

        private void CreateTestTableWithData()
        {
            CreateTestTable();

            // Insert test data
            var insertQueries = new[]
            {
                "INSERT INTO users (id, name, age) VALUES (1, 'John', 25)",
                "INSERT INTO users (id, name, age) VALUES (2, 'Jane', 30)",
                "INSERT INTO users (id, name, age) VALUES (3, 'Bob', 35)"
            };

            foreach (var sql in insertQueries)
            {
                _executor.Execute(ParseQuery(sql));
            }
        }

        private void CreateTestTablesWithJoinData()
        {
            // Create users table
            CreateTestTableWithData();

            // Create orders table
            string createOrdersSql = "CREATE TABLE orders (id INT PRIMARY KEY, user_id INT, product VARCHAR(100))";
            _executor.Execute(ParseQuery(createOrdersSql));

            // Insert order data
            var orderInserts = new[]
            {
                "INSERT INTO orders (id, user_id, product) VALUES (1, 1, 'Laptop')",
                "INSERT INTO orders (id, user_id, product) VALUES (2, 1, 'Mouse')",
                "INSERT INTO orders (id, user_id, product) VALUES (3, 2, 'Keyboard')"
                // Note: Bob (id=3) has no orders for LEFT JOIN testing
            };

            foreach (var sql in orderInserts)
            {
                _executor.Execute(ParseQuery(sql));
            }
        }

        private IQuery ParseQuery(string sql)
        {
            var tokenizer = new Tokenizer(sql);
            var tokens = tokenizer.Tokenize();
            var parser = new Parser(tokens);
            return parser.Parse();
        }

        #endregion
    }
}