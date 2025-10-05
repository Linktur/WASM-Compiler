namespace Compilers_project.Lexer;

public enum TokenType
{
    // хз,как описать спецфигня
    Eof, Error, NewLine,

    // идентификаторы/литералы
    Identifier, IntegerLiteral, RealLiteral, BooleanLiteral,

    // ключевые слова
    Var, Type, Record, Array, Routine, Is, End,
    If, Then, Else, While, For, Reverse, Loop, Return, Print,
    And, Or, Xor, Not, In,

    // знаки/операторы
    LParen, RParen, LBracket, RBracket, Comma, Colon, Semicolon, Dot,
    DotDot, Assign, Arrow, Plus, Minus, Star, Slash, Percent,
    Less, LessEqual, Greater, GreaterEqual, Equal, NotEqual
}
