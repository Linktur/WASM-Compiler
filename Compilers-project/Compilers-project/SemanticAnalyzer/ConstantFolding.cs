using System;
using System.Collections.Generic;
using System.Linq;
using Compilers_project.Lexer;
using Compilers_project.Parser.AST;

namespace Compilers_project.SemanticAnalyzer;

/// <summary>
/// Оптимизатор для выполнения сворачивания констант (constant folding).
/// Вычисляет выражения с константными значениями во время компиляции.
/// </summary>
public static class ConstantFolding
{
    /// <summary>
    /// Пытается свернуть выражение с константами.
    /// </summary>
    /// <param name="expr">Исходное выражение</param>
    /// <returns>Свернутое выражение или исходное, если сворачивание невозможно</returns>
    public static Expr TryFold(Expr expr)
    {
        return expr switch
        {
            LiteralInt or LiteralReal or LiteralBool => expr, // Уже константа
            UnaryExpr unary => TryFoldUnary(unary),
            BinaryExpr binary => TryFoldBinary(binary),
            _ => expr
        };
    }

    /// <summary>
    /// Пытается свернуть унарное выражение.
    /// </summary>
    private static Expr TryFoldUnary(UnaryExpr unary)
    {
        // Сначала сворачиваем операнд
        var foldedOperand = TryFold(unary.Operand);

        // Если операнд не изменился, проверяем на константу
        if (foldedOperand is LiteralInt intLit)
        {
            return unary.Op switch
            {
                "+" => intLit,
                "-" => new LiteralInt(unary.Span, -intLit.Value),
                _ => unary
            };
        }

        if (foldedOperand is LiteralReal realLit)
        {
            return unary.Op switch
            {
                "+" => realLit,
                "-" => new LiteralReal(unary.Span, -realLit.Value),
                _ => unary
            };
        }

        if (foldedOperand is LiteralBool boolLit)
        {
            return unary.Op switch
            {
                "not" => new LiteralBool(unary.Span, !boolLit.Value),
                _ => unary
            };
        }

        // Если операнд изменился, но не стал константой, возвращаем новое унарное выражение
        if (foldedOperand != unary.Operand)
        {
            return new UnaryExpr(unary.Span, unary.Op, foldedOperand);
        }

        return unary;
    }

    /// <summary>
    /// Пытается свернуть бинарное выражение.
    /// </summary>
    private static Expr TryFoldBinary(BinaryExpr binary)
    {
        // Сначала сворачиваем операнды
        var foldedLeft = TryFold(binary.Left);
        var foldedRight = TryFold(binary.Right);

        // Если оба операнда - целочисленные константы
        if (foldedLeft is LiteralInt leftInt && foldedRight is LiteralInt rightInt)
        {
            return binary.Op switch
            {
                "+" => new LiteralInt(binary.Span, leftInt.Value + rightInt.Value),
                "-" => new LiteralInt(binary.Span, leftInt.Value - rightInt.Value),
                "*" => new LiteralInt(binary.Span, leftInt.Value * rightInt.Value),

                "/" => rightInt.Value != 0
                    ? new LiteralInt(binary.Span, leftInt.Value / rightInt.Value)
                    : binary, // Деление на ноль оставляем как есть

                "%" => rightInt.Value != 0
                    ? new LiteralInt(binary.Span, leftInt.Value % rightInt.Value)
                    : binary, // Деление на ноль оставляем как есть

                "<" => new LiteralBool(binary.Span, leftInt.Value < rightInt.Value),
                "<=" => new LiteralBool(binary.Span, leftInt.Value <= rightInt.Value),
                ">" => new LiteralBool(binary.Span, leftInt.Value > rightInt.Value),
                ">=" => new LiteralBool(binary.Span, leftInt.Value >= rightInt.Value),
                "=" => new LiteralBool(binary.Span, leftInt.Value == rightInt.Value),
                "/=" => new LiteralBool(binary.Span, leftInt.Value != rightInt.Value),

                _ => binary
            };
        }

        // Если оба операнда - вещественные константы
        if (foldedLeft is LiteralReal leftReal && foldedRight is LiteralReal rightReal)
        {
            return binary.Op switch
            {
                "+" => new LiteralReal(binary.Span, leftReal.Value + rightReal.Value),
                "-" => new LiteralReal(binary.Span, leftReal.Value - rightReal.Value),
                "*" => new LiteralReal(binary.Span, leftReal.Value * rightReal.Value),

                "/" => rightReal.Value != 0
                    ? new LiteralReal(binary.Span, leftReal.Value / rightReal.Value)
                    : binary, // Деление на ноль оставляем как есть

                "%" => rightReal.Value != 0
                    ? new LiteralReal(binary.Span, leftReal.Value % rightReal.Value)
                    : binary, // Деление на ноль оставляем как есть

                "<" => new LiteralBool(binary.Span, leftReal.Value < rightReal.Value),
                "<=" => new LiteralBool(binary.Span, leftReal.Value <= rightReal.Value),
                ">" => new LiteralBool(binary.Span, leftReal.Value > rightReal.Value),
                ">=" => new LiteralBool(binary.Span, leftReal.Value >= rightReal.Value),
                "=" => new LiteralBool(binary.Span, Math.Abs(leftReal.Value - rightReal.Value) < 1e-10),
                "/=" => new LiteralBool(binary.Span, Math.Abs(leftReal.Value - rightReal.Value) >= 1e-10),

                _ => binary
            };
        }

        // Смешанные арифметические операции (int + real, etc.)
        if (IsArithmeticMixed(binary.Op, foldedLeft, foldedRight))
        {
            return TryFoldMixedArithmetic(binary, foldedLeft, foldedRight);
        }

        // Если оба операнда - булевы константы
        if (foldedLeft is LiteralBool leftBool && foldedRight is LiteralBool rightBool)
        {
            return binary.Op switch
            {
                "and" => new LiteralBool(binary.Span, leftBool.Value && rightBool.Value),
                "or" => new LiteralBool(binary.Span, leftBool.Value || rightBool.Value),
                "xor" => new LiteralBool(binary.Span, leftBool.Value ^ rightBool.Value),
                "=" => new LiteralBool(binary.Span, leftBool.Value == rightBool.Value),
                "/=" => new LiteralBool(binary.Span, leftBool.Value != rightBool.Value),

                _ => binary
            };
        }

        // Оптимизации коротких логических операций
        if (IsLogicalOperator(binary.Op) && foldedLeft is LiteralBool leftConst)
        {
            return OptimizeShortCircuit(binary, leftConst.Value);
        }

        // Если операнды изменились, но результат не константа, возвращаем новое выражение
        if (foldedLeft != binary.Left || foldedRight != binary.Right)
        {
            return new BinaryExpr(binary.Span, binary.Op, foldedLeft, foldedRight);
        }

        return binary;
    }

    /// <summary>
    /// Проверяет, является ли оператор арифметическим и требует смешанной типизации.
    /// </summary>
    private static bool IsArithmeticMixed(string op, Expr left, Expr right)
    {
        if (op is not ("+" or "-" or "*" or "/" or "%"))
            return false;

        bool leftIsNum = left is LiteralInt or LiteralReal;
        bool rightIsNum = right is LiteralInt or LiteralReal;

        return leftIsNum && rightIsNum;
    }

    /// <summary>
    /// Выполняет сворачивание смешанных арифметических операций.
    /// </summary>
    private static Expr TryFoldMixedArithmetic(BinaryExpr binary, Expr left, Expr right)
    {
        double leftValue = left switch
        {
            LiteralInt li => li.Value,
            LiteralReal lr => lr.Value,
            _ => throw new ArgumentException("Expected numeric literal")
        };

        double rightValue = right switch
        {
            LiteralInt ri => ri.Value,
            LiteralReal rr => rr.Value,
            _ => throw new ArgumentException("Expected numeric literal")
        };

        var result = binary.Op switch
        {
            "+" => leftValue + rightValue,
            "-" => leftValue - rightValue,
            "*" => leftValue * rightValue,

            "/" => rightValue != 0 ? leftValue / rightValue : double.NaN,
            "%" => rightValue != 0 ? leftValue % rightValue : double.NaN,

            _ => double.NaN
        };

        if (double.IsNaN(result))
            return binary;

        // Если оба операнда были целыми и результат целый, возвращаем целое
        if (left is LiteralInt && right is LiteralInt &&
            binary.Op is not ("/" or "%") && result == Math.Truncate(result))
        {
            return new LiteralInt(binary.Span, (long)result);
        }

        return new LiteralReal(binary.Span, result);
    }

    /// <summary>
    /// Проверяет, является ли оператор логическим.
    /// </summary>
    private static bool IsLogicalOperator(string op)
    {
        return op is "and" or "or";
    }

    /// <summary>
    /// Оптимизация коротких логических операций (short-circuit).
    /// </summary>
    private static Expr OptimizeShortCircuit(BinaryExpr binary, bool leftValue)
    {
        return binary.Op switch
        {
            "and" when !leftValue => new LiteralBool(binary.Span, false), // false and X = false
            "or" when leftValue => new LiteralBool(binary.Span, true),   // true or X = true
            _ => binary
        };
    }

    /// <summary>
    /// Проверяет, является ли выражение константным.
    /// </summary>
    public static bool IsConstant(Expr expr)
    {
        return expr switch
        {
            LiteralInt or LiteralReal or LiteralBool => true,
            UnaryExpr unary => IsConstant(unary.Operand),
            BinaryExpr binary => IsConstant(binary.Left) && IsConstant(binary.Right),
            _ => false
        };
    }

    /// <summary>
    /// Проверяет, является ли выражение целочисленной константой.
    /// </summary>
    public static bool IsIntegerConstant(Expr expr)
    {
        var folded = TryFold(expr);
        return folded is LiteralInt;
    }

    /// <summary>
    /// Получает целочисленное значение константного выражения.
    /// </summary>
    public static long? GetIntegerConstantValue(Expr expr)
    {
        var folded = TryFold(expr);
        return folded is LiteralInt lit ? lit.Value : null;
    }
}