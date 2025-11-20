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

### Быстрый старт

```bash
# 1. Перейти в директорию проекта
cd Compilers-project/Compilers-project

# 2. Скомпилировать программу в .wat (текстовый формат)
dotnet run -- --compile ../../TestCases/Test7

# 3. Сгенерировать .wasm (бинарный формат) - требует wat2wasm в PATH
dotnet run -- --wasm ../../TestCases/Test7
```

### Варианты компиляции

```bash
# Только .wat файл
dotnet run -- --compile TestCases/Test1

# .wat + .wasm (автоматически вызывает wat2wasm)
dotnet run -- --wasm TestCases/Test1

# Указать выходной файл
dotnet run -- --compile TestCases/Test1 -o output.wat
```

### Установка WABT (для генерации .wasm)

**Windows (winget):**
```bash
winget install wabt
```

**macOS:**
```bash
brew install wabt
```

**Linux:**
```bash
# Скачать с https://github.com/WebAssembly/wabt/releases
# Или собрать из исходников
```

**Проверка установки:**
```bash
wat2wasm --version
```

### Примеры

#### Пример 1: Простой цикл (Test7)

**Исходный код**:
```
routine main() is
  for i in 1 .. 3 loop
    print i
  end
end
```

**Компиляция:**
```bash
dotnet run -- --wasm TestCases/Test7
```

**Результат** (`Test7.wat`):
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

#### Пример 2: Записи (records)

**Исходный код** (`Test11`):
```
type Point is record
  var x : integer
  var y : integer
end

routine main() is
  var p : Point
  p.x := 7
  p.y := 9
  print p.x
  print p.y
end
```

**Компиляция:**
```bash
dotnet run -- --wasm TestCases/Test11
```

**Ключевые элементы WAT:**
```wat
(func $main (export "main")
  (local $p i32)              ; Адрес записи в памяти
  (i32.const 0)
  (local.set $p)              ; p указывает на адрес 0

  ; p.x := 7
  (local.get $p)              ; Адрес базы
  (i32.const 7)
  (i32.store)                 ; Сохранить в offset 0

  ; p.y := 9
  (local.get $p)
  (i32.const 4)               ; Смещение поля y
  (i32.add)
  (i32.const 9)
  (i32.store)                 ; Сохранить в offset 4
)
```

#### Пример 3: Преобразования типов

**Исходный код**:
```
routine main() is
  var x : integer is 10
  var y : real is 3.14
  var z : real

  // integer → real
  z := x
  print z

  // real → integer
  var a : integer
  a := y
  print a

  // Смешанная арифметика: integer + real → real
  z := x + y
  print z
end
```

**Ключевые преобразования в WAT:**
```wat
; z := x (integer → real)
(local.get $x)          ; i32
(f64.convert_i32_s)     ; Преобразование в f64
(local.set $z)

; a := y (real → integer)
(local.get $y)          ; f64
(i32.trunc_f64_s)       ; Усечение до i32
(local.set $a)

; z := x + y (смешанные типы)
(local.get $x)          ; i32
(f64.convert_i32_s)     ; Преобразование x в f64
(local.get $y)          ; f64
(f64.add)               ; Сложение f64
(local.set $z)
```

### Запуск WASM

#### Вариант 1: Node.js (рекомендуется)

Создайте `run.js` в корне проекта:
```javascript
const fs = require('fs');

// Путь к .wasm файлу
const wasmBuffer = fs.readFileSync('TestCases/Test7.wasm');

// Импорты из хост-окружения
const imports = {
  env: {
    print_i32: (value) => console.log(value),
    print_f64: (value) => console.log(value)
  }
};

// Загрузка и запуск
WebAssembly.instantiate(wasmBuffer, imports).then(result => {
  result.instance.exports.main();
});
```

Запуск:
```bash
node run.js
```

Вывод:
```
1
2
3
```

#### Вариант 2: Браузер

Создайте `index.html`:
```html
<!DOCTYPE html>
<html>
<head><meta charset="utf-8"></head>
<body>
  <h1>WASM Test</h1>
  <div id="output"></div>

  <script>
  const output = document.getElementById('output');

  const imports = {
    env: {
      print_i32: (v) => {
        output.innerHTML += v + '<br>';
      },
      print_f64: (v) => {
        output.innerHTML += v + '<br>';
      }
    }
  };

  fetch('TestCases/Test7.wasm')
    .then(r => r.arrayBuffer())
    .then(bytes => WebAssembly.instantiate(bytes, imports))
    .then(result => {
      result.instance.exports.main();
    })
    .catch(err => console.error(err));
  </script>
</body>
</html>
```

Запуск через локальный сервер:
```bash
# Python
python -m http.server 8000

# Node.js
npx http-server

# Открыть http://localhost:8000
```

#### Вариант 3: wasmtime (standalone runtime)

```bash
# Установка
curl https://wasmtime.dev/install.sh -sSf | bash

# Запуск
wasmtime TestCases/Test7.wasm
```

### Поддерживаемые конструкции

**Типы данных:**
- `integer` → i32
- `real` → f64
- `boolean` → i32 (0/1)
- `array[N] T` → линейная память (адрес i32)
- `record` → линейная память (адрес i32)

**Автоматическое преобразование типов:**
- `integer → real` (расширение через f64.convert_i32_s)
- `real → integer` (сужение через i32.trunc_f64_s)
- `integer → boolean` (0 → false, не-0 → true)
- `boolean → integer` (тривиально, оба i32)
- `boolean → real` (через f64.convert_i32_s)
- `real → boolean` ❌ (запрещено согласно спецификации)

**Операторы:**
- Арифметические: `+`, `-`, `*`, `/`, `%` (поддержка как i32, так и f64)
- Сравнения: `<`, `<=`, `>`, `>=`, `=`, `/=` (поддержка как i32, так и f64)
- Логические: `and`, `or`, `xor`, `not`

**Управляющие конструкции:**
- `if ... then ... else ... end`
- `while ... loop ... end`
- `for i in start .. end loop ... end`
- `return`
- `print`

**Записи (records):**
- Объявление: `type T is record var x : integer; var y : real end`
- Доступ к полям: `r.field`
- Присваивание полям: `r.field := expr`
- Записи хранятся в линейной памяти с вычисленными смещениями полей

### Ограничения

- Массивы поддерживают только примитивные типы (integer, real, boolean)
- Записи используют семантику ссылок (присваивание копирует адрес, а не значение)
- Операции смешанных типов (integer + real) автоматически приводятся к real
