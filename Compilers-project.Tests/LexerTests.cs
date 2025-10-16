using Compilers_project.Lexer;
using Xunit;
using LexerClass = Compilers_project.Lexer.Lexer;

namespace Compilers_project.Tests;

public class LexerTests
{
    [Fact]
    public void Lexer_ShouldRecognizeKeywords()
    {
        var lexer = new LexerClass("var type routine if while for");
        
        Assert.Equal(TokenType.Var, lexer.NextToken().Type);
        Assert.Equal(TokenType.Type, lexer.NextToken().Type);
        Assert.Equal(TokenType.Routine, lexer.NextToken().Type);
        Assert.Equal(TokenType.If, lexer.NextToken().Type);
        Assert.Equal(TokenType.While, lexer.NextToken().Type);
        Assert.Equal(TokenType.For, lexer.NextToken().Type);
    }

    [Fact]
    public void Lexer_ShouldRecognizeIdentifiers()
    {
        var lexer = new LexerClass("myVar _test x123");
        
        var t1 = lexer.NextToken();
        Assert.Equal(TokenType.Identifier, t1.Type);
        Assert.Equal("myVar", t1.Text);
        
        var t2 = lexer.NextToken();
        Assert.Equal(TokenType.Identifier, t2.Type);
        Assert.Equal("_test", t2.Text);
        
        var t3 = lexer.NextToken();
        Assert.Equal(TokenType.Identifier, t3.Type);
        Assert.Equal("x123", t3.Text);
    }

    [Fact]
    public void Lexer_ShouldRecognizeIntegerLiterals()
    {
        var lexer = new LexerClass("0 42 999");
        
        var t1 = lexer.NextToken();
        Assert.Equal(TokenType.IntegerLiteral, t1.Type);
        Assert.Equal(0L, t1.IntValue);
        
        var t2 = lexer.NextToken();
        Assert.Equal(TokenType.IntegerLiteral, t2.Type);
        Assert.Equal(42L, t2.IntValue);
        
        var t3 = lexer.NextToken();
        Assert.Equal(TokenType.IntegerLiteral, t3.Type);
        Assert.Equal(999L, t3.IntValue);
    }

    [Fact]
    public void Lexer_ShouldRecognizeRealLiterals()
    {
        var lexer = new LexerClass("3.14 1.0 2.5e10");
        
        var t1 = lexer.NextToken();
        Assert.Equal(TokenType.RealLiteral, t1.Type);
        Assert.Equal(3.14, t1.RealValue);
        
        var t2 = lexer.NextToken();
        Assert.Equal(TokenType.RealLiteral, t2.Type);
        Assert.Equal(1.0, t2.RealValue);
        
        var t3 = lexer.NextToken();
        Assert.Equal(TokenType.RealLiteral, t3.Type);
        Assert.Equal(2.5e10, t3.RealValue);
    }

    [Fact]
    public void Lexer_ShouldRecognizeBooleanLiterals()
    {
        var lexer = new LexerClass("true false");
        
        var t1 = lexer.NextToken();
        Assert.Equal(TokenType.BooleanLiteral, t1.Type);
        Assert.True(t1.BoolValue);
        
        var t2 = lexer.NextToken();
        Assert.Equal(TokenType.BooleanLiteral, t2.Type);
        Assert.False(t2.BoolValue);
    }

    [Fact]
    public void Lexer_ShouldRecognizeOperators()
    {
        var lexer = new LexerClass("+ - * / % := = /= < <= > >= .. =>");
        
        Assert.Equal(TokenType.Plus, lexer.NextToken().Type);
        Assert.Equal(TokenType.Minus, lexer.NextToken().Type);
        Assert.Equal(TokenType.Star, lexer.NextToken().Type);
        Assert.Equal(TokenType.Slash, lexer.NextToken().Type);
        Assert.Equal(TokenType.Percent, lexer.NextToken().Type);
        Assert.Equal(TokenType.Assign, lexer.NextToken().Type);
        Assert.Equal(TokenType.Equal, lexer.NextToken().Type);
        Assert.Equal(TokenType.NotEqual, lexer.NextToken().Type);
        Assert.Equal(TokenType.Less, lexer.NextToken().Type);
        Assert.Equal(TokenType.LessEqual, lexer.NextToken().Type);
        Assert.Equal(TokenType.Greater, lexer.NextToken().Type);
        Assert.Equal(TokenType.GreaterEqual, lexer.NextToken().Type);
        Assert.Equal(TokenType.DotDot, lexer.NextToken().Type);
        Assert.Equal(TokenType.Arrow, lexer.NextToken().Type);
    }

    [Fact]
    public void Lexer_ShouldRecognizePunctuation()
    {
        var lexer = new LexerClass("( ) [ ] , ; : .");
        
        Assert.Equal(TokenType.LParen, lexer.NextToken().Type);
        Assert.Equal(TokenType.RParen, lexer.NextToken().Type);
        Assert.Equal(TokenType.LBracket, lexer.NextToken().Type);
        Assert.Equal(TokenType.RBracket, lexer.NextToken().Type);
        Assert.Equal(TokenType.Comma, lexer.NextToken().Type);
        Assert.Equal(TokenType.Semicolon, lexer.NextToken().Type);
        Assert.Equal(TokenType.Colon, lexer.NextToken().Type);
        Assert.Equal(TokenType.Dot, lexer.NextToken().Type);
    }

    [Fact]
    public void Lexer_ShouldSkipComments()
    {
        var lexer = new LexerClass("var x // this is a comment\nprint");
        
        Assert.Equal(TokenType.Var, lexer.NextToken().Type);
        Assert.Equal(TokenType.Identifier, lexer.NextToken().Type);
        Assert.Equal(TokenType.NewLine, lexer.NextToken().Type);
        Assert.Equal(TokenType.Print, lexer.NextToken().Type);
    }

    [Fact]
    public void Lexer_ShouldRecognizeNewLines()
    {
        var lexer = new LexerClass("var\nx\n");
        
        Assert.Equal(TokenType.Var, lexer.NextToken().Type);
        Assert.Equal(TokenType.NewLine, lexer.NextToken().Type);
        Assert.Equal(TokenType.Identifier, lexer.NextToken().Type);
        Assert.Equal(TokenType.NewLine, lexer.NextToken().Type);
    }

    [Fact]
    public void Lexer_ShouldDistinguishRangeFromReal()
    {
        var lexer = new LexerClass("1..10");
        
        var t1 = lexer.NextToken();
        Assert.Equal(TokenType.IntegerLiteral, t1.Type);
        Assert.Equal(1L, t1.IntValue);
        
        Assert.Equal(TokenType.DotDot, lexer.NextToken().Type);
        
        var t2 = lexer.NextToken();
        Assert.Equal(TokenType.IntegerLiteral, t2.Type);
        Assert.Equal(10L, t2.IntValue);
    }

    [Fact]
    public void Lexer_ShouldReportErrorForMultipleDecimalPoints()
    {
        var lexer = new LexerClass("1.3.5");
        
        var token = lexer.NextToken();
        Assert.Equal(TokenType.Error, token.Type);
        Assert.Contains("multiple decimal points", token.Text);
    }

    [Fact]
    public void Lexer_ShouldReportErrorForUnexpectedCharacter()
    {
        var lexer = new LexerClass("var @ x");
        
        Assert.Equal(TokenType.Var, lexer.NextToken().Type);
        
        var errorToken = lexer.NextToken();
        Assert.Equal(TokenType.Error, errorToken.Type);
        Assert.Contains("Unexpected char", errorToken.Text);
    }

    [Fact]
    public void Lexer_ShouldReportErrorForMalformedInteger()
    {
        var lexer = new LexerClass("999999999999999999999999999999");
        
        var token = lexer.NextToken();
        Assert.Equal(TokenType.Error, token.Type);
        Assert.Contains("Malformed integer", token.Text);
    }

    [Fact]
    public void Lexer_ShouldHandleScientificNotation()
    {
        var lexer = new LexerClass("1.5e10 2.0e-5 3.0e+2");
        
        var t1 = lexer.NextToken();
        Assert.Equal(TokenType.RealLiteral, t1.Type);
        Assert.Equal(1.5e10, t1.RealValue);
        
        var t2 = lexer.NextToken();
        Assert.Equal(TokenType.RealLiteral, t2.Type);
        Assert.Equal(2.0e-5, t2.RealValue);
        
        var t3 = lexer.NextToken();
        Assert.Equal(TokenType.RealLiteral, t3.Type);
        Assert.Equal(3.0e+2, t3.RealValue);
    }

    [Fact]
    public void Lexer_ShouldHandleComplexExpression()
    {
        var lexer = new LexerClass("var x := 10 + 20 * 3");
        
        Assert.Equal(TokenType.Var, lexer.NextToken().Type);
        Assert.Equal(TokenType.Identifier, lexer.NextToken().Type);
        Assert.Equal(TokenType.Assign, lexer.NextToken().Type);
        Assert.Equal(TokenType.IntegerLiteral, lexer.NextToken().Type);
        Assert.Equal(TokenType.Plus, lexer.NextToken().Type);
        Assert.Equal(TokenType.IntegerLiteral, lexer.NextToken().Type);
        Assert.Equal(TokenType.Star, lexer.NextToken().Type);
        Assert.Equal(TokenType.IntegerLiteral, lexer.NextToken().Type);
    }

    [Fact]
    public void Lexer_PeekShouldNotConsumeTokens()
    {
        var lexer = new LexerClass("var x");
        
        var peeked = lexer.Peek(1);
        Assert.Equal(TokenType.Var, peeked.Type);
        
        var next = lexer.NextToken();
        Assert.Equal(TokenType.Var, next.Type);
    }
}
