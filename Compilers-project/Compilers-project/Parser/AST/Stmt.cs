using System.Collections.Generic;
using Compilers_project.Lexer;

namespace Compilers_project.Parser.AST;

public abstract class Stmt : Node
{
    protected Stmt(Span span) : base(span) { }
}

public sealed class Block : Node
{
    // блок может смешивать декларации и операторы.
    public IReadOnlyList<Node> Items { get; }
    public Block(Span span, IReadOnlyList<Node> items) : base(span) => Items = items;
}

public sealed class AssignStmt : Stmt
{
    public Expr Target { get; }  // семантика проверит, что это LValue (Name/Field/Index)
    public Expr Value  { get; }

    public AssignStmt(Span span, Expr target, Expr value) : base(span)
        => (Target, Value) = (target, value);
}

public sealed class CallStmt : Stmt
{
    public string Name { get; }
    public IReadOnlyList<Expr> Args { get; }

    public CallStmt(Span span, string name, IReadOnlyList<Expr> args) : base(span)
        => (Name, Args) = (name, args);
}

public sealed class IfStmt : Stmt
{
    public Expr Condition { get; }
    public Block Then { get; }
    public Block? Else { get; }

    public IfStmt(Span span, Expr cond, Block thenBlk, Block? elseBlk) : base(span)
        => (Condition, Then, Else) = (cond, thenBlk, elseBlk);
}

public sealed class WhileStmt : Stmt
{
    public Expr Condition { get; }
    public Block Body { get; }

    public WhileStmt(Span span, Expr cond, Block body) : base(span)
        => (Condition, Body) = (cond, body);
}

public sealed class ForStmt : Stmt
{
    public string Iterator { get; }
    public Expr First { get; }
    public Expr? Second { get; } // null => foreach по коллекции (например, массиву)
    public bool Reverse { get; }
    public Block Body { get; }

    public ForStmt(Span span, string iterator, Expr first, Expr? second, bool reverse, Block body) : base(span)
        => (Iterator, First, Second, Reverse, Body) = (iterator, first, second, reverse, body);
}

public sealed class ReturnStmt : Stmt
{
    public Expr? Value { get; }
    public ReturnStmt(Span span, Expr? value) : base(span) => Value = value;
}

public sealed class PrintStmt : Stmt
{
    public IReadOnlyList<Expr> Items { get; }
    public PrintStmt(Span span, IReadOnlyList<Expr> items) : base(span) => Items = items;
}

// Для восстановления после ошибок
public sealed class EmptyStmt : Stmt
{
    public EmptyStmt(Span span) : base(span) { }
}

// Оператор-блок (используется в оптимизациях)
public sealed class BlockStmt : Stmt
{
    public Block Block { get; }

    public BlockStmt(Span span, Block block) : base(span) => Block = block;
}
