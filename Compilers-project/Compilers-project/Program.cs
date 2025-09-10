using Compilers_project.Lexer;

namespace Compilers_project;

internal class Program
{
    private static void Main()
    {
        var src = @"type Box is record
  var v : integer
end

routine main() is
  var a : Box
  var b : Box
  a.v := 1
  b := a
  a.v := 42
  print b.v
end";

        var lex = new Lexer.Lexer(src);
        for (;;)
        {
            var t = lex.NextToken();
            Console.WriteLine($"{t.Type,-15} @ {t.Span.Line}:{t.Span.Col}  '{t.Text}'");
            if (t.Type == TokenType.Eof) break;
        }
    }
}