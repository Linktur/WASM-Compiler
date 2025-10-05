using System.Collections.Generic;
using Compilers_project.Lexer;

namespace Compilers_project.Parser;

/// <summary>Простая копилка диагностик для лексера/парсера/семантики.</summary>
public sealed class Diagnostics
{
    public readonly List<(Span Where, string Message)> Items = new();
    public bool HasErrors => Items.Count > 0;

    public void Error(Span where, string message) => Items.Add((where, message));
}