using System.Collections.Generic;
using Compilers_project.Lexer;

namespace Compilers_project.Parser.AST;

public abstract class Decl : Node
{
    protected Decl(Span span) : base(span) { }
}

public sealed class VarDecl : Decl
{
    public string Name { get; }
    public TypeRef? Type { get; }      // может отсутствовать
    public Expr? Initializer { get; }  // может отсутствовать

    public VarDecl(Span span, string name, TypeRef? type, Expr? init) : base(span)
        => (Name, Type, Initializer) = (name, type, init);
}

public sealed class TypeDecl : Decl
{
    public string Name { get; }
    public TypeRef Type { get; }

    public TypeDecl(Span span, string name, TypeRef type) : base(span)
        => (Name, Type) = (name, type);
}

public sealed class RoutineDecl : Decl
{
    public string Name { get; }
    public IReadOnlyList<Param> Parameters { get; }
    public TypeRef? ReturnType { get; }
    public RoutineBody? Body { get; }  // null для forward declarations

    public RoutineDecl(Span span, string name, IReadOnlyList<Param> parameters, TypeRef? returnType, RoutineBody? body)
        : base(span) => (Name, Parameters, ReturnType, Body) = (name, parameters, returnType, body);
}

public sealed class Param
{
    public string Name { get; }
    public TypeRef Type { get; }

    public Param(string name, TypeRef type) => (Name, Type) = (name, type);
}


public abstract class RoutineBody { }

// Вариант 1: рутина задаётся через стрелку => expr
// Пример: routine add(a: integer, b: integer) : integer => a + b
public sealed class ExprBody : RoutineBody
{
    public Expr Expr { get; }

    public ExprBody(Expr expr)
    {
        Expr = expr;
    }
}

// Вариант 2: рутина задаётся через блок is ... end
// Пример:
// routine main()
// is
//   print 1;
// end
public sealed class BlockBody : RoutineBody
{
    public Block Block { get; }

    public BlockBody(Block block)
    {
        Block = block;
    }
}
