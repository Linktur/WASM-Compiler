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

    public SimpleWasmGenerator(ProgramNode program)
    {
        _program = program;
    }

    /// <summary>
    /// Генератор WAT из AST
    /// </summary>
    public string Generate()
    {
        Line("(module");
        _indent++;

        // функции печати из хост-окружения
        Line("(import \"env\" \"print_i32\" (func $print_i32 (param i32)))");
        Line("(import \"env\" \"print_f64\" (func $print_f64 (param f64)))");
        Line("");

        // генерация всех функций
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

        if (routine.Name == "main")
        {
            sig.Append(" (export \"main\")");
        }

        Line(sig.ToString());
        _indent++;

        // Тело
        if (routine.Body is BlockBody blockBody)
        {
            // локальные переменных
            var localDecls = new List<(string name, string type)>();
            CollectLocals(blockBody.Block, localDecls);

            foreach (var (name, type) in localDecls)
            {
                Line($"(local ${name} {type})");
                _locals[name] = _localCount++;
            }

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
                var type = v.Type != null ? TypeRefToWasm(v.Type) : "i32";
                locals.Add((v.Name, type));
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
                // Инициализация переменной
                if (varDecl.Initializer != null)
                {
                    GenerateExpr(varDecl.Initializer);
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
            case AssignStmt assign:
                GenerateExpr(assign.Value);
                if (assign.Target is NameExpr name)
                {
                    Line($"(local.set ${name.Name})");
                }
                break;

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
                    // f64 для вещественных, i32 для остальных
                    if (item is LiteralReal)
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
                GenerateExpr(bin.Left);
                GenerateExpr(bin.Right);
                Line($"({BinaryOpToWasm(bin.Op)})");
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
        }
    }

    /// <summary>
    /// Преобразует оператор в WASM инструкцию.
    /// </summary>
    private string BinaryOpToWasm(string op) => op switch
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

    /// <summary>
    /// Преобразует тип языка в WASM тип.
    /// integer, boolean -> i32
    /// real -> f64
    /// </summary>
    private string TypeRefToWasm(TypeRef? type) => type switch
    {
        PrimitiveTypeRef p when p.Name == "real" => "f64",
        PrimitiveTypeRef p when p.Name == "boolean" => "i32",
        _ => "i32"
    };

    /// <summary>
    /// Выводит строку с отступом.
    /// </summary>
    private void Line(string text)
    {
        _output.AppendLine(new string(' ', _indent * 2) + text);
    }
}
