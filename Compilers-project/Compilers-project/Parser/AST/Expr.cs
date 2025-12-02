using System.Collections.Generic;
using Compilers_project.Lexer;

namespace Compilers_project.Parser.AST;

public abstract class Expr : Node
{
    protected Expr(Span span) : base(span) { }
}

public sealed class LiteralInt : Expr
{
    public long Value { get; } 
    public LiteralInt (Span s, long v) : base(s) => Value = v;
}

public sealed class LiteralReal : Expr
{
    public double Value { get; } 
    public LiteralReal(Span s, double v) : base(s) => Value = v;
}

public sealed class LiteralBool : Expr
{
    public bool Value { get; } 
    public LiteralBool(Span s, bool v) : base(s) => Value = v;
}

public sealed class NameExpr : Expr
{
    public string Name { get; } 
    public NameExpr  (Span s, string n) : base(s) => Name = n;
}

public sealed class FieldExpr : Expr
{
    public Expr Receiver { get; } 
    public string Field { get; } 
    public FieldExpr(Span s, Expr r, string f) : base(s) => (Receiver, Field) = (r, f);
}

public sealed class IndexExpr : Expr
{
    public Expr Receiver { get; } 
    public Expr Index { get; } 
    public IndexExpr(Span s, Expr r, Expr i) : base(s) => (Receiver, Index) = (r, i);
}

public sealed class CallExpr : Expr
{
    public string Name { get; }
    public IReadOnlyList<Expr> Args { get; }
    public CallExpr(Span s, string n, IReadOnlyList<Expr> a) : base(s) => (Name, Args) = (n, a);
}

public sealed class UnaryExpr : Expr
{
    public string Op { get; } 
    public Expr Operand { get; } 
    public UnaryExpr (Span s, string op, Expr o) : base(s) => (Op, Operand) = (op, o);
}

public sealed class BinaryExpr : Expr
{
    public string Op { get; } 
    public Expr Left { get; } 
    public Expr Right { get; } 
    public BinaryExpr(Span s, string op, Expr l, Expr r) : base(s) => (Op, Left, Right) = (op, l, r);
}