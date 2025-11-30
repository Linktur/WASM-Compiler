namespace Compilers_project.Lexer;

/// <summary>
/// Представляет токен, распознанный лексером.
/// </summary>
public readonly record struct Token(
    /// <summary>Тип токена</summary>
    TokenType Type,
    /// <summary>Позиция токена в исходном коде</summary>
    Span Span,
    /// <summary>Текстовое представление токена</summary>
    string? Text = null,
    /// <summary>Значение для целочисленных литералов</summary>
    long? IntValue = null,
    /// <summary>Значение для вещественных литералов</summary>
    double? RealValue = null,
    /// <summary>Значение для булевых литералов</summary>
    bool? BoolValue = null
)
{
    /// <summary>
    /// Возвращает строковое представление токена для отладки.
    /// </summary>
    public override string ToString()
    {
        var value = Type switch
        {
            TokenType.IntegerLiteral => IntValue?.ToString() ?? Text,
            TokenType.RealLiteral => RealValue?.ToString() ?? Text,
            TokenType.BooleanLiteral => BoolValue?.ToString() ?? Text,
            TokenType.Identifier => Text,
            TokenType.Error => Text,
            _ => Text
        };

        return value != null ? $"{Type}({value})" : Type.ToString();
    }

    /// <summary>
    /// Проверяет, является ли токен литералом.
    /// </summary>
    public bool IsLiteral => Type is TokenType.IntegerLiteral or TokenType.RealLiteral or TokenType.BooleanLiteral;

    /// <summary>
    /// Проверяет, является ли токен ключевым словом.
    /// </summary>
    public bool IsKeyword => Type is >= TokenType.Var and <= TokenType.Not;

    /// <summary>
    /// Проверяет, является ли токен оператором.
    /// </summary>
    public bool IsOperator => Type is >= TokenType.DotDot and <= TokenType.NotEqual;

    /// <summary>
    /// Проверяет, является ли токен разделителем.
    /// </summary>
    public bool IsPunctuation => Type is TokenType.LParen or TokenType.RParen or TokenType.LBracket or
                                TokenType.RBracket or TokenType.Comma or TokenType.Colon or
                                TokenType.Semicolon or TokenType.Dot;

    /// <summary>
    /// Получает значение токена как object, если оно доступно.
    /// </summary>
    public object? Value => Type switch
    {
        TokenType.IntegerLiteral => IntValue,
        TokenType.RealLiteral => RealValue,
        TokenType.BooleanLiteral => BoolValue,
        _ => Text
    };
}