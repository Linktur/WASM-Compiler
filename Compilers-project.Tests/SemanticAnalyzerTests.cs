using Xunit;
using LexerClass = Compilers_project.Lexer.Lexer;
using ParserClass = Compilers_project.Parser.Parser;
using Analyzer = Compilers_project.SemanticAnalyzer.SemanticAnalyzer;

namespace Compilers_project.Tests;

public class SemanticAnalyzerTests
{
    private static Analyzer Analyze(string source)
    {
        var lexer = new LexerClass(source);
        var parser = new ParserClass(lexer);
        var program = parser.ParseProgram();
        
        Assert.False(parser.Diag.HasErrors, "Parser should not have errors");
        
        var analyzer = new Analyzer();
        analyzer.Analyze(program);
        
        return analyzer;
    }
    
    [Fact]
    public void SemanticAnalyzer_ValidProgram_ShouldSucceed()
    {
        var source = @"
routine main() is
    var x : integer
    x := 42
    print x
end
";
        var analyzer = Analyze(source);
        Assert.False(analyzer.Diagnostics.HasErrors, "Should have no semantic errors");
    }
    
    [Fact]
    public void SemanticAnalyzer_UndeclaredVariable_ShouldError()
    {
        var source = @"
routine main() is
    print undeclared
end
";
        var analyzer = Analyze(source);
        Assert.True(analyzer.Diagnostics.HasErrors, "Should have semantic errors");
        Assert.Contains(analyzer.Diagnostics.Items, item => item.Message.Contains("Undeclared identifier"));
    }
    
    [Fact]
    public void SemanticAnalyzer_UndefinedType_ShouldError()
    {
        var source = @"
routine main() is
    var x : UndefinedType
end
";
        var analyzer = Analyze(source);
        Assert.True(analyzer.Diagnostics.HasErrors, "Should have semantic errors");
        Assert.Contains(analyzer.Diagnostics.Items, item => item.Message.Contains("Undefined type"));
    }
    
    [Fact]
    public void SemanticAnalyzer_TypeMismatch_RealToBoolean_ShouldError()
    {
        var source = @"
routine main() is
    var b : boolean
    var r : real
    b := r
end
";
        var analyzer = Analyze(source);
        Assert.True(analyzer.Diagnostics.HasErrors, "Should have semantic errors");
        Assert.Contains(analyzer.Diagnostics.Items, item => item.Message.Contains("Cannot assign"));
    }
    
    [Fact]
    public void SemanticAnalyzer_TypeConversion_IntegerToReal_ShouldSucceed()
    {
        var source = @"
routine main() is
    var r : real
    var i : integer
    i := 10
    r := i
    print r
end
";
        var analyzer = Analyze(source);
        Assert.False(analyzer.Diagnostics.HasErrors, "Integer to real conversion should be allowed");
    }
    
    [Fact]
    public void SemanticAnalyzer_TypeConversion_BooleanToInteger_ShouldSucceed()
    {
        var source = @"
routine main() is
    var i : integer
    var b : boolean
    b := true
    i := b
    print i
end
";
        var analyzer = Analyze(source);
        Assert.False(analyzer.Diagnostics.HasErrors, "Boolean to integer conversion should be allowed");
    }
    
    [Fact]
    public void SemanticAnalyzer_FunctionCall_CorrectArguments_ShouldSucceed()
    {
        var source = @"
routine add(a: integer, b: integer) : integer is
    var result : integer
    result := a + b
    return result
end

routine main() is
    var x : integer
    x := add(1, 2)
    print x
end
";
        var analyzer = Analyze(source);
        Assert.False(analyzer.Diagnostics.HasErrors, "Function call with correct arguments should succeed");
    }
    
    [Fact]
    public void SemanticAnalyzer_FunctionCall_WrongArgumentCount_ShouldError()
    {
        var source = @"
routine add(a: integer, b: integer) : integer is
    return a + b
end

routine main() is
    var x : integer
    x := add(1)
end
";
        var analyzer = Analyze(source);
        Assert.True(analyzer.Diagnostics.HasErrors, "Function call with wrong argument count should error");
        Assert.Contains(analyzer.Diagnostics.Items, item => item.Message.Contains("expects") && item.Message.Contains("arguments"));
    }
    
    [Fact]
    public void SemanticAnalyzer_ArrayAccess_ValidIndex_ShouldSucceed()
    {
        var source = @"
type IntArray is array[5] integer

routine main() is
    var arr : IntArray
    arr[1] := 10
    print arr[1]
end
";
        var analyzer = Analyze(source);
        Assert.False(analyzer.Diagnostics.HasErrors, "Array access with valid index should succeed");
    }
    
    [Fact]
    public void SemanticAnalyzer_ArrayAccess_RealIndex_ShouldSucceed()
    {
        var source = @"
type IntArray is array[5] integer

routine main() is
    var arr : IntArray
    var r : real
    r := 1.5
    arr[r] := 10
end
";
        var analyzer = Analyze(source);
        // Real is assignable to integer per spec (narrowing with rounding)
        Assert.False(analyzer.Diagnostics.HasErrors, "Array access with real index should succeed (converts to integer)");
    }
    
    [Fact]
    public void SemanticAnalyzer_IfStatement_BooleanCondition_ShouldSucceed()
    {
        var source = @"
routine main() is
    var x : integer
    x := 5
    if x > 0 then
        print 1
    else
        print 0
    end
end
";
        var analyzer = Analyze(source);
        Assert.False(analyzer.Diagnostics.HasErrors, "If statement with boolean condition should succeed");
    }
    
    [Fact]
    public void SemanticAnalyzer_IfStatement_IntegerCondition_ShouldSucceed()
    {
        var source = @"
routine main() is
    var x : integer
    x := 5
    if x then
        print 1
    end
end
";
        var analyzer = Analyze(source);
        // Integer is assignable to boolean per spec (0->false, 1->true)
        Assert.False(analyzer.Diagnostics.HasErrors, "If statement with integer condition should succeed (converts to boolean)");
    }
    
    [Fact]
    public void SemanticAnalyzer_WhileLoop_BooleanCondition_ShouldSucceed()
    {
        var source = @"
routine main() is
    var i : integer
    i := 0
    while i < 10 loop
        i := i + 1
    end
end
";
        var analyzer = Analyze(source);
        Assert.False(analyzer.Diagnostics.HasErrors, "While loop with boolean condition should succeed");
    }
    
    [Fact]
    public void SemanticAnalyzer_ForLoop_IntegerRange_ShouldSucceed()
    {
        var source = @"
routine main() is
    for i in 1 .. 10 loop
        print i
    end
end
";
        var analyzer = Analyze(source);
        Assert.False(analyzer.Diagnostics.HasErrors, "For loop with integer range should succeed");
    }
    
    [Fact]
    public void SemanticAnalyzer_ReturnType_BooleanToInteger_ShouldSucceed()
    {
        var source = @"
routine getNumber() : integer is
    return true
end

routine main() is
    var x : integer
    x := getNumber()
end
";
        var analyzer = Analyze(source);
        // Boolean is assignable to integer per spec (true=1, false=0)
        Assert.False(analyzer.Diagnostics.HasErrors, "Return boolean from integer function should succeed (converts to integer)");
    }
    
    [Fact]
    public void SemanticAnalyzer_BinaryOperation_ValidTypes_ShouldSucceed()
    {
        var source = @"
routine main() is
    var a : integer
    var b : integer
    var c : integer
    a := 5
    b := 10
    c := a + b
    print c
end
";
        var analyzer = Analyze(source);
        Assert.False(analyzer.Diagnostics.HasErrors, "Binary operation with valid types should succeed");
    }
    
    [Fact]
    public void SemanticAnalyzer_RecordType_ValidFieldAccess_ShouldSucceed()
    {
        var source = @"
type Point is record
    var x : integer
    var y : integer
end

routine main() is
    var p : Point
    p.x := 10
    p.y := 20
    print p.x
end
";
        var analyzer = Analyze(source);
        Assert.False(analyzer.Diagnostics.HasErrors, "Record field access should succeed");
    }
    
    [Fact]
    public void SemanticAnalyzer_RecordType_InvalidFieldAccess_ShouldError()
    {
        var source = @"
type Point is record
    var x : integer
    var y : integer
end

routine main() is
    var p : Point
    p.z := 30
end
";
        var analyzer = Analyze(source);
        Assert.True(analyzer.Diagnostics.HasErrors, "Invalid record field access should error");
        Assert.Contains(analyzer.Diagnostics.Items, item => item.Message.Contains("does not have field"));
    }
    
    [Fact]
    public void SemanticAnalyzer_ScopeManagement_ShadowingVariable_ShouldSucceed()
    {
        var source = @"
routine main() is
    var x : integer
    x := 10
    if true then
        var x : integer
        x := 20
        print x
    end
    print x
end
";
        var analyzer = Analyze(source);
        Assert.False(analyzer.Diagnostics.HasErrors, "Variable shadowing in nested scope should be allowed");
    }
    
    [Fact]
    public void SemanticAnalyzer_ArrayAccess_NonArrayType_ShouldError()
    {
        var source = @"
routine main() is
    var x : integer
    x := 5
    print x[1]
end
";
        var analyzer = Analyze(source);
        Assert.True(analyzer.Diagnostics.HasErrors, "Indexing non-array type should error");
        Assert.Contains(analyzer.Diagnostics.Items, item => item.Message.Contains("Cannot index"));
    }
    
    [Fact]
    public void SemanticAnalyzer_FieldAccess_NonRecordType_ShouldError()
    {
        var source = @"
routine main() is
    var x : integer
    x := 5
    print x.field
end
";
        var analyzer = Analyze(source);
        Assert.True(analyzer.Diagnostics.HasErrors, "Field access on non-record type should error");
        Assert.Contains(analyzer.Diagnostics.Items, item => item.Message.Contains("Cannot access field"));
    }
    
    [Fact]
    public void SemanticAnalyzer_VoidReturn_WithValue_ShouldError()
    {
        var source = @"
routine doSomething() is
    return 42
end

routine main() is
    doSomething()
end
";
        var analyzer = Analyze(source);
        Assert.True(analyzer.Diagnostics.HasErrors, "Returning value from void routine should error");
        Assert.Contains(analyzer.Diagnostics.Items, item => item.Message.Contains("Cannot return a value from a void routine"));
    }
}
