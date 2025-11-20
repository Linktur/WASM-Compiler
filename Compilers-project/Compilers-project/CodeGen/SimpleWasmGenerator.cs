using System.Globalization;
using System.Text;
using Compilers_project.Parser.AST;

namespace Compilers_project.CodeGen;

/// <summary>
/// Генератор кода WebAssembly.
/// Преобразует AST в текстовый формат WAT (.wat).
///
/// Поддерживает:
/// - Переменные (integer, real, boolean)
/// - Массивы (через линейную память WASM)
/// - Записи (records) (через линейную память WASM)
/// - Преобразования типов (integer↔real, boolean↔integer, boolean↔real)
/// - Арифметические и логические операции
/// - Условные операторы (if/else)
/// - Циклы (while, for)
/// - Функции с параметрами и возвратом
/// - Вывод (print)
/// </summary>
public class SimpleWasmGenerator
{
    private readonly ProgramNode _program;
    private readonly StringBuilder _output = new();
    private int _indent = 0;

    // Локальные переменные текущей функции
    private readonly Dictionary<string, int> _locals = new();
    private int _localCount;

    // Типы локальных переменных (имя переменной -> TypeRef)
    private readonly Dictionary<string, TypeRef?> _localTypes = new();

    // Распределение памяти для массивов (offset в байтах)
    private int _memoryOffset = 0;

    // Адреса массивов в памяти (имя переменной -> offset)
    private readonly Dictionary<string, int> _arrayAddresses = new();

    // Определения типов (имя типа -> TypeRef)
    private readonly Dictionary<string, TypeRef> _typeDefinitions = new();

    // Информация о размерах и смещениях полей записей (имя типа -> список (имя поля, offset, размер))
    private readonly Dictionary<string, List<(string name, int offset, int size)>> _recordLayouts = new();

    public SimpleWasmGenerator(ProgramNode program)
    {
        _program = program;
    }

    /// <summary>
    /// Генерирует WASM модуль из AST программы.
    /// </summary>
    public string Generate()
    {
        // Собираем определения типов
        foreach (var decl in _program.Decls)
        {
            if (decl is TypeDecl typeDecl)
            {
                _typeDefinitions[typeDecl.Name] = typeDecl.Type;

                // Вычисляем layout для записей
                if (typeDecl.Type is RecordTypeRef recordType)
                {
                    ComputeRecordLayout(typeDecl.Name, recordType);
                }
            }
        }

        Line("(module");
        _indent++;

        // Импорт функций печати из хост-окружения
        Line("(import \"env\" \"print_i32\" (func $print_i32 (param i32)))");
        Line("(import \"env\" \"print_f64\" (func $print_f64 (param f64)))");
        Line("");

        // Линейная память для массивов (1 страница = 64KB)
        Line("(memory 1)");
        Line("");

        // Генерация всех функций
        foreach (var decl in _program.Decls)
        {
            if (decl is RoutineDecl routine && routine.Body != null)
            {
                GenerateRoutine(routine);
                Line("");
            }
        }

        _indent--;
        Line(")");

        return _output.ToString();
    }

    /// <summary>
    /// Генерирует одну функцию.
    /// </summary>
    private void GenerateRoutine(RoutineDecl routine)
    {
        _locals.Clear();
        _localCount = 0;
        _arrayAddresses.Clear();
        _localTypes.Clear();

        // Сигнатура функции
        var sig = new StringBuilder($"(func ${routine.Name}");

        // Параметры
        foreach (var p in routine.Parameters)
        {
            var wasmType = TypeRefToWasm(p.Type);
            sig.Append($" (param ${p.Name} {wasmType})");
            _locals[p.Name] = _localCount++;
        }

        // Тип возврата
        if (routine.ReturnType != null)
        {
            sig.Append($" (result {TypeRefToWasm(routine.ReturnType)})");
        }

        // Экспорт main
        if (routine.Name == "main")
        {
            sig.Append(" (export \"main\")");
        }

        Line(sig.ToString());
        _indent++;

        // Тело функции
        if (routine.Body is BlockBody blockBody)
        {
            // Сбор локальных переменных
            var localDecls = new List<(string name, string type)>();
            CollectLocals(blockBody.Block, localDecls);

            foreach (var (name, type) in localDecls)
            {
                Line($"(local ${name} {type})");
                _locals[name] = _localCount++;
            }

            // Генерация операторов
            GenerateBlock(blockBody.Block);
        }
        else if (routine.Body is ExprBody exprBody)
        {
            GenerateExpr(exprBody.Expr);
        }

        _indent--;
        Line(")");
    }

    /// <summary>
    /// Собирает все локальные переменные из блока (включая вложенные).
    /// </summary>
    private void CollectLocals(Block block, List<(string, string)> locals)
    {
        foreach (var item in block.Items)
        {
            if (item is VarDecl v)
            {
                // Сохраняем тип переменной
                _localTypes[v.Name] = v.Type;

                // Разрешаем именованные типы
                var resolvedType = ResolveType(v.Type);

                // Массивы хранятся в памяти, переменная хранит адрес
                if (resolvedType is ArrayTypeRef arrayType)
                {
                    locals.Add((v.Name, "i32")); // адрес в памяти

                    // Вычисляем размер массива
                    int size = 0;
                    if (arrayType.Size is LiteralInt lit)
                    {
                        size = (int)lit.Value;
                    }

                    // Резервируем память (4 байта на элемент i32)
                    _arrayAddresses[v.Name] = _memoryOffset;
                    _memoryOffset += size * 4;
                }
                // Записи также хранятся в памяти
                else if (resolvedType is RecordTypeRef ||
                         (v.Type is NamedTypeRef named && _recordLayouts.ContainsKey(named.Name)))
                {
                    locals.Add((v.Name, "i32")); // адрес в памяти

                    // Вычисляем размер записи
                    int recordSize = 0;
                    if (v.Type is NamedTypeRef namedType && _recordLayouts.TryGetValue(namedType.Name, out var layout))
                    {
                        recordSize = layout.Sum(f => f.size);
                    }
                    else if (resolvedType is RecordTypeRef recType)
                    {
                        recordSize = recType.Fields.Sum(f => GetTypeSize(f.Type));
                    }

                    // Резервируем память
                    _arrayAddresses[v.Name] = _memoryOffset;
                    _memoryOffset += recordSize;
                }
                else
                {
                    var type = v.Type != null ? TypeRefToWasm(v.Type) : "i32";
                    locals.Add((v.Name, type));
                }
            }
            else if (item is ForStmt forStmt)
            {
                // Итератор цикла - локальная переменная
                locals.Add((forStmt.Iterator, "i32"));
                CollectLocals(forStmt.Body, locals);
            }
            else if (item is WhileStmt whileStmt)
            {
                CollectLocals(whileStmt.Body, locals);
            }
            else if (item is IfStmt ifStmt)
            {
                CollectLocals(ifStmt.Then, locals);
                if (ifStmt.Else != null)
                    CollectLocals(ifStmt.Else, locals);
            }
        }
    }

    /// <summary>
    /// Генерирует блок операторов.
    /// </summary>
    private void GenerateBlock(Block block)
    {
        foreach (var item in block.Items)
        {
            if (item is VarDecl varDecl)
            {
                // Разрешаем именованные типы
                var resolvedType = ResolveType(varDecl.Type);

                // Инициализация массива - сохраняем адрес
                if (resolvedType is ArrayTypeRef && _arrayAddresses.ContainsKey(varDecl.Name))
                {
                    Line($"(i32.const {_arrayAddresses[varDecl.Name]})");
                    Line($"(local.set ${varDecl.Name})");
                }
                // Инициализация записи - сохраняем адрес
                else if ((resolvedType is RecordTypeRef ||
                          (varDecl.Type is NamedTypeRef named && _recordLayouts.ContainsKey(named.Name))) &&
                         _arrayAddresses.ContainsKey(varDecl.Name))
                {
                    Line($"(i32.const {_arrayAddresses[varDecl.Name]})");
                    Line($"(local.set ${varDecl.Name})");
                }
                // Инициализация обычной переменной
                else if (varDecl.Initializer != null)
                {
                    var targetTypeName = GetPrimitiveTypeName(varDecl.Type);
                    if (targetTypeName != null)
                    {
                        GenerateExprWithConversion(varDecl.Initializer, targetTypeName);
                    }
                    else
                    {
                        GenerateExpr(varDecl.Initializer);
                    }
                    Line($"(local.set ${varDecl.Name})");
                }
            }
            else if (item is Stmt stmt)
            {
                GenerateStmt(stmt);
            }
        }
    }

    /// <summary>
    /// Генерирует оператор.
    /// </summary>
    private void GenerateStmt(Stmt stmt)
    {
        switch (stmt)
        {
            // Присваивание: x := expr или a[i] := expr или r.field := expr
            case AssignStmt assign:
                if (assign.Target is IndexExpr indexExpr)
                {
                    // a[i] := expr
                    // Вычисляем адрес: base + (index - 1) * 4
                    if (indexExpr.Receiver is NameExpr arrayName)
                    {
                        Line($"(local.get ${arrayName.Name})"); // базовый адрес
                        GenerateExpr(indexExpr.Index);          // индекс
                        Line("(i32.const 1)");                  // массивы с 1
                        Line("(i32.sub)");                      // index - 1
                        Line("(i32.const 4)");                  // размер элемента
                        Line("(i32.mul)");                      // (index - 1) * 4
                        Line("(i32.add)");                      // base + offset
                        GenerateExpr(assign.Value);             // значение
                        Line("(i32.store)");                    // сохранить в память
                    }
                }
                else if (assign.Target is FieldExpr fieldExpr)
                {
                    // r.field := expr
                    // Вычисляем адрес поля в записи
                    if (fieldExpr.Receiver is NameExpr recordName)
                    {
                        var offset = GetFieldOffset(recordName.Name, fieldExpr.Field);
                        if (offset.HasValue)
                        {
                            Line($"(local.get ${recordName.Name})"); // базовый адрес записи
                            if (offset.Value > 0)
                            {
                                Line($"(i32.const {offset.Value})"); // offset поля
                                Line("(i32.add)");                   // base + offset
                            }
                            GenerateExpr(assign.Value);              // значение
                            Line("(i32.store)");                     // сохранить
                        }
                        else
                        {
                            Line($";; ERROR: Unknown field {recordName.Name}.{fieldExpr.Field}");
                        }
                    }
                }
                else if (assign.Target is NameExpr name)
                {
                    // x := expr
                    // Проверяем, нужна ли конверсия типов
                    if (_localTypes.TryGetValue(name.Name, out var targetType))
                    {
                        var targetTypeName = GetPrimitiveTypeName(targetType);
                        if (targetTypeName != null)
                        {
                            GenerateExprWithConversion(assign.Value, targetTypeName);
                        }
                        else
                        {
                            GenerateExpr(assign.Value);
                        }
                    }
                    else
                    {
                        GenerateExpr(assign.Value);
                    }
                    Line($"(local.set ${name.Name})");
                }
                break;

            // Условный оператор
            case IfStmt ifStmt:
                GenerateExpr(ifStmt.Condition);
                Line("(if");
                _indent++;
                Line("(then");
                _indent++;
                GenerateBlock(ifStmt.Then);
                _indent--;
                Line(")");
                if (ifStmt.Else != null)
                {
                    Line("(else");
                    _indent++;
                    GenerateBlock(ifStmt.Else);
                    _indent--;
                    Line(")");
                }
                _indent--;
                Line(")");
                break;

            // Цикл while
            case WhileStmt whileStmt:
                Line("(block $break");
                _indent++;
                Line("(loop $continue");
                _indent++;
                GenerateExpr(whileStmt.Condition);
                Line("(i32.eqz)");       // Инвертируем условие
                Line("(br_if $break)");  // Выход если false
                GenerateBlock(whileStmt.Body);
                Line("(br $continue)");  // Продолжить цикл
                _indent--;
                Line(")");
                _indent--;
                Line(")");
                break;

            // Цикл for i in start..end
            case ForStmt forStmt:
                if (forStmt.Second != null)
                {
                    // Инициализация итератора
                    GenerateExpr(forStmt.First);
                    Line($"(local.set ${forStmt.Iterator})");

                    Line("(block $break");
                    _indent++;
                    Line("(loop $continue");
                    _indent++;

                    // Проверка: i > end -> выход
                    Line($"(local.get ${forStmt.Iterator})");
                    GenerateExpr(forStmt.Second);
                    Line("(i32.gt_s)");
                    Line("(br_if $break)");

                    // Тело цикла
                    GenerateBlock(forStmt.Body);

                    // Инкремент: i := i + 1
                    Line($"(local.get ${forStmt.Iterator})");
                    Line("(i32.const 1)");
                    Line("(i32.add)");
                    Line($"(local.set ${forStmt.Iterator})");

                    Line("(br $continue)");
                    _indent--;
                    Line(")");
                    _indent--;
                    Line(")");
                }
                break;

            // Возврат из функции
            case ReturnStmt returnStmt:
                if (returnStmt.Value != null)
                {
                    GenerateExpr(returnStmt.Value);
                }
                Line("(return)");
                break;

            // Вывод
            case PrintStmt print:
                foreach (var item in print.Items)
                {
                    GenerateExpr(item);
                    // Определяем тип выражения для выбора правильной функции печати
                    var exprType = InferExprType(item);
                    if (exprType == "real")
                    {
                        Line("(call $print_f64)");
                    }
                    else
                    {
                        Line("(call $print_i32)");
                    }
                }
                break;

            // Вызов процедуры
            case CallStmt call:
                foreach (var arg in call.Args)
                {
                    GenerateExpr(arg);
                }
                Line($"(call ${call.Name})");
                break;

            case EmptyStmt:
                break;
        }
    }

    /// <summary>
    /// Генерирует выражение (кладёт результат на стек).
    /// </summary>
    private void GenerateExpr(Expr expr)
    {
        switch (expr)
        {
            // Целое число
            case LiteralInt lit:
                Line($"(i32.const {lit.Value})");
                break;

            // Вещественное число
            case LiteralReal lit:
                Line($"(f64.const {lit.Value.ToString(CultureInfo.InvariantCulture)})");
                break;

            // Логическое значение (true=1, false=0)
            case LiteralBool lit:
                Line($"(i32.const {(lit.Value ? 1 : 0)})");
                break;

            // Чтение переменной
            case NameExpr name:
                Line($"(local.get ${name.Name})");
                break;

            // Бинарная операция
            case BinaryExpr bin:
                // Определяем тип операндов
                var leftType = InferExprType(bin.Left);
                var rightType = InferExprType(bin.Right);

                // Если хотя бы один операнд real - приводим к real
                var targetOperandType = (leftType == "real" || rightType == "real") ? "real" : "integer";

                // Генерируем левый операнд с конверсией
                if (targetOperandType != null && leftType != null)
                {
                    GenerateExprWithConversion(bin.Left, targetOperandType);
                }
                else
                {
                    GenerateExpr(bin.Left);
                }

                // Генерируем правый операнд с конверсией
                if (targetOperandType != null && rightType != null)
                {
                    GenerateExprWithConversion(bin.Right, targetOperandType);
                }
                else
                {
                    GenerateExpr(bin.Right);
                }

                // Используем соответствующую операцию (i32 или f64)
                Line($"({BinaryOpToWasm(bin.Op, targetOperandType)})");
                break;

            // Унарная операция
            case UnaryExpr unary:
                if (unary.Op == "-")
                {
                    // Отрицание: 0 - x
                    Line("(i32.const 0)");
                    GenerateExpr(unary.Operand);
                    Line("(i32.sub)");
                }
                else if (unary.Op == "not")
                {
                    // Логическое НЕ
                    GenerateExpr(unary.Operand);
                    Line("(i32.eqz)");
                }
                else
                {
                    GenerateExpr(unary.Operand);
                }
                break;

            // Вызов функции
            case CallExpr call:
                foreach (var arg in call.Args)
                {
                    GenerateExpr(arg);
                }
                Line($"(call ${call.Name})");
                break;

            // Индексация массива: a[i]
            case IndexExpr indexExpr:
                if (indexExpr.Receiver is NameExpr arrayName)
                {
                    // Вычисляем адрес: base + (index - 1) * 4
                    Line($"(local.get ${arrayName.Name})"); // базовый адрес
                    GenerateExpr(indexExpr.Index);          // индекс
                    Line("(i32.const 1)");                  // массивы с 1
                    Line("(i32.sub)");                      // index - 1
                    Line("(i32.const 4)");                  // размер элемента
                    Line("(i32.mul)");                      // (index - 1) * 4
                    Line("(i32.add)");                      // base + offset
                    Line("(i32.load)");                     // загрузить из памяти
                }
                break;

            // Доступ к полю записи: r.field
            case FieldExpr fieldExpr:
                if (fieldExpr.Receiver is NameExpr recordName)
                {
                    var offset = GetFieldOffset(recordName.Name, fieldExpr.Field);
                    if (offset.HasValue)
                    {
                        Line($"(local.get ${recordName.Name})"); // базовый адрес записи
                        if (offset.Value > 0)
                        {
                            Line($"(i32.const {offset.Value})"); // offset поля
                            Line("(i32.add)");                   // base + offset
                        }
                        Line("(i32.load)");                      // загрузить значение
                    }
                    else
                    {
                        Line($";; ERROR: Unknown field {recordName.Name}.{fieldExpr.Field}");
                        Line("(i32.const 0)"); // dummy value
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Преобразует оператор в WASM инструкцию.
    /// </summary>
    private string BinaryOpToWasm(string op, string? type = null)
    {
        // Для real используем f64 операции
        if (type == "real")
        {
            return op switch
            {
                "+" => "f64.add",
                "-" => "f64.sub",
                "*" => "f64.mul",
                "/" => "f64.div",
                "<" => "f64.lt",
                "<=" => "f64.le",
                ">" => "f64.gt",
                ">=" => "f64.ge",
                "=" => "f64.eq",
                "/=" => "f64.ne",
                _ => "f64.add"
            };
        }

        // Для integer и boolean используем i32 операции
        return op switch
        {
            "+" => "i32.add",
            "-" => "i32.sub",
            "*" => "i32.mul",
            "/" => "i32.div_s",
            "%" => "i32.rem_s",
            "<" => "i32.lt_s",
            "<=" => "i32.le_s",
            ">" => "i32.gt_s",
            ">=" => "i32.ge_s",
            "=" => "i32.eq",
            "/=" => "i32.ne",
            "and" => "i32.and",
            "or" => "i32.or",
            "xor" => "i32.xor",
            _ => "i32.add"
        };
    }

    /// <summary>
    /// Преобразует тип языка в WASM тип.
    /// integer, boolean -> i32
    /// real -> f64
    /// array -> i32 (адрес в памяти)
    /// </summary>
    private string TypeRefToWasm(TypeRef? type) => type switch
    {
        PrimitiveTypeRef p when p.Name == "real" => "f64",
        PrimitiveTypeRef p when p.Name == "boolean" => "i32",
        ArrayTypeRef => "i32", // массивы передаются как адрес
        _ => "i32"
    };

    /// <summary>
    /// Выводит строку с отступом.
    /// </summary>
    private void Line(string text)
    {
        _output.AppendLine(new string(' ', _indent * 2) + text);
    }

    /// <summary>
    /// Разрешает именованный тип в базовый TypeRef.
    /// </summary>
    private TypeRef? ResolveType(TypeRef? type)
    {
        if (type is NamedTypeRef named && _typeDefinitions.TryGetValue(named.Name, out var resolved))
        {
            return resolved;
        }
        return type;
    }

    /// <summary>
    /// Вычисляет размер типа в байтах.
    /// </summary>
    private int GetTypeSize(TypeRef? type)
    {
        var resolved = ResolveType(type);
        return resolved switch
        {
            PrimitiveTypeRef p when p.Name == "real" => 8,      // f64
            PrimitiveTypeRef p when p.Name == "integer" => 4,   // i32
            PrimitiveTypeRef p when p.Name == "boolean" => 4,   // i32
            ArrayTypeRef => 4,  // адрес
            RecordTypeRef => 4, // адрес
            NamedTypeRef n when _recordLayouts.ContainsKey(n.Name) => 4, // адрес на record
            _ => 4
        };
    }

    /// <summary>
    /// Вычисляет layout записи (смещения полей).
    /// </summary>
    private void ComputeRecordLayout(string typeName, RecordTypeRef recordType)
    {
        if (_recordLayouts.ContainsKey(typeName))
            return;

        var layout = new List<(string name, int offset, int size)>();
        int currentOffset = 0;

        foreach (var field in recordType.Fields)
        {
            int fieldSize = GetTypeSize(field.Type);
            layout.Add((field.Name, currentOffset, fieldSize));
            currentOffset += fieldSize;
        }

        _recordLayouts[typeName] = layout;
    }

    /// <summary>
    /// Находит offset поля в записи по имени переменной и имени поля.
    /// </summary>
    private int? GetFieldOffset(string varName, string fieldName)
    {
        // Получаем тип переменной
        if (!_localTypes.TryGetValue(varName, out var varType))
            return null;

        // Разрешаем именованный тип
        if (varType is NamedTypeRef named)
        {
            // Ищем layout этого типа
            if (_recordLayouts.TryGetValue(named.Name, out var layout))
            {
                var field = layout.FirstOrDefault(f => f.name == fieldName);
                if (field != default)
                    return field.offset;
            }
        }

        return null;
    }

    /// <summary>
    /// Определяет примитивный тип выражения (для простых случаев).
    /// </summary>
    private string? InferExprType(Expr expr)
    {
        return expr switch
        {
            LiteralInt => "integer",
            LiteralReal => "real",
            LiteralBool => "boolean",
            NameExpr name when _localTypes.TryGetValue(name.Name, out var type) => GetPrimitiveTypeName(type),
            BinaryExpr bin => InferBinaryExprType(bin),
            UnaryExpr unary => InferExprType(unary.Operand),
            _ => null
        };
    }

    /// <summary>
    /// Определяет тип бинарного выражения.
    /// </summary>
    private string? InferBinaryExprType(BinaryExpr bin)
    {
        var leftType = InferExprType(bin.Left);
        var rightType = InferExprType(bin.Right);

        // Если хотя бы один операнд real - результат real
        if (leftType == "real" || rightType == "real")
            return "real";

        // Операции сравнения возвращают boolean
        if (bin.Op is "=" or "/=" or "<" or "<=" or ">" or ">=")
            return "boolean";

        // Логические операции возвращают boolean
        if (bin.Op is "and" or "or" or "xor")
            return "boolean";

        // Арифметические операции на integer возвращают integer
        if (leftType == "integer" && rightType == "integer")
            return "integer";

        return null;
    }

    /// <summary>
    /// Получает имя примитивного типа из TypeRef.
    /// </summary>
    private string? GetPrimitiveTypeName(TypeRef? type)
    {
        var resolved = ResolveType(type);
        return resolved switch
        {
            PrimitiveTypeRef p => p.Name,
            _ => null
        };
    }

    /// <summary>
    /// Генерирует выражение с автоматическим приведением к нужному типу.
    /// </summary>
    private void GenerateExprWithConversion(Expr expr, string targetType)
    {
        var sourceType = InferExprType(expr);
        GenerateExpr(expr);

        // Если типы известны и различаются - добавляем конверсию
        if (sourceType != null && targetType != null && sourceType != targetType)
        {
            EmitConversion(sourceType, targetType);
        }
    }

    /// <summary>
    /// Генерирует инструкции для преобразования типов.
    /// Согласно спецификации:
    /// - integer → real (расширение)
    /// - integer → boolean (0 → false, не-0 → true)
    /// - real → integer (сужение с округлением)
    /// - real → boolean (ЗАПРЕЩЕНО)
    /// - boolean → integer (false → 0, true → 1) [тривиально, оба i32]
    /// - boolean → real (false → 0.0, true → 1.0)
    /// </summary>
    private void EmitConversion(string from, string to)
    {
        if (from == to)
            return;

        switch (from, to)
        {
            // integer → real: знаковое преобразование i32 в f64
            case ("integer", "real"):
                Line("(f64.convert_i32_s)");
                break;

            // real → integer: усечение f64 до i32
            case ("real", "integer"):
                Line("(i32.trunc_f64_s)");
                break;

            // integer → boolean: любое не-0 → true (1)
            // Используем i32.eqz дважды: x != 0 → (x == 0) == 0
            case ("integer", "boolean"):
                Line("(i32.const 0)");
                Line("(i32.ne)");
                break;

            // boolean → integer: тривиально, оба i32
            case ("boolean", "integer"):
                // Нет конверсии - оба представлены как i32
                break;

            // boolean → real: false → 0.0, true → 1.0
            case ("boolean", "real"):
                Line("(f64.convert_i32_s)");
                break;

            // real → boolean: ЗАПРЕЩЕНО по спецификации
            case ("real", "boolean"):
                Line(";; ERROR: real → boolean conversion is forbidden");
                Line("(i32.const 0)"); // dummy value
                break;

            default:
                Line($";; WARNING: Unknown conversion {from} → {to}");
                break;
        }
    }
}
