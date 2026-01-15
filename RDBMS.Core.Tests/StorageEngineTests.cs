using RDBMS.Core.Models;
using RDBMS.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDBMS.Core.Tests;

public class StorageEngineTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly StorageEngine _storage;

    public StorageEngineTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDbPath);
        _storage = new StorageEngine(_testDbPath);
    }

    [Fact]
    public void CreateTable_NewTable_CreatesFile()
    {
        // Arrange
        var table = new Table("users");
        table.Columns.Add(new Column("id", DataType.INT) { IsPrimaryKey = true });
        table.Columns.Add(new Column("name", DataType.VARCHAR) { MaxLength = 100 });

        // Act
        _storage.CreateTable(table);

        // Assert
        var schemaFile = Path.Combine(_testDbPath, "users_schema.json");
        Assert.True(File.Exists(schemaFile));
    }

    public void Dispose()
    {
        // Cleanup: Delete temp directory after each test
        if (Directory.Exists(_testDbPath))
        {
            Directory.Delete(_testDbPath, true);
        }
    }
}
