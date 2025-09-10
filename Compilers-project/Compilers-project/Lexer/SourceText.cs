namespace Compilers_project.Lexer;


internal sealed class SourceText
{
    public SourceText(string text)
    {
        Text = text ?? string.Empty;
    }

    public string Text { get; }

    public int Position { get; private set; }

    public int Line { get; private set; } = 1;

    public int Col { get; private set; } = 1;

    public bool IsEof => Position >= Text.Length;


    /// <summary>
    ///     Посмотреть текущий символ без сдвига курсора.
    /// </summary>
    public char Peek()
    {
        return Position < Text.Length ? Text[Position] : '\0';
    }

    /// <summary>
    ///     Посмотреть символ после текущего (lookahead на 1) без сдвига.
    /// </summary>
    public char PeekAhead()
    {
        return Position + 1 < Text.Length ? Text[Position + 1] : '\0';
    }

    /// <summary>
    ///     Считать текущий символ и сдвинуться на 1 вправо.
    ///     Колонка увеличивается на 1. Для перевода строки используйте <see cref="ConsumeNewLine" />.
    ///     Возвращает '\0' на EOF (позиция остаётся на конце).
    /// </summary>
    public char Advance()
    {
        if (IsEof) return '\0';

        var c = Text[Position++];
        Col++; // перенос строки НЕ обрабатываем здесь — это делает ConsumeNewLine()
        return c;
    }

    /// <summary>
    ///     Если под курсором перевод строки, потребляет его как одну логическую новую строку.
    ///     Поддерживает варианты: "\n", "\r", "\r\n".
    ///     По факту сдвигает курсор на 1 или 2 символа и обновляет Line/Col.
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

    /// <summary>
    ///     Перемотать к какой-то позиции.
    ///     Для корректности Line/Col пересчитываются путём повторного прохода от начала.
    /// </summary>
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
    ///     Создать <see cref="Span" /> для токена/лексемы,
    /// </summary>
    public Span MakeSpan(int startPos, int startLine, int startCol)
    {
        var length = Math.Max(0, Position - startPos);
        return new Span(startPos, length, startLine, startCol);
    }
}