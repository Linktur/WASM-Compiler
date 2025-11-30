namespace Compilers_project.Lexer;

/// <summary>
/// Типы токенов, распознаваемых лексером.
/// </summary>
public enum TokenType
{
    // Специальные токены
    /// <summary>Конец файла</summary>
    Eof,
    /// <summary>Ошибка лексического анализа</summary>
    Error,
    /// <summary>Перевод строки</summary>
    NewLine,

    // Идентификаторы и литералы
    /// <summary>Идентификатор переменной или функции</summary>
    Identifier,
    /// <summary>Целочисленный литерал</summary>
    IntegerLiteral,
    /// <summary>Вещественный литерал</summary>
    RealLiteral,
    /// <summary>Булев литерал (true/false)</summary>
    BooleanLiteral,

    // Ключевые слова - объявления
    /// <summary>Объявление переменной</summary>
    Var,
    /// <summary>Объявление типа</summary>
    Type,
    /// <summary>Объявление записи (структуры)</summary>
    Record,
    /// <summary>Объявление массива</summary>
    Array,
    /// <summary>Объявление процедуры/функции</summary>
    Routine,
    /// <summary>Ключев слово для инициализации</summary>
    Is,
    /// <summary>Завершение блока</summary>
    End,

    // Ключевые слова - управление потоком
    /// <summary>Условный оператор</summary>
    If,
    /// <summary>Тогда (часть if)</summary>
    Then,
    /// <summary>Иначе (часть if)</summary>
    Else,
    /// <summary>Цикл while</summary>
    While,
    /// <summary>Цикл for</summary>
    For,
    /// <summary>Обратный итератор для for</summary>
    Reverse,
    /// <summary>Тело цикла</summary>
    Loop,
    /// <summary>Возврат из функции</summary>
    Return,
    /// <summary>Вывод на консоль</summary>
    Print,

    // Ключевые слова - логические операторы
    /// <summary>Логическое И</summary>
    And,
    /// <summary>Логическое ИЛИ</summary>
    Or,
    /// <summary>Логическое исключающее ИЛИ</summary>
    Xor,
    /// <summary>Логическое НЕ</summary>
    Not,
    /// <summary>Оператор принадлежности (for in)</summary>
    In,

    // Разделители и скобки
    /// <summary>Открывающая круглая скобка</summary>
    LParen,
    /// <summary>Закрывающая круглая скобка</summary>
    RParen,
    /// <summary>Открывающая квадратная скобка</summary>
    LBracket,
    /// <summary>Закрывающая квадратная скобка</summary>
    RBracket,
    /// <summary>Запятая</summary>
    Comma,
    /// <summary>Двоеточие</summary>
    Colon,
    /// <summary>Точка с запятой</summary>
    Semicolon,
    /// <summary>Точка</summary>
    Dot,

    // Операторы
    /// <summary>Диапазон (две точки)</summary>
    DotDot,
    /// <summary>Присваивание</summary>
    Assign,
    /// <summary>Стрелка (для функций)</summary>
    Arrow,
    /// <summary>Сложение</summary>
    Plus,
    /// <summary>Вычитание</summary>
    Minus,
    /// <summary>Умножение</summary>
    Star,
    /// <summary>Деление</summary>
    Slash,
    /// <summary>Остаток от деления</summary>
    Percent,

    // Операторы сравнения
    /// <summary>Меньше</summary>
    Less,
    /// <summary>Меньше или равно</summary>
    LessEqual,
    /// <summary>Больше</summary>
    Greater,
    /// <summary>Больше или равно</summary>
    GreaterEqual,
    /// <summary>Равно</summary>
    Equal,
    /// <summary>Не равно</summary>
    NotEqual
}
