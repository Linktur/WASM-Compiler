using Compilers_project.Lexer;

namespace Compilers_project.Parser.AST;

public abstract class Node
{
    public Span Span { get; }

    protected Node(Span span) => Span = span;
}