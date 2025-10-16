using System;
using System.Collections.Generic;
using System.Linq;
using Compilers_project.Lexer;
using Compilers_project.Lexer.Interfaces;
using Compilers_project.Parser.AST;

namespace Compilers_project.Parser;

/// <summary>
/// Главный парсер: декларации, блоки, операторы, типы. Выражения делегирует в <see cref="ExprParser"/>.
/// </summary>
public sealed class Parser
{
    private readonly ILexer _lx;
    private Token _t; // текущий токен

    public Diagnostics Diag { get; } = new();
    private readonly ExprParser _expr;

    public Parser(ILexer lexer)
    {
        _lx = lexer ?? throw new ArgumentNullException(nameof(lexer));
        _t = _lx.NextToken();
        _expr = new ExprParser(this);
    }

    internal Token Current => _t;
    internal void Next() => _t = _lx.NextToken();

    internal bool Accept(TokenType k)
    {
        if (_t.Type == k) { Next(); return true; }
        return false;
    }

    internal Token Expect(TokenType k, string message)
    {
        if (_t.Type == k)
        {
            var tok = _t; 
            Next();
            return tok;
        }
        Diag.Error(_t.Span, $"{message} (got: {_t.Type})");
        // возвращаем фиктивный токен нужного типа, чтобы не падать
        return new Token(k, _t.Span, null);
    }

    private static bool IsSeparator(Token t)
        => t.Type == TokenType.NewLine || t.Type == TokenType.Semicolon;

    private void SkipOptionalSeparators()
    {
        while (IsSeparator(_t)) Next();
    }

    private void RecoverTo(params TokenType[] sentinels)
    {
        var set = new HashSet<TokenType>(sentinels);
        while (_t.Type != TokenType.Eof && !IsSeparator(_t) && !set.Contains(_t.Type))
            Next();
        if (IsSeparator(_t)) SkipOptionalSeparators();
    }

    public ProgramNode ParseProgram()
    {
        var decls = new List<Decl>();
        var startSpan = _t.Span;
        SkipOptionalSeparators();

        while (_t.Type != TokenType.Eof)
        {
            var before = _t.Span;
            try
            {
                if (_t.Type == TokenType.Var || _t.Type == TokenType.Type)
                {
                    decls.Add(ParseSimpleDecl());
                }
                else if (_t.Type == TokenType.Routine)
                {
                    decls.Add(ParseRoutineDecl());
                }
                else
                {
                    Diag.Error(_t.Span, "declaration expected");
                    RecoverTo(TokenType.Var, TokenType.Type, TokenType.Routine, TokenType.Eof);
                }
            }
            finally
            {
                SkipOptionalSeparators();
            }
        }

        var endSpan = _t.Span;
        var programSpan = new Span(startSpan.Start, endSpan.End - startSpan.Start, startSpan.Line, startSpan.Col);
        return new ProgramNode(programSpan, decls);
    }

    // Декларации
    private Decl ParseSimpleDecl()
    {
        if (Accept(TokenType.Var))  return ParseVarDecl();
        if (Accept(TokenType.Type)) return ParseTypeDecl();
        // сюда не зайдём, защитный код:
        Diag.Error(_t.Span, "expected 'var' or 'type'");
        return new VarDecl(_t.Span, "<error>", null, null);
    }

    private VarDecl ParseVarDecl()
    {
        var start = _t.Span;
        var nameTok = Expect(TokenType.Identifier, "variable name expected");
        TypeRef? type = null;
        Expr? init = null;

        if (Accept(TokenType.Colon))
            type = ParseType();

        if (Accept(TokenType.Is))
            init = _expr.ParseExpression();

        var end = _t.Span;
        var span = new Span(start.Start, end.End - start.Start, start.Line, start.Col);
        return new VarDecl(span, nameTok.Text!, type, init);
    }

    private TypeDecl ParseTypeDecl()
    {
        var start = _t.Span;
        var nameTok = Expect(TokenType.Identifier, "type name expected");
        Expect(TokenType.Is, "expected 'is'");
        var typ = ParseType();

        var end = _t.Span;
        var span = new Span(start.Start, end.End - start.Start, start.Line, start.Col);
        return new TypeDecl(span, nameTok.Text!, typ);
    }

    private RoutineDecl ParseRoutineDecl()
    {
        var start = _t.Span;
        Expect(TokenType.Routine, "expected 'routine'");
        var name = Expect(TokenType.Identifier, "routine name expected").Text!;
        Expect(TokenType.LParen, "expected '('");

        var pars = new List<Param>();
        if (_t.Type != TokenType.RParen)
        {
            for (;;)
            {
                var pid = Expect(TokenType.Identifier, "parameter name expected").Text!;
                Expect(TokenType.Colon, "':' expected");
                var ptype = ParseType();
                pars.Add(new Param(pid, ptype));
                if (!Accept(TokenType.Comma)) break;
            }
        }
        Expect(TokenType.RParen, "expected ')'");

        TypeRef? ret = null;
        if (Accept(TokenType.Colon))
            ret = ParseType();

        // Forward declaration
        if (_t.Type == TokenType.NewLine || _t.Type == TokenType.Semicolon || _t.Type == TokenType.Eof)
        {
            var span = new Span(start.Start, _t.Span.Start - start.Start, start.Line, start.Col);
            return new RoutineDecl(span, name, pars, ret, null);
        }

        // => expr
        if (Accept(TokenType.Arrow))
        {
            var expr = _expr.ParseExpression();
            var end = _t.Span;
            var span = new Span(start.Start, end.End - start.Start, start.Line, start.Col);
            return new RoutineDecl(span, name, pars, ret, new ExprBody(expr));
        }

        // is Block end
        Expect(TokenType.Is, "expected 'is'");
        var body = ParseBlock();
        Expect(TokenType.End, "expected 'end'");

        var endSpan = _t.Span;
        var rspan = new Span(start.Start, endSpan.End - start.Start, start.Line, start.Col);
        return new RoutineDecl(rspan, name, pars, ret, new BlockBody(body));
    }

    // типы
    private TypeRef ParseType()
    {
        var start = _t.Span;

        // Примитивы/алиасы как идентификаторы
        if (_t.Type == TokenType.Identifier)
        {
            var id = _t; Next();
            if (id.Text == "integer" || id.Text == "real" || id.Text == "boolean")
                return new PrimitiveTypeRef(id.Span, id.Text!);
            return new NamedTypeRef(id.Span, id.Text!);
        }

        if (Accept(TokenType.Record))
        {
            var fields = new List<VarDecl>();
            SkipOptionalSeparators();
            while (Accept(TokenType.Var))
            {
                fields.Add(ParseVarDecl());
                SkipOptionalSeparators();
            }
            var endTok = Expect(TokenType.End, "expected 'end' to close record");
            var span = new Span(start.Start, endTok.Span.End - start.Start, start.Line, start.Col);
            return new RecordTypeRef(span, fields);
        }

        if (Accept(TokenType.Array))
        {
            Expr? size = null;
            if (Accept(TokenType.LBracket))
            {
                if (_t.Type != TokenType.RBracket)
                    size = _expr.ParseExpression();
                Expect(TokenType.RBracket, "expected ']'");
            }
            var elem = ParseType();
            var end = elem.Span;
            var span = new Span(start.Start, end.End - start.Start, start.Line, start.Col);
            return new ArrayTypeRef(span, elem, size);
        }

        Diag.Error(_t.Span, "type expected");
        return new NamedTypeRef(_t.Span, "<error>");
    }

    // блоки и операторы

    private Block ParseBlock()
    {
        var start = _t.Span;
        var items = new List<Node>();
        SkipOptionalSeparators();

        while (_t.Type != TokenType.End && _t.Type != TokenType.Eof && _t.Type != TokenType.Else)
        {
            if (_t.Type == TokenType.Var || _t.Type == TokenType.Type)
                items.Add(ParseSimpleDecl());
            else
                items.Add(ParseStatement());

            SkipOptionalSeparators();
        }

        var end = _t.Span;
        var span = new Span(start.Start, end.End - start.Start, start.Line, start.Col);
        return new Block(span, items);
    }

    private Stmt ParseStatement()
    {
        switch (_t.Type)
        {
            case TokenType.If:     return ParseIf();
            case TokenType.While:  return ParseWhile();
            case TokenType.For:    return ParseFor();
            case TokenType.Return: return ParseReturn();
            case TokenType.Print:  return ParsePrint();
            case TokenType.Identifier:
                return ParseAssignOrCall();
            default:
                Diag.Error(_t.Span, "statement expected");
                var bad = new EmptyStmt(_t.Span);
                RecoverTo(TokenType.End);
                return bad;
        }
    }

    private Stmt ParseAssignOrCall()
    {
        // Сначала парсим «базу»: имя / имя() / имя.поле / имя[индекс] (+цепочки)
        var start = _t.Span;
        var expr = ParsePrimaryWithPostfix();

        // Присваивание?
        if (Accept(TokenType.Assign))
        {
            var rhs = _expr.ParseExpression();
            var end = rhs.Span;
            var span = new Span(start.Start, end.End - start.Start, start.Line, start.Col);
            return new AssignStmt(span, expr, rhs);
        }

        // Вызов-процедура как оператор (уже распарсен в ParsePrimaryWithPostfix)
        if (expr is CallExpr ce)
        {
            return new CallStmt(ce.Span, ce.Name, ce.Args);
        }

        Diag.Error(start, "expected ':=' or '(' after identifier");
        return new EmptyStmt(start);
    }

    private IfStmt ParseIf()
    {
        var start = _t.Span;
        Expect(TokenType.If, "expected 'if'");
        var cond = _expr.ParseExpression();
        Expect(TokenType.Then, "expected 'then'");
        var thenBlk = ParseBlock();

        Block? elseBlk = null;
        if (Accept(TokenType.Else))
            elseBlk = ParseBlock();

        var endTok = Expect(TokenType.End, "expected 'end'");
        var span = new Span(start.Start, endTok.Span.End - start.Start, start.Line, start.Col);
        return new IfStmt(span, cond, thenBlk, elseBlk);
    }

    private WhileStmt ParseWhile()
    {
        var start = _t.Span;
        Expect(TokenType.While, "expected 'while'");
        var cond = _expr.ParseExpression();
        Expect(TokenType.Loop, "expected 'loop'");
        var body = ParseBlock();
        var endTok = Expect(TokenType.End, "expected 'end'");
        var span = new Span(start.Start, endTok.Span.End - start.Start, start.Line, start.Col);
        return new WhileStmt(span, cond, body);
    }

    private ForStmt ParseFor()
    {
        var start = _t.Span;
        Expect(TokenType.For, "expected 'for'");
        var iter = Expect(TokenType.Identifier, "iterator name expected").Text!;
        Expect(TokenType.In, "expected 'in'");

        var first = _expr.ParseExpression();
        Expr? second = null;
        bool reverse = false;

        if (Accept(TokenType.DotDot))
            second = _expr.ParseExpression();

        if (Accept(TokenType.Reverse))
            reverse = true;

        Expect(TokenType.Loop, "expected 'loop'");
        var body = ParseBlock();
        var endTok = Expect(TokenType.End, "expected 'end'");
        var span = new Span(start.Start, endTok.Span.End - start.Start, start.Line, start.Col);
        return new ForStmt(span, iter, first, second, reverse, body);
    }

    private ReturnStmt ParseReturn()
    {
        var start = _t.Span;
        Expect(TokenType.Return, "expected 'return'");

        // return без выражения разрешим, если дальше сепаратор/конец
        Expr? val = null;
        if (_t.Type != TokenType.NewLine && _t.Type != TokenType.Semicolon && _t.Type != TokenType.End)
            val = _expr.ParseExpression();

        var end = val?.Span ?? start;
        var span = new Span(start.Start, (val?.Span.End ?? start.End) - start.Start, start.Line, start.Col);
        return new ReturnStmt(span, val);
    }

    private PrintStmt ParsePrint()
    {
        // TODO: а не надо ли это унифицировать
        var start = _t.Span;
        Expect(TokenType.Print, "expected 'print'");
        var items = new List<Expr> { _expr.ParseExpression() };
        while (Accept(TokenType.Comma))
            items.Add(_expr.ParseExpression());
        var end = items.Last().Span;
        var span = new Span(start.Start, end.End - start.Start, start.Line, start.Col);
        return new PrintStmt(span, items);
    }

    // Для парсера выражений
    // ====== Первичка и постфиксы (для LValue/имён на уровне операторов) ======
    public Expr ParsePrimaryWithPostfix()
    {
        var e = ParsePrimaryCore();

        // Цепочки . и []
        while (true)
        {
            if (Accept(TokenType.Dot))
            {
                var id = Expect(TokenType.Identifier, "field name expected");
                var span = new Span(e.Span.Start, id.Span.End - e.Span.Start, e.Span.Line, e.Span.Col);
                e = new FieldExpr(span, e, id.Text!);
                continue;
            }
            if (Accept(TokenType.LBracket))
            {
                var idx = _expr.ParseExpression();
                var rbr = Expect(TokenType.RBracket, "expected ']'");
                var span = new Span(e.Span.Start, rbr.Span.End - e.Span.Start, e.Span.Line, e.Span.Col);
                e = new IndexExpr(span, e, idx);
                continue;
            }
            break;
        }
        return e;
    }

    private Expr ParsePrimaryCore()
    {
        if (_t.Type == TokenType.Identifier)
        {
            var id = _t; Next();
            // Вызов-функция прямо в выражении: name '(' args ')'
            if (Accept(TokenType.LParen))
            {
                var args = ParseArgList();
                var rpar = Expect(TokenType.RParen, "expected ')'");
                var span = new Span(id.Span.Start, rpar.Span.End - id.Span.Start, id.Span.Line, id.Span.Col);
                return new CallExpr(span, id.Text!, args);
            }
            return new NameExpr(id.Span, id.Text!);
        }
        if (_t.Type == TokenType.IntegerLiteral) { var t = _t; Next(); return new LiteralInt (t.Span, t.IntValue!.Value); }
        if (_t.Type == TokenType.RealLiteral)    { var t = _t; Next(); return new LiteralReal(t.Span, t.RealValue!.Value); }
        if (_t.Type == TokenType.BooleanLiteral) { var t = _t; Next(); return new LiteralBool(t.Span, t.BoolValue!.Value); }
        if (Accept(TokenType.LParen))
        {
            var e = _expr.ParseExpression();
            var r = Expect(TokenType.RParen, "expected ')'");
            var span = new Span(e.Span.Start, r.Span.End - e.Span.Start, e.Span.Line, e.Span.Col);
            return e;
        }

        Diag.Error(_t.Span, "primary expression expected");
        var bad = _t; Next();
        return new LiteralInt(bad.Span, 0);
    }

    internal List<Expr> ParseArgList()
    {
        var args = new List<Expr>();
        if (_t.Type == TokenType.RParen) return args;
        for (;;)
        {
            args.Add(_expr.ParseExpression());
            if (!Accept(TokenType.Comma)) break;
        }
        return args;
    }
}
