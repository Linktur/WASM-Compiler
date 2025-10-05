using Compilers_project.Lexer;
using Compilers_project.Parser.AST;

namespace Compilers_project.Parser;

/// <summary>
/// Pratt-парсер выражений: префиксы (унарные) + инфиксы по таблице binding power.
/// </summary>
internal sealed class ExprParser
{
    private readonly Parser _p;

    public ExprParser(Parser parent) => _p = parent;

    public Expr ParseExpression(int minBp = 0)
    {
        // ---- prefix / первичный операнд ----
        Expr lhs;
        var start = _p.Current.Span;

        if (_p.Accept(TokenType.Plus))
        {
            var op = "+";
            var rhs = ParseExpression(6);
            var span = new Span(start.Start, rhs.Span.End - start.Start, start.Line, start.Col);
            lhs = new UnaryExpr(span, op, rhs);
        }
        else if (_p.Accept(TokenType.Minus))
        {
            var op = "-";
            var rhs = ParseExpression(6);
            var span = new Span(start.Start, rhs.Span.End - start.Start, start.Line, start.Col);
            lhs = new UnaryExpr(span, op, rhs);
        }
        else if (_p.Accept(TokenType.Not))
        {
            var op = "not";
            var rhs = ParseExpression(6);
            var span = new Span(start.Start, rhs.Span.End - start.Start, start.Line, start.Col);
            lhs = new UnaryExpr(span, op, rhs);
        }
        else
        {
            // используем методы Parser для первички (имена/литералы/вызовы/скобки)
            // и постфиксы ('.' и '[]') он тоже обрабатывает.
            lhs = _p.ParsePrimaryWithPostfix();
        }

        // ---- infix ----
        while (true)
        {
            var (op, lbp, rbp) = CurrentInfixBindingPower();
            if (lbp < 0 || lbp < minBp) break;

            var opTok = _p.Current; _p.Next(); // съели оператор
            var rhs = ParseExpression(rbp);

            var span = new Span(lhs.Span.Start, rhs.Span.End - lhs.Span.Start, lhs.Span.Line, lhs.Span.Col);
            lhs = new BinaryExpr(span, op, lhs, rhs);
        }

        return lhs;
    }

    private (string op, int lbp, int rbp) CurrentInfixBindingPower()
        => _p.Current.Type switch
        {
            // Постфиксы '.' и '[]' уже разобраны в ParsePrimaryWithPostfix (самый высокий приоритет).

            // * / %
            TokenType.Star    => ("*", 5, 6),
            TokenType.Slash   => ("/", 5, 6),
            TokenType.Percent => ("%", 5, 6),

            // + -
            TokenType.Plus    => ("+", 4, 5),
            TokenType.Minus   => ("-", 4, 5),

            // сравнения
            TokenType.Less         => ("<", 3, 4),
            TokenType.LessEqual    => ("<=",3, 4),
            TokenType.Greater      => (">", 3, 4),
            TokenType.GreaterEqual => (">=",3, 4),
            TokenType.Equal        => ("=", 3, 4),
            TokenType.NotEqual     => ("/=",3, 4),

            // логика
            TokenType.And   => ("and", 2, 3),
            TokenType.Xor   => ("xor", 1, 2),
            TokenType.Or    => ("or",  0, 1),

            _ => ("", -1, -1)
        };
}
