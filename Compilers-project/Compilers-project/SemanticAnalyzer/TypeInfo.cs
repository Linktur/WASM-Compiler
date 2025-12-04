using System.Collections.Generic;

namespace Compilers_project.SemanticAnalyzer;

/// <summary>
/// Базовый класс для представления типов в системе типов.
/// </summary>
public abstract class TypeInfo
{
    public abstract string Name { get; }
    
    /// <summary>
    /// Проверяет, совместим ли тип this с целевым типом target
    /// (можно ли присвоить значение типа this переменной типа target)
    /// </summary>
    public abstract bool IsAssignableTo(TypeInfo target);
    
    public override string ToString() => Name;
}

/// <summary>
/// Примитивный тип (integer, real, boolean)
/// </summary>
public sealed class PrimitiveType : TypeInfo
{
    public static readonly PrimitiveType Integer = new("integer");
    public static readonly PrimitiveType Real = new("real");
    public static readonly PrimitiveType Boolean = new("boolean");
    public static readonly PrimitiveType Void = new("void"); // для процедур без возвращаемого значения
    
    private readonly string _name;
    
    private PrimitiveType(string name) => _name = name;
    
    public override string Name => _name;
    
    public override bool IsAssignableTo(TypeInfo target)
    {
        if (target is PrimitiveType prim)
        {
            // integer -> integer (exact)
            if (this == Integer && prim == Integer) return true;
            // integer -> real (widening)
            if (this == Integer && prim == Real) return true;
            // integer -> boolean (0/1 conversion)
            if (this == Integer && prim == Boolean) return true;
            
            // real -> real (exact)
            if (this == Real && prim == Real) return true;
            // real -> integer (NOT ALLOWED - would cause data loss)
            // if (this == Real && prim == Integer) return false;
            // real -> boolean is ILLEGAL per spec
            
            // boolean -> boolean (exact)
            if (this == Boolean && prim == Boolean) return true;
            // boolean -> integer (true=1, false=0)
            if (this == Boolean && prim == Integer) return true;
            // boolean -> real (true=1.0, false=0.0)
            if (this == Boolean && prim == Real) return true;
        }
        return false;
    }
}

/// <summary>
/// Тип массива
/// </summary>
public sealed class ArrayType : TypeInfo
{
    public TypeInfo ElementType { get; }
    public int? Size { get; } // null для массивов без размера (в параметрах)
    
    public ArrayType(TypeInfo elementType, int? size)
    {
        ElementType = elementType;
        Size = size;
    }
    
    public override string Name => Size.HasValue ? $"array[{Size}] {ElementType.Name}" : $"array[] {ElementType.Name}";
    
    public override bool IsAssignableTo(TypeInfo target)
    {
        // Массивы - ссылочные типы, присваивание копирует ссылку
        // Типы должны точно совпадать
        if (target is ArrayType arrType)
        {
            // Элементы должны иметь тот же тип
            if (!ElementType.Equals(arrType.ElementType))
                return false;
            
            // Размер может не совпадать при параметрах
            return true;
        }
        return false;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is ArrayType other)
        {
            return ElementType.Equals(other.ElementType);
        }
        return false;
    }
    
    public override int GetHashCode() => ElementType.GetHashCode();
}

/// <summary>
/// Тип записи (record)
/// </summary>
public sealed class RecordType : TypeInfo
{
    private readonly string _name;
    public Dictionary<string, TypeInfo> Fields { get; }
    
    public RecordType(string name, Dictionary<string, TypeInfo> fields)
    {
        _name = name;
        Fields = fields;
    }
    
    public override string Name => _name;
    
    public override bool IsAssignableTo(TypeInfo target)
    {
        // Записи - ссылочные типы, присваивание копирует ссылку
        // Проверяем по имени (должны быть из одного объявления)
        return target is RecordType rt && rt.Name == this.Name;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is RecordType other)
        {
            return Name == other.Name;
        }
        return false;
    }
    
    public override int GetHashCode() => Name.GetHashCode();
}

/// <summary>
/// Тип ошибки - используется когда тип не может быть определён
/// </summary>
public sealed class ErrorType : TypeInfo
{
    public static readonly ErrorType Instance = new();
    
    private ErrorType() { }
    
    public override string Name => "<error>";
    
    public override bool IsAssignableTo(TypeInfo target) => true; // позволяет избежать каскада ошибок
}
