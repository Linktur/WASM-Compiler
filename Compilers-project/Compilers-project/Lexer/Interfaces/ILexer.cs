namespace Compilers_project.Lexer.Interfaces;

public interface ILexer
{
    Token NextToken(); 
    Token Peek(int k = 1);     
    void  Reset(int position = 0);
}