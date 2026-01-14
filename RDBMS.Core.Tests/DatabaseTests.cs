using RDBMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDBMS.Core.Tests;

public class DatabaseTests : IDisposable
{
    private readonly Database _db;
    private readonly string _testDbPath;

    public DatabaseTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}");
        _db = new Database("TestDB", _testDbPath);
    }

    [Fact]
    public void FullWorkflow_CreateInsertSelect_WorksEndToEnd()
    {
        // Arrange: Create table
        var columns = new List<Column>
        {
            new Column("id", DataType.INT) { IsPrimaryKey = true },
            new Column("name", DataType.VARCHAR) { MaxLength = 100 }
        };
        _db.CreateTable("users", columns);

        // Act: Insert data
        _db.InsertRow("users", new Dictionary<string, object?>
        {
            { "id", 1 },
            { "name", "John Doe" }
        });

        // Assert: Select and verify
        var rows = _db.Select("users", row => true);
        Assert.Single(rows);
        Assert.Equal(1, rows[0]["id"]);
        Assert.Equal("John Doe", rows[0]["name"]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDbPath))
        {
            Directory.Delete(_testDbPath, true);
        }
    }
}