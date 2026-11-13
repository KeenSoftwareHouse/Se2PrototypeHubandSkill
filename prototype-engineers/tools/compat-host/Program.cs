// compat-host <plugin.dll> <Game2Dir>
// Loads the game build's assemblies and the plugin into this process (nothing of the game runs),
// then calls SE2Core.Compat.RunOffline() inside the plugin and prints its report.
// Exit: 0 ran, 2 usage, 3 plugin not on SE2Core, 4 the check itself crashed.
using System.Reflection;
using System.Runtime.Loader;

if (args.Length < 2)
{
    Console.Error.WriteLine("usage: compat-host <plugin.dll> <Game2Dir>");
    return 2;
}
string plugin = Path.GetFullPath(args[0]);
string game = Path.GetFullPath(args[1]);
string pluginDir = Path.GetDirectoryName(plugin)!;

// Plugin-local dependencies (0Harmony) first, then the game folder.
AssemblyLoadContext.Default.Resolving += (ctx, name) =>
{
    foreach (var dir in new[] { pluginDir, game })
    {
        string path = Path.Combine(dir, name.Name + ".dll");
        if (File.Exists(path))
            return ctx.LoadFromAssemblyPath(path);
    }
    return null;
};

// Load every game assembly up front so lookups by type name (AccessTools.TypeByName) see them all,
// exactly as they would in the running game.
foreach (var dll in Directory.GetFiles(game, "*.dll"))
{
    string n = Path.GetFileNameWithoutExtension(dll);
    if (!(n.StartsWith("VRage", StringComparison.Ordinal) || n.StartsWith("Game2", StringComparison.Ordinal) || n.StartsWith("Keen", StringComparison.Ordinal)))
        continue;
    try { AssemblyLoadContext.Default.LoadFromAssemblyPath(dll); }
    catch { /* native or duplicate - irrelevant for type lookups */ }
}

Assembly asm;
try { asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(plugin); }
catch (Exception ex)
{
    Console.WriteLine($"CRASH|plugin did not load: {ex.Message}");
    return 4;
}

var compat = asm.GetType("SE2Core.Compat");
if (compat?.GetMethod("RunOffline") is not MethodInfo run)
{
    Console.WriteLine("NOCORE|plugin does not use SE2Core - only the rebuild check applies");
    return 3;
}
try
{
    Console.Write((string)run.Invoke(null, null)!);
    return 0;
}
catch (TargetInvocationException ex)
{
    Console.WriteLine($"CRASH|{ex.InnerException?.GetType().Name}: {ex.InnerException?.Message}");
    return 4;
}
