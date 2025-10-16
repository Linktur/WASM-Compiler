# Compilers Project

Таргет - WASM
Имплементация на C#

## Пример программы

```
routine main() is
  var x : integer is 10
  print x
end
```

## Запуск

```bash
# Сборка
dotnet build -c Release Compilers-project/Compilers-project.sln

# Показать справку
dotnet run --project Compilers-project/Compilers-project

# Запуск с конкретным файлом
dotnet run --project Compilers-project/Compilers-project -- TestCases/Test1

# Запуск со всеми файлами в папке
dotnet run --project Compilers-project/Compilers-project -- TestCases

# Вывод токенов (режим лексера)
dotnet run --project Compilers-project/Compilers-project -- --lex TestCases/Test1

# Визуализация AST (древовидный вывод)
dotnet run --project Compilers-project/Compilers-project -- --ast TestCases/Test1

# Экспорт AST в JSON
dotnet run --project Compilers-project/Compilers-project -- --ast-json TestCases/Test1

# Комбинация флагов
dotnet run --project Compilers-project/Compilers-project -- --lex --ast TestCases/Test1
```

## Тестирование

```bash
# Запуск всех тестов
dotnet test Compilers-project.Tests

# Запуск тестов с фильтром
dotnet test Compilers-project.Tests --filter "FullyQualifiedName~LexerTests"

# Подробнее см. Compilers-project.Tests/README.md
```

## Структура

- `Lexer/` — лексический анализатор (токенизация)
- `Parser/` — синтаксический анализатор и визуализация AST
- `TestCases/` — примеры программ для тестирования

## Визуализация AST

Для визуализации абстрактного синтаксического дерева:

1. **Текстовый вывод в консоль:**
   ```bash
   dotnet run --project Compilers-project/Compilers-project -- --ast TestCases/Test1
   ```

2. **JSON для веб-визуализации:**
   ```bash
   dotnet run --project Compilers-project/Compilers-project -- --ast-json TestCases/Test1 > ast.json
   ```
   
3. **Откройте `ast-viewer.html` в браузере** и вставьте JSON для интерактивной визуализации
