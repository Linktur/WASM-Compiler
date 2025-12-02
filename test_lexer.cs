using System;
using Compilers_project.Lexer;

class TestLexer
{
    static void Main()
    {
        var source = @"
routine main()
is
    var x : integer is 2 + 3
    var y : integer is 9
    print x, y
end";

        Console.WriteLine("Tokens:");
        var lexer = new Lexer(source);

        while (true)
        {
            var token = lexer.NextToken();
            Console.WriteLine($"{token.Type}: '{token.Text?.Replace("\n", "\\n").Replace("\r", "\\r")}'");

            if (token.Type == TokenType.Eof)
                break;
        }
    }
}