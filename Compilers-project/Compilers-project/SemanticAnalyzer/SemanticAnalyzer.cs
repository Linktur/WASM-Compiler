using System.Collections.Generic;
using System.Linq;
using Compilers_project.Lexer;
using Compilers_project.Parser;
using Compilers_project.Parser.AST;

namespace Compilers_project.SemanticAnalyzer;

/// <summary>
/// Семантический анализатор, выполняющий проверку типов, областей видимости и другие проверки
/// </summary>
public sealed class SemanticAnalyzer
{
    private readonly Diagnostics _diagnostics = new();
    private SymbolTable _currentScope;
    private RoutineSymbol? _currentRoutine; // для проверки return
    
    public Diagnostics Diagnostics => _diagnostics;
    
    private SymbolTable? _globalScope;
    
    public SemanticAnalyzer()
    {
        _globalScope = new SymbolTable();
        _currentScope = _globalScope;
        InitializeBuiltins();
    }
    
    /// <summary>
    /// Инициализация встроенных типов
    /// </summary>
    private void InitializeBuiltins()
    {
        _globalScope!.TryDefine(new TypeSymbol("integer", PrimitiveType.Integer));
        _globalScope!.TryDefine(new TypeSymbol("real", PrimitiveType.Real));
        _globalScope!.TryDefine(new TypeSymbol("boolean", PrimitiveType.Boolean));
    }
    
    /// <summary>
    /// Анализ программы
    /// </summary>
    public void Analyze(ProgramNode program)
    {
        // Первый проход: собираем декларации верхнего уровня (типы и функции)
        foreach (var decl in program.Decls)
        {
            if (decl is TypeDecl typeDecl)
            {
                var type = ResolveTypeRef(typeDecl.Type);
                if (!_currentScope.TryDefine(new TypeSymbol(typeDecl.Name, type)))
                {
                    _diagnostics.Error(typeDecl.Span, $"Type '{typeDecl.Name}' is already defined");
                }
            }
            else if (decl is RoutineDecl routineDecl)
            {
                // Регистрируем сигнатуру функции
                var returnType = routineDecl.ReturnType != null 
                    ? ResolveTypeRef(routineDecl.ReturnType) 
                    : PrimitiveType.Void;
                
                var parameters = new List<(string, TypeInfo)>();
                foreach (var param in routineDecl.Parameters)
                {
                    var paramType = ResolveTypeRef(param.Type);
                    parameters.Add((param.Name, paramType));
                }
                
                var routine = new RoutineSymbol(routineDecl.Name, returnType, parameters);
                if (!_currentScope.TryDefine(routine))
                {
                    _diagnostics.Error(routineDecl.Span, $"Routine '{routineDecl.Name}' is already defined");
                }
            }
        }
        
        // Второй проход: анализируем тела функций и глобальные переменные
        foreach (var decl in program.Decls)
        {
            AnalyzeDeclaration(decl);
        }
    }
    
    private void AnalyzeDeclaration(Decl decl)
    {
        switch (decl)
        {
            case VarDecl varDecl:
                AnalyzeVarDecl(varDecl);
                break;
            case TypeDecl:
                // Уже обработано в первом проходе
                break;
            case RoutineDecl routineDecl:
                AnalyzeRoutine(routineDecl);
                break;
        }
    }
    
    private void AnalyzeVarDecl(VarDecl varDecl)
    {
        TypeInfo varType;
        
        if (varDecl.Type != null)
        {
            varType = ResolveTypeRef(varDecl.Type);
            
            if (varDecl.Initializer != null)
            {
                var initType = AnalyzeExpression(varDecl.Initializer);
                if (!initType.IsAssignableTo(varType))
                {
                    _diagnostics.Error(varDecl.Span, 
                        $"Cannot assign '{initType.Name}' to '{varType.Name}'");
                }
            }
        }
        else if (varDecl.Initializer != null)
        {
            // Вывод типа из инициализатора
            varType = AnalyzeExpression(varDecl.Initializer);
        }
        else
        {
            _diagnostics.Error(varDecl.Span, 
                $"Variable '{varDecl.Name}' must have either a type or an initializer");
            varType = ErrorType.Instance;
        }
        
        if (!_currentScope.TryDefine(new VarSymbol(varDecl.Name, varType)))
        {
            _diagnostics.Error(varDecl.Span, $"Variable '{varDecl.Name}' is already defined");
        }
    }
    
    private void AnalyzeRoutine(RoutineDecl routineDecl)
    {
        if (routineDecl.Body == null)
        {
            // Forward declaration - ничего не делаем
            return;
        }
        
        // Ищем символ функции
        var symbol = _currentScope.Lookup(routineDecl.Name) as RoutineSymbol;
        if (symbol == null)
        {
            _diagnostics.Error(routineDecl.Span, $"Internal error: routine '{routineDecl.Name}' not found");
            return;
        }
        
        _currentRoutine = symbol;
        
        // Создаем новую область видимости для тела функции
        _currentScope = _currentScope.CreateChild();
        
        // Добавляем параметры в область видимости
        foreach (var param in routineDecl.Parameters)
        {
            var paramType = ResolveTypeRef(param.Type);
            if (!_currentScope.TryDefine(new VarSymbol(param.Name, paramType)))
            {
                _diagnostics.Error(routineDecl.Span, $"Parameter '{param.Name}' is already defined");
            }
        }
        
        // Анализируем тело функции
        switch (routineDecl.Body)
        {
            case ExprBody exprBody:
                var exprType = AnalyzeExpression(exprBody.Expr);
                if (symbol.ReturnType != PrimitiveType.Void)
                {
                    if (!exprType.IsAssignableTo(symbol.ReturnType))
                    {
                        _diagnostics.Error(routineDecl.Span,
                            $"Cannot return '{exprType.Name}' from function expecting '{symbol.ReturnType.Name}'");
                    }
                }
                break;
            
            case BlockBody blockBody:
                AnalyzeBlock(blockBody.Block);
                break;
        }
        
        // Выходим из области видимости функции
        _currentScope = _globalScope!;
        _currentRoutine = null;
    }
    
    private void AnalyzeBlock(Block block)
    {
        foreach (var item in block.Items)
        {
            if (item is Decl decl)
            {
                AnalyzeDeclaration(decl);
            }
            else if (item is Stmt stmt)
            {
                AnalyzeStatement(stmt);
            }
        }
    }
    
    private void AnalyzeStatement(Stmt stmt)
    {
        switch (stmt)
        {
            case AssignStmt assign:
                AnalyzeAssignment(assign);
                break;
            
            case CallStmt call:
                AnalyzeCallStmt(call);
                break;
            
            case IfStmt ifStmt:
                AnalyzeIfStmt(ifStmt);
                break;
            
            case WhileStmt whileStmt:
                AnalyzeWhileStmt(whileStmt);
                break;
            
            case ForStmt forStmt:
                AnalyzeForStmt(forStmt);
                break;
            
            case ReturnStmt returnStmt:
                AnalyzeReturnStmt(returnStmt);
                break;
            
            case PrintStmt printStmt:
                AnalyzePrintStmt(printStmt);
                break;
            
            case EmptyStmt:
                // Ничего не делаем
                break;
        }
    }
    
    private void AnalyzeAssignment(AssignStmt assign)
    {
        var targetType = AnalyzeExpression(assign.Target);
        var valueType = AnalyzeExpression(assign.Value);
        
        // Проверяем, что target - это LValue
        if (!IsLValue(assign.Target))
        {
            _diagnostics.Error(assign.Span, "Left side of assignment must be a variable, field or array element");
        }
        
        if (!valueType.IsAssignableTo(targetType))
        {
            _diagnostics.Error(assign.Span, 
                $"Cannot assign '{valueType.Name}' to '{targetType.Name}'");
        }
    }
    
    private void AnalyzeCallStmt(CallStmt call)
    {
        var symbol = _currentScope.Lookup(call.Name);
        
        if (symbol is not RoutineSymbol routine)
        {
            _diagnostics.Error(call.Span, $"'{call.Name}' is not a routine");
            return;
        }
        
        if (call.Args.Count != routine.Parameters.Count)
        {
            _diagnostics.Error(call.Span, 
                $"'{call.Name}' expects {routine.Parameters.Count} arguments but got {call.Args.Count}");
            return;
        }
        
        for (int i = 0; i < call.Args.Count; i++)
        {
            var argType = AnalyzeExpression(call.Args[i]);
            var paramType = routine.Parameters[i].Type;
            
            if (!argType.IsAssignableTo(paramType))
            {
                _diagnostics.Error(call.Span,
                    $"Argument {i + 1} type '{argType.Name}' is not assignable to parameter type '{paramType.Name}'");
            }
        }
    }
    
    private void AnalyzeIfStmt(IfStmt ifStmt)
    {
        var condType = AnalyzeExpression(ifStmt.Condition);
        
        if (!condType.IsAssignableTo(PrimitiveType.Boolean))
        {
            _diagnostics.Error(ifStmt.Span, 
                $"Condition must be of type 'boolean', got '{condType.Name}'");
        }
        
        var savedScope = _currentScope;
        _currentScope = _currentScope.CreateChild();
        AnalyzeBlock(ifStmt.Then);
        _currentScope = savedScope;
        
        if (ifStmt.Else != null)
        {
            _currentScope = _currentScope.CreateChild();
            AnalyzeBlock(ifStmt.Else);
            _currentScope = savedScope;
        }
    }
    
    private void AnalyzeWhileStmt(WhileStmt whileStmt)
    {
        var condType = AnalyzeExpression(whileStmt.Condition);
        
        if (!condType.IsAssignableTo(PrimitiveType.Boolean))
        {
            _diagnostics.Error(whileStmt.Span,
                $"Loop condition must be of type 'boolean', got '{condType.Name}'");
        }
        
        var savedScope = _currentScope;
        _currentScope = _currentScope.CreateChild();
        AnalyzeBlock(whileStmt.Body);
        _currentScope = savedScope;
    }
    
    private void AnalyzeForStmt(ForStmt forStmt)
    {
        var savedScope = _currentScope;
        _currentScope = _currentScope.CreateChild();
        
        // Переменная цикла - integer
        _currentScope.TryDefine(new VarSymbol(forStmt.Iterator, PrimitiveType.Integer));
        
        var firstType = AnalyzeExpression(forStmt.First);
        
        if (forStmt.Second != null)
        {
            // for i in expr1 .. expr2
            var secondType = AnalyzeExpression(forStmt.Second);
            
            if (!firstType.IsAssignableTo(PrimitiveType.Integer))
            {
                _diagnostics.Error(forStmt.Span,
                    $"Range start must be 'integer', got '{firstType.Name}'");
            }
            
            if (!secondType.IsAssignableTo(PrimitiveType.Integer))
            {
                _diagnostics.Error(forStmt.Span,
                    $"Range end must be 'integer', got '{secondType.Name}'");
            }
        }
        else
        {
            // for i in array - итерация по массиву
            if (firstType is not ArrayType)
            {
                _diagnostics.Error(forStmt.Span,
                    $"For-in loop expects an array, got '{firstType.Name}'");
            }
        }
        
        AnalyzeBlock(forStmt.Body);
        _currentScope = savedScope;
    }
    
    private void AnalyzeReturnStmt(ReturnStmt returnStmt)
    {
        if (_currentRoutine == null)
        {
            _diagnostics.Error(returnStmt.Span, "Return statement outside of routine");
            return;
        }
        
        if (returnStmt.Value != null)
        {
            var valueType = AnalyzeExpression(returnStmt.Value);
            
            if (_currentRoutine.ReturnType == PrimitiveType.Void)
            {
                _diagnostics.Error(returnStmt.Span, 
                    "Cannot return a value from a void routine");
            }
            else if (!valueType.IsAssignableTo(_currentRoutine.ReturnType))
            {
                _diagnostics.Error(returnStmt.Span,
                    $"Cannot return '{valueType.Name}' from routine expecting '{_currentRoutine.ReturnType.Name}'");
            }
        }
        else if (_currentRoutine.ReturnType != PrimitiveType.Void)
        {
            _diagnostics.Error(returnStmt.Span,
                $"Must return a value of type '{_currentRoutine.ReturnType.Name}'");
        }
    }
    
    private void AnalyzePrintStmt(PrintStmt printStmt)
    {
        foreach (var item in printStmt.Items)
        {
            AnalyzeExpression(item);
        }
    }
    
    private TypeInfo AnalyzeExpression(Expr expr)
    {
        return expr switch
        {
            LiteralInt => PrimitiveType.Integer,
            LiteralReal => PrimitiveType.Real,
            LiteralBool => PrimitiveType.Boolean,
            NameExpr name => AnalyzeNameExpr(name),
            FieldExpr field => AnalyzeFieldExpr(field),
            IndexExpr index => AnalyzeIndexExpr(index),
            CallExpr call => AnalyzeCallExpr(call),
            UnaryExpr unary => AnalyzeUnaryExpr(unary),
            BinaryExpr binary => AnalyzeBinaryExpr(binary),
            _ => ErrorType.Instance
        };
    }
    
    private TypeInfo AnalyzeNameExpr(NameExpr name)
    {
        var symbol = _currentScope.Lookup(name.Name);
        
        if (symbol == null)
        {
            _diagnostics.Error(name.Span, $"Undeclared identifier '{name.Name}'");
            return ErrorType.Instance;
        }
        
        if (symbol is VarSymbol varSym)
        {
            return varSym.Type;
        }
        
        _diagnostics.Error(name.Span, $"'{name.Name}' is not a variable");
        return ErrorType.Instance;
    }
    
    private TypeInfo AnalyzeFieldExpr(FieldExpr field)
    {
        var receiverType = AnalyzeExpression(field.Receiver);
        
        if (receiverType is RecordType record)
        {
            if (record.Fields.TryGetValue(field.Field, out var fieldType))
            {
                return fieldType;
            }
            
            _diagnostics.Error(field.Span, 
                $"Record type '{record.Name}' does not have field '{field.Field}'");
            return ErrorType.Instance;
        }
        
        _diagnostics.Error(field.Span, 
            $"Cannot access field of non-record type '{receiverType.Name}'");
        return ErrorType.Instance;
    }
    
    private TypeInfo AnalyzeIndexExpr(IndexExpr index)
    {
        var receiverType = AnalyzeExpression(index.Receiver);
        var indexType = AnalyzeExpression(index.Index);
        
        if (receiverType is ArrayType array)
        {
            if (!indexType.IsAssignableTo(PrimitiveType.Integer))
            {
                _diagnostics.Error(index.Span, 
                    $"Array index must be 'integer', got '{indexType.Name}'");
            }
            
            return array.ElementType;
        }
        
        _diagnostics.Error(index.Span, 
            $"Cannot index non-array type '{receiverType.Name}'");
        return ErrorType.Instance;
    }
    
    private TypeInfo AnalyzeCallExpr(CallExpr call)
    {
        var symbol = _currentScope.Lookup(call.Name);
        
        if (symbol is not RoutineSymbol routine)
        {
            _diagnostics.Error(call.Span, $"'{call.Name}' is not a routine");
            return ErrorType.Instance;
        }
        
        if (call.Args.Count != routine.Parameters.Count)
        {
            _diagnostics.Error(call.Span,
                $"'{call.Name}' expects {routine.Parameters.Count} arguments but got {call.Args.Count}");
            return ErrorType.Instance;
        }
        
        for (int i = 0; i < call.Args.Count; i++)
        {
            var argType = AnalyzeExpression(call.Args[i]);
            var paramType = routine.Parameters[i].Type;
            
            if (!argType.IsAssignableTo(paramType))
            {
                _diagnostics.Error(call.Span,
                    $"Argument {i + 1} type '{argType.Name}' is not assignable to parameter type '{paramType.Name}'");
            }
        }
        
        return routine.ReturnType;
    }
    
    private TypeInfo AnalyzeUnaryExpr(UnaryExpr unary)
    {
        var operandType = AnalyzeExpression(unary.Operand);
        
        return unary.Op switch
        {
            "+" or "-" => operandType.IsAssignableTo(PrimitiveType.Integer) || operandType.IsAssignableTo(PrimitiveType.Real)
                ? operandType
                : ReportInvalidUnaryOp(unary, operandType),
            
            "not" => operandType.IsAssignableTo(PrimitiveType.Boolean)
                ? PrimitiveType.Boolean
                : ReportInvalidUnaryOp(unary, operandType),
            
            _ => ReportInvalidUnaryOp(unary, operandType)
        };
    }
    
    private TypeInfo ReportInvalidUnaryOp(UnaryExpr unary, TypeInfo operandType)
    {
        _diagnostics.Error(unary.Span, 
            $"Operator '{unary.Op}' cannot be applied to type '{operandType.Name}'");
        return ErrorType.Instance;
    }
    
    private TypeInfo AnalyzeBinaryExpr(BinaryExpr binary)
    {
        var leftType = AnalyzeExpression(binary.Left);
        var rightType = AnalyzeExpression(binary.Right);
        
        // Арифметические операторы: + - * / %
        if (binary.Op is "+" or "-" or "*" or "/" or "%")
        {
            if (leftType is ErrorType || rightType is ErrorType)
                return ErrorType.Instance;
            
            // Оба integer -> integer
            if (leftType == PrimitiveType.Integer && rightType == PrimitiveType.Integer)
                return PrimitiveType.Integer;
            
            // Один или оба real -> real
            if ((leftType == PrimitiveType.Integer || leftType == PrimitiveType.Real) &&
                (rightType == PrimitiveType.Integer || rightType == PrimitiveType.Real))
                return PrimitiveType.Real;
            
            _diagnostics.Error(binary.Span,
                $"Operator '{binary.Op}' cannot be applied to types '{leftType.Name}' and '{rightType.Name}'");
            return ErrorType.Instance;
        }
        
        // Операторы сравнения: < <= > >= = /=
        if (binary.Op is "<" or "<=" or ">" or ">=" or "=" or "/=")
        {
            if (leftType is ErrorType || rightType is ErrorType)
                return ErrorType.Instance;
            
            // Можно сравнивать integer и real между собой
            if ((leftType == PrimitiveType.Integer || leftType == PrimitiveType.Real) &&
                (rightType == PrimitiveType.Integer || rightType == PrimitiveType.Real))
                return PrimitiveType.Boolean;
            
            // Можно сравнивать boolean с boolean
            if (leftType == PrimitiveType.Boolean && rightType == PrimitiveType.Boolean)
                return PrimitiveType.Boolean;
            
            _diagnostics.Error(binary.Span,
                $"Operator '{binary.Op}' cannot be applied to types '{leftType.Name}' and '{rightType.Name}'");
            return ErrorType.Instance;
        }
        
        // Логические операторы: and or xor
        if (binary.Op is "and" or "or" or "xor")
        {
            if (!leftType.IsAssignableTo(PrimitiveType.Boolean))
            {
                _diagnostics.Error(binary.Span,
                    $"Left operand of '{binary.Op}' must be 'boolean', got '{leftType.Name}'");
            }
            
            if (!rightType.IsAssignableTo(PrimitiveType.Boolean))
            {
                _diagnostics.Error(binary.Span,
                    $"Right operand of '{binary.Op}' must be 'boolean', got '{rightType.Name}'");
            }
            
            return PrimitiveType.Boolean;
        }
        
        _diagnostics.Error(binary.Span, $"Unknown operator '{binary.Op}'");
        return ErrorType.Instance;
    }
    
    private TypeInfo ResolveTypeRef(TypeRef typeRef)
    {
        return typeRef switch
        {
            PrimitiveTypeRef prim => ResolvePrimitiveType(prim),
            NamedTypeRef named => ResolveNamedType(named),
            RecordTypeRef record => ResolveRecordType(record),
            ArrayTypeRef array => ResolveArrayType(array),
            _ => ErrorType.Instance
        };
    }
    
    private TypeInfo ResolvePrimitiveType(PrimitiveTypeRef prim)
    {
        return prim.Name switch
        {
            "integer" => PrimitiveType.Integer,
            "real" => PrimitiveType.Real,
            "boolean" => PrimitiveType.Boolean,
            _ => ErrorType.Instance
        };
    }
    
    private TypeInfo ResolveNamedType(NamedTypeRef named)
    {
        var symbol = _currentScope.Lookup(named.Name);
        
        if (symbol is TypeSymbol typeSym)
        {
            return typeSym.Type;
        }
        
        _diagnostics.Error(named.Span, $"Undefined type '{named.Name}'");
        return ErrorType.Instance;
    }
    
    private TypeInfo ResolveRecordType(RecordTypeRef record)
    {
        var fields = new Dictionary<string, TypeInfo>();
        
        foreach (var field in record.Fields)
        {
            if (field.Type == null)
            {
                _diagnostics.Error(field.Span, "Record field must have a type");
                continue;
            }
            
            var fieldType = ResolveTypeRef(field.Type);
            
            if (fields.ContainsKey(field.Name))
            {
                _diagnostics.Error(field.Span, $"Duplicate field '{field.Name}' in record");
            }
            else
            {
                fields[field.Name] = fieldType;
            }
        }
        
        return new RecordType("<anonymous>", fields);
    }
    
    private TypeInfo ResolveArrayType(ArrayTypeRef array)
    {
        var elementType = ResolveTypeRef(array.Element);
        
        int? size = null;
        if (array.Size != null)
        {
            // Вычисляем константное выражение для размера
            if (array.Size is LiteralInt literal)
            {
                size = (int)literal.Value;
            }
            else
            {
                _diagnostics.Error(array.Span, "Array size must be a constant integer expression");
            }
        }
        
        return new ArrayType(elementType, size);
    }
    
    private bool IsLValue(Expr expr)
    {
        return expr is NameExpr or FieldExpr or IndexExpr;
    }
}
