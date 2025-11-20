using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Compilers_project.Lexer;
using Compilers_project.Parser;
using Compilers_project.CodeGen;

namespace Compilers_project;

internal class Program
{
    private static void Main(string[] args)
    {
        bool dumpTokens = false;
        bool dumpAst = false;
        bool dumpAstJson = false;
        bool runSemantic = false;
        bool compile = false;
        bool generateWasm = false;
        string? outputPath = null;
        var inputs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--lex")
                dumpTokens = true;
            else if (arg == "--ast")
                dumpAst = true;
            else if (arg == "--ast-json")
                dumpAstJson = true;
            else if (arg == "--semantic")
                runSemantic = true;
            else if (arg == "--compile")
                compile = true;
            else if (arg == "--wasm")
            {
                compile = true;
                generateWasm = true;
            }
            else if (arg == "-o" && i + 1 < args.Length)
                outputPath = args[++i];
            else
                inputs.Add(arg);
        }

        if (inputs.Count == 0)
        {
            Console.Error.WriteLine("Usage: Compilers-project [options] <file|directory>");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Options:");
            Console.Error.WriteLine("  --lex        Dump tokens from lexer");
            Console.Error.WriteLine("  --ast        Print AST tree");
            Console.Error.WriteLine("  --ast-json   Export AST as JSON");
            Console.Error.WriteLine("  --semantic   Run semantic analysis");
            Console.Error.WriteLine("  --compile    Generate WASM text format (.wat)");
            Console.Error.WriteLine("  --wasm       Generate WASM binary (.wasm) via wat2wasm");
            Console.Error.WriteLine("  -o <file>    Output file path");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Examples:");
            Console.Error.WriteLine("  Compilers-project TestCases/Test1");
            Console.Error.WriteLine("  Compilers-project --lex TestCases/Test1");
            Console.Error.WriteLine("  Compilers-project --ast TestCases/Test1");
            Console.Error.WriteLine("  Compilers-project --ast-json TestCases/Test1");
            Console.Error.WriteLine("  Compilers-project TestCases");
            return;
        }

        foreach (var path in inputs)
        {
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    if (!IsHidden(file))
                        RunParserOnFile(file, dumpTokens, dumpAst, dumpAstJson, runSemantic, compile, generateWasm, outputPath);
                }
            }
            else if (File.Exists(path))
            {
                RunParserOnFile(path, dumpTokens, dumpAst, dumpAstJson, runSemantic, compile, generateWasm, outputPath);
            }
            else
            {
                Console.Error.WriteLine($"warning: path '{path}' does not exist.");
            }
        }
    }

    private static void RunParserOnFile(string filePath, bool dumpTokens, bool dumpAst, bool dumpAstJson, bool runSemantic, bool compile, bool generateWasm, string? outputPath)
    {
        Console.WriteLine($"=== {filePath} ===");

        try
        {
            var source = ReadSourcePreservingLineCount(filePath);
            if (dumpTokens)
            {
                DumpTokens(source);
                Console.WriteLine();
            }

            var parser = new Parser.Parser(new Lexer.Lexer(source));
            var program = parser.ParseProgram();

            if (!parser.Diag.HasErrors)
            {
                Console.WriteLine("Parse succeeded with no diagnostics.");

                if (dumpAst)
                {
                    Console.WriteLine();
                    Console.WriteLine("=== AST ===");
                    var printer = new AstVisualizer.AstPrinter();
                    Console.WriteLine(printer.Print(program));
                }

                if (dumpAstJson)
                {
                    Console.WriteLine();
                    Console.WriteLine("=== AST JSON ===");
                    var exporter = new AstVisualizer.AstJsonExporter();
                    Console.WriteLine(exporter.Export(program));
                }

                if (runSemantic)
                {
                    Console.WriteLine();
                    Console.WriteLine("=== Semantic Analysis ===");
                    var analyzer = new SemanticAnalyzer.SemanticAnalyzer();
                    analyzer.Analyze(program);

                    if (!analyzer.Diagnostics.HasErrors)
                    {
                        Console.WriteLine("Semantic analysis succeeded with no errors.");
                    }
                    else
                    {
                        foreach (var (span, message) in analyzer.Diagnostics.Items)
                        {
                            Console.WriteLine($"[{span.Line}:{span.Col}] {message}");
                        }
                    }
                }

                if (compile)
                {
                    Console.WriteLine();
                    Console.WriteLine("=== WASM Code Generation ===");
                    var generator = new SimpleWasmGenerator(program);
                    var wat = generator.Generate();

                    var outFile = outputPath ?? Path.ChangeExtension(filePath, ".wat");
                    File.WriteAllText(outFile, wat);
                    Console.WriteLine($"Generated: {outFile}");

                    // Если нужен бинарный .wasm - вызываем wat2wasm
                    if (generateWasm)
                    {
                        var wasmFile = Path.ChangeExtension(outFile, ".wasm");
                        try
                        {
                            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "wat2wasm",
                                Arguments = $"\"{outFile}\" -o \"{wasmFile}\"",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            });

                            if (process != null)
                            {
                                process.WaitForExit();
                                if (process.ExitCode == 0)
                                {
                                    Console.WriteLine($"Generated: {wasmFile}");
                                }
                                else
                                {
                                    var error = process.StandardError.ReadToEnd();
                                    Console.Error.WriteLine($"wat2wasm failed: {error}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Failed to run wat2wasm: {ex.Message}");
                            Console.Error.WriteLine("Make sure wat2wasm is installed and in PATH");
                            Console.Error.WriteLine("Install from: https://github.com/WebAssembly/wabt");
                        }
                    }
                }
            }
            else
            {
                foreach (var (span, message) in parser.Diag.Items)
                {
                    Console.WriteLine($"[{span.Line}:{span.Col}] {message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: failed to process '{filePath}': {ex.Message}");
        }

        Console.WriteLine();
    }

    private static void DumpTokens(string source)
    {
        var lex = new Lexer.Lexer(source);
        for (;;)
        {
            var t = lex.NextToken();
            Console.WriteLine($"{t.Type,-15} @ {t.Span.Line}:{t.Span.Col}  '{t.Text}'");
            if (t.Type == TokenType.Eof) break;
        }
    }

    private static string ReadSourcePreservingLineCount(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("//"))
            {
                builder.AppendLine();
                continue;
            }

            builder.AppendLine(line);
        }
        return builder.ToString();
    }

    private static bool IsHidden(string filePath)
    {
        var name = Path.GetFileName(filePath);
        if (string.IsNullOrEmpty(name)) return false;
        return name[0] == '.';
    }
}
