using System;
using System.Collections.Generic;
using System.Text.Json;
using Compilers_project.Parser.AST;

namespace Compilers_project.AstVisualizer;

/// <summary>
/// Экспортирует AST в JSON для визуализации
/// </summary>
public class AstJsonExporter
{
    public string Export(ProgramNode program)
    {
        var root = new Dictionary<string, object>
        {
            ["type"] = "Program",
            ["declarations"] = ConvertDecls(program.Decls)
        };

        return JsonSerializer.Serialize(root, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    private List<Dictionary<string, object>> ConvertDecls(IReadOnlyList<Decl> decls)
    {
        var result = new List<Dictionary<string, object>>();
        foreach (var decl in decls)
        {
            result.Add(ConvertDecl(decl));
        }
        return result;
    }

    private Dictionary<string, object> ConvertDecl(Decl decl)
    {
        return decl switch
        {
            VarDecl v => new Dictionary<string, object>
            {
                ["type"] = "VarDecl",
                ["name"] = v.Name,
                ["varType"] = v.Type != null ? ConvertTypeRef(v.Type) : null!,
                ["initializer"] = v.Initializer != null ? ConvertExpr(v.Initializer) : null!
            },

            TypeDecl t => new Dictionary<string, object>
            {
                ["type"] = "TypeDecl",
                ["name"] = t.Name,
                ["typeRef"] = ConvertTypeRef(t.Type)
            },

            RoutineDecl r => new Dictionary<string, object>
            {
                ["type"] = "RoutineDecl",
                ["name"] = r.Name,
                ["parameters"] = ConvertParams(r.Parameters),
                ["returnType"] = r.ReturnType != null ? ConvertTypeRef(r.ReturnType) : null!,
                ["body"] = r.Body != null ? ConvertRoutineBody(r.Body) : null!
            },

            _ => new Dictionary<string, object> { ["type"] = "Unknown" }
        };
    }

    private List<Dictionary<string, object>> ConvertParams(IReadOnlyList<Param> parameters)
    {
        var result = new List<Dictionary<string, object>>();
        foreach (var p in parameters)
        {
            result.Add(new Dictionary<string, object>
            {
                ["name"] = p.Name,
                ["type"] = ConvertTypeRef(p.Type)
            });
        }
        return result;
    }

    private Dictionary<string, object> ConvertTypeRef(TypeRef type)
    {
        return type switch
        {
            PrimitiveTypeRef p => new Dictionary<string, object>
            {
                ["kind"] = "Primitive",
                ["name"] = p.Name
            },

            NamedTypeRef n => new Dictionary<string, object>
            {
                ["kind"] = "Named",
                ["name"] = n.Name
            },

            RecordTypeRef r => new Dictionary<string, object>
            {
                ["kind"] = "Record",
                ["fields"] = ConvertDecls(r.Fields)
            },

            ArrayTypeRef a => new Dictionary<string, object>
            {
                ["kind"] = "Array",
                ["size"] = a.Size != null ? ConvertExpr(a.Size) : null!,
                ["elementType"] = ConvertTypeRef(a.Element)
            },

            _ => new Dictionary<string, object> { ["kind"] = "Unknown" }
        };
    }

    private Dictionary<string, object> ConvertRoutineBody(RoutineBody body)
    {
        return body switch
        {
            ExprBody e => new Dictionary<string, object>
            {
                ["kind"] = "Expr",
                ["expr"] = ConvertExpr(e.Expr)
            },

            BlockBody b => new Dictionary<string, object>
            {
                ["kind"] = "Block",
                ["items"] = ConvertBlockItems(b.Block.Items)
            },

            _ => new Dictionary<string, object> { ["kind"] = "Unknown" }
        };
    }

    private List<Dictionary<string, object>> ConvertBlockItems(IReadOnlyList<Node> items)
    {
        var result = new List<Dictionary<string, object>>();
        foreach (var item in items)
        {
            if (item is Decl d)
                result.Add(ConvertDecl(d));
            else if (item is Stmt s)
                result.Add(ConvertStmt(s));
        }
        return result;
    }

    private Dictionary<string, object> ConvertStmt(Stmt stmt)
    {
        return stmt switch
        {
            AssignStmt a => new Dictionary<string, object>
            {
                ["type"] = "Assign",
                ["target"] = ConvertExpr(a.Target),
                ["value"] = ConvertExpr(a.Value)
            },

            CallStmt c => new Dictionary<string, object>
            {
                ["type"] = "Call",
                ["name"] = c.Name,
                ["arguments"] = ConvertExprs(c.Args)
            },

            IfStmt i => new Dictionary<string, object>
            {
                ["type"] = "If",
                ["condition"] = ConvertExpr(i.Condition),
                ["thenBlock"] = ConvertBlockItems(i.Then.Items),
                ["elseBlock"] = i.Else != null ? ConvertBlockItems(i.Else.Items) : null!
            },

            WhileStmt w => new Dictionary<string, object>
            {
                ["type"] = "While",
                ["condition"] = ConvertExpr(w.Condition),
                ["body"] = ConvertBlockItems(w.Body.Items)
            },

            ForStmt f => new Dictionary<string, object>
            {
                ["type"] = "For",
                ["iterator"] = f.Iterator,
                ["rangeStart"] = ConvertExpr(f.First),
                ["rangeEnd"] = f.Second != null ? ConvertExpr(f.Second) : null!,
                ["reverse"] = f.Reverse,
                ["body"] = ConvertBlockItems(f.Body.Items)
            },

            ReturnStmt r => new Dictionary<string, object>
            {
                ["type"] = "Return",
                ["value"] = r.Value != null ? ConvertExpr(r.Value) : null!
            },

            PrintStmt p => new Dictionary<string, object>
            {
                ["type"] = "Print",
                ["items"] = ConvertExprs(p.Items)
            },

            EmptyStmt => new Dictionary<string, object>
            {
                ["type"] = "Empty"
            },

            _ => new Dictionary<string, object> { ["type"] = "Unknown" }
        };
    }

    private List<Dictionary<string, object>> ConvertExprs(IReadOnlyList<Expr> exprs)
    {
        var result = new List<Dictionary<string, object>>();
        foreach (var expr in exprs)
        {
            result.Add(ConvertExpr(expr));
        }
        return result;
    }

    private Dictionary<string, object> ConvertExpr(Expr expr)
    {
        return expr switch
        {
            LiteralInt i => new Dictionary<string, object>
            {
                ["kind"] = "IntLiteral",
                ["value"] = i.Value
            },

            LiteralReal r => new Dictionary<string, object>
            {
                ["kind"] = "RealLiteral",
                ["value"] = r.Value
            },

            LiteralBool b => new Dictionary<string, object>
            {
                ["kind"] = "BoolLiteral",
                ["value"] = b.Value
            },

            NameExpr n => new Dictionary<string, object>
            {
                ["kind"] = "Name",
                ["name"] = n.Name
            },

            FieldExpr f => new Dictionary<string, object>
            {
                ["kind"] = "FieldAccess",
                ["receiver"] = ConvertExpr(f.Receiver),
                ["field"] = f.Field
            },

            IndexExpr idx => new Dictionary<string, object>
            {
                ["kind"] = "IndexAccess",
                ["receiver"] = ConvertExpr(idx.Receiver),
                ["index"] = ConvertExpr(idx.Index)
            },

            CallExpr c => new Dictionary<string, object>
            {
                ["kind"] = "Call",
                ["name"] = c.Name,
                ["arguments"] = ConvertExprs(c.Args)
            },

            UnaryExpr u => new Dictionary<string, object>
            {
                ["kind"] = "UnaryOp",
                ["operator"] = u.Op,
                ["operand"] = ConvertExpr(u.Operand)
            },

            BinaryExpr bin => new Dictionary<string, object>
            {
                ["kind"] = "BinaryOp",
                ["operator"] = bin.Op,
                ["left"] = ConvertExpr(bin.Left),
                ["right"] = ConvertExpr(bin.Right)
            },

            _ => new Dictionary<string, object> { ["kind"] = "Unknown" }
        };
    }
}
