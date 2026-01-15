using RDBMS.Core;
using RDBMS.Core.Models;

namespace RDBMS.CLI;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== RDBMS Test ===\n");

        try
        {
            // Create database
            var db = new Database("TestDB");
            Console.WriteLine("✓ Database created\n");

            // Create a users table
            var columns = new List<Column>
            {
                new Column("id", DataType.INT) { IsPrimaryKey = true, IsNotNull = true },
                new Column("name", DataType.VARCHAR) { MaxLength = 100, IsNotNull = true },
                new Column("email", DataType.VARCHAR) { MaxLength = 200, IsUnique = true },
                new Column("age", DataType.INT),
                new Column("active", DataType.BOOLEAN),
                new Column("created_at", DataType.DATETIME)
            };

            db.CreateTable("users", columns);
            Console.WriteLine("✓ Table 'users' created\n");

            // Print schema
            db.PrintTableSchema("users");

            // Insert some data
            db.InsertRow("users", new Dictionary<string, object?>
            {
                { "id", 1 },
                { "name", "John Doe" },
                { "email", "john@example.com" },
                { "age", 30 },
                { "active", true },
                { "created_at", DateTime.Now }
            });

            db.InsertRow("users", new Dictionary<string, object?>
            {
                { "id", 2 },
                { "name", "Jane Smith" },
                { "email", "jane@example.com" },
                { "age", 25 },
                { "active", true },
                { "created_at", DateTime.Now }
            });

            db.InsertRow("users", new Dictionary<string, object?>
            {
                { "id", 3 },
                { "name", "Bob Wilson" },
                { "email", "bob@example.com" },
                { "age", 35 },
                { "active", false },
                { "created_at", DateTime.Now }
            });

            Console.WriteLine("✓ Inserted 3 rows\n");

            // Print data
            db.PrintTableData("users");

            // Create an index
            db.CreateIndex("users", "idx_active", "active");
            Console.WriteLine("✓ Created index on 'active' column\n");

            // Select active users
            var activeUsers = db.Select("users", row =>
                row["active"] is bool active && active);

            Console.WriteLine($"Active users: {activeUsers.Count}");
            foreach (var user in activeUsers)
            {
                Console.WriteLine($"  - {user["name"]} ({user["email"]})");
            }
            Console.WriteLine();

            // Update a user
            int updated = db.Update("users",
                row => (int)row["id"]! == 1,
                row => row["age"] = 31);

            Console.WriteLine($"✓ Updated {updated} row(s)\n");

            // Delete a user
            int deleted = db.Delete("users",
                row => (int)row["id"]! == 3);

            Console.WriteLine($"✓ Deleted {deleted} row(s)\n");

            // Print final state
            db.PrintTableData("users");

            // Database info
            db.PrintDatabaseInfo();

            Console.WriteLine("✓ All tests passed!\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}