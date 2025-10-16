using Compilers_project.Parser.AST;
using Xunit;
using ParserClass = Compilers_project.Parser.Parser;
using LexerClass = Compilers_project.Lexer.Lexer;

namespace Compilers_project.Tests;

public class ParserTests
{
    [Fact]
    public void Parser_ShouldParseSimpleVarDeclaration()
    {
        var parser = new ParserClass(new LexerClass("var x : integer"));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
        Assert.Single(program.Decls);
        
        var varDecl = Assert.IsType<VarDecl>(program.Decls[0]);
        Assert.Equal("x", varDecl.Name);
    }

    [Fact]
    public void Parser_ShouldParseVarWithInitializer()
    {
        var parser = new ParserClass(new LexerClass("var x : integer is 42"));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
        var varDecl = Assert.IsType<VarDecl>(program.Decls[0]);
        Assert.Equal("x", varDecl.Name);
        Assert.NotNull(varDecl.Initializer);
    }

    [Fact]
    public void Parser_ShouldParseTypeDeclaration()
    {
        var parser = new ParserClass(new LexerClass("type MyInt is integer"));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
        var typeDecl = Assert.IsType<TypeDecl>(program.Decls[0]);
        Assert.Equal("MyInt", typeDecl.Name);
    }

    [Fact]
    public void Parser_ShouldParseRecordType()
    {
        var source = @"
type Point is record
  var x : integer
  var y : integer
end";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
        var typeDecl = Assert.IsType<TypeDecl>(program.Decls[0]);
        var recordType = Assert.IsType<RecordTypeRef>(typeDecl.Type);
        Assert.Equal(2, recordType.Fields.Count);
    }

    [Fact]
    public void Parser_ShouldParseArrayType()
    {
        var parser = new ParserClass(new LexerClass("type A3 is array[3] integer"));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
        var typeDecl = Assert.IsType<TypeDecl>(program.Decls[0]);
        var arrayType = Assert.IsType<ArrayTypeRef>(typeDecl.Type);
        Assert.NotNull(arrayType.Size);
    }

    [Fact]
    public void Parser_ShouldParseSimpleRoutine()
    {
        var source = @"
routine main() is
  print 42
end";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
        var routine = Assert.IsType<RoutineDecl>(program.Decls[0]);
        Assert.Equal("main", routine.Name);
        Assert.NotNull(routine.Body);
    }

    [Fact]
    public void Parser_ShouldParseRoutineWithParameters()
    {
        var source = "routine add(a : integer, b : integer) : integer => a + b";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
        var routine = Assert.IsType<RoutineDecl>(program.Decls[0]);
        Assert.Equal("add", routine.Name);
        Assert.Equal(2, routine.Parameters.Count);
        Assert.NotNull(routine.ReturnType);
    }

    [Fact]
    public void Parser_ShouldParseForwardDeclaration()
    {
        var source = "routine inc(x : integer) : integer";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
        var routine = Assert.IsType<RoutineDecl>(program.Decls[0]);
        Assert.Equal("inc", routine.Name);
        Assert.Null(routine.Body);
    }

    [Fact]
    public void Parser_ShouldParseIfStatement()
    {
        var source = @"
routine test() is
  if true then
    print 1
  end
end";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
    }

    [Fact]
    public void Parser_ShouldParseIfElseStatement()
    {
        var source = @"
routine test() is
  if true then
    print 1
  else
    print 2
  end
end";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
    }

    [Fact]
    public void Parser_ShouldParseWhileLoop()
    {
        var source = @"
routine test() is
  while true loop
    print 1
  end
end";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
    }

    [Fact]
    public void Parser_ShouldParseForLoop()
    {
        var source = @"
routine test() is
  for i in 1..10 loop
    print i
  end
end";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
    }

    [Fact]
    public void Parser_ShouldParseAssignment()
    {
        var source = @"
routine test() is
  var x : integer
  x := 42
end";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
    }

    [Fact]
    public void Parser_ShouldParseFieldAccess()
    {
        var source = @"
routine test() is
  var p : Point
  p.x := 10
end";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
    }

    [Fact]
    public void Parser_ShouldParseArrayAccess()
    {
        var source = @"
routine test() is
  var arr : A3
  arr[1] := 10
end";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
    }

    [Fact]
    public void Parser_ShouldParseFunctionCall()
    {
        var source = @"
routine test() is
  print add(1, 2)
end";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
    }

    [Fact]
    public void Parser_ShouldParseProcedureCall()
    {
        var source = @"
routine test() is
  setfirst(arr)
end";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
    }

    [Fact]
    public void Parser_ShouldParseReturnStatement()
    {
        var source = @"
routine test() : integer is
  return 42
end";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
    }

    [Fact]
    public void Parser_ShouldParseBinaryExpression()
    {
        var source = "routine test() : integer => 1 + 2 * 3";
        var parser = new ParserClass(new LexerClass(source));
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors);
    }

    [Fact]
    public void Parser_ShouldReportErrorForInvalidSyntax()
    {
        var parser = new ParserClass(new LexerClass("var 123"));
        var program = parser.ParseProgram();
        
        Assert.True(parser.Diag.HasErrors);
    }
}
