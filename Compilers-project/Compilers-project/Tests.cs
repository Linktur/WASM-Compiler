using System;
using System.Collections.Generic;
using System.Linq;
using LexerClass = Compilers_project.Lexer.Lexer;
using ParserClass = Compilers_project.Parser.Parser;
using SemanticAnalyzerClass = Compilers_project.SemanticAnalyzer.SemanticAnalyzer;
using ProgramOptimizer = Compilers_project.SemanticAnalyzer.ProgramOptimizer;
using Compilers_project.Lexer;
using Compilers_project.Parser.AST;

namespace Compilers_project;
public static class CompilerTests
{
    private static readonly Dictionary<string, string> TestCases = new()
    {
        // Basic Language Features
        ["Basic Literals"] = @"
routine main() is
  var x : integer is 42
  var y : real is 3.14
  var b : boolean is true
  print x, y, b
end",

        ["Variable Operations"] = @"
routine main() is
  var x : integer
  var y : real
  x := 10
  y := 2.5
  print x, y
end",

        ["Arrays"] = @"
routine main() is
  var arr : array[3] integer
  arr[0] := 10
  arr[1] := 20
  arr[2] := 30
  print arr[0], arr[1], arr[2]
end",

        ["Records"] = @"
type Point is record
  var x : integer
  var y : integer
end

routine main() is
  var p : Point
  p.x := 5
  p.y := 10
  print p.x, p.y
end",

        ["Procedures"] = @"
routine add(a : integer, b : integer) : integer is
  return a + b
end

routine main() is
  var result : integer is add(3, 7)
  print result
end",

        ["Forward Declaration"] = @"
routine test() : integer

routine main() is
  print test()
end

routine test() : integer => 42",

        ["Arrow Functions"] = @"
routine square(x : integer) : integer => x * x
routine is_positive(x : integer) : boolean => x > 0

routine main() is
  var s : integer is square(5)
  var p : boolean is is_positive(-3)
  print s, p
end",

        ["Boolean Logic"] = @"
routine main() is
  var a : boolean is true and false
  var b : boolean is true or false
  var c : boolean is not true
  var d : boolean is 5 > 3 and 2 < 4
  print a, b, c, d
end",

        ["If Statements"] = @"
routine main() is
  var x : integer is 10
  var result : integer

  if x > 5 then
    result := 1
  else
    result := 0
  end

  if x = 10 then
    result := result + 100
  end

  print result
end",

        ["While Loops"] = @"
routine main() is
  var i : integer is 0
  var sum : integer is 0

  while i < 5 loop
    sum := sum + i
    i := i + 1
  end

  print sum
end",

        ["For Loops"] = @"
routine main() is
  var sum : integer is 0

  for i in 1..5 loop
    sum := sum + i
  end

  print sum
end",

        ["Complex Expressions"] = @"
routine main() is
  var a : integer is 2 + 3 * 4
  var b : real is (1.5 + 2.5) / 2.0
  var c : boolean is (5 > 3) and (2 < 4)
  print a, b, c
end",

        ["Nested Structures"] = @"
type Address is record
  var street : integer
  var number : integer
end

type Person is record
  var age : integer
  var address : Address
end

routine is_adult(p : Person) : boolean is
  return p.age >= 18
end

routine main() is
  var person : Person
  var addr : Address

  addr.street := 123
  addr.number := 45

  person.age := 25
  person.address := addr

  print is_adult(person)
  print person.address.street
end",

        ["Recursion"] = @"
routine factorial(n : integer) : integer is
  if n <= 1 then
    return 1
  else
    return n * factorial(n - 1)
  end
end

routine main() is
  print factorial(5)
end",

        ["Math Operations"] = @"
routine gcd(a : integer, b : integer) : integer is
  if b = 0 then
    return a
  else
    return gcd(b, a % b)
  end
end

routine main() is
  print gcd(48, 18)
end"
    };

    /// <summary>
    /// Запустить все тесты
    /// </summary>
    public static void RunAllTests()
    {
        Console.WriteLine("Running Compiler Tests");
        Console.WriteLine("========================\n");

        var totalTests = TestCases.Count;
        var passedTests = 0;
        var failedTests = 0;

        foreach (var testCase in TestCases)
        {
            Console.WriteLine($"{testCase.Key}");
            Console.WriteLine(new string('-', testCase.Key.Length));

            try
            {
                var lexer = new LexerClass(testCase.Value);
                var parser = new ParserClass(lexer);
                var program = parser.ParseProgram();

                if (parser.Diag.HasErrors)
                {
                    Console.WriteLine("Parse failed:");
                    foreach (var error in parser.Diag.Items)
                        Console.WriteLine($"   - {error.Message}");
                    failedTests++;
                }
                else
                {
                    // Semantic analysis
                    var semanticAnalyzer = new SemanticAnalyzerClass();
                    semanticAnalyzer.Analyze(program);

                    if (semanticAnalyzer.Diagnostics.Items.Count > 0)
                    {
                        Console.WriteLine("Semantic warnings:");
                        foreach (var diag in semanticAnalyzer.Diagnostics.Items)
                            Console.WriteLine($"   - {diag.Message}");
                    }

                    // Optimization
                    var optimized = ProgramOptimizer.Optimize(program);

                    Console.WriteLine("Passed");
                    passedTests++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                failedTests++;
            }

            Console.WriteLine();
        }

        // Results
        Console.WriteLine("========================");
        Console.WriteLine($"Results: {passedTests}/{totalTests} passed");

        if (failedTests == 0)
        {
            Console.WriteLine("All tests passed!");
        }
        else
        {
            Console.WriteLine($"{failedTests} tests failed");
        }

        Console.WriteLine($"Success Rate: {passedTests * 100 / totalTests}%");
    }

    /// <summary>
    /// Запустить конкретный тест с детальной информацией
    /// </summary>
    public static void RunTest(string testName, bool verbose = false)
    {
        if (TestCases.TryGetValue(testName, out var source))
        {
            Console.WriteLine($"Running Test: {testName}");
            Console.WriteLine(new string('=', testName.Length + 14));
            Console.WriteLine("Source Code:");
            Console.WriteLine(source.Trim());
            Console.WriteLine(new string('-', 50));

            try
            {
                // Step 1: Lexical Analysis
                if (verbose)
                {
                    Console.WriteLine("\n[LEXER] Tokenizing...");
                    var lexer = new LexerClass(source);
                    var tokenCount = 0;
                    while (true)
                    {
                        var token = lexer.NextToken();
                        if (verbose)
                        {
                            Console.WriteLine($"   Token {tokenCount++}: {token.Type} = '{token.Text?.Replace("\n", "\\n")}'");
                        }
                        if (token.Type == TokenType.Eof) break;
                    }
                    Console.WriteLine($"[LEXER] Total tokens: {tokenCount}");
                    Console.WriteLine();

                    lexer = new LexerClass(source); // Reset lexer
                }
                else
                {
                    Console.WriteLine("\n[LEXER] Tokenizing completed");
                }

                // Step 2: Syntactic Analysis
                Console.WriteLine("[PARSER] Parsing AST...");
                var parser = new ParserClass(new LexerClass(source));
                var program = parser.ParseProgram();

                if (parser.Diag.HasErrors)
                {
                    Console.WriteLine("[PARSER] Parse failed:");
                    foreach (var error in parser.Diag.Items)
                        Console.WriteLine($"   - {error.Message}");
                    return;
                }
                else
                {
                    Console.WriteLine("[PARSER] Parsing successful!");
                    Console.WriteLine($"[PARSER] Found {program.Decls.Count} declarations");

                    if (verbose)
                    {
                        foreach (var decl in program.Decls)
                        {
                            Console.WriteLine($"   Declaration: {decl.GetType().Name}");
                            if (decl is RoutineDecl routine)
                            {
                                Console.WriteLine($"     Routine: {routine.Name}, Parameters: {routine.Parameters.Count}");
                                Console.WriteLine($"     Has body: {routine.Body != null}");
                            }
                        }
                    }
                }

                // Step 3: Semantic Analysis
                Console.WriteLine("\n[SEMANTIC] Type checking...");
                var semanticAnalyzer = new SemanticAnalyzerClass();
                semanticAnalyzer.Analyze(program);

                if (semanticAnalyzer.Diagnostics.Items.Count > 0)
                {
                    Console.WriteLine("[SEMANTIC] Semantic warnings:");
                    foreach (var diag in semanticAnalyzer.Diagnostics.Items)
                        Console.WriteLine($"   - {diag.Message}");
                }
                else
                {
                    Console.WriteLine("[SEMANTIC] Semantic analysis passed!");
                }

                // Step 4: Optimization
                Console.WriteLine("[OPTIMIZER] Applying optimizations...");
                var optimized = ProgramOptimizer.Optimize(program);
                Console.WriteLine("[OPTIMIZER] Optimization complete!");

                // Final Statistics
                Console.WriteLine($"\nFinal Statistics:");
                Console.WriteLine($"   Original declarations: {program.Decls.Count}");
                Console.WriteLine($"   Parse errors: {parser.Diag.Items.Count}");
                Console.WriteLine($"   Semantic warnings: {semanticAnalyzer.Diagnostics.Items.Count}");
                Console.WriteLine($"   Test status: PASSED");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception occurred: {ex.Message}");
                if (verbose)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
        }
        else
        {
            Console.WriteLine($"Test '{testName}' not found!");
            Console.WriteLine("\nAvailable tests:");
            foreach (var key in TestCases.Keys)
                Console.WriteLine($"   • {key}");
        }
    }

    /// <summary>
    /// Показать список всех доступных тестов
    /// </summary>
    public static void ListTests()
    {
        Console.WriteLine("Available Tests:");
        int i = 1;
        foreach (var key in TestCases.Keys)
        {
            Console.WriteLine($"{i++.ToString().PadLeft(2)}. {key}");
        }
    }

    /// <summary>
    /// Запустить тест по номеру
    /// </summary>
    public static void RunTestByNumber(int number)
    {
        var testList = TestCases.Keys.ToList();
        if (number > 0 && number <= testList.Count)
        {
            RunTest(testList[number - 1]);
        }
        else
        {
            Console.WriteLine($"Invalid test number. Available: 1-{testList.Count}");
        }
    }
}