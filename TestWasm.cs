using System;
using System.IO;
using Compilers_project.Lexer;
using Compilers_project.Parser;
using Compilers_project.CodeGen;
using Compilers_project.CodeGen.Wasm;

// Simple test to verify WASM generation works
var source = @"
routine main() is
  print 42
  print 3.14
  print true
end
";

var parser = new Parser(new Lexer(source));
var program = parser.ParseProgram();

if (parser.Diag.HasErrors)
{
    Console.WriteLine("Parse errors:");
    foreach (var (span, msg) in parser.Diag.Items)
        Console.WriteLine($"  [{span.Line}:{span.Col}] {msg}");
    return;
}

var analyzer = new Compilers_project.SemanticAnalyzer.SemanticAnalyzer();
analyzer.Analyze(program);

if (analyzer.Diagnostics.HasErrors)
{
    Console.WriteLine("Semantic errors:");
    foreach (var (span, msg) in analyzer.Diagnostics.Items)
        Console.WriteLine($"  [{span.Line}:{span.Col}] {msg}");
    return;
}

Console.WriteLine("Generating WASM...");
var generator = new WasmCodeGenerator(program);
var module = generator.Generate();

// Write text format
var textWriter = new WasmTextWriter(module);
var wat = textWriter.Write();
File.WriteAllText("output.wat", wat);
Console.WriteLine("Written to output.wat");
Console.WriteLine();
Console.WriteLine(wat);

// Write binary format
var binaryWriter = new WasmBinaryWriter(module);
var bytes = binaryWriter.Write();
File.WriteAllBytes("output.wasm", bytes);
Console.WriteLine($"Written to output.wasm ({bytes.Length} bytes)");
