using System.Collections.Generic;
using Compilers_project.Lexer;

namespace Compilers_project.Parser.AST;

public abstract class TypeRef : Node
{
    protected TypeRef(Span s) : base(s) { }
}

public sealed class PrimitiveTypeRef : TypeRef
{
    public string Name { get; }
    public PrimitiveTypeRef(Span s, string name) : base(s) => Name = name;
}

public sealed class NamedTypeRef : TypeRef
{
    public string Name { get; }
    public NamedTypeRef(Span s, string name) : base(s) => Name = name;
}

public sealed class RecordTypeRef : TypeRef
{
    public IReadOnlyList<VarDecl> Fields { get; }
    public RecordTypeRef(Span s, IReadOnlyList<VarDecl> fields) : base(s) => Fields = fields;
}

public sealed class ArrayTypeRef : TypeRef
{
    public TypeRef Element { get; }
    public Expr? Size { get; } // может отсутствовать (например, в параметрах)
    public ArrayTypeRef(Span s, TypeRef elem, Expr? size) : base(s) => (Element, Size) = (elem, size);
}