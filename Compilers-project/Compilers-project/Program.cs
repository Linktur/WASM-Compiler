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
        var inputs = new List<string>();
        foreach (var arg in args)
        {
            if (arg == "--lex")
                dumpTokens = true;
            else
                inputs.Add(arg);
        }

        if (inputs.Count == 0)
        {
            var discovered = FindTestDirectory();
            if (discovered is null)
            {
                Console.Error.WriteLine(
                    "error: no inputs were provided and the TestCases directory could not be located.");
                return;
            }

            inputs.Add(discovered);
            Console.WriteLine($"No inputs specified, using test directory: {discovered}");
            Console.WriteLine();
        }

        foreach (var path in inputs)
        {
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    if (!IsHidden(file))
                        RunParserOnFile(file, dumpTokens);
                }
            }
            else if (File.Exists(path))
            {
                RunParserOnFile(path, dumpTokens);
            }
            else
            {
                Console.Error.WriteLine($"warning: path '{path}' does not exist.");
            }
        }
    }

    private static string? FindTestDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "TestCases");
            if (Directory.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        return null;
    }

    private static void RunParserOnFile(string filePath, bool dumpTokens)
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
            parser.ParseProgram();

            if (!parser.Diag.HasErrors)
            {
                Console.WriteLine("Parse succeeded with no diagnostics.");
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
