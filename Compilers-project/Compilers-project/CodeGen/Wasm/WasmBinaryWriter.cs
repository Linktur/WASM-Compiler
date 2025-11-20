namespace Compilers_project.CodeGen.Wasm;

/// <summary>
/// Writes a WASM module to binary format
/// </summary>
public class WasmBinaryWriter
{
    private readonly WasmModule _module;
    private readonly MemoryStream _output;

    public WasmBinaryWriter(WasmModule module)
    {
        _module = module;
        _output = new MemoryStream();
    }

    public byte[] Write()
    {
        // Magic number and version
        WriteBytes(new byte[] { 0x00, 0x61, 0x73, 0x6D }); // \0asm
        WriteBytes(new byte[] { 0x01, 0x00, 0x00, 0x00 }); // version 1

        // Write sections in order
        WriteTypeSection();
        WriteImportSection();
        WriteFunctionSection();
        WriteMemorySection();
        WriteGlobalSection();
        WriteExportSection();
        WriteCodeSection();
        WriteDataSection();

        return _output.ToArray();
    }

    private void WriteTypeSection()
    {
        if (_module.Types.Count == 0) return;

        var section = new MemoryStream();
        WriteVarU32(section, (uint)_module.Types.Count);

        foreach (var type in _module.Types)
        {
            section.WriteByte(0x60); // func type
            WriteVarU32(section, (uint)type.Parameters.Count);
            foreach (var param in type.Parameters)
                section.WriteByte((byte)param);
            WriteVarU32(section, (uint)type.Results.Count);
            foreach (var result in type.Results)
                section.WriteByte((byte)result);
        }

        WriteSection(1, section.ToArray());
    }

    private void WriteImportSection()
    {
        if (_module.Imports.Count == 0) return;

        var section = new MemoryStream();
        WriteVarU32(section, (uint)_module.Imports.Count);

        foreach (var import in _module.Imports)
        {
            WriteName(section, import.Module);
            WriteName(section, import.Name);
            section.WriteByte(0x00); // import kind = function
            WriteVarU32(section, (uint)import.TypeIndex);
        }

        WriteSection(2, section.ToArray());
    }

    private void WriteFunctionSection()
    {
        if (_module.Functions.Count == 0) return;

        var section = new MemoryStream();
        WriteVarU32(section, (uint)_module.Functions.Count);

        foreach (var func in _module.Functions)
        {
            WriteVarU32(section, (uint)func.TypeIndex);
        }

        WriteSection(3, section.ToArray());
    }

    private void WriteMemorySection()
    {
        if (_module.Memory == null) return;

        var section = new MemoryStream();
        WriteVarU32(section, 1); // one memory

        if (_module.Memory.Limits.Max.HasValue)
        {
            section.WriteByte(0x01); // has max
            WriteVarU32(section, _module.Memory.Limits.Min);
            WriteVarU32(section, _module.Memory.Limits.Max.Value);
        }
        else
        {
            section.WriteByte(0x00); // no max
            WriteVarU32(section, _module.Memory.Limits.Min);
        }

        WriteSection(5, section.ToArray());
    }

    private void WriteGlobalSection()
    {
        if (_module.Globals.Count == 0) return;

        var section = new MemoryStream();
        WriteVarU32(section, (uint)_module.Globals.Count);

        foreach (var global in _module.Globals)
        {
            section.WriteByte((byte)global.Type);
            section.WriteByte((byte)(global.Mutable ? 0x01 : 0x00));
            WriteInstruction(section, global.InitExpr);
            section.WriteByte((byte)WasmOpcode.End);
        }

        WriteSection(6, section.ToArray());
    }

    private void WriteExportSection()
    {
        if (_module.Exports.Count == 0) return;

        var section = new MemoryStream();
        WriteVarU32(section, (uint)_module.Exports.Count);

        foreach (var export in _module.Exports)
        {
            WriteName(section, export.Name);
            section.WriteByte((byte)export.Kind);
            WriteVarU32(section, export.Index);
        }

        WriteSection(7, section.ToArray());
    }

    private void WriteCodeSection()
    {
        if (_module.Functions.Count == 0) return;

        var section = new MemoryStream();
        WriteVarU32(section, (uint)_module.Functions.Count);

        foreach (var func in _module.Functions)
        {
            var funcBody = new MemoryStream();

            // Compress locals: group consecutive same-type locals
            var localGroups = CompressLocals(func.Locals);
            WriteVarU32(funcBody, (uint)localGroups.Count);
            foreach (var (count, type) in localGroups)
            {
                WriteVarU32(funcBody, (uint)count);
                funcBody.WriteByte((byte)type);
            }

            // Write instructions
            foreach (var instr in func.Body)
            {
                WriteInstruction(funcBody, instr);
            }
            funcBody.WriteByte((byte)WasmOpcode.End);

            // Write function body size and content
            var funcBytes = funcBody.ToArray();
            WriteVarU32(section, (uint)funcBytes.Length);
            section.Write(funcBytes, 0, funcBytes.Length);
        }

        WriteSection(10, section.ToArray());
    }

    private void WriteDataSection()
    {
        if (_module.DataSegments.Count == 0) return;

        var section = new MemoryStream();
        WriteVarU32(section, (uint)_module.DataSegments.Count);

        foreach (var segment in _module.DataSegments)
        {
            WriteVarU32(section, segment.MemoryIndex);
            // Write offset as i32.const init expression
            section.WriteByte((byte)WasmOpcode.I32Const);
            WriteVarS32(section, segment.Offset);
            section.WriteByte((byte)WasmOpcode.End);
            // Write data
            WriteVarU32(section, (uint)segment.Data.Length);
            section.Write(segment.Data, 0, segment.Data.Length);
        }

        WriteSection(11, section.ToArray());
    }

    private List<(int count, WasmValueType type)> CompressLocals(List<WasmValueType> locals)
    {
        var result = new List<(int, WasmValueType)>();
        if (locals.Count == 0) return result;

        var currentType = locals[0];
        var count = 1;

        for (int i = 1; i < locals.Count; i++)
        {
            if (locals[i] == currentType)
            {
                count++;
            }
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

    private void WriteInstruction(MemoryStream stream, WasmInstruction instr)
    {
        switch (instr)
        {
            case InstrBlock block:
                stream.WriteByte((byte)WasmOpcode.Block);
                stream.WriteByte((byte)block.BlockType);
                foreach (var i in block.Body)
                    WriteInstruction(stream, i);
                stream.WriteByte((byte)WasmOpcode.End);
                break;

            case InstrLoop loop:
                stream.WriteByte((byte)WasmOpcode.Loop);
                stream.WriteByte((byte)loop.BlockType);
                foreach (var i in loop.Body)
                    WriteInstruction(stream, i);
                stream.WriteByte((byte)WasmOpcode.End);
                break;

            case InstrIf ifInstr:
                stream.WriteByte((byte)WasmOpcode.If);
                stream.WriteByte((byte)ifInstr.BlockType);
                foreach (var i in ifInstr.ThenBody)
                    WriteInstruction(stream, i);
                if (ifInstr.ElseBody.Count > 0)
                {
                    stream.WriteByte((byte)WasmOpcode.Else);
                    foreach (var i in ifInstr.ElseBody)
                        WriteInstruction(stream, i);
                }
                stream.WriteByte((byte)WasmOpcode.End);
                break;

            case InstrBr br:
                stream.WriteByte((byte)WasmOpcode.Br);
                WriteVarU32(stream, br.LabelIndex);
                break;

            case InstrBrIf brIf:
                stream.WriteByte((byte)WasmOpcode.BrIf);
                WriteVarU32(stream, brIf.LabelIndex);
                break;

            case InstrCall call:
                stream.WriteByte((byte)WasmOpcode.Call);
                WriteVarU32(stream, call.FuncIndex);
                break;

            case InstrLocalGet get:
                stream.WriteByte((byte)WasmOpcode.LocalGet);
                WriteVarU32(stream, get.LocalIndex);
                break;

            case InstrLocalSet set:
                stream.WriteByte((byte)WasmOpcode.LocalSet);
                WriteVarU32(stream, set.LocalIndex);
                break;

            case InstrLocalTee tee:
                stream.WriteByte((byte)WasmOpcode.LocalTee);
                WriteVarU32(stream, tee.LocalIndex);
                break;

            case InstrGlobalGet gget:
                stream.WriteByte((byte)WasmOpcode.GlobalGet);
                WriteVarU32(stream, gget.GlobalIndex);
                break;

            case InstrGlobalSet gset:
                stream.WriteByte((byte)WasmOpcode.GlobalSet);
                WriteVarU32(stream, gset.GlobalIndex);
                break;

            case InstrI32Load load:
                stream.WriteByte((byte)WasmOpcode.I32Load);
                WriteVarU32(stream, load.Align);
                WriteVarU32(stream, load.Offset);
                break;

            case InstrF64Load load:
                stream.WriteByte((byte)WasmOpcode.F64Load);
                WriteVarU32(stream, load.Align);
                WriteVarU32(stream, load.Offset);
                break;

            case InstrI32Store store:
                stream.WriteByte((byte)WasmOpcode.I32Store);
                WriteVarU32(stream, store.Align);
                WriteVarU32(stream, store.Offset);
                break;

            case InstrF64Store store:
                stream.WriteByte((byte)WasmOpcode.F64Store);
                WriteVarU32(stream, store.Align);
                WriteVarU32(stream, store.Offset);
                break;

            case InstrI32Const i32c:
                stream.WriteByte((byte)WasmOpcode.I32Const);
                WriteVarS32(stream, i32c.Value);
                break;

            case InstrI64Const i64c:
                stream.WriteByte((byte)WasmOpcode.I64Const);
                WriteVarS64(stream, i64c.Value);
                break;

            case InstrF32Const f32c:
                stream.WriteByte((byte)WasmOpcode.F32Const);
                stream.Write(BitConverter.GetBytes(f32c.Value), 0, 4);
                break;

            case InstrF64Const f64c:
                stream.WriteByte((byte)WasmOpcode.F64Const);
                stream.Write(BitConverter.GetBytes(f64c.Value), 0, 8);
                break;

            case InstrSimple simple:
                stream.WriteByte((byte)simple.Opcode);
                break;

            default:
                stream.WriteByte((byte)instr.Opcode);
                break;
        }
    }

    private void WriteSection(byte id, byte[] content)
    {
        _output.WriteByte(id);
        WriteVarU32(_output, (uint)content.Length);
        _output.Write(content, 0, content.Length);
    }

    private void WriteBytes(byte[] bytes)
    {
        _output.Write(bytes, 0, bytes.Length);
    }

    private void WriteName(MemoryStream stream, string name)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(name);
        WriteVarU32(stream, (uint)bytes.Length);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteVarU32(MemoryStream stream, uint value)
    {
        do
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0)
                b |= 0x80;
            stream.WriteByte(b);
        } while (value != 0);
    }

    private static void WriteVarS32(MemoryStream stream, int value)
    {
        bool more = true;
        while (more)
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;
            if ((value == 0 && (b & 0x40) == 0) || (value == -1 && (b & 0x40) != 0))
                more = false;
            else
                b |= 0x80;
            stream.WriteByte(b);
        }
    }

    private static void WriteVarS64(MemoryStream stream, long value)
    {
        bool more = true;
        while (more)
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;
            if ((value == 0 && (b & 0x40) == 0) || (value == -1 && (b & 0x40) != 0))
                more = false;
            else
                b |= 0x80;
            stream.WriteByte(b);
        }
    }
}
