using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Compilers_project.WebAssembly;

public class WasmRunner
{
    private readonly string _nodePath;

    public WasmRunner()
    {
        _nodePath = FindNodeExecutable() ?? throw new InvalidOperationException("Node.js not found. Please install Node.js to run WebAssembly.");
    }

    public bool ValidateWat(string watFile)
    {
        try
        {
            var content = File.ReadAllText(watFile);

            if (!content.TrimStart().StartsWith("(module"))
            {
                Console.Error.WriteLine($"[VALIDATION] Error: Not a valid WebAssembly module (missing module)");
                return false;
            }

            if (!content.Contains("(func $main"))
            {
                Console.Error.WriteLine($"[VALIDATION] Error: Missing main function");
                return false;
            }

            var balance = 0;
            foreach (var c in content)
            {
                if (c == '(') balance++;
                else if (c == ')') balance--;
                if (balance < 0)
                {
                    Console.Error.WriteLine($"[VALIDATION] Error: Unbalanced parentheses");
                    return false;
                }
            }

            if (balance != 0)
            {
                Console.Error.WriteLine($"[VALIDATION] Error: Unbalanced parentheses");
                return false;
            }

            Console.WriteLine("[VALIDATION] ✅ .wat file is valid");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[VALIDATION] ❌ Error validating {watFile}: {ex.Message}");
            return false;
        }
    }

    public bool ConvertWatToWasm(string watFile, string wasmFile)
    {
        try
        {
            Console.WriteLine($"[CONVERT] Converting {watFile} to {wasmFile}...");

            if (TryWat2Wasm(watFile, wasmFile))
            {
                Console.WriteLine($"[CONVERT] ✅ Converted using wat2wasm");
                return true;
            }

            Console.Error.WriteLine("[CONVERT] ❌ wat2wasm not found. Please install WABT:");

            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CONVERT] ❌ Error converting {watFile}: {ex.Message}");
            return false;
        }
    }

    public bool RunWasm(string wasmFile)
    {
        try
        {
            if (!File.Exists(wasmFile))
            {
                Console.Error.WriteLine($"[RUN] File {wasmFile} not found");
                return false;
            }

            var jsCode = GenerateRunnerScript(wasmFile);
            var tempJsFile = Path.GetTempFileName();
            File.WriteAllText(tempJsFile, jsCode);

            try
            {
                return RunJavaScript(tempJsFile);
            }
            finally
            {
                File.Delete(tempJsFile);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[RUN] ❌ Error running {wasmFile}: {ex.Message}");
            return false;
        }
    }

    public bool ValidateConvertAndRun(string inputPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(inputPath);
        var watFile = $"{fileName}.wat";
        var wasmFile = $"{fileName}.wasm";

        if (!ValidateWat(watFile))
            return false;

        if (!ConvertWatToWasm(watFile, wasmFile))
            return false;

        return RunWasm(wasmFile);
    }

    private string? FindNodeExecutable()
    {
        var possiblePaths = new[]
        {
            "node",
            "node.exe",
            "/usr/bin/node",
            "/usr/local/bin/node",
            "C:\\Program Files\\nodejs\\node.exe"
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return path;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private bool TryWat2Wasm(string watFile, string wasmFile)
    {
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            var assemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var possiblePaths = new[] {
                "wat2wasm",
                Path.Combine(currentDir, "wat2wasm"),
                Path.Combine(assemblyDir ?? "", "wat2wasm"),
                Path.Combine(currentDir, "bin", "Debug", "net8.0", "wat2wasm"),
                Path.Combine(currentDir, "bin", "Release", "net8.0", "wat2wasm")
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = path,
                            Arguments = $"\"{watFile}\" -o \"{wasmFile}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"[CONVERT] ✅ Using {path}");
                        return true;
                    }
                }
                catch
                {
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool RunJavaScript(string jsFile)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _nodePath,
                Arguments = jsFile,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();

        while (!process.StandardOutput.EndOfStream)
        {
            var line = process.StandardOutput.ReadLine();
            if (line != null)
            {
                Console.WriteLine(line);
            }
        }

        var errors = process.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(errors))
        {
            Console.Error.WriteLine($"[RUN] ⚠️ {errors}");
        }

        process.WaitForExit();

        return process.ExitCode == 0;
    }

    private string GenerateRunnerScript(string wasmFile)
    {
        return $@"
const fs = require('fs');

async function runWasm() {{
    try {{
        const wasmBytes = fs.readFileSync('{wasmFile}');

        const env = {{
            print_i32: (value) => console.log(value),
            print_f64: (value) => console.log(value)
        }};

        const {{ instance }} = await WebAssembly.instantiate(wasmBytes, {{ env }});
        instance.exports.main();

    }} catch (error) {{
        console.error('❌ WebAssembly execution error:', error.message);
        process.exit(1);
    }}
}}

runWasm();
";
    }
}