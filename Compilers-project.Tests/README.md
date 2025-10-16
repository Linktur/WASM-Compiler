# Тесты компилятора

Автоматические тесты для лексера и парсера компилятора.

## Запуск тестов

```bash
# Запустить все тесты
dotnet test Compilers-project.Tests

# Запустить с подробным выводом
dotnet test Compilers-project.Tests --verbosity detailed

# Запустить только тесты лексера
dotnet test Compilers-project.Tests --filter "FullyQualifiedName~LexerTests"

# Запустить только тесты парсера
dotnet test Compilers-project.Tests --filter "FullyQualifiedName~ParserTests"

# Запустить интеграционные тесты
dotnet test Compilers-project.Tests --filter "FullyQualifiedName~IntegrationTests"

# Запустить конкретный тест
dotnet test Compilers-project.Tests --filter "FullyQualifiedName~Lexer_ShouldRecognizeKeywords"
```

## Структура тестов

### LexerTests.cs
Тесты лексического анализатора:
- Распознавание ключевых слов
- Распознавание идентификаторов
- Распознавание литералов (целые, вещественные, булевы)
- Распознавание операторов и пунктуации
- Обработка комментариев
- Обработка переводов строк
- Обработка ошибок (неожиданные символы, некорректные числа)

### ParserTests.cs
Тесты синтаксического анализатора:
- Объявления переменных
- Объявления типов (record, array)
- Объявления функций и процедур
- Forward declarations
- Операторы (if, while, for, присваивание)
- Выражения (арифметические, логические)
- Вызовы функций и процедур
- Доступ к полям и элементам массивов

### IntegrationTests.cs
Интеграционные тесты:
- Тесты на реальных файлах из папки TestCases
- Комплексные программы
- Проверка корректности парсинга без ошибок

## Добавление новых тестов

1. Создайте новый метод с атрибутом `[Fact]` для одного теста
2. Или используйте `[Theory]` с `[InlineData]` для параметризованных тестов
3. Используйте `Assert.*` методы для проверки результатов

Пример:
```csharp
[Fact]
public void MyNewTest()
{
    var lexer = new LexerClass("var x");
    var token = lexer.NextToken();
    Assert.Equal(TokenType.Var, token.Type);
}
```

## Покрытие тестами

- ✅ Лексер: все основные токены и ошибки
- ✅ Парсер: все конструкции языка
- ✅ Интеграция: все тестовые файлы из TestCases
