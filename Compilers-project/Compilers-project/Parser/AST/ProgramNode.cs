using System.Collections.Generic;
using Compilers_project.Lexer;

namespace Compilers_project.Parser.AST;

/// <summary>Корень AST: список деклараций верхнего уровня.</summary>
public sealed class ProgramNode : Node
{
    public IReadOnlyList<Decl> Decls { get; }

    public ProgramNode(Span span, IReadOnlyList<Decl> decls) : base(span)
        => Decls = decls;
}