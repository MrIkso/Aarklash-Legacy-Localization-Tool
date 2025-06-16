using System;
using System.Diagnostics;
using System.IO;

namespace AarklashTextConverter2
{
    class Program
    {
        private static readonly string _separator = "".PadLeft(Console.BufferWidth > 1 ? Console.BufferWidth - 1 : 80, '-');

        public static void Main(string[] args)
        {
            // If no arguments are provided, show the help screen.
            if (args.Length == 0)
            {
                ExitWithHelp();
                return;
            }

            try
            {
                // 1. Parse command-line arguments to determine mode and file paths.
                Arguments arguments = ParseArgs(args);
                var converter = new DbJsonConverter();

                // 2. Execute the appropriate conversion method.
                switch (arguments.Mode)
                {
                    case Mode.DbToJson:
                        Console.WriteLine($"Exporting '{arguments.SourceFile}' to '{arguments.TargetFile}'...");
                        converter.ExportToJson(arguments.SourceFile, arguments.TargetFile);
                        break;

                    case Mode.JsonToDb:
                        // For JsonToDb, we need the original .db file as a template.
                        // We assume it has the same name as the JSON file, but with a .db extension.
                        string originalDbFilePath = Path.ChangeExtension(arguments.SourceFile, ".db");

                        if (!File.Exists(originalDbFilePath))
                        {
                            throw new FileNotFoundException(
                                $"The original .db file is required as a template but was not found at the expected location: {originalDbFilePath}");
                        }

                        Console.WriteLine($"Importing from '{arguments.SourceFile}' to '{arguments.TargetFile}' using '{originalDbFilePath}' as template...");
                        converter.ImportFromJson(arguments.SourceFile, originalDbFilePath, arguments.TargetFile);
                        break;

                    default:
                        // This case should not be reached if ParseArgs is correct.
                        throw new InvalidOperationException("Unexpected error: Unknown conversion mode.");
                }

                Console.WriteLine("\nConversion successful!");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nAN ERROR OCCURRED:");
                Console.WriteLine(_separator);
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(_separator);
                Console.ResetColor();
                WaitingForAnyKey();

                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Parses command-line arguments to determine the operation mode and file paths.
        /// </summary>
        private static Arguments ParseArgs(string[] args)
        {
            if (args.Length != 1)
            {
                ExitWithHelp();
                return null; // ExitWithHelp will terminate the application
            }

            string filePath = args[0];
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be empty.");
            }

            string ext = Path.GetExtension(filePath)?.ToLowerInvariant();

            if (string.IsNullOrEmpty(ext))
            {
                throw new ArgumentException("Could not determine file extension.");
            }

            Mode mode;
            switch (ext)
            {
                case ".db":
                    mode = Mode.DbToJson;
                    break;
                case ".json":
                    mode = Mode.JsonToDb;
                    break;
                default:
                    throw new ArgumentException($"Unsupported file extension: '{ext}'. Please provide a .db or .json file.");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Source file not found.", filePath);
            }

            return new Arguments(filePath, mode);
        }

        /// <summary>
        /// Displays help information and exits the application.
        /// </summary>
        private static void ExitWithHelp()
        {
            // Use a fallback version if assembly info is unavailable.
            var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0";
            var processName = Process.GetCurrentProcess().ProcessName;

            Console.WriteLine(_separator);
            Console.WriteLine($"AarklashTextConverter2 v{version} by MrIkso");
            Console.WriteLine("Converts Aarklash: Legacy localization files between .db and .json formats.");
            Console.WriteLine();
            Console.WriteLine("USAGE:");
            Console.WriteLine($"  {processName}.exe <file>");
            Console.WriteLine();
            Console.WriteLine("ARGUMENTS:");
            Console.WriteLine("  <file>    Absolute or relative path to the source file.");
            Console.WriteLine("            - If a .db file is provided, it will be converted to .json.");
            Console.WriteLine("            - If a .json file is provided, it will be converted to .db.");
            Console.WriteLine();
            Console.WriteLine("EXAMPLE:");
            Console.WriteLine($"  {processName}.exe Default_loc_en.db");
            Console.WriteLine("  (This will create 'Default_loc_en.json' in the same directory)");
            Console.WriteLine();
            Console.WriteLine($"  {processName}.exe Default_loc_en.json");
            Console.WriteLine("  (This will create 'Default_loc_en.db' using the original .db file as a template)");
            Console.WriteLine(_separator);

            WaitingForAnyKey();
            Environment.Exit(0);
        }

        private static void WaitingForAnyKey()
        {
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// A simple data structure to hold parsed arguments.
        /// </summary>
        private sealed class Arguments
        {
            public readonly string SourceFile;
            public readonly string TargetFile;
            public readonly Mode Mode;

            public Arguments(string filePath, Mode mode)
            {
                SourceFile = filePath ?? throw new ArgumentNullException(nameof(filePath));
                Mode = mode;

                // Determine the target file extension based on the conversion mode.
                string targetExtension = mode == Mode.DbToJson ? ".json" : ".db";
                TargetFile = Path.ChangeExtension(filePath, targetExtension);
            }
        }

        /// <summary>
        /// Defines the conversion direction.
        /// </summary>
        private enum Mode
        {
            DbToJson, // Convert .db -> .json
            JsonToDb  // Convert .json -> .db
        }
    }
}