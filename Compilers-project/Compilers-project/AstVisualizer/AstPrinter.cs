using System;
using System.Collections.Generic;
using System.Text;
using Compilers_project.Parser.AST;

namespace Compilers_project.AstVisualizer;

/// <summary>
/// Печатает AST в виде дерева в консоль
/// </summary>
public class AstPrinter
{
    private readonly StringBuilder _sb = new();
    private int _indent = 0;

    public string Print(ProgramNode program)
    {
        _sb.Clear();
        _indent = 0;
        
        PrintLine("Program");
        _indent++;
        foreach (var decl in program.Decls)
        {
            PrintDecl(decl);
        }
        _indent--;
        
        return _sb.ToString();
    }

    private void PrintDecl(Decl decl)
    {
        switch (decl)
        {
            case VarDecl v:
                PrintLine($"VarDecl: {v.Name}");
                _indent++;
                if (v.Type != null)
                {
                    PrintLine("Type:");
                    _indent++;
                    PrintTypeRef(v.Type);
                    _indent--;
                }
                if (v.Initializer != null)
                {
                    PrintLine("Initializer:");
                    _indent++;
                    PrintExpr(v.Initializer);
                    _indent--;
                }
                _indent--;
                break;

            case TypeDecl t:
                PrintLine($"TypeDecl: {t.Name}");
                _indent++;
                PrintTypeRef(t.Type);
                _indent--;
                break;

            case RoutineDecl r:
                PrintLine($"RoutineDecl: {r.Name}");
                _indent++;
                
                if (r.Parameters.Count > 0)
                {
                    PrintLine("Parameters:");
                    _indent++;
                    foreach (var p in r.Parameters)
                    {
                        PrintLine($"Param: {p.Name}");
                        _indent++;
                        PrintTypeRef(p.Type);
                        _indent--;
                    }
                    _indent--;
                }
                
                if (r.ReturnType != null)
                {
                    PrintLine("ReturnType:");
                    _indent++;
                    PrintTypeRef(r.ReturnType);
                    _indent--;
                }
                
                if (r.Body != null)
                {
                    PrintLine("Body:");
                    _indent++;
                    PrintRoutineBody(r.Body);
                    _indent--;
                }
                else
                {
                    PrintLine("Body: <forward declaration>");
                }
                
                _indent--;
                break;
        }
    }

    private void PrintTypeRef(TypeRef type)
    {
        switch (type)
        {
            case PrimitiveTypeRef p:
                PrintLine($"PrimitiveType: {p.Name}");
                break;

            case NamedTypeRef n:
                PrintLine($"NamedType: {n.Name}");
                break;

            case RecordTypeRef r:
                PrintLine("RecordType:");
                _indent++;
                foreach (var field in r.Fields)
                {
                    PrintDecl(field);
                }
                _indent--;
                break;

            case ArrayTypeRef a:
                PrintLine("ArrayType:");
                _indent++;
                if (a.Size != null)
                {
                    PrintLine("Size:");
                    _indent++;
                    PrintExpr(a.Size);
                    _indent--;
                }
                PrintLine("ElementType:");
                _indent++;
                PrintTypeRef(a.Element);
                _indent--;
                _indent--;
                break;
        }
    }

    private void PrintRoutineBody(RoutineBody body)
    {
        switch (body)
        {
            case ExprBody e:
                PrintLine("ExprBody:");
                _indent++;
                PrintExpr(e.Expr);
                _indent--;
                break;

            case BlockBody b:
                PrintLine("BlockBody:");
                _indent++;
                PrintBlock(b.Block);
                _indent--;
                break;
        }
    }

    private void PrintBlock(Block block)
    {
        foreach (var item in block.Items)
        {
            if (item is Decl d)
                PrintDecl(d);
            else if (item is Stmt s)
                PrintStmt(s);
        }
    }

    private void PrintStmt(Stmt stmt)
    {
        switch (stmt)
        {
            case AssignStmt a:
                PrintLine("AssignStmt:");
                _indent++;
                PrintLine("Target:");
                _indent++;
                PrintExpr(a.Target);
                _indent--;
                PrintLine("Value:");
                _indent++;
                PrintExpr(a.Value);
                _indent--;
                _indent--;
                break;

            case CallStmt c:
                PrintLine($"CallStmt: {c.Name}");
                _indent++;
                if (c.Args.Count > 0)
                {
                    PrintLine("Arguments:");
                    _indent++;
                    foreach (var arg in c.Args)
                        PrintExpr(arg);
                    _indent--;
                }
                _indent--;
                break;

            case IfStmt i:
                PrintLine("IfStmt:");
                _indent++;
                PrintLine("Condition:");
                _indent++;
                PrintExpr(i.Condition);
                _indent--;
                PrintLine("Then:");
                _indent++;
                PrintBlock(i.Then);
                _indent--;
                if (i.Else != null)
                {
                    PrintLine("Else:");
                    _indent++;
                    PrintBlock(i.Else);
                    _indent--;
                }
                _indent--;
                break;

            case WhileStmt w:
                PrintLine("WhileStmt:");
                _indent++;
                PrintLine("Condition:");
                _indent++;
                PrintExpr(w.Condition);
                _indent--;
                PrintLine("Body:");
                _indent++;
                PrintBlock(w.Body);
                _indent--;
                _indent--;
                break;

            case ForStmt f:
                PrintLine($"ForStmt: {f.Iterator}");
                _indent++;
                PrintLine("Range:");
                _indent++;
                PrintExpr(f.First);
                if (f.Second != null)
                {
                    PrintLine("to");
                    PrintExpr(f.Second);
                }
                if (f.Reverse)
                    PrintLine("(reverse)");
                _indent--;
                PrintLine("Body:");
                _indent++;
                PrintBlock(f.Body);
                _indent--;
                _indent--;
                break;

            case ReturnStmt r:
                PrintLine("ReturnStmt:");
                if (r.Value != null)
                {
                    _indent++;
                    PrintExpr(r.Value);
                    _indent--;
                }
                break;

            case PrintStmt p:
                PrintLine("PrintStmt:");
                _indent++;
                foreach (var item in p.Items)
                    PrintExpr(item);
                _indent--;
                break;

            case EmptyStmt:
                PrintLine("EmptyStmt");
                break;
        }
    }

    private void PrintExpr(Expr expr)
    {
        switch (expr)
        {
            case LiteralInt i:
                PrintLine($"IntLiteral: {i.Value}");
                break;

            case LiteralReal r:
                PrintLine($"RealLiteral: {r.Value}");
                break;

            case LiteralBool b:
                PrintLine($"BoolLiteral: {b.Value}");
                break;

            case NameExpr n:
                PrintLine($"Name: {n.Name}");
                break;

            case FieldExpr f:
                PrintLine($"FieldAccess: .{f.Field}");
                _indent++;
                PrintExpr(f.Receiver);
                _indent--;
                break;

            case IndexExpr idx:
                PrintLine("IndexAccess:");
                _indent++;
                PrintLine("Array:");
                _indent++;
                PrintExpr(idx.Receiver);
                _indent--;
                PrintLine("Index:");
                _indent++;
                PrintExpr(idx.Index);
                _indent--;
                _indent--;
                break;

            case CallExpr c:
                PrintLine($"Call: {c.Name}");
                _indent++;
                if (c.Args.Count > 0)
                {
                    PrintLine("Arguments:");
                    _indent++;
                    foreach (var arg in c.Args)
                        PrintExpr(arg);
                    _indent--;
                }
                _indent--;
                break;

            case UnaryExpr u:
                PrintLine($"UnaryOp: {u.Op}");
                _indent++;
                PrintExpr(u.Operand);
                _indent--;
                break;

            case BinaryExpr bin:
                PrintLine($"BinaryOp: {bin.Op}");
                _indent++;
                PrintLine("Left:");
                _indent++;
                PrintExpr(bin.Left);
                _indent--;
                PrintLine("Right:");
                _indent++;
                PrintExpr(bin.Right);
                _indent--;
                _indent--;
                break;
        }
    }

    private void PrintLine(string text)
    {
        _sb.Append(new string(' ', _indent * 2));
        _sb.AppendLine(text);
    }
}
