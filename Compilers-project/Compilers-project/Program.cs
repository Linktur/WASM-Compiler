using System;

namespace Compilers_project;

/// <summary>
/// WASM Compiler - Simple Test System
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("WASM Compiler - Test System");
        Console.WriteLine("=============================\n");

        if (args.Length > 0)
        {
            // Command line arguments
            if (args[0] == "all")
            {
                CompilerTests.RunAllTests();
            }
            else if (args[0] == "list")
            {
                CompilerTests.ListTests();
            }
            else if (args.Length > 1 && args[1] == "verbose")
            {
                CompilerTests.RunTest(args[0], true);
            }
            else if (int.TryParse(args[0], out int testNumber))
            {
                CompilerTests.RunTestByNumber(testNumber);
            }
            else
            {
                CompilerTests.RunTest(args[0]);
            }
        }
        else
        {
            // Interactive menu
            ShowMenu();
        }
    }

    static void ShowMenu()
    {
        while (true)
        {
            Console.WriteLine("Test Menu:");
            Console.WriteLine("1. Run All Tests");
            Console.WriteLine("2. List Available Tests");
            Console.WriteLine("3. Run Specific Test by Number");
            Console.WriteLine("4. Run Specific Test by Name");
            Console.WriteLine("5. Run Test with Verbose Output");
            Console.WriteLine("0. Exit");
            Console.WriteLine();

            Console.Write("Select option (0-5): ");
            var input = Console.ReadLine();

            if (string.IsNullOrEmpty(input))
                continue;

            switch (input.Trim())
            {
                case "0":
                    Console.WriteLine("Goodbye!");
                    return;

                case "1":
                    CompilerTests.RunAllTests();
                    break;

                case "2":
                    CompilerTests.ListTests();
                    break;

                case "3":
                    Console.Write("Enter test number: ");
                    var numInput = Console.ReadLine();
                    if (int.TryParse(numInput, out int testNum))
                    {
                        CompilerTests.RunTestByNumber(testNum);
                    }
                    else
                    {
                        Console.WriteLine("Invalid number");
                    }
                    break;

                case "4":
                    Console.Write("Enter test name: ");
                    var testName = Console.ReadLine();
                    if (!string.IsNullOrEmpty(testName))
                    {
                        CompilerTests.RunTest(testName);
                    }
                    break;

                case "5":
                    Console.Write("Enter test name for verbose mode: ");
                    var verboseTestName = Console.ReadLine();
                    if (!string.IsNullOrEmpty(verboseTestName))
                    {
                        CompilerTests.RunTest(verboseTestName, true);
                    }
                    break;

                default:
                    Console.WriteLine("Invalid option. Please try again.\n");
                    break;
            }

            Console.WriteLine("\n" + new string('=', 50) + "\n");
        }
    }
}