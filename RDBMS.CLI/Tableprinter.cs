using RDBMS.Core.Execution;

namespace RDBMS.CLI
{
    /// <summary>
    /// Formats query results into pretty ASCII tables
    /// </summary>
    public static class TablePrinter
    {
        /// <summary>
        /// Print a query result with appropriate formatting
        /// </summary>
        public static void PrintResult(QueryResult result)
        {
            if (!result.Success)
            {
                PrintError(result.ErrorMessage ?? "Unknown error");
                return;
            }

            // For SELECT queries with data
            if (result.Data != null && result.Data.Count > 0)
            {
                PrintTable(result.Data, result.ColumnNames ?? new List<string>());
            }
            // For other queries or SELECT with no results
            else
            {
                PrintSuccess(result.Message ?? "Query executed successfully", result.RowsAffected);
            }
        }

        /// <summary>
        /// Print data as an ASCII table
        /// </summary>
        private static void PrintTable(List<Dictionary<string, object>> rows, List<string> columns)
        {
            if (rows.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("0 rows returned.");
                Console.ResetColor();
                return;
            }

            // Calculate column widths
            var widths = CalculateColumnWidths(rows, columns);

            // Print table
            PrintSeparator(columns, widths);
            PrintHeader(columns, widths);
            PrintSeparator(columns, widths);
            PrintRows(rows, columns, widths);
            PrintSeparator(columns, widths);

            // Print summary
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{rows.Count} row(s) returned.");
            Console.ResetColor();
        }

        /// <summary>
        /// Calculate the width needed for each column
        /// </summary>
        private static Dictionary<string, int> CalculateColumnWidths(
            List<Dictionary<string, object>> rows,
            List<string> columns)
        {
            var widths = new Dictionary<string, int>();

            foreach (var col in columns)
            {
                // Start with column name length
                int maxWidth = col.Length;

                // Check all row values
                foreach (var row in rows)
                {
                    if (row.TryGetValue(col, out var value))
                    {
                        string valueStr = FormatValue(value);
                        maxWidth = Math.Max(maxWidth, valueStr.Length);
                    }
                }

                // Minimum width of 4 for "NULL"
                widths[col] = Math.Max(maxWidth, 4);
            }

            return widths;
        }

        /// <summary>
        /// Print the separator line
        /// </summary>
        private static void PrintSeparator(List<string> columns, Dictionary<string, int> widths)
        {
            Console.Write("+");
            foreach (var col in columns)
            {
                Console.Write(new string('-', widths[col] + 2));
                Console.Write("+");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Print the header row
        /// </summary>
        private static void PrintHeader(List<string> columns, Dictionary<string, int> widths)
        {
            Console.Write("| ");
            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(col.PadRight(widths[col]));
                Console.ResetColor();

                if (i < columns.Count - 1)
                    Console.Write(" | ");
                else
                    Console.Write(" |");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Print all data rows
        /// </summary>
        private static void PrintRows(
            List<Dictionary<string, object>> rows,
            List<string> columns,
            Dictionary<string, int> widths)
        {
            foreach (var row in rows)
            {
                Console.Write("| ");
                for (int i = 0; i < columns.Count; i++)
                {
                    var col = columns[i];
                    string value = "NULL";

                    if (row.TryGetValue(col, out var cellValue))
                    {
                        value = FormatValue(cellValue);
                    }

                    // Right-align numbers, left-align everything else
                    if (IsNumeric(cellValue))
                    {
                        Console.Write(value.PadLeft(widths[col]));
                    }
                    else
                    {
                        if (cellValue == null)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                        }
                        Console.Write(value.PadRight(widths[col]));
                        Console.ResetColor();
                    }

                    if (i < columns.Count - 1)
                        Console.Write(" | ");
                    else
                        Console.Write(" |");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Format a value for display
        /// </summary>
        private static string FormatValue(object? value)
        {
            if (value == null)
                return "NULL";

            if (value is DateTime dt)
                return dt.ToString("yyyy-MM-dd HH:mm:ss");

            if (value is decimal dec)
                return dec.ToString("0.##");

            if (value is double dbl)
                return dbl.ToString("0.##");

            if (value is float flt)
                return flt.ToString("0.##");

            if (value is bool b)
                return b ? "TRUE" : "FALSE";

            return value.ToString() ?? "NULL";
        }

        /// <summary>
        /// Check if a value is numeric
        /// </summary>
        private static bool IsNumeric(object? value)
        {
            return value is int || value is long || value is decimal ||
                   value is double || value is float;
        }

        /// <summary>
        /// Print a success message
        /// </summary>
        private static void PrintSuccess(string message, int rowsAffected)
        {
            Console.ForegroundColor = ConsoleColor.Green;

            if (rowsAffected > 0)
            {
                Console.WriteLine($"{message} ({rowsAffected} row(s) affected)");
            }
            else
            {
                Console.WriteLine(message);
            }

            Console.ResetColor();
        }

        /// <summary>
        /// Print an error message
        /// </summary>
        private static void PrintError(string errorMessage)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {errorMessage}");
            Console.ResetColor();
        }
    }
}