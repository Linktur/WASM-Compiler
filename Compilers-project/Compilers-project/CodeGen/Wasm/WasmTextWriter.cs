using System.Text;

namespace Compilers_project.CodeGen.Wasm;

/// <summary>
/// Writes a WASM module to WAT (WebAssembly Text) format
/// </summary>
public class WasmTextWriter
{
    private readonly WasmModule _module;
    private readonly StringBuilder _sb;
    private int _indent;

    public WasmTextWriter(WasmModule module)
    {
        _module = module;
        _sb = new StringBuilder();
        _indent = 0;
    }

    public string Write()
    {
        AppendLine("(module");
        _indent++;

        WriteTypes();
        WriteImports();
        WriteMemory();
        WriteGlobals();
        WriteFunctions();
        WriteExports();
        WriteData();

        _indent--;
        AppendLine(")");

        return _sb.ToString();
    }

    private void WriteTypes()
    {
        for (int i = 0; i < _module.Types.Count; i++)
        {
            var type = _module.Types[i];
            var sb = new StringBuilder();
            sb.Append($"(type (;{i};) (func");

            if (type.Parameters.Count > 0)
            {
                sb.Append(" (param");
                foreach (var p in type.Parameters)
                    sb.Append($" {TypeToString(p)}");
                sb.Append(")");
            }

            if (type.Results.Count > 0)
            {
                sb.Append(" (result");
                foreach (var r in type.Results)
                    sb.Append($" {TypeToString(r)}");
                sb.Append(")");
            }

            sb.Append("))");
            AppendLine(sb.ToString());
        }
    }

    private void WriteImports()
    {
        for (int i = 0; i < _module.Imports.Count; i++)
        {
            var import = _module.Imports[i];
            AppendLine($"(import \"{import.Module}\" \"{import.Name}\" (func (;{i};) (type {import.TypeIndex})))");
        }
    }

    private void WriteMemory()
    {
        if (_module.Memory == null) return;

        var limits = _module.Memory.Limits;
        if (limits.Max.HasValue)
            AppendLine($"(memory (;0;) {limits.Min} {limits.Max})");
        else
            AppendLine($"(memory (;0;) {limits.Min})");
    }

    private void WriteGlobals()
    {
        for (int i = 0; i < _module.Globals.Count; i++)
        {
            var global = _module.Globals[i];
            var mutStr = global.Mutable ? "mut " : "";
            var initValue = GetInitExprString(global.InitExpr);
            AppendLine($"(global (;{i};) ({mutStr}{TypeToString(global.Type)}) ({initValue}))");
        }
    }

    private void WriteFunctions()
    {
        for (int i = 0; i < _module.Functions.Count; i++)
        {
            var func = _module.Functions[i];
            var funcIdx = _module.Imports.Count + i;

            var sb = new StringBuilder();
            sb.Append($"(func (;{funcIdx};) (type {func.TypeIndex})");

            // Write locals
            if (func.Locals.Count > 0)
            {
                var localGroups = CompressLocals(func.Locals);
                foreach (var (count, type) in localGroups)
                {
                    sb.Append($" (local");
                    for (int j = 0; j < count; j++)
                        sb.Append($" {TypeToString(type)}");
                    sb.Append(")");
                }
            }

            AppendLine(sb.ToString());
            _indent++;

            // Write body
            foreach (var instr in func.Body)
            {
                WriteInstruction(instr);
            }

            _indent--;
            AppendLine(")");
        }
    }

    private void WriteExports()
    {
        foreach (var export in _module.Exports)
        {
            var kind = export.Kind switch
            {
                WasmExportKind.Function => "func",
                WasmExportKind.Memory => "memory",
                WasmExportKind.Global => "global",
                WasmExportKind.Table => "table",
                _ => "func"
            };
            AppendLine($"(export \"{export.Name}\" ({kind} {export.Index}))");
        }
    }

    private void WriteData()
    {
        foreach (var segment in _module.DataSegments)
        {
            var dataStr = EscapeString(segment.Data);
            AppendLine($"(data (;0;) (i32.const {segment.Offset}) \"{dataStr}\")");
        }
    }

    private void WriteInstruction(WasmInstruction instr)
    {
        switch (instr)
        {
            case InstrBlock block:
                AppendLine($"block{BlockTypeString(block.BlockType)}");
                _indent++;
                foreach (var i in block.Body)
                    WriteInstruction(i);
                _indent--;
                AppendLine("end");
                break;

            case InstrLoop loop:
                AppendLine($"loop{BlockTypeString(loop.BlockType)}");
                _indent++;
                foreach (var i in loop.Body)
                    WriteInstruction(i);
                _indent--;
                AppendLine("end");
                break;

            case InstrIf ifInstr:
                AppendLine($"if{BlockTypeString(ifInstr.BlockType)}");
                _indent++;
                foreach (var i in ifInstr.ThenBody)
                    WriteInstruction(i);
                if (ifInstr.ElseBody.Count > 0)
                {
                    _indent--;
                    AppendLine("else");
                    _indent++;
                    foreach (var i in ifInstr.ElseBody)
                        WriteInstruction(i);
                }
                _indent--;
                AppendLine("end");
                break;

            case InstrBr br:
                AppendLine($"br {br.LabelIndex}");
                break;

            case InstrBrIf brIf:
                AppendLine($"br_if {brIf.LabelIndex}");
                break;

            case InstrCall call:
                AppendLine($"call {call.FuncIndex}");
                break;

            case InstrLocalGet get:
                AppendLine($"local.get {get.LocalIndex}");
                break;

            case InstrLocalSet set:
                AppendLine($"local.set {set.LocalIndex}");
                break;

            case InstrLocalTee tee:
                AppendLine($"local.tee {tee.LocalIndex}");
                break;

            case InstrGlobalGet gget:
                AppendLine($"global.get {gget.GlobalIndex}");
                break;

            case InstrGlobalSet gset:
                AppendLine($"global.set {gset.GlobalIndex}");
                break;

            case InstrI32Load load:
                AppendLine($"i32.load offset={load.Offset}");
                break;

            case InstrF64Load load:
                AppendLine($"f64.load offset={load.Offset}");
                break;

            case InstrI32Store store:
                AppendLine($"i32.store offset={store.Offset}");
                break;

            case InstrF64Store store:
                AppendLine($"f64.store offset={store.Offset}");
                break;

            case InstrI32Const i32c:
                AppendLine($"i32.const {i32c.Value}");
                break;

            case InstrI64Const i64c:
                AppendLine($"i64.const {i64c.Value}");
                break;

            case InstrF32Const f32c:
                AppendLine($"f32.const {f32c.Value}");
                break;

            case InstrF64Const f64c:
                AppendLine($"f64.const {f64c.Value}");
                break;

            default:
                AppendLine(OpcodeToString(instr.Opcode));
                break;
        }
    }

    private string OpcodeToString(WasmOpcode opcode) => opcode switch
    {
        WasmOpcode.Unreachable => "unreachable",
        WasmOpcode.Nop => "nop",
        WasmOpcode.Return => "return",
        WasmOpcode.Drop => "drop",
        WasmOpcode.Select => "select",

        WasmOpcode.I32Eqz => "i32.eqz",
        WasmOpcode.I32Eq => "i32.eq",
        WasmOpcode.I32Ne => "i32.ne",
        WasmOpcode.I32LtS => "i32.lt_s",
        WasmOpcode.I32GtS => "i32.gt_s",
        WasmOpcode.I32LeS => "i32.le_s",
        WasmOpcode.I32GeS => "i32.ge_s",

        WasmOpcode.I32Add => "i32.add",
        WasmOpcode.I32Sub => "i32.sub",
        WasmOpcode.I32Mul => "i32.mul",
        WasmOpcode.I32DivS => "i32.div_s",
        WasmOpcode.I32RemS => "i32.rem_s",
        WasmOpcode.I32And => "i32.and",
        WasmOpcode.I32Or => "i32.or",
        WasmOpcode.I32Xor => "i32.xor",

        WasmOpcode.F64Eq => "f64.eq",
        WasmOpcode.F64Ne => "f64.ne",
        WasmOpcode.F64Lt => "f64.lt",
        WasmOpcode.F64Gt => "f64.gt",
        WasmOpcode.F64Le => "f64.le",
        WasmOpcode.F64Ge => "f64.ge",

        WasmOpcode.F64Add => "f64.add",
        WasmOpcode.F64Sub => "f64.sub",
        WasmOpcode.F64Mul => "f64.mul",
        WasmOpcode.F64Div => "f64.div",
        WasmOpcode.F64Neg => "f64.neg",

        WasmOpcode.I32TruncF64S => "i32.trunc_f64_s",
        WasmOpcode.F64ConvertI32S => "f64.convert_i32_s",

        _ => opcode.ToString().ToLower()
    };

    private string TypeToString(WasmValueType type) => type switch
    {
        WasmValueType.I32 => "i32",
        WasmValueType.I64 => "i64",
        WasmValueType.F32 => "f32",
        WasmValueType.F64 => "f64",
        _ => "i32"
    };

    private string BlockTypeString(WasmBlockType bt)
    {
        if (bt == WasmBlockType.Void) return "";
        return $" (result {TypeToString((WasmValueType)bt)})";
    }

    private string GetInitExprString(WasmInstruction instr) => instr switch
    {
        InstrI32Const i => $"i32.const {i.Value}",
        InstrI64Const i => $"i64.const {i.Value}",
        InstrF32Const f => $"f32.const {f.Value}",
        InstrF64Const f => $"f64.const {f.Value}",
        _ => "i32.const 0"
    };

    private List<(int count, WasmValueType type)> CompressLocals(List<WasmValueType> locals)
    {
        var result = new List<(int, WasmValueType)>();
        if (locals.Count == 0) return result;

        var currentType = locals[0];
        var count = 1;

        for (int i = 1; i < locals.Count; i++)
        {
            if (locals[i] == currentType)
                count++;
            else
            {
                result.Add((count, currentType));
                currentType = locals[i];
                count = 1;
            }
        }
        result.Add((count, currentType));

        return result;
    }

    private string EscapeString(byte[] data)
    {
        var sb = new StringBuilder();
        foreach (var b in data)
        {
            if (b >= 32 && b < 127 && b != '"' && b != '\\')
                sb.Append((char)b);
            else
                sb.Append($"\\{b:x2}");
        }
        return sb.ToString();
    }

    private void AppendLine(string line)
    {
        _sb.Append(new string(' ', _indent * 2));
        _sb.AppendLine(line);
    }
}
