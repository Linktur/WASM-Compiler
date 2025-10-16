using System.IO;
using Xunit;
using ParserClass = Compilers_project.Parser.Parser;
using LexerClass = Compilers_project.Lexer.Lexer;

namespace Compilers_project.Tests;

public class IntegrationTests
{
    private string GetTestCasePath(string testName)
    {
        // Ищем папку TestCases относительно текущей директории
        var current = Directory.GetCurrentDirectory();
        while (current != null)
        {
            var testCasesPath = Path.Combine(current, "TestCases", testName);
            if (File.Exists(testCasesPath))
                return testCasesPath;
            
            current = Directory.GetParent(current)?.FullName;
        }
        
        throw new FileNotFoundException($"Test case {testName} not found");
    }

    private string ReadTestCase(string testName)
    {
        var path = GetTestCasePath(testName);
        return File.ReadAllText(path);
    }

    [Theory]
    [InlineData("Test1")]  // Simple print statements
    [InlineData("Test2")]  // Variables and arithmetic
    [InlineData("Test3")]  // Conditionals
    [InlineData("Test4")]  // Loops
    [InlineData("Test5")]  // Functions
    [InlineData("Test6")]  // Arrays
    [InlineData("Test7")]  // Records
    [InlineData("Test8")]  // Complex expressions
    [InlineData("Test9")]  // Nested structures
    [InlineData("Test10")] // Edge cases
    [InlineData("Test11")] // Record with field access
    [InlineData("Test12")] // Advanced features
    [InlineData("Test13")] // More complex scenarios
    [InlineData("Test14")] // Forward declarations
    [InlineData("Test15")] // Array operations
    public void IntegrationTest_ShouldParseTestCaseWithoutErrors(string testName)
    {
        var source = ReadTestCase(testName);
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors, 
            $"Test {testName} failed with errors: {string.Join(", ", parser.Diag.Items.Select(i => i.Message))}");
    }

    [Fact]
    public void IntegrationTest_SimplePrintProgram()
    {
        var source = @"
routine main() is
  print 42
  print 3.14
  print true
end";
        
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
        Assert.Single(program.Decls);
    }

    [Fact]
    public void IntegrationTest_VariableDeclarationAndUsage()
    {
        var source = @"
routine main() is
  var x : integer
  x := 10
  print x
end";
        
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
    }

    [Fact]
    public void IntegrationTest_RecordTypeAndUsage()
    {
        var source = @"
type Point is record
  var x : integer
  var y : integer
end

routine main() is
  var p : Point
  p.x := 7
  p.y := 9
  print p.x
  print p.y
end";
        
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
        Assert.Equal(2, program.Decls.Count);
    }

    [Fact]
    public void IntegrationTest_ArrayTypeAndUsage()
    {
        var source = @"
type A3 is array[3] integer

routine main() is
  var x : A3
  x[1] := 10
  x[2] := 20
  x[3] := 30
  print x[1]
end";
        
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
    }

    [Fact]
    public void IntegrationTest_ForwardDeclarationAndImplementation()
    {
        var source = @"
routine inc(x : integer) : integer

routine main() is
  print inc(5)
end

routine inc(x : integer) : integer => x + 1";
        
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
        Assert.Equal(3, program.Decls.Count);
    }

    [Fact]
    public void IntegrationTest_CommentsAreIgnored()
    {
        var source = @"
// This is a comment
routine main() is
  var x : integer is 10 // inline comment
  print x
end
// trailing comment";
        
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
    }
}
