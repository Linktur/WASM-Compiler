# Compilers Project

Компилятор для учебного языка программирования с лексером и парсером.

## Требования

- .NET 8.0 SDK

## Запуск

```bash
# Сборка
dotnet build -c Release Compilers-project/Compilers-project.sln

# Запуск с тестовыми файлами (автоматически найдёт папку TestCases)
dotnet run --project Compilers-project/Compilers-project/Compilers-project.csproj

# Запуск с конкретным файлом
dotnet run --project Compilers-project/Compilers-project/Compilers-project.csproj -- TestCases/Test1

# Вывод токенов (режим лексера)
dotnet run --project Compilers-project/Compilers-project/Compilers-project.csproj -- --lex TestCases/Test1
```

## Структура

- `Lexer/` — лексический анализатор (токенизация)
- `Parser/` — синтаксический анализатор и AST
- `TestCases/` — примеры программ для тестирования

## Пример программы

```
routine main() is
  var x := 10
  print x
end
```
