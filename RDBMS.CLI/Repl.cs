using RDBMS.CLI;
using RDBMS.Core.Execution;
using RDBMS.Core.Parsing;
using RDBMS.Core.Storage;
using RDBMS.Core.Models;

namespace RDBMS.CLI
{
    /// <summary>
    /// Read-Eval-Print Loop for interactive SQL execution
    /// </summary>
    public class Repl
    {
        private readonly StorageEngine _storage;
        private readonly QueryExecutor _executor;
        private bool _running;
        private readonly string _dataDirectory;

        public Repl(string dataDirectory)
        {
            _dataDirectory = dataDirectory;

            try
            {
                _storage = new StorageEngine(dataDirectory);
                _executor = new QueryExecutor(_storage);
                _running = true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to initialize RDBMS: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.ResetColor();
                _running = false;
                throw;
            }
        }

        /// <summary>
        /// Start the REPL loop
        /// </summary>
        public void Start()
        {
            if (!_running)
            {
                Console.WriteLine("REPL initialization failed. Cannot start.");
                return;
            }

            Console.WriteLine($"Data directory: {_dataDirectory}");
            Console.WriteLine();

            while (_running)
            {
                try
                {
                    // Read command (supports multi-line)
                    string command = ReadCommand();

                    if (string.IsNullOrWhiteSpace(command))
                    {
                        continue;
                    }

                    // Handle special commands (start with .)
                    if (command.StartsWith("."))
                    {
                        HandleSpecialCommand(command);
                    }
                    else
                    {
                        // Execute SQL
                        ExecuteSql(command);
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ResetColor();
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Read a command from the user (supports multi-line SQL)
        /// </summary>
        private string ReadCommand()
        {
            var lines = new List<string>();
            string prompt = "RDBMS> ";
            bool isFirstLine = true;

            while (true)
            {
                Console.Write(prompt);
                string? line = Console.ReadLine();

                if (line == null)
                {
                    // Handle Ctrl+C or EOF
                    _running = false;
                    return string.Empty;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    if (isFirstLine)
                    {
                        return string.Empty;
                    }
                    continue;
                }

                lines.Add(line);

                // Check if command is complete
                // Special commands are single-line
                if (line.StartsWith("."))
                {
                    break;
                }

                // SQL commands end with semicolon
                if (line.TrimEnd().EndsWith(";"))
                {
                    break;
                }

                // Set continuation prompt
                prompt = "         ...> ";
                isFirstLine = false;
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Execute SQL statement
        /// </summary>
        private void ExecuteSql(string sql)
        {
            try
            {
                // Remove trailing semicolon if present
                sql = sql.TrimEnd().TrimEnd(';');

                // Tokenize
                var tokenizer = new Tokenizer(sql);
                var tokens = tokenizer.Tokenize();

                // Parse
                var parser = new Parser(tokens);
                var query = parser.Parse();

                // Execute
                var result = _executor.Execute(query);

                // Display result
                TablePrinter.PrintResult(result);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Handle special commands (starting with .)
        /// </summary>
        private void HandleSpecialCommand(string command)
        {
            command = command.Trim().ToLower();

            if (command == ".exit" || command == ".quit")
            {
                _running = false;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Goodbye!");
                Console.ResetColor();
            }
            else if (command == ".help")
            {
                ShowHelp();
            }
            else if (command == ".tables")
            {
                ListTables();
            }
            else if (command.StartsWith(".schema "))
            {
                string tableName = command.Substring(8).Trim();
                ShowSchema(tableName);
            }
            else if (command == ".clear")
            {
                Console.Clear();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Unknown command: {command}");
                Console.WriteLine("Type '.help' for available commands.");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Display help information
        /// </summary>
        private void ShowHelp()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    HELP - Commands                     ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            Console.WriteLine("Special Commands:");
            Console.WriteLine("  .help              Show this help message");
            Console.WriteLine("  .tables            List all tables");
            Console.WriteLine("  .schema <table>    Show table schema");
            Console.WriteLine("  .clear             Clear the screen");
            Console.WriteLine("  .exit | .quit      Exit the REPL");
            Console.WriteLine();

            Console.WriteLine("SQL Commands:");
            Console.WriteLine("  CREATE TABLE       Create a new table");
            Console.WriteLine("  DROP TABLE         Drop a table");
            Console.WriteLine("  CREATE INDEX       Create an index");
            Console.WriteLine("  INSERT INTO        Insert rows");
            Console.WriteLine("  SELECT             Query data");
            Console.WriteLine("  UPDATE             Update rows");
            Console.WriteLine("  DELETE FROM        Delete rows");
            Console.WriteLine();

            Console.WriteLine("Data Types:");
            Console.WriteLine("  INT                Integer numbers");
            Console.WriteLine("  VARCHAR(n)         Variable-length string (max n chars)");
            Console.WriteLine("  BOOLEAN            True/False values");
            Console.WriteLine("  DATETIME           Date and time");
            Console.WriteLine("  DECIMAL            Decimal numbers");
            Console.WriteLine();

            Console.WriteLine("Constraints:");
            Console.WriteLine("  PRIMARY KEY        Unique identifier (auto-indexed)");
            Console.WriteLine("  UNIQUE             No duplicate values");
            Console.WriteLine("  NOT NULL           Value required");
            Console.WriteLine();

            Console.WriteLine("Examples:");
            Console.WriteLine("  CREATE TABLE users (id INT PRIMARY KEY, name VARCHAR(100));");
            Console.WriteLine("  INSERT INTO users VALUES (1, 'John'), (2, 'Jane');");
            Console.WriteLine("  SELECT * FROM users WHERE id = 1;");
            Console.WriteLine("  UPDATE users SET name = 'Johnny' WHERE id = 1;");
            Console.WriteLine("  DELETE FROM users WHERE id = 1;");
            Console.WriteLine();

            Console.WriteLine("Tips:");
            Console.WriteLine("  - SQL statements can span multiple lines");
            Console.WriteLine("  - End SQL statements with semicolon (;)");
            Console.WriteLine("  - SQL keywords are case-insensitive");
            Console.WriteLine("  - Table and column names are case-sensitive");
        }

        /// <summary>
        /// List all tables in the database
        /// </summary>
        private void ListTables()
        {
            try
            {
                var tables = _storage.ListTables();

                if (tables.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No tables found.");
                    Console.ResetColor();
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Tables ({tables.Count}):");
                Console.ResetColor();

                foreach (var tableName in tables.OrderBy(t => t))
                {
                    Console.WriteLine($"  • {tableName}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error listing tables: {ex.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Show schema for a specific table
        /// </summary>
        private void ShowSchema(string tableName)
        {
            try
            {
                var table = _storage.GetTable(tableName);

                if (table == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Table '{tableName}' not found.");
                    Console.ResetColor();
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"╔════════════════════════════════════════════════════════╗");
                Console.WriteLine($"║  Table: {tableName.PadRight(47)}║");
                Console.WriteLine($"╚════════════════════════════════════════════════════════╝");
                Console.ResetColor();
                Console.WriteLine();

                Console.WriteLine("Columns:");
                foreach (var column in table.Columns)
                {
                    var constraints = new List<string>();

                    if (column.IsPrimaryKey)
                        constraints.Add("PRIMARY KEY");
                    if (column.IsUnique && !column.IsPrimaryKey)
                        constraints.Add("UNIQUE");
                    if (column.IsNotNull)
                        constraints.Add("NOT NULL");

                    string typeInfo = column.Type.ToString();
                    if (column.Type == DataType.VARCHAR && column.MaxLength.HasValue)
                    {
                        typeInfo = $"VARCHAR({column.MaxLength})";
                    }

                    string constraintInfo = constraints.Count > 0
                        ? $" [{string.Join(", ", constraints)}]"
                        : "";

                    Console.WriteLine($"  • {column.Name}: {typeInfo}{constraintInfo}");
                }

                // Show indexes
                if (table.Indexes.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Indexes:");
                    foreach (var index in table.Indexes)
                    {
                        Console.WriteLine($"  • {index.Name} on column '{index.ColumnName}'");
                    }
                }

                // Show row count
                Console.WriteLine();
                Console.WriteLine($"Row count: {table.Rows.Count}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error showing schema: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}