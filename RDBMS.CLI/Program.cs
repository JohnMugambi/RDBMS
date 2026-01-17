using RDBMS.Core;
using RDBMS.Core.Models;
using RDBMS.CLI;

namespace RDBMS.CLI;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            // Parse command line arguments
            string dataDirectory = GetDataDirectory(args);

            // Ensure data directory exists
            Directory.CreateDirectory(dataDirectory);

            // Display welcome banner
            DisplayWelcomeBanner();

            // Create and start REPL
            var repl = new Repl(dataDirectory);
            repl.Start();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                   FATAL ERROR                          ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
            Console.ResetColor();
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        
        }
           
    }

    private static string GetDataDirectory(string[] args)
    {
        for (int i =0; i< args.Length -1; i++)
        {
            if (args[i] =="--data" || args[i] == "-d")
            {
                return args[i + 1];
            }
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".rbdms");
    }

    private static void DisplayWelcomeBanner()
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                                        ║");
        Console.WriteLine("║               RDBMS - Interactive Shell                ║");
        Console.WriteLine("║                                                        ║");
        Console.WriteLine("║                      DevChallenge                      ║");
        Console.WriteLine("║                                                        ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("Welcome to Simple RDBMS!");
        Console.WriteLine("Type '.help' for available commands or enter SQL statements.");
        Console.WriteLine("Type '.exit' or '.quit' to exit.");
        Console.WriteLine();
    }
}