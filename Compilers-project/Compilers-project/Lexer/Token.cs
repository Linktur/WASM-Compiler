namespace Compilers_project.Lexer;

public readonly record struct Token(
    TokenType Type,
    Span Span,
    string? Text = null,
    long? IntValue = null,
    double? RealValue = null,
    bool? BoolValue = null
);