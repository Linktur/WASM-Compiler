using System.Globalization;
using Compilers_project.Lexer.Interfaces;

namespace Compilers_project.Lexer;

public sealed class Lexer : ILexer
{
    /// <summary>Источник символов с учётом позиции/строки/колонки.</summary>
    private readonly SourceText _src;

    /// <summary>Таблица ключевых слов: текст → тип токена.</summary>
    private readonly Dictionary<string, TokenType> _kw;

    /// <summary>Буфер lookahead (результаты NextCore для реализации f(k)).</summary>
    private readonly List<Token> _tokenList = new();

    #region Construction

    /// <summary>
    /// Создаёт лексер для заданной строки исходника.
    /// </summary>
    public Lexer(string source)
    {
        _src = new SourceText(source);
        _kw = InitKeywords();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Возвращает следующий токен, учитывая буфер <see cref="Peek"/>.
    /// </summary>
    public Token NextToken()
    {
        if (_tokenList.Count > 0)
        {
            var t = _tokenList[0];
            _tokenList.RemoveAt(0);
            return t;
        }
        return NextCore();
    }

    /// <summary>
    /// Заглядывает вперёд на k-й токен, не потребляя его.
    /// </summary>
    public Token Peek(int k = 1)
    {
        while (_tokenList.Count < k)
            _tokenList.Add(NextCore());
        return _tokenList[k - 1];
    }

    /// <summary>
    /// Сбрасывает лексер на указанную позицию исходника.
    /// Очищает буфер lookahead.
    /// </summary>
    public void Reset(int position = 0)
    {
        _tokenList.Clear();
        _src.Reset(position);
    }

    #endregion

    #region Core Lexing

    /// <summary>
    /// Основной цикл распознавания одного токена:
    /// пропуск пробелов -> NewLine -> идентификатор/ключевое -> число -> оператор/знак.
    /// </summary>
    private Token NextCore()
    {
        SkipSpacesAndComments(); // пробел/таб/комментарии

        // Перевод строки — значимый токен
        var nl = TryLexNewLine();
        if (nl.HasValue) return nl.Value;

        if (_src.IsEof)
            return Tok(TokenType.Eof, _src.Position, 0, _src.Line, _src.Col, null);

        int startPos  = _src.Position;
        int startLine = _src.Line;
        int startCol  = _src.Col;

        var id = TryLexIdentifier(startPos, startLine, startCol);
        if (id.HasValue) return id.Value;

        var num = TryLexNumber(startPos, startLine, startCol);
        if (num.HasValue) return num.Value;

        return LexOperatorOrPunct(startPos, startLine, startCol);
    }

    #region Utility Methods

    /// <summary>
    /// Пропускает пробелы, табуляцию и комментарии. Переводы строк не трогаем.
    /// </summary>
    private void SkipSpacesAndComments()
    {
        while (!_src.IsEof)
        {
            char c = _src.Peek();
            
            // Пробелы и табы
            if (c == ' ' || c == '\t') 
            { 
                _src.Advance(); 
                continue; 
            }
            
            // Комментарии //
            if (c == '/' && _src.PeekAhead() == '/')
            {
                // Пропускаем до конца строки
                _src.Advance(); // первый /
                _src.Advance(); // второй /
                while (!_src.IsEof && _src.Peek() != '\n' && _src.Peek() != '\r')
                    _src.Advance();
                continue;
            }
            
            break;
        }
    }

    /// <summary>
    /// Распознаёт перевод строки (LF, CR, CRLF) как один токен NewLine.
    /// Если под курсором не перевод строки — возвращает null.
    /// </summary>
    private Token? TryLexNewLine()
    {
        char c = _src.Peek();
        if (c == '\r' || c == '\n')
        {
            int startPos = _src.Position;
            int startLine = _src.Line;
            int startCol = _src.Col;
            _src.ConsumeNewLine();
            var span = new Span(startPos, 1, startLine, startCol);
            return new Token(TokenType.NewLine, span, "\\n");
        }
        return null;
    }

    /// <summary>
    /// Проверка: допустим ли символ как первый в идентификаторе.
    /// </summary>
    private static bool IsIdentStart(char c)
        => c == '_' || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

    /// <summary>
    /// Проверка: допустим ли символ как продолжение идентификатора (буква/цифра/'_').
    /// </summary>
    private static bool IsIdentPart(char c)
        => IsIdentStart(c) || (c >= '0' && c <= '9');

    /// <summary>
    /// Пытается распознать идентификатор/ключевое/булев литерал с позиции <paramref name="startPos"/>.
    /// Успех готовый токен; иначе null.
    /// </summary>
    private Token? TryLexIdentifier(int startPos, int startLine, int startCol)
    {
        char c = _src.Peek();
        if (!IsIdentStart(c)) return null;

        _src.Advance();
        while (!_src.IsEof && IsIdentPart(_src.Peek()))
            _src.Advance();

        var span = _src.MakeSpan(startPos, startLine, startCol);
        string text = _src.Text.Substring(span.Start, span.Length);

        return CreateIdentifierOrKeywordToken(text, startPos, startLine, startCol);
    }

    #endregion

    #region Number Parsing

    /// <summary>
    /// Пытается распознать число: целое или вещественное (с опциональной экспонентой).
    /// Специально различает кейс диапазона <c>1..3</c> и вещественного <c>1.23</c>.
    /// Успех готовый токен; иначе null.
    /// </summary>
    private Token? TryLexNumber(int startPos, int startLine, int startCol)
    {
        char c = _src.Peek();
        if (!char.IsDigit(c)) return null;

        // Читаем целую часть
        ReadIntegerPart();

        // Проверяем на двусмысленность: 1..3  vs  1.23
        if (_src.IsEof || _src.Peek() != '.')
        {
            return ParseInteger(startPos, startLine, startCol);
        }

        // Начинается с точки - проверяем что дальше
        char nextChar = _src.PeekAhead();
        if (nextChar == '.')
        {
            return ParseInteger(startPos, startLine, startCol);
        }

        if (char.IsDigit(nextChar))
        {
            return ParseRealNumber(startPos, startLine, startCol);
        }

        return ParseInteger(startPos, startLine, startCol);
    }

    /// <summary>
    /// Читает целую часть числа (последовательность цифр).
    /// </summary>
    private void ReadIntegerPart()
    {
        _src.Advance(); // первая цифра уже проверена
        while (!_src.IsEof && char.IsDigit(_src.Peek()))
            _src.Advance();
    }

    /// <summary>
    /// Парсит целое число.
    /// </summary>
    private Token ParseInteger(int startPos, int startLine, int startCol)
    {
        var span = _src.MakeSpan(startPos, startLine, startCol);
        string text = _src.Text.Substring(span.Start, span.Length);
        return CreateIntegerToken(text, startPos, startLine, startCol);
    }

    /// <summary>
    /// Парсит вещественное число с дробной частью и опциональной экспонентой.
    /// </summary>
    private Token ParseRealNumber(int startPos, int startLine, int startCol)
    {
        _src.Advance(); // пропускаем точку
        ReadFractionalPart();
        TryParseExponent();

        // Проверка на ошибку типа 1.3.5 (несколько точек)
        if (HasMultipleDecimalPoints())
        {
            var errSpan = _src.MakeSpan(startPos, startLine, startCol);
            return ErrorToken("Invalid number literal: multiple decimal points", startPos, startLine, startCol, errSpan.Length);
        }

        var span = _src.MakeSpan(startPos, startLine, startCol);
        string text = _src.Text.Substring(span.Start, span.Length);
        return CreateRealToken(text, startPos, startLine, startCol);
    }

    /// <summary>
    /// Читает дробную часть вещественного числа.
    /// </summary>
    private void ReadFractionalPart()
    {
        while (!_src.IsEof && char.IsDigit(_src.Peek()))
            _src.Advance();
    }

    /// <summary>
    /// Пытается распознать научную нотацию (экспоненту).
    /// </summary>
    private void TryParseExponent()
    {
        if (_src.IsEof || (_src.Peek() != 'e' && _src.Peek() != 'E'))
            return;

        int savePos = _src.Position;
        _src.Advance(); // пропускаем 'e' или 'E'

        // Опциональный знак
        if (!_src.IsEof && (_src.Peek() == '+' || _src.Peek() == '-'))
            _src.Advance();

        // Должны быть цифры после экспоненты
        if (!_src.IsEof && char.IsDigit(_src.Peek()))
        {
            while (!_src.IsEof && char.IsDigit(_src.Peek()))
                _src.Advance();
        }
        else
        {
            _src.Reset(savePos); // откатываемся, если экспонента некорректна
        }
    }

    /// <summary>
    /// Проверяет наличие нескольких десятичных точек в числе.
    /// </summary>
    private bool HasMultipleDecimalPoints()
    {
        return !_src.IsEof && _src.Peek() == '.' && char.IsDigit(_src.PeekAhead());
    }

    #endregion

    #region Operator Parsing

    /// <summary>
    /// Распознаёт операторы и разделители.
    /// Многосимвольные комбинации обрабатываются первыми
    /// </summary>
    private Token LexOperatorOrPunct(int startPos, int startLine, int startCol)
    {
        char c = _src.Peek();

        // Проверяем многосимвольные операторы
        var multiCharToken = TryLexMultiCharOperator(c, startPos, startLine, startCol);
        if (multiCharToken != null)
            return multiCharToken.Value;

        // Проверяем одиночные символы
        return LexSingleCharOperator(c, startPos, startLine, startCol);
    }

    /// <summary>
    /// Пытается распознать многосимвольный оператор.
    /// </summary>
    private Token? TryLexMultiCharOperator(char c, int startPos, int startLine, int startCol)
    {
        return c switch
        {
            ':' => TryLexAssignOperator(startPos, startLine, startCol),
            '=' => TryLexArrowOperator(startPos, startLine, startCol),
            '.' => TryLexDotDotOperator(startPos, startLine, startCol),
            '<' => TryLexLessOperator(startPos, startLine, startCol),
            '>' => TryLexGreaterOperator(startPos, startLine, startCol),
            '/' => TryLexNotEqualOperator(startPos, startLine, startCol),
            _ => null
        };
    }

    /// <summary>
    /// Распознает операторы := или :.
    /// </summary>
    private Token TryLexAssignOperator(int startPos, int startLine, int startCol)
    {
        _src.Advance();
        if (_src.Peek() == '=')
        {
            _src.Advance();
            return Make(TokenType.Assign, startPos, startLine, startCol);
        }
        return Make(TokenType.Colon, startPos, startLine, startCol);
    }

    /// <summary>
    /// Распознает операторы => или =.
    /// </summary>
    private Token TryLexArrowOperator(int startPos, int startLine, int startCol)
    {
        _src.Advance();
        if (_src.Peek() == '>')
        {
            _src.Advance();
            return Make(TokenType.Arrow, startPos, startLine, startCol);
        }
        return Make(TokenType.Equal, startPos, startLine, startCol);
    }

    /// <summary>
    /// Распознает операторы .. или ..
    /// </summary>
    private Token TryLexDotDotOperator(int startPos, int startLine, int startCol)
    {
        _src.Advance();
        if (_src.Peek() == '.')
        {
            _src.Advance();
            return Make(TokenType.DotDot, startPos, startLine, startCol);
        }
        return Make(TokenType.Dot, startPos, startLine, startCol);
    }

    /// <summary>
    /// Распознает операторы <= или <.
    /// </summary>
    private Token TryLexLessOperator(int startPos, int startLine, int startCol)
    {
        _src.Advance();
        if (_src.Peek() == '=')
        {
            _src.Advance();
            return Make(TokenType.LessEqual, startPos, startLine, startCol);
        }
        return Make(TokenType.Less, startPos, startLine, startCol);
    }

    /// <summary>
    /// Распознает операторы >= или >.
    /// </summary>
    private Token TryLexGreaterOperator(int startPos, int startLine, int startCol)
    {
        _src.Advance();
        if (_src.Peek() == '=')
        {
            _src.Advance();
            return Make(TokenType.GreaterEqual, startPos, startLine, startCol);
        }
        return Make(TokenType.Greater, startPos, startLine, startCol);
    }

    /// <summary>
    /// Распознает операторы /= или /.
    /// </summary>
    private Token TryLexNotEqualOperator(int startPos, int startLine, int startCol)
    {
        _src.Advance();
        if (_src.Peek() == '=')
        {
            _src.Advance();
            return Make(TokenType.NotEqual, startPos, startLine, startCol);
        }
        return Make(TokenType.Slash, startPos, startLine, startCol);
    }

    /// <summary>
    /// Распознает одиночные символы-операторы.
    /// </summary>
    private Token LexSingleCharOperator(char c, int startPos, int startLine, int startCol)
    {
        _src.Advance();
        return c switch
        {
            '(' => Make(TokenType.LParen, startPos, startLine, startCol),
            ')' => Make(TokenType.RParen, startPos, startLine, startCol),
            '[' => Make(TokenType.LBracket, startPos, startLine, startCol),
            ']' => Make(TokenType.RBracket, startPos, startLine, startCol),
            ',' => Make(TokenType.Comma, startPos, startLine, startCol),
            ';' => Make(TokenType.Semicolon, startPos, startLine, startCol),
            '+' => Make(TokenType.Plus, startPos, startLine, startCol),
            '-' => Make(TokenType.Minus, startPos, startLine, startCol),
            '*' => Make(TokenType.Star, startPos, startLine, startCol),
            '%' => Make(TokenType.Percent, startPos, startLine, startCol),
            // '\r' и '\n' уже обрабатываются в TryLexNewLine()
            _ => ErrorToken($"Unexpected char '{c}'", startPos, startLine, startCol, 1)
        };
    }

    #endregion

    #region Keyword Initialization

    /// <summary>
    /// Создаёт таблицу ключевых слов (строка -> тип токена).
    /// </summary>
    private static Dictionary<string, TokenType> InitKeywords() => new(StringComparer.Ordinal)
    {
        ["var"]=TokenType.Var, ["type"]=TokenType.Type, ["record"]=TokenType.Record,
        ["array"]=TokenType.Array, ["routine"]=TokenType.Routine, ["is"]=TokenType.Is,
        ["end"]=TokenType.End, ["if"]=TokenType.If, ["then"]=TokenType.Then, ["else"]=TokenType.Else,
        ["while"]=TokenType.While, ["for"]=TokenType.For, ["in"]=TokenType.In, ["reverse"]=TokenType.Reverse,
        ["loop"]=TokenType.Loop, ["return"]=TokenType.Return, ["print"]=TokenType.Print,
        ["and"]=TokenType.And, ["or"]=TokenType.Or, ["xor"]=TokenType.Xor, ["not"]=TokenType.Not
    };

    #endregion

    #region Token Creation Helpers

    /// <summary>
    /// Упаковка токена заданного типа с вычислением <see cref="Span"/>.
    /// </summary>
    private Token Make(TokenType kind, int startPos, int startLine, int startCol, string? text = null)
    {
        var span = _src.MakeSpan(startPos, startLine, startCol);
        return new Token(kind, span, text);
    }

    /// <summary>
    /// Упаковка токена заданного типа с явными параметрами длины и координат (для EOF).
    /// </summary>
    private Token Tok(TokenType kind, int startPos, int len, int line, int col, string? text)
        => new(kind, new Span(startPos, len, line, col), text);

    /// <summary>
    /// Создаёт токен ошибки (Error) с текстом сообщения и корректным <see cref="Span"/>.
    /// </summary>
    private Token ErrorToken(string msg, int startPos, int startLine, int startCol, int len)
        => new(TokenType.Error, new Span(startPos, len, startLine, startCol), msg);

    /// <summary>
    /// Создаёт токен для целочисленного литерала.
    /// </summary>
    private Token CreateIntegerToken(string text, int startPos, int startLine, int startCol)
    {
        var span = _src.MakeSpan(startPos, startLine, startCol);
        if (long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            return new Token(TokenType.IntegerLiteral, span, text, value);
        return ErrorToken("Malformed integer literal", startPos, startLine, startCol, span.Length);
    }

    /// <summary>
    /// Создаёт токен для вещественного литерала.
    /// </summary>
    private Token CreateRealToken(string text, int startPos, int startLine, int startCol)
    {
        var span = _src.MakeSpan(startPos, startLine, startCol);
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return new Token(TokenType.RealLiteral, span, text, null, value);
        return ErrorToken("Malformed real literal", startPos, startLine, startCol, span.Length);
    }

    /// <summary>
    /// Создаёт токен для булевого литерала.
    /// </summary>
    private Token CreateBooleanToken(string text, int startPos, int startLine, int startCol)
    {
        var span = _src.MakeSpan(startPos, startLine, startCol);
        bool value = text == "true";
        return new Token(TokenType.BooleanLiteral, span, text, null, null, value);
    }

    /// <summary>
    /// Создаёт токен для идентификатора или ключевого слова.
    /// </summary>
    private Token CreateIdentifierOrKeywordToken(string text, int startPos, int startLine, int startCol)
    {
        var span = _src.MakeSpan(startPos, startLine, startCol);

        if (text == "true" || text == "false")
            return CreateBooleanToken(text, startPos, startLine, startCol);

        if (_kw.TryGetValue(text, out var tokenType))
            return new Token(tokenType, span, text);

        return new Token(TokenType.Identifier, span, text);
    }

    #endregion

    #endregion
}
