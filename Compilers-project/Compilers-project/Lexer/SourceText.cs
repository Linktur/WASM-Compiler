namespace Compilers_project.Lexer;

/// <summary>
/// Управляет позицией в исходном тексте с отслеживанием строки и колонки.
/// Предоставляет методы для навигации по тексту и создания span'ов для токенов.
/// </summary>
internal sealed class SourceText
{
    #region Construction

    /// <summary>
    /// Создает новый экземпляр SourceText для указанного исходного кода.
    /// </summary>
    /// <param name="text">Исходный текст программы</param>
    public SourceText(string text)
    {
        Text = text ?? string.Empty;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Исходный текст программы.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Текущая позиция в тексте (0-based).
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    /// Текущий номер строки (1-based).
    /// </summary>
    public int Line { get; private set; } = 1;

    /// <summary>
    /// Текущий номер колонки (1-based).
    /// </summary>
    public int Col { get; private set; } = 1;

    /// <summary>
    /// Указывает, достигнут ли конец файла.
    /// </summary>
    public bool IsEof => Position >= Text.Length;

    #endregion

    #region Navigation Methods

    /// <summary>
    /// Посмотреть текущий символ без сдвига курсора.
    /// </summary>
    /// <returns>Текущий символ или '\0' если достигнут конец файла</returns>
    public char Peek()
    {
        return Position < Text.Length ? Text[Position] : '\0';
    }

    /// <summary>
    /// Посмотреть символ после текущего (lookahead на 1) без сдвига.
    /// </summary>
    /// <returns>Следующий символ или '\0' если достигнут конец файла</returns>
    public char PeekAhead()
    {
        return Position + 1 < Text.Length ? Text[Position + 1] : '\0';
    }

    /// <summary>
    /// Считать текущий символ и сдвинуться на 1 вправо.
    /// Колонка увеличивается на 1. Для перевода строки используйте <see cref="ConsumeNewLine" />.
    /// </summary>
    /// <returns>Текущий символ или '\0' если достигнут конец файла</returns>
    public char Advance()
    {
        if (IsEof) return '\0';

        var c = Text[Position++];
        Col++; // перенос строки НЕ обрабатываем здесь — это делает ConsumeNewLine()
        return c;
    }

    /// <summary>
    /// Если под курсором перевод строки, потребляет его как одну логическую новую строку.
    /// Поддерживает варианты: "\n", "\r", "\r\n".
    /// </summary>
    public void ConsumeNewLine()
    {
        if (IsEof) return;

        var c = Peek();
        if (c == '\r')
        {
            // CR или CRLF
            Advance(); // съели '\r'
            if (Peek() == '\n') // если это CRLF — съесть и '\n'
                Advance();

            Line++;
            Col = 1;
            return;
        }

        if (c == '\n')
        {
            Advance(); // LF
            Line++;
            Col = 1;
        }
    }

    #endregion

    #region Position Management

    /// <summary>
    /// Перемотать к указанной позиции.
    /// Для корректности Line/Col пересчитываются путем повторного прохода от начала.
    /// </summary>
    /// <param name="position">Новая позиция в тексте</param>
    public void Reset(int position)
    {
        if (position < 0) position = 0;
        if (position > Text.Length) position = Text.Length;

        Position = 0;
        Line = 1;
        Col = 1;

        while (Position < position)
        {
            var c = Peek();
            if (c == '\r')
            {
                Advance();
                if (Peek() == '\n') Advance(); // CRLF
                Line++;
                Col = 1;
            }
            else if (c == '\n')
            {
                Advance(); // LF
                Line++;
                Col = 1;
            }
            else
            {
                Advance();
            }
        }
    }

    /// <summary>
    /// Создать Span для токена/лексемы на основе начальной позиции.
    /// </summary>
    /// <param name="startPos">Начальная позиция токена</param>
    /// <param name="startLine">Начальная строка токена</param>
    /// <param name="startCol">Начальная колонка токена</param>
    /// <returns>Span, описывающий позицию токена</returns>
    public Span MakeSpan(int startPos, int startLine, int startCol)
    {
        var length = Math.Max(0, Position - startPos);
        return new Span(startPos, length, startLine, startCol);
    }

    /// <summary>
    /// Получить подстроку от текущей позиции до указанной длины.
    /// </summary>
    /// <param name="length">Длина подстроки</param>
    /// <returns>Подстрока или пустая строка если недостаточно символов</returns>
    public string Substring(int length)
    {
        if (Position + length > Text.Length)
            length = Text.Length - Position;

        return Text.Substring(Position, length);
    }

    /// <summary>
    /// Проверить, начинается ли указанный текст с текущей позиции.
    /// </summary>
    /// <param name="text">Текст для проверки</param>
    /// <returns>true если текст начинается с текущей позиции</returns>
    public bool Matches(string text)
    {
        if (string.IsNullOrEmpty(text) || Position + text.Length > Text.Length)
            return false;

        return Text.Substring(Position, text.Length) == text;
    }

    #endregion
}