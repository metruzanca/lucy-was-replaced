using GleamRuntime;

var wasmPath = args switch
{
    [var p] => p,
    _ => Path.GetFullPath("../../src/Plugin/Embedded/gleam_wasm_bg.wasm"),
};

if (!File.Exists(wasmPath))
{
    Console.Error.WriteLine($"wasm not found: {wasmPath}");
    return 1;
}

var wasmBytes = File.ReadAllBytes(wasmPath);
Console.WriteLine($"loaded gleam wasm: {wasmBytes.Length} bytes");

using var compiler = new GleamCompiler(wasmBytes);
compiler.LogSink = new Sink();

// 1. hello
{
    var project = compiler.NewProject();
    project.WriteModule("main", "pub fn main() {\n  Nil\n}\n");
    project.CompilePackage("javascript");
    var js = project.ReadCompiledJavascript("main");
    Console.WriteLine("\n== hello main ==");
    Console.WriteLine(js);
}

// 2. multi-module
{
    var project = compiler.NewProject();
    project.WriteModule("helper", """
        pub fn add(a: Int, b: Int) -> Int {
          a + b
        }

        pub fn describe(n: Int) -> String {
          case n {
            0 -> "zero"
            _ -> "other"
          }
        }
        """);
    project.WriteModule("main", """
        import helper

        pub fn main() {
          helper.describe(helper.add(1, 2))
        }
        """);
    project.CompilePackage("javascript");
    Console.WriteLine("\n== main.mjs ==");
    Console.WriteLine(project.ReadCompiledJavascript("main"));
    Console.WriteLine("\n== helper.mjs ==");
    Console.WriteLine(project.ReadCompiledJavascript("helper"));
}

// 3. compile error
{
    var project = compiler.NewProject();
    project.WriteModule("main", "pub fn main() { let x = ");
    try
    {
        project.CompilePackage("javascript");
        Console.WriteLine("\n== error case: unexpectedly compiled ==");
    }
    catch (GleamCompileException e)
    {
        Console.WriteLine("\n== compile error diagnostics ==");
        Console.WriteLine(e.Message);
    }
}

// 4. warnings
{
    var project = compiler.NewProject();
    project.WriteModule("main", "pub fn main() {\n  Nil\n}\n");
    project.CompilePackage("javascript");
    var warnings = project.TakeWarnings();
    Console.WriteLine($"\n== warnings ({warnings.Count}) ==");
    foreach (var w in warnings) Console.WriteLine(w);
}

return 0;

sealed class Sink : IGleamLogSink
{
    public void Log(string message) => Console.WriteLine($"[wasm:log] {message}");
    public void Error(string message) => Console.Error.WriteLine($"[wasm:error] {message}");
}