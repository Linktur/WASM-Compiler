## Lexer Overview

- **Project**: `Compilers_project` (C# / .NET 8)
- **Goal**: Transform source text into a stream of tokens for the parser
- **Core Types**: `Lexer`, `SourceText`, `Token`, `TokenType`, `Span`

---

## Architecture

- **SourceText**: cursor over input with position, line, column; methods: `Peek`, `Advance`, `ConsumeNewLine`, `Reset`, `IsEof`.
- **Span**: precise token location metadata: `(start, length, line, col)`.
- **TokenType**: enums for special, literals/identifiers, keywords, operators/punct.
- **Token**: `(Type, Span, Text?)` where `Text` stores the lexeme if relevant.
- **Lexer**:
  - Public API: `NextToken()`, `Peek(k = 1)`, `Reset(position)`
  - Buffered lookahead using an internal list to implement `Peek`

---

## Token Categories

- **Special**: `Eof`, `Error`, `NewLine`
- **Identifiers/Literals**: `Identifier`, `IntegerLiteral`, `RealLiteral`, `BooleanLiteral`
- **Keywords**: `var`, `type`, `record`, `array`, `routine`, `is`, `end`, `if`, `then`, `else`, `while`, `for`, `reverse`, `loop`, `return`, `print`, `and`, `or`, `xor`, `not`
- **Operators/Punct**: `(` `)` `[` `]` `,` `;` `.` `..` `:=` `->` `+` `-` `*` `/` `%` `<` `<=` `>` `>=` `=` `<>`

---

## Main Algorithm (per token)

1. `SkipSpaces()` — skip spaces and tabs (keep newlines)
2. `TryLexNewLine()` — emit a single `NewLine` for `\n`, `\r`, or `\r\n`
3. If `IsEof` — emit `Eof`
4. Capture start position `(pos, line, col)`
5. `TryLexIdentifier()` — identifier or keyword via dictionary lookup
6. `TryLexNumber()` — integer or real literal
7. `LexOperatorOrPunct()` — operators and punctuation; compound tokens via lookahead
8. Otherwise — emit `Error` with message and span

---

## Lookahead & Buffering

- `Peek(k)` fills an internal token buffer with `NextCore` results without consuming
- `NextToken()` prefers buffered tokens first, then calls `NextCore`
- `Reset(position)` clears buffer and repositions `SourceText`

---

## Newlines Are Significant

- Newlines produce explicit `NewLine` tokens
- `ConsumeNewLine()` handles `LF`, `CR`, and `CRLF`, updating `(line, col)`

---

## Error Handling

- Unknown characters → `Error` token with message and span
- Malformed numbers can be reported as `Error` during numeric lexing

---

## Example

Input:

```text
var x := 10\nprint(x)
```

Tokens:

- `Var`("var")
- `Identifier`("x")
- `Assign`(":=")
- `IntegerLiteral`("10")
- `NewLine`
- `Print`("print")
- `LParen`("(")
- `Identifier`("x")
- `RParen`(")")
- `NewLine`
- `Eof`

---

## Running the Lexer

- Build:
  - `dotnet restore Compilers-project/Compilers-project.sln`
  - `dotnet build -c Release Compilers-project/Compilers-project.sln`
- Run with a file:
  - `dotnet run -c Release --project Compilers-project/Compilers-project/Compilers-project.csproj -- TestCases/Test3`

---

## Key Takeaways

- Deterministic single-pass tokenization
- Explicit newline tokens improve layout-sensitive parsing
- Accurate spans for user-friendly diagnostics
- Simple yet extensible operator/keyword recognition


