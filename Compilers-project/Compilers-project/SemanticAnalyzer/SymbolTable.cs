using System.Collections.Generic;

namespace Compilers_project.SemanticAnalyzer;

/// <summary>
/// Базовый класс для всех символов (переменные, типы, функции)
/// </summary>
public abstract class Symbol
{
    public string Name { get; }
    
    protected Symbol(string name) => Name = name;
}

/// <summary>
/// Символ переменной
/// </summary>
public sealed class VarSymbol : Symbol
{
    public TypeInfo Type { get; }
    
    public VarSymbol(string name, TypeInfo type) : base(name)
        => Type = type;
}

/// <summary>
/// Символ типа
/// </summary>
public sealed class TypeSymbol : Symbol
{
    public TypeInfo Type { get; }
    
    public TypeSymbol(string name, TypeInfo type) : base(name)
        => Type = type;
}

/// <summary>
/// Символ функции/процедуры
/// </summary>
public sealed class RoutineSymbol : Symbol
{
    public TypeInfo ReturnType { get; }
    public List<(string Name, TypeInfo Type)> Parameters { get; }
    
    public RoutineSymbol(string name, TypeInfo returnType, List<(string, TypeInfo)> parameters) : base(name)
    {
        ReturnType = returnType;
        Parameters = parameters;
    }
}

/// <summary>
/// Таблица символов с поддержкой вложенных областей видимости
/// </summary>
public sealed class SymbolTable
{
    private readonly SymbolTable? _parent;
    private readonly Dictionary<string, Symbol> _symbols = new();
    
    public SymbolTable(SymbolTable? parent = null)
    {
        _parent = parent;
    }
    
    /// <summary>
    /// Добавить символ в текущую область видимости
    /// </summary>
    public bool TryDefine(Symbol symbol)
    {
        if (_symbols.ContainsKey(symbol.Name))
            return false;
        
        _symbols[symbol.Name] = symbol;
        return true;
    }
    
    /// <summary>
    /// Найти символ в текущей или родительских областях видимости
    /// </summary>
    public Symbol? Lookup(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
            return symbol;
        
        return _parent?.Lookup(name);
    }
    
    /// <summary>
    /// Проверить, есть ли символ только в текущей области видимости (без учета родительских)
    /// </summary>
    public bool ExistsInCurrentScope(string name)
    {
        return _symbols.ContainsKey(name);
    }
    
    /// <summary>
    /// Создать вложенную область видимости
    /// </summary>
    public SymbolTable CreateChild()
    {
        return new SymbolTable(this);
    }
}
