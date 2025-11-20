namespace Compilers_project.CodeGen.Wasm;

/// <summary>
/// WebAssembly value types
/// </summary>
public enum WasmValueType : byte
{
    I32 = 0x7F,
    I64 = 0x7E,
    F32 = 0x7D,
    F64 = 0x7C
}

/// <summary>
/// WebAssembly block types for control flow
/// </summary>
public enum WasmBlockType : byte
{
    Void = 0x40,
    I32 = 0x7F,
    I64 = 0x7E,
    F32 = 0x7D,
    F64 = 0x7C
}

/// <summary>
/// WebAssembly function type signature
/// </summary>
public class WasmFuncType
{
    public List<WasmValueType> Parameters { get; } = new();
    public List<WasmValueType> Results { get; } = new();

    public WasmFuncType() { }

    public WasmFuncType(List<WasmValueType> parameters, List<WasmValueType> results)
    {
        Parameters = parameters;
        Results = results;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not WasmFuncType other) return false;
        return Parameters.SequenceEqual(other.Parameters) && Results.SequenceEqual(other.Results);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var p in Parameters) hash.Add(p);
        foreach (var r in Results) hash.Add(r);
        return hash.ToHashCode();
    }
}

/// <summary>
/// WebAssembly limits for memory and tables
/// </summary>
public class WasmLimits
{
    public uint Min { get; set; }
    public uint? Max { get; set; }

    public WasmLimits(uint min, uint? max = null)
    {
        Min = min;
        Max = max;
    }
}

/// <summary>
/// WebAssembly memory type
/// </summary>
public class WasmMemoryType
{
    public WasmLimits Limits { get; set; }

    public WasmMemoryType(uint minPages, uint? maxPages = null)
    {
        Limits = new WasmLimits(minPages, maxPages);
    }
}
