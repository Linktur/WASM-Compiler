using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Compilers_project.Lexer;
using Compilers_project.Parser;
using Compilers_project.SemanticAnalyzer;
using Compilers_project.CodeGen;
using Compilers_project.AstVisualizer;
using Compilers_project.WebAssembly;
using ParserClass = Compilers_project.Parser.Parser;
using LexerClass = Compilers_project.Lexer.Lexer;
using SemanticAnalyzerClass = Compilers_project.SemanticAnalyzer.SemanticAnalyzer;

namespace Compilers_project;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowUsage();
            return;
        }

        try
        {
            var options = ParseArguments(args);
            if (options == null)
            {
                ShowUsage();
                return;
            }

            ExecuteCompiler(options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static CompilerOptions ParseArguments(string[] args)
    {
        var options = new CompilerOptions();
        var positionalArgs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--lex":
                    options.ShowLexemes = true;
                    break;
                case "--ast":
                    options.ShowAst = true;
                    break;
                case "--ast-json":
                    options.ShowAstJson = true;
                    break;
                case "--semantic":
                    options.ShowSemantic = true;
                    break;
                case "--compile":
                    options.CompileWat = true;
                    break;
                case "--wasm":
                    options.CompileWasm = true;
                    break;
                case "--validate":
                    options.ValidateWat = true;
                    break;
                case "--run":
                    options.RunWasm = true;
                    break;
                case "--all":
                    options.ShowLexemes = true;
                    options.ShowAst = true;
                    options.ShowSemantic = true;
                    options.CompileWat = true;
                    options.ValidateWat = true;
                    break;
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        options.OutputFile = args[++i];
                    }
                    else
                    {
                        Console.Error.WriteLine("Error: -o requires a filename");
                        return null!;
                    }
                    break;
                case "--help":
                case "-h":
                    ShowUsage();
                    return null!;
                default:
                    if (!args[i].StartsWith("--"))
                    {
                        positionalArgs.Add(args[i]);
                    }
                    else
                    {
                        Console.Error.WriteLine($"Error: Unknown option {args[i]}");
                        return null!;
                    }
                    break;
            }
        }

        if (!options.ShowLexemes && !options.ShowAst && !options.ShowAstJson &&
            !options.ShowSemantic && !options.CompileWat && !options.CompileWasm &&
            !options.ValidateWat && !options.RunWasm)
        {
            options.CompileWat = true;
        }

        options.InputFiles = positionalArgs;
        return options;
    }

    static void ExecuteCompiler(CompilerOptions options)
    {
        if (options.InputFiles.Count == 0)
        {
            Console.Error.WriteLine("Error: No input files specified");
            return;
        }

        foreach (var inputFile in options.InputFiles)
        {
            ProcessFile(inputFile, options);
            if (options.InputFiles.Count > 1)
            {
                Console.WriteLine(new string('=', 60));
            }
        }
    }

    static void ProcessFile(string inputPath, CompilerOptions options)
    {
        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Error: File '{inputPath}' not found");
            return;
        }

        var source = File.ReadAllText(inputPath);
        var directory = Path.GetDirectoryName(inputPath);
        var fileName = string.IsNullOrEmpty(directory)
            ? Path.GetFileNameWithoutExtension(inputPath)
            : Path.Combine(directory, Path.GetFileNameWithoutExtension(inputPath));

        try
        {
            if (options.ShowLexemes)
            {
                Console.WriteLine("\n[LEXER] Tokens:");
                var lexer = new LexerClass(source);
                var tokenCount = 0;
                while (true)
                {
                    var token = lexer.NextToken();
                    Console.WriteLine($"  {token.Type,-15} '{token.Text?.Replace("\n", "\\n")}'");
                    tokenCount++;
                    if (token.Type == TokenType.Eof) break;
                }
                Console.WriteLine($"Total tokens: {tokenCount}\n");
                lexer = new LexerClass(source);
            }

            var parser = new ParserClass(new LexerClass(source));
            var program = parser.ParseProgram();

            if (parser.Diag.HasErrors)
            {
                Console.WriteLine("[PARSER] Parse errors:");
                foreach (var error in parser.Diag.Items)
                    Console.WriteLine($"   - {error.Message}");
                return;
            }

            if (options.ShowAst)
            {
                Console.WriteLine("[PARSER] Abstract Syntax Tree:");
                var astPrinter = new AstPrinter();
                var astString = astPrinter.Print(program);
                Console.WriteLine(astString);
            }

            if (options.ShowAstJson)
            {
                Console.WriteLine("[PARSER] AST JSON:");
                var jsonExporter = new AstJsonExporter();
                var json = jsonExporter.Export(program);
                Console.WriteLine(json);
            }

            if (options.ShowSemantic)
            {
                Console.WriteLine("[SEMANTIC] Semantic Analysis:");
                var semanticAnalyzer = new SemanticAnalyzerClass();
                semanticAnalyzer.Analyze(program);

                if (semanticAnalyzer.Diagnostics.Items.Count > 0)
                {
                    Console.WriteLine("Semantic warnings:");
                    foreach (var diag in semanticAnalyzer.Diagnostics.Items)
                        Console.WriteLine($"   - {diag.Message}");
                }
                else
                {
                    Console.WriteLine("Semantic analysis passed!");
                }
            }

            if (options.CompileWat || options.CompileWasm)
            {
                Console.WriteLine("[CODEGEN] WebAssembly Generation:");
                var wasmGenerator = new SimpleWasmGenerator(program);
                var watCode = wasmGenerator.Generate();

                var outputFile = options.OutputFile ?? $"{fileName}.wat";
                File.WriteAllText(outputFile, watCode);
                Console.WriteLine($"Generated: {outputFile}");

                if (options.CompileWasm)
                {
                    var wasmFile = options.OutputFile?.Replace(".wat", ".wasm") ?? $"{fileName}.wasm";
                    if (RunWat2Wasm(outputFile, wasmFile))
                    {
                        Console.WriteLine($"Generated: {wasmFile}");
                    }
                }
            }

            if (options.ValidateWat)
            {
                Console.WriteLine("[WASM] Validation:");
                var watFile = $"{fileName}.wat";
                if (File.Exists(watFile))
                {
                    try
                    {
                        var wasmRunner = new WasmRunner();
                        wasmRunner.ValidateWat(watFile);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[WASM] Validation failed: {ex.Message}");
                    }
                }
                else
                {
                    Console.Error.WriteLine($"[WASM] .wat file not found: {watFile}");
                }
            }

            if (options.RunWasm)
            {
                var wasmFile = $"{fileName}.wasm";
                var watFile = $"{fileName}.wat";

                try
                {
                    var wasmRunner = new WasmRunner();

                    if (!File.Exists(wasmFile) && File.Exists(watFile))
                    {
                        Console.WriteLine("[WASM] .wasm not found, converting from .wat...");
                        if (!wasmRunner.ConvertWatToWasm(watFile, wasmFile))
                        {
                            Console.Error.WriteLine("[WASM] Failed to convert .wat to .wasm");
                            return;
                        }
                    }

                    if (File.Exists(wasmFile))
                    {
                        wasmRunner.RunWasm(wasmFile);
                    }
                    else
                    {
                        Console.Error.WriteLine($"[WASM] Neither .wasm nor .wat file found");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[WASM] Execution failed: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing '{inputPath}': {ex.Message}");
        }
    }

    static bool RunWat2Wasm(string watFile, string wasmFile)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "wat2wasm",
                    Arguments = $"\"{watFile}\" -o \"{wasmFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                Console.Error.WriteLine($"wat2wasm error: {error}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to run wat2wasm: {ex.Message}");
            Console.Error.WriteLine("Make sure WABT is installed and wat2wasm is in your PATH");
            return false;
        }
    }

    static void ShowUsage()
    {
        Console.WriteLine("WASM Compiler - Simple Compiler for WASM Target");
        Console.WriteLine("===============================================");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run -- <options> <input-file>");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --lex          Show lexical analysis (tokens)");
        Console.WriteLine("  --ast          Show abstract syntax tree");
        Console.WriteLine("  --ast-json     Export AST as JSON");
        Console.WriteLine("  --semantic     Run semantic analysis");
        Console.WriteLine("  --compile       Compile to .wat format");
        Console.WriteLine("  --wasm          Compile to .wat and .wasm formats");
        Console.WriteLine("  --validate     Validate generated .wat file");
        Console.WriteLine("  --run          Compile and execute WebAssembly");
        Console.WriteLine("  --all          Run full pipeline (lex, ast, semantic, compile, validate)");
        Console.WriteLine("  -o <file>      Specify output file name");
        Console.WriteLine("  --help, -h     Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run -- TestCases/Test1             # Compile to .wat (default)");
        Console.WriteLine("  dotnet run -- --lex TestCases/Test1      # Show tokens");
        Console.WriteLine("  dotnet run -- --ast TestCases/Test1      # Show AST");
        Console.WriteLine("  dotnet run -- --ast-json TestCases/Test1 # Export AST as JSON");
        Console.WriteLine("  dotnet run -- --semantic TestCases/Test1 # Semantic analysis");
        Console.WriteLine("  dotnet run -- --validate TestCases/Test1 # Validate .wat file");
        Console.WriteLine("  dotnet run -- --run TestCases/Test1      # Compile and execute");
        Console.WriteLine("  dotnet run -- --all TestCases/Test1       # Full pipeline");
        Console.WriteLine("  dotnet run -- --compile TestCases/Test1 -o output.wat  # Custom output");
        Console.WriteLine();
    }
}

class CompilerOptions
{
    public List<string> InputFiles { get; set; } = new();
    public bool ShowLexemes { get; set; }
    public bool ShowAst { get; set; }
    public bool ShowAstJson { get; set; }
    public bool ShowSemantic { get; set; }
    public bool CompileWat { get; set; }
    public bool CompileWasm { get; set; }
    public bool ValidateWat { get; set; }
    public bool RunWasm { get; set; }
    public string? OutputFile { get; set; }
}