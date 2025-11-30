using Compilers_project.Parser.AST;

namespace Compilers_project.SemanticAnalyzer;

/// <summary>
/// Оптимизатор программы - применяет constant folding и dead code elimination на уровне AST.
/// </summary>
public static class ProgramOptimizer
{
    /// <summary>
    /// Оптимизирует программу целиком.
    /// </summary>
    public static ProgramNode Optimize(ProgramNode program) =>
        new(program.Span, [..program.Decls.Select(OptimizeDeclaration).Where(d => d != null).Select(d => d!)]);

    /// <summary>
    /// Оптимизирует отдельную декларацию.
    /// </summary>
    private static Decl? OptimizeDeclaration(Decl decl) => decl switch
    {
        VarDecl varDecl => OptimizeVarDecl(varDecl),
        RoutineDecl routineDecl => OptimizeRoutineDecl(routineDecl),
        TypeDecl => decl, // Типы не требуют оптимизации
        _ => decl
    };

    /// <summary>
    /// Оптимизирует декларацию переменной.
    /// </summary>
    private static Decl OptimizeVarDecl(VarDecl varDecl) =>
        varDecl.Initializer == null ? varDecl
        : new VarDecl(varDecl.Span, varDecl.Name, varDecl.Type, OptimizeExpression(varDecl.Initializer));

    /// <summary>
    /// Оптимизирует декларацию процедуры.
    /// </summary>
    private static Decl OptimizeRoutineDecl(RoutineDecl routineDecl) =>
        routineDecl.Body == null ? routineDecl
        : new RoutineDecl(routineDecl.Span, routineDecl.Name, routineDecl.Parameters,
                           routineDecl.ReturnType, OptimizeRoutineBody(routineDecl.Body));

    /// <summary>
    /// Оптимизирует тело процедуры.
    /// </summary>
    private static RoutineBody OptimizeRoutineBody(RoutineBody body) => body switch
    {
        ExprBody exprBody => new ExprBody(OptimizeExpression(exprBody.Expr)),
        BlockBody blockBody => new BlockBody(OptimizeBlock(blockBody.Block)),
        _ => body
    };

    /// <summary>
    /// Оптимизирует блок операторов.
    /// </summary>
    private static Block OptimizeBlock(Block block) =>
        new(block.Span, block.Items
            .Select(item => item switch
            {
                Decl decl => OptimizeDeclaration(decl) ?? decl,
                Stmt stmt => OptimizeStatement(stmt),
                _ => item
            })
            .Where(item => item is not EmptyStmt)
            .Cast<Node>()
            .ToList());

    /// <summary>
    /// Оптимизирует оператор.
    /// </summary>
    private static Stmt OptimizeStatement(Stmt stmt) => stmt switch
    {
        AssignStmt assign => OptimizeAssignStmt(assign),
        CallStmt call => OptimizeCallStmt(call),
        IfStmt ifStmt => OptimizeIfStmt(ifStmt),
        WhileStmt whileStmt => OptimizeWhileStmt(whileStmt),
        ForStmt forStmt => OptimizeForStmt(forStmt),
        ReturnStmt returnStmt => OptimizeReturnStmt(returnStmt),
        PrintStmt print => OptimizePrintStmt(print),
        EmptyStmt => stmt,
        _ => stmt
    };

    /// <summary>
    /// Оптимизирует присваивание.
    /// </summary>
    private static Stmt OptimizeAssignStmt(AssignStmt assign) =>
        new AssignStmt(assign.Span, OptimizeExpression(assign.Target), OptimizeExpression(assign.Value));

    /// <summary>
    /// Оптимизирует вызов процедуры.
    /// </summary>
    private static Stmt OptimizeCallStmt(CallStmt call) =>
        new CallStmt(call.Span, call.Name, call.Args.Select(OptimizeExpression).ToList());

    /// <summary>
    /// Оптимизирует условный оператор.
    /// </summary>
    private static Stmt OptimizeIfStmt(IfStmt ifStmt)
    {
        var condition = OptimizeExpression(ifStmt.Condition);
        var thenBlock = OptimizeBlock(ifStmt.Then);
        var elseBlock = ifStmt.Else != null ? OptimizeBlock(ifStmt.Else) : null;

        // Dead code elimination
        if (condition is LiteralBool literal)
        {
            return literal.Value
                ? new BlockStmt(thenBlock.Span, thenBlock)           // if true → then
                : elseBlock != null
                    ? new BlockStmt(elseBlock.Span, elseBlock)      // if false → else
                    : new EmptyStmt(ifStmt.Span);                    // if false → empty
        }

        return new IfStmt(ifStmt.Span, condition, thenBlock, elseBlock);
    }

    /// <summary>
    /// Оптимизирует цикл while.
    /// </summary>
    private static Stmt OptimizeWhileStmt(WhileStmt whileStmt)
    {
        var condition = OptimizeExpression(whileStmt.Condition);
        var body = OptimizeBlock(whileStmt.Body);

        // while false → empty (dead code)
        return condition is LiteralBool { Value: false }
            ? new EmptyStmt(whileStmt.Span)
            : new WhileStmt(whileStmt.Span, condition, body);
    }

    /// <summary>
    /// Оптимизирует цикл for.
    /// </summary>
    private static Stmt OptimizeForStmt(ForStmt forStmt) =>
        new ForStmt(forStmt.Span, forStmt.Iterator,
                    OptimizeExpression(forStmt.First),
                    forStmt.Second != null ? OptimizeExpression(forStmt.Second) : null,
                    forStmt.Reverse,
                    OptimizeBlock(forStmt.Body));

    /// <summary>
    /// Оптимизирует возврат из функции.
    /// </summary>
    private static Stmt OptimizeReturnStmt(ReturnStmt returnStmt) =>
        returnStmt.Value != null
            ? new ReturnStmt(returnStmt.Span, OptimizeExpression(returnStmt.Value))
            : returnStmt;

    /// <summary>
    /// Оптимизирует оператор печати.
    /// </summary>
    private static Stmt OptimizePrintStmt(PrintStmt print) =>
        new PrintStmt(print.Span, print.Items.Select(OptimizeExpression).ToList());

    /// <summary>
    /// Оптимизирует выражение через constant folding.
    /// </summary>
    private static Expr OptimizeExpression(Expr expr) => ConstantFolding.TryFold(expr);
}