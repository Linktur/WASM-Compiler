namespace Compilers_project.CodeGen.Wasm;

/// <summary>
/// Represents a WebAssembly function
/// </summary>
public class WasmFunction
{
    public string Name { get; set; } = "";
    public int TypeIndex { get; set; }
    public List<WasmValueType> Locals { get; } = new();
    public List<WasmInstruction> Body { get; } = new();
    public bool IsExported { get; set; } = false;
}

/// <summary>
/// Represents an imported function
/// </summary>
public class WasmImport
{
    public string Module { get; set; } = "";
    public string Name { get; set; } = "";
    public int TypeIndex { get; set; }
}

/// <summary>
/// Represents a global variable
/// </summary>
public class WasmGlobal
{
    public WasmValueType Type { get; set; }
    public bool Mutable { get; set; }
    public WasmInstruction InitExpr { get; set; } = new InstrI32Const(0);
    public string? ExportName { get; set; }
}

/// <summary>
/// Represents an export entry
/// </summary>
public class WasmExport
{
    public string Name { get; set; } = "";
    public WasmExportKind Kind { get; set; }
    public uint Index { get; set; }
}

public enum WasmExportKind : byte
{
    Function = 0x00,
    Table = 0x01,
    Memory = 0x02,
    Global = 0x03
}

/// <summary>
/// Represents a complete WebAssembly module
/// </summary>
public class WasmModule
{
    // Type section - function signatures
    public List<WasmFuncType> Types { get; } = new();

    // Import section - imported functions
    public List<WasmImport> Imports { get; } = new();

    // Function section - function type indices (for non-imported functions)
    public List<WasmFunction> Functions { get; } = new();

    // Memory section
    public WasmMemoryType? Memory { get; set; }

    // Global section
    public List<WasmGlobal> Globals { get; } = new();

    // Export section
    public List<WasmExport> Exports { get; } = new();

    // Data section for initialized memory
    public List<WasmDataSegment> DataSegments { get; } = new();

    // Helper to get or add a function type
    public int GetOrAddFuncType(WasmFuncType funcType)
    {
        for (int i = 0; i < Types.Count; i++)
        {
            if (Types[i].Equals(funcType))
                return i;
        }
        Types.Add(funcType);
        return Types.Count - 1;
    }

    // Add an import and return its function index
    public int AddImport(string module, string name, WasmFuncType funcType)
    {
        var typeIndex = GetOrAddFuncType(funcType);
        Imports.Add(new WasmImport
        {
            Module = module,
            Name = name,
            TypeIndex = typeIndex
        });
        return Imports.Count - 1;
    }

    // Add a function and return its function index (after imports)
    public int AddFunction(WasmFunction func)
    {
        func.TypeIndex = GetOrAddFuncType(new WasmFuncType(
            GetParamsFromTypeIndex(func.TypeIndex),
            GetResultsFromTypeIndex(func.TypeIndex)
        ));
        Functions.Add(func);
        return Imports.Count + Functions.Count - 1;
    }

    private List<WasmValueType> GetParamsFromTypeIndex(int typeIndex)
    {
        if (typeIndex >= 0 && typeIndex < Types.Count)
            return Types[typeIndex].Parameters;
        return new List<WasmValueType>();
    }

    private List<WasmValueType> GetResultsFromTypeIndex(int typeIndex)
    {
        if (typeIndex >= 0 && typeIndex < Types.Count)
            return Types[typeIndex].Results;
        return new List<WasmValueType>();
    }

    // Get total function count (imports + defined)
    public int TotalFunctionCount => Imports.Count + Functions.Count;
}

/// <summary>
/// Represents a data segment for initializing memory
/// </summary>
public class WasmDataSegment
{
    public uint MemoryIndex { get; set; } = 0;
    public int Offset { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
}
