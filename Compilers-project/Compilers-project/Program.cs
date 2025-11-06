using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Compilers_project.Lexer;
using Compilers_project.Parser;

namespace Compilers_project;

internal class Program
{
    private static void Main(string[] args)
    {
        bool dumpTokens = false;
        bool dumpAst = false;
        bool dumpAstJson = false;
        bool runSemantic = false;
        var inputs = new List<string>();
        
        foreach (var arg in args)
        {
            if (arg == "--lex")
                dumpTokens = true;
            else if (arg == "--ast")
                dumpAst = true;
            else if (arg == "--ast-json")
                dumpAstJson = true;
            else if (arg == "--semantic")
                runSemantic = true;
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
                        RunParserOnFile(file, dumpTokens, dumpAst, dumpAstJson, runSemantic);
                }
            }
            else if (File.Exists(path))
            {
                RunParserOnFile(path, dumpTokens, dumpAst, dumpAstJson, runSemantic);
            }
            else
            {
                Console.Error.WriteLine($"warning: path '{path}' does not exist.");
            }
        }
    }

    private static void RunParserOnFile(string filePath, bool dumpTokens, bool dumpAst, bool dumpAstJson, bool runSemantic)
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
