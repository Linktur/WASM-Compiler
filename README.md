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

# Семантический анализ
dotnet run --project Compilers-project/Compilers-project -- --semantic TestCases/Test1

# Комбинация флагов
dotnet run --project Compilers-project/Compilers-project -- --lex --ast --semantic TestCases/Test1
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
- `SemanticAnalyzer/` — семантический анализ (проверка типов, областей видимости)
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

## Семантический анализ

Семантический анализатор выполняет проверку типов и корректности программы:

```bash
# Запуск семантического анализа
dotnet run --project Compilers-project/Compilers-project -- --semantic TestCases/Test1
```

**Возможности семантического анализатора:**
- Проверка объявлений переменных и типов
- Проверка типов в выражениях и операторах
- Автоматическое приведение типов (integer ↔ real, boolean ↔ integer)
- Проверка вызовов функций (количество и типы аргументов)
- Проверка областей видимости
- Проверка доступа к полям записей и элементам массивов
- Проверка условий в if, while, for
- Проверка return в функциях

**Поддерживаемые преобразования типов (согласно спецификации):**
- `integer → real` (расширение)
- `integer → boolean` (0 → false, 1 → true)
- `real → integer` (сужение с округлением)
- `real → boolean` ❌ (запрещено)
- `boolean → integer` (false → 0, true → 1)
- `boolean → real` (false → 0.0, true → 1.0)

## Генерация кода WebAssembly

Компилятор генерирует WebAssembly Text Format (.wat):

```bash
# Компиляция в WASM
dotnet run --project Compilers-project/Compilers-project -- --compile TestCases/Test1

# Указать выходной файл
dotnet run --project Compilers-project/Compilers-project -- --compile TestCases/Test1 -o output.wat
```

### Пример генерируемого кода

Исходный код:
```
routine main() is
  for i in 1 .. 3 loop
    print i
  end
end
```

Результат (Test1.wat):
```wat
(module
  (import "env" "print_i32" (func $print_i32 (param i32)))
  (import "env" "print_f64" (func $print_f64 (param f64)))

  (func $main (export "main")
    (local $i i32)
    (i32.const 1)
    (local.set $i)
    (block $break
      (loop $continue
        (local.get $i)
        (i32.const 3)
        (i32.gt_s)
        (br_if $break)
        (local.get $i)
        (call $print_i32)
        (local.get $i)
        (i32.const 1)
        (i32.add)
        (local.set $i)
        (br $continue)
      )
    )
  )
)
```

### Запуск WASM

#### 1. Конвертация в бинарный формат

Установите [WABT](https://github.com/WebAssembly/wabt):
```bash
wat2wasm output.wat -o output.wasm
```

#### 2. Запуск в Node.js

Создайте `run.js`:
```javascript
const fs = require('fs');

const wasmBuffer = fs.readFileSync('output.wasm');

const imports = {
  env: {
    print_i32: (value) => console.log(value),
    print_f64: (value) => console.log(value)
  }
};

WebAssembly.instantiate(wasmBuffer, imports).then(result => {
  result.instance.exports.main();
});
```

Запуск:
```bash
node run.js
```

#### 3. Запуск в браузере

```html
<!DOCTYPE html>
<html>
<body>
<script>
const imports = {
  env: {
    print_i32: (v) => console.log(v),
    print_f64: (v) => console.log(v)
  }
};

fetch('output.wasm')
  .then(r => r.arrayBuffer())
  .then(bytes => WebAssembly.instantiate(bytes, imports))
  .then(result => result.instance.exports.main());
</script>
</body>
</html>
```

### Поддерживаемые конструкции

**Типы данных:**
- `integer` → i32
- `real` → f64
- `boolean` → i32 (0/1)

**Операторы:**
- Арифметические: `+`, `-`, `*`, `/`, `%`
- Сравнения: `<`, `<=`, `>`, `>=`, `=`, `/=`
- Логические: `and`, `or`, `xor`, `not`

**Управляющие конструкции:**
- `if ... then ... else ... end`
- `while ... loop ... end`
- `for i in start .. end loop ... end`
- `return`
- `print`

### Ограничения

Упрощённая версия генератора:
- Нет автоматического преобразования типов
- Нет поддержки массивов и записей
- Все операции выполняются над i32 (кроме f64 литералов)
