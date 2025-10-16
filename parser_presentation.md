---
marp: true
paginate: true
footer: CupTeam @ Innopolis University CC Course
---

## Parser Presentation

**Team**: CupTeam
**Members**: Igor Kuzmenkov & Vsevolod Nazmudinov
**Target**: WASM
**Tech Stack**: C# / .NET 8, xUnitm, Hand written parser
**Repository**: [Linktur/WASM-Compiler](https://github.com/Linktur/WASM-Compiler)

---
## Example Program 1: Simple Arithmetic: Source
```
routine main() is
  var x : integer is 10
  var y : integer is 20
  var sum : integer is x + y
  print sum
end
```
---
## Example Program 1: Simple Arithmetic: Lexer
```
Routine         @ 1:1  'routine'
Identifier      @ 1:9  'main'
LParen          @ 1:13  ''
RParen          @ 1:14  ''
Is              @ 1:16  'is'
NewLine         @ 1:18  '\n'
Var             @ 2:3  'var'
Identifier      @ 2:7  'x'
Colon           @ 2:9  ''
Identifier      @ 2:11  'integer'
Is              @ 2:19  'is'
IntegerLiteral  @ 2:22  '10'
NewLine         @ 2:24  '\n'
Var             @ 3:3  'var'
Identifier      @ 3:7  'y'
Colon           @ 3:9  ''
Identifier      @ 3:11  'integer'
Is              @ 3:19  'is'
IntegerLiteral  @ 3:22  '20'
NewLine         @ 3:24  '\n'
Var             @ 4:3  'var'
Identifier      @ 4:7  'sum'
Colon           @ 4:11  ''
Identifier      @ 4:13  'integer'
Is              @ 4:21  'is'
Identifier      @ 4:24  'x'
Plus            @ 4:26  ''
Identifier      @ 4:28  'y'
NewLine         @ 4:29  '\n'
Print           @ 5:3  'print'
Identifier      @ 5:9  'sum'
NewLine         @ 5:12  '\n'
End             @ 6:1  'end'
NewLine         @ 6:4  '\n'
Eof             @ 7:1  ''
```
---
## Example Program 1: Simple Arithmetic: Parser
```
Program
  RoutineDecl: main
    Body:
      BlockBody:
        VarDecl: x
          Type:
            PrimitiveType: integer
          Initializer:
            IntLiteral: 10
        VarDecl: y
          Type:
            PrimitiveType: integer
          Initializer:
            IntLiteral: 20
        VarDecl: sum
          Type:
            PrimitiveType: integer
          Initializer:
            BinaryOp: +
              Left:
                Name: x
              Right:
                Name: y
        PrintStmt:
          Name: sum
```
---

## Example Program 2: Record Types: Source

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
---
## Example Program 2: Record Types: Lexer
```
Type            @ 1:1  'type'
Identifier      @ 1:6  'Point'
Is              @ 1:12  'is'
Record          @ 1:15  'record'
NewLine         @ 1:21  '\n'
Var             @ 2:3  'var'
Identifier      @ 2:7  'x'
Colon           @ 2:9  ''
Identifier      @ 2:11  'integer'
NewLine         @ 2:18  '\n'
Var             @ 3:3  'var'
Identifier      @ 3:7  'y'
Colon           @ 3:9  ''
Identifier      @ 3:11  'integer'
NewLine         @ 3:18  '\n'
End             @ 4:1  'end'
NewLine         @ 4:4  '\n'
NewLine         @ 5:1  '\n'
Routine         @ 6:1  'routine'
Identifier      @ 6:9  'main'
LParen          @ 6:13  ''
RParen          @ 6:14  ''
Is              @ 6:16  'is'
NewLine         @ 6:18  '\n'
Var             @ 7:3  'var'
Identifier      @ 7:7  'p'
Colon           @ 7:9  ''
Identifier      @ 7:11  'Point'
NewLine         @ 7:16  '\n'
Identifier      @ 8:3  'p'
Dot             @ 8:4  ''
Identifier      @ 8:5  'x'
Assign          @ 8:7  ''
IntegerLiteral  @ 8:10  '7'
NewLine         @ 8:11  '\n'
Identifier      @ 9:3  'p'
Dot             @ 9:4  ''
Identifier      @ 9:5  'y'
Assign          @ 9:7  ''
IntegerLiteral  @ 9:10  '9'
NewLine         @ 9:11  '\n'
Print           @ 10:3  'print'
Identifier      @ 10:9  'p'
Dot             @ 10:10  ''
Identifier      @ 10:11  'x'
NewLine         @ 10:12  '\n'
Print           @ 11:3  'print'
Identifier      @ 11:9  'p'
Dot             @ 11:10  ''
Identifier      @ 11:11  'y'
NewLine         @ 11:12  '\n'
End             @ 12:1  'end'
NewLine         @ 12:4  '\n'
Eof             @ 13:1  ''
```
---
## Example Program 2: Record Types: Parser
```
Program
  TypeDecl: Point
    RecordType:
      VarDecl: x
        Type:
          PrimitiveType: integer
      VarDecl: y
        Type:
          PrimitiveType: integer
  RoutineDecl: main
    Body:
      BlockBody:
        VarDecl: p
          Type:
            NamedType: Point
        AssignStmt:
          Target:
            FieldAccess: .x
              Name: p
          Value:
            IntLiteral: 7
        AssignStmt:
          Target:
            FieldAccess: .y
              Name: p
          Value:
            IntLiteral: 9
        PrintStmt:
          FieldAccess: .x
            Name: p
        PrintStmt:
          FieldAccess: .y
            Name: p
```
---
## Parser Architecture

### Core Components

1. **Parser** (`Parser.cs`)
   - Main entry point: `ParseProgram()`
   - Recursive descent parser
   - Error recovery with diagnostics
   - Delegates expression parsing to `ExprParser`

2. **ExprParser** (`ExprParser.cs`)
   - Handles operator precedence
   - Pratt parsing for expressions
   - Binary operators: `+`, `-`, `*`, `/`, `%`, `and`, `or`, `xor`
   - Unary operators: `-`, `not`
   - Comparison: `<`, `<=`, `>`, `>=`, `=`, `/=`

3. **AST Nodes** (`AST/*.cs`)
   - Immutable node hierarchy
   - Position tracking via `Span`
   - Type-safe representation

---

## AST Node Hierarchy

```
Node (abstract base)
├── Decl (declarations)
│   ├── VarDecl
│   ├── TypeDecl
│   └── RoutineDecl
├── Stmt (statements)
│   ├── AssignStmt
│   ├── CallStmt
│   ├── IfStmt
│   ├── WhileStmt
│   ├── ForStmt
│   ├── ReturnStmt
│   ├── PrintStmt
│   └── EmptyStmt
├── Expr (expressions)
│   ├── LiteralInt
│   ├── LiteralReal
│   ├── LiteralBool
│   ├── NameExpr
│   ├── FieldExpr
│   ├── IndexExpr
│   ├── CallExpr
│   ├── UnaryExpr
│   └── BinaryExpr
├── TypeRef (type references)
│   ├── PrimitiveTypeRef
│   ├── NamedTypeRef
│   ├── RecordTypeRef
│   └── ArrayTypeRef
└── ProgramNode (root)
```

---

## AST Node Implementation

### Base Node
```csharp
public abstract class Node
{
    public Span Span { get; }
    protected Node(Span span) => Span = span;
}
```

### Example: Variable Declaration
```csharp
public sealed class VarDecl : Decl
{
    public string Name { get; }
    public TypeRef? Type { get; }
    public Expr? Initializer { get; }

    public VarDecl(Span span, string name, 
                   TypeRef? type, Expr? init) 
        : base(span)
        => (Name, Type, Initializer) = (name, type, init);
}
```

### Example: Binary Expression
```csharp
public sealed class BinaryExpr : Expr
{
    public string Op { get; }
    public Expr Left { get; }
    public Expr Right { get; }

    public BinaryExpr(Span span, string op, 
                      Expr l, Expr r) 
        : base(span)
        => (Op, Left, Right) = (op, l, r);
}
```

---

## Core Parsing Logic

### Program Structure
```csharp
public ProgramNode ParseProgram()
{
    var decls = new List<Decl>();
    SkipOptionalSeparators();

    while (_t.Type != TokenType.Eof)
    {
        if (_t.Type == TokenType.Var || 
            _t.Type == TokenType.Type)
        {
            decls.Add(ParseSimpleDecl());
        }
        else if (_t.Type == TokenType.Routine)
        {
            decls.Add(ParseRoutineDecl());
        }
        else
        {
            Diag.Error(_t.Span, "declaration expected");
            RecoverTo(TokenType.Var, TokenType.Type, 
                     TokenType.Routine, TokenType.Eof);
        }
        SkipOptionalSeparators();
    }

    return new ProgramNode(span, decls);
}
```

---

## Parsing Declarations

### Variable Declaration
```csharp
private VarDecl ParseVarDecl(bool afterVar = false)
{
    var start = _t.Span;
    if (!afterVar) 
        Expect(TokenType.Var, "expected 'var'");

    var nameTok = Expect(TokenType.Identifier, 
                        "variable name expected");
    TypeRef? type = null;
    Expr? init = null;

    if (Accept(TokenType.Colon))
        type = ParseType();

    if (Accept(TokenType.Is))
        init = _expr.ParseExpression();

    return new VarDecl(span, nameTok.Text!, type, init);
}
```

---

## Parsing Types

### Type Reference Parsing
```csharp
private TypeRef ParseType()
{
    // Primitive or named type
    if (_t.Type == TokenType.Identifier)
    {
        var id = _t; Next();
        if (id.Text == "integer" || 
            id.Text == "real" || 
            id.Text == "boolean")
            return new PrimitiveTypeRef(id.Span, id.Text!);
        return new NamedTypeRef(id.Span, id.Text!);
    }

    // Record type
    if (Accept(TokenType.Record))
    {
        var fields = new List<VarDecl>();
        SkipOptionalSeparators();
        while (_t.Type == TokenType.Var)
        {
            Next(); // consume 'var'
            fields.Add(ParseVarDecl(afterVar: true));
            SkipOptionalSeparators();
        }
        Expect(TokenType.End, "expected 'end'");
        return new RecordTypeRef(span, fields);
    }

    // Array type
    if (Accept(TokenType.Array))
    {
        Expr? size = null;
        if (Accept(TokenType.LBracket))
        {
            if (_t.Type != TokenType.RBracket)
                size = _expr.ParseExpression();
            Expect(TokenType.RBracket, "expected ']'");
        }
        var elem = ParseType();
        return new ArrayTypeRef(span, elem, size);
    }

    Diag.Error(_t.Span, "type expected");
    return new NamedTypeRef(_t.Span, "<error>");
}
```

---

## Parsing Routines

### Routine Declaration
```csharp
private RoutineDecl ParseRoutineDecl()
{
    Expect(TokenType.Routine, "expected 'routine'");
    var name = Expect(TokenType.Identifier, 
                     "routine name expected").Text!;
    Expect(TokenType.LParen, "expected '('");

    // Parse parameters
    var pars = new List<Param>();
    if (_t.Type != TokenType.RParen)
    {
        for (;;)
        {
            var pid = Expect(TokenType.Identifier, 
                           "parameter name expected").Text!;
            Expect(TokenType.Colon, "':' expected");
            var ptype = ParseType();
            pars.Add(new Param(pid, ptype));
            if (!Accept(TokenType.Comma)) break;
        }
    }
    Expect(TokenType.RParen, "expected ')'");

    // Optional return type
    TypeRef? ret = null;
    if (Accept(TokenType.Colon))
        ret = ParseType();

    // Forward declaration (no body)
    if (_t.Type == TokenType.NewLine || 
        _t.Type == TokenType.Semicolon || 
        _t.Type == TokenType.Eof)
    {
        return new RoutineDecl(span, name, pars, ret, null);
    }

    // Expression body: => expr
    if (Accept(TokenType.Arrow))
    {
        var expr = _expr.ParseExpression();
        return new RoutineDecl(span, name, pars, ret, 
                              new ExprBody(expr));
    }

    // Block body: is ... end
    Expect(TokenType.Is, "expected 'is'");
    var body = ParseBlock();
    Expect(TokenType.End, "expected 'end'");
    return new RoutineDecl(span, name, pars, ret, 
                          new BlockBody(body));
}
```

---

## Parsing Statements

### Statement Dispatch
```csharp
private Stmt ParseStatement()
{
    switch (_t.Type)
    {
        case TokenType.If:     
            return ParseIf();
        case TokenType.While:  
            return ParseWhile();
        case TokenType.For:    
            return ParseFor();
        case TokenType.Return: 
            return ParseReturn();
        case TokenType.Print:  
            return ParsePrint();
        case TokenType.Identifier:
            return ParseAssignOrCall();
        default:
            Diag.Error(_t.Span, "statement expected");
            RecoverTo(TokenType.End);
            return new EmptyStmt(_t.Span);
    }
}
```

---

## Parsing Control Flow

### If Statement
```csharp
private IfStmt ParseIf()
{
    Expect(TokenType.If, "expected 'if'");
    var cond = _expr.ParseExpression();
    Expect(TokenType.Then, "expected 'then'");
    var thenBlk = ParseBlock();

    Block? elseBlk = null;
    if (Accept(TokenType.Else))
        elseBlk = ParseBlock();

    Expect(TokenType.End, "expected 'end'");
    return new IfStmt(span, cond, thenBlk, elseBlk);
}
```

### For Loop
```csharp
private ForStmt ParseFor()
{
    Expect(TokenType.For, "expected 'for'");
    var iter = Expect(TokenType.Identifier, 
                     "iterator name expected").Text!;
    Expect(TokenType.In, "expected 'in'");

    var first = _expr.ParseExpression();
    Expr? second = null;
    bool reverse = false;

    if (Accept(TokenType.DotDot))
        second = _expr.ParseExpression();

    if (Accept(TokenType.Reverse))
        reverse = true;

    Expect(TokenType.Loop, "expected 'loop'");
    var body = ParseBlock();
    Expect(TokenType.End, "expected 'end'");
    
    return new ForStmt(span, iter, first, second, 
                      reverse, body);
}
```

---

## Expression Parsing (Pratt)

### Operator Precedence
```csharp
private int GetPrecedence(TokenType t)
{
    return t switch
    {
        TokenType.Or => 10,
        TokenType.Xor => 20,
        TokenType.And => 30,
        TokenType.Equal or TokenType.NotEqual => 40,
        TokenType.Less or TokenType.LessEqual or 
        TokenType.Greater or TokenType.GreaterEqual => 50,
        TokenType.Plus or TokenType.Minus => 60,
        TokenType.Star or TokenType.Slash or 
        TokenType.Percent => 70,
        _ => 0
    };
}
```

### Binary Expression Parsing
```csharp
public Expr ParseExpression(int minPrec = 0)
{
    var left = ParsePrimary();

    while (true)
    {
        var prec = GetPrecedence(_t.Type);
        if (prec < minPrec) break;

        var op = _t;
        Next();
        var right = ParseExpression(prec + 1);
        
        left = new BinaryExpr(span, 
                             GetOpString(op.Type), 
                             left, right);
    }

    return left;
}
```

---

## Error Recovery

### Diagnostic System
```csharp
public class Diagnostics
{
    private readonly List<(Span Span, string Message)> _items = new();

    public bool HasErrors => _items.Count > 0;
    
    public IReadOnlyList<(Span Span, string Message)> Items 
        => _items;

    public void Error(Span span, string message)
    {
        _items.Add((span, message));
    }
}
```

### Recovery Strategy
```csharp
private void RecoverTo(params TokenType[] sentinels)
{
    var set = new HashSet<TokenType>(sentinels);
    
    // Skip tokens until we find a sentinel or separator
    while (_t.Type != TokenType.Eof && 
           !IsSeparator(_t) && 
           !set.Contains(_t.Type))
    {
        Next();
    }
    
    if (IsSeparator(_t)) 
        SkipOptionalSeparators();
}
```

---

## Key Features

### 1. Newline Significance
- Newlines are explicit tokens (`TokenType.NewLine`)
- Used as statement separators
- Allows layout-sensitive parsing

### 2. Forward Declarations
- Routines can be declared without body
- Body provided later in the file
- Enables mutual recursion

### 3. Type System
- Primitive types: `integer`, `real`, `boolean`
- User-defined types: `record`, `array`
- Type aliases via `type` declarations

### 4. Expression-Oriented
- Routines can have expression bodies: `=> expr`
- Compact syntax for simple functions

---

## Bonus: unit tests, ast visualization

## AST Visualization

### Text Output
```bash
dotnet run --project Compilers-project/Compilers-project \
  -- --ast TestCases/Test1
```

### JSON Export
```bash
dotnet run --project Compilers-project/Compilers-project \
  -- --ast-json TestCases/Test1 > ast.json
```

---

## Testing

```
dotnet test Compilers-project.Tests
```

---

# Thanks

