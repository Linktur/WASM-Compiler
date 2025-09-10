namespace Compilers_project.Lexer;

public readonly record struct Span(int Start, int Length, int Line, int Col)
{
    public int End => Start + Length;
}