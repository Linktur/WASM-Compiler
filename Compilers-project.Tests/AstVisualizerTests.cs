using Compilers_project.AstVisualizer;
using Xunit;
using ParserClass = Compilers_project.Parser.Parser;
using LexerClass = Compilers_project.Lexer.Lexer;

namespace Compilers_project.Tests;

public class AstVisualizerTests
{
    [Fact]
    public void AstPrinter_ShouldPrintSimpleProgram()
    {
        var source = @"
routine main() is
  print 42
end";
        
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        var printer = new AstPrinter();
        var output = printer.Print(program);
        
        Assert.Contains("Program", output);
        Assert.Contains("RoutineDecl: main", output);
        Assert.Contains("PrintStmt", output);
        Assert.Contains("IntLiteral: 42", output);
    }

    [Fact]
    public void AstPrinter_ShouldPrintRecordType()
    {
        var source = @"
type Point is record
  var x : integer
  var y : integer
end";
        
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        var printer = new AstPrinter();
        var output = printer.Print(program);
        
        Assert.Contains("TypeDecl: Point", output);
        Assert.Contains("RecordType", output);
        Assert.Contains("VarDecl: x", output);
        Assert.Contains("VarDecl: y", output);
    }

    [Fact]
    public void AstJsonExporter_ShouldExportValidJson()
    {
        var source = @"
routine main() is
  print 42
end";
        
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        var exporter = new AstJsonExporter();
        var json = exporter.Export(program);
        
        Assert.Contains("\"type\": \"Program\"", json);
        Assert.Contains("\"type\": \"RoutineDecl\"", json);
        Assert.Contains("\"name\": \"main\"", json);
        Assert.Contains("\"type\": \"Print\"", json);
        Assert.Contains("\"kind\": \"IntLiteral\"", json);
        Assert.Contains("\"value\": 42", json);
    }

    [Fact]
    public void AstJsonExporter_ShouldHandleComplexStructures()
    {
        var source = @"
type Point is record
  var x : integer
  var y : integer
end

routine main() is
  var p : Point
  p.x := 7
  print p.x
end";
        
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        var exporter = new AstJsonExporter();
        var json = exporter.Export(program);
        
        // Проверяем, что JSON валидный
        Assert.NotEmpty(json);
        Assert.Contains("\"type\": \"TypeDecl\"", json);
        Assert.Contains("\"kind\": \"Record\"", json);
        Assert.Contains("\"type\": \"Assign\"", json);
        Assert.Contains("\"kind\": \"FieldAccess\"", json);
    }

    [Fact]
    public void AstPrinter_ShouldHandleForwardDeclaration()
    {
        var source = "routine inc(x : integer) : integer";
        
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        var printer = new AstPrinter();
        var output = printer.Print(program);
        
        Assert.Contains("RoutineDecl: inc", output);
        Assert.Contains("Body: <forward declaration>", output);
    }

    [Fact]
    public void AstPrinter_ShouldHandleExpressions()
    {
        var source = "routine test() : integer => 1 + 2 * 3";
        
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        var printer = new AstPrinter();
        var output = printer.Print(program);
        
        Assert.Contains("ExprBody", output);
        Assert.Contains("BinaryOp", output);
    }
}
