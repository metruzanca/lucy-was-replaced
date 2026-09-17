using GleamRuntime;

var wasmPath = args.Length >= 1 && args[0] != "--file"
    ? args[0]
    : Resolve("src/Plugin/Embedded/gleam_wasm_bg.wasm");
var stdlibDir = args.Length >= 2 && args[0] != "--file"
    ? args[1]
    : Resolve("src/GleamRuntime/Embedded/stdlib");

static string Resolve(string repoRelative)
    {
        var candidates = new[] { repoRelative, "../../" + repoRelative };
        return candidates.FirstOrDefault(p => File.Exists(p) || Directory.Exists(p))
            ?? throw new FileNotFoundException($"cannot locate {repoRelative} from cwd {Environment.CurrentDirectory}");
    }

// `--file <path.gleam>`: compile + run one Gleam file and print its output (fast iteration).
if (args is ["--file", var filePath])
{
    var file = Path.GetFullPath(filePath);
    var source = File.ReadAllText(file);
    var embeddedDir = Resolve("src/GleamRuntime/Embedded");
    var runner = new GleamRunner(
        File.ReadAllBytes(wasmPath),
        GleamStdlib.LoadSources(stdlibDir),
        RuntimeFiles(embeddedDir, stdlibDir),
        GameModules(embeddedDir));
    try
    {
        var compiled = runner.Compile(source);
        var sink = new Sink();
        var result = compiled.Run(sink, TimeSpan.FromSeconds(30), enableDrones: true);
        if (result.Error != null)
        {
            Console.Error.WriteLine("RUNTIME ERROR: " + result.Error.Message);
            return 1;
        }
        return 0;
    }
    catch (GleamCompileException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

static Dictionary<string, string> RuntimeFiles(string embeddedDir, string stdlibDir) => new()
{
    ["gleam"] = GleamStdlib.LoadPrelude(embeddedDir),
    ["gleam_stdlib"] = GleamStdlib.LoadExternal(stdlibDir, "gleam_stdlib.mjs"),
    ["dict"] = GleamStdlib.LoadExternal(stdlibDir, "dict.mjs"),
    ["game_ffi"] = File.ReadAllText(Path.Combine(embeddedDir, "game_ffi.mjs")),
};

static List<(string, string)> GameModules(string embeddedDir) =>
    GleamStdlib.LoadGameModules(embeddedDir);

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

// 5. stdlib probe: write the whole stdlib, compile a program that uses it, inspect imports
{
    var stdlib = GleamStdlib.LoadSources(stdlibDir);

    var project = compiler.NewProject();
    foreach (var (name, code) in stdlib)
        project.WriteModule(name, code);

    project.WriteModule("main", """
        import gleam/io
        import gleam/list
        import gleam/int

        pub fn main() {
          io.println(int.to_string(list.length([1, 2, 3])))
        }
        """);
    project.CompilePackage("javascript");

    foreach (var mod in new[] { "main", "gleam/io", "gleam/list" })
    {
        Console.WriteLine($"\n===== {mod}.mjs =====");
        Console.WriteLine(project.ReadCompiledJavascript(mod));
    }
}

// 6. compile + run a real Gleam program via the Jint host
{
    var embeddedDir = Path.GetFullPath("src/GleamRuntime/Embedded");
    var stdlib = GleamStdlib.LoadSources(stdlibDir);
    var runtimeFiles = new Dictionary<string, string>
    {
        ["gleam"] = GleamStdlib.LoadPrelude(embeddedDir),
        ["gleam_stdlib"] = GleamStdlib.LoadExternal(Path.Combine(embeddedDir, "stdlib"), "gleam_stdlib.mjs"),
        ["dict"] = GleamStdlib.LoadExternal(Path.Combine(embeddedDir, "stdlib"), "dict.mjs"),
    };

    using var runner = new GleamRunner(wasmBytes, stdlib, runtimeFiles);
    var compiled = runner.Compile("""
        import gleam/io
        import gleam/list
        import gleam/int

        pub fn main() {
          let xs = [1, 2, 3, 4, 5]
          let total = list.fold(xs, 0, fn(acc, x) { acc + x })
          let doubled = list.map(xs, fn(x) { x * 2 })
          io.println("total: " <> int.to_string(total))
          io.println("doubled: " <> int.to_string(list.length(doubled)))
        }
        """);

    Console.WriteLine("\n== run output ==");
    var sink = new Sink();
    var result = compiled.Run(sink, TimeSpan.FromSeconds(5));
    Console.WriteLine(result.IsOk ? "== run ok ==" : $"== run error: {result.Error}");
}

// 7. full-language probe: records, `use`, and error mapping
{
    var embeddedDir = Path.GetFullPath("src/GleamRuntime/Embedded");
    var stdlib = GleamStdlib.LoadSources(stdlibDir);
    var runtimeFiles = new Dictionary<string, string>
    {
        ["gleam"] = GleamStdlib.LoadPrelude(embeddedDir),
        ["gleam_stdlib"] = GleamStdlib.LoadExternal(Path.Combine(embeddedDir, "stdlib"), "gleam_stdlib.mjs"),
        ["dict"] = GleamStdlib.LoadExternal(Path.Combine(embeddedDir, "stdlib"), "dict.mjs"),
    };
    using var runner2 = new GleamRunner(wasmBytes, stdlib, runtimeFiles);

    var recordsUse = runner2.Compile("""
        import gleam/io
        import gleam/int
        import gleam/result

        pub type Point {
          Point(x: Int, y: Int)
        }

        pub fn main() {
          let p = Point(3, 4)
          let q = Point(..p, x: 10)
          io.println("x: " <> int.to_string(q.x) <> " y: " <> int.to_string(q.y))
        }
        """);
    Console.WriteLine("\n== records + record update ==");
    var r1 = recordsUse.Run(new Sink());
    Console.WriteLine(r1.IsOk ? "ok" : $"error: {r1.Error}");

    var useExpr = runner2.Compile("""
        import gleam/io
        import gleam/int

        pub fn main() {
          use n <- with_value(42)
          io.println(int.to_string(n))
        }

        fn with_value(n: Int, continuation: fn(Int) -> Nil) -> Nil {
          continuation(n)
        }
        """);
    Console.WriteLine("\n== `use` expression ==");
    var r2 = useExpr.Run(new Sink());
    Console.WriteLine(r2.IsOk ? "ok" : $"error: {r2.Error}");
}

return 0;

sealed class Sink : IGleamLogSink
{
    public void Log(string message) => Console.WriteLine(message);
    public void Error(string message) => Console.Error.WriteLine(message);
}