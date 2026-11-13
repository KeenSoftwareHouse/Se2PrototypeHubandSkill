// SE2Core - shared prototype core. VENDORED into each plugin's SE2Core\ folder.
// The original lives in the prototype-engineers skill (core\) and changes only with a skill update;
// scripts\update-core.ps1 then refreshes every project. Do not edit either copy.
using System.Collections.Concurrent;

namespace SE2Core;

/// <summary>
/// Thread-safe file log. Any thread may call <see cref="Write"/>; lines are queued and written by
/// <see cref="Flush"/> (the plugin's pump calls it), so game threads never wait on disk.
/// Output: %APPDATA%\SpaceEngineers2\&lt;Short&gt;\&lt;Short&gt;_&lt;time&gt;_&lt;pid&gt;.log - one file per process
/// (the game starts twice, so the newest file is the real game).
/// Never AllocConsole: a console window on the game's D3D12 present path can black-screen it.
/// </summary>
internal static class Log
{
    private static readonly ConcurrentQueue<string> Queue = new();
    private static readonly object WriterLock = new();
    private static StreamWriter? _writer;

    public static string Short { get; private set; } = "Plugin";
    public static string? Path { get; private set; }

    public static void Init(string shortName)
    {
        Short = shortName;
        try
        {
            string dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers2", shortName);
            Directory.CreateDirectory(dir);
            Path = System.IO.Path.Combine(dir, $"{shortName}_{DateTime.Now:yyyyMMdd_HHmmss}_{Environment.ProcessId}.log");
            lock (WriterLock)
                _writer = new StreamWriter(Path, append: false) { AutoFlush = true };
        }
        catch
        {
            _writer = null; // logging is best-effort; never take down the game
        }
    }

    /// <summary>Queue a line. Prefixed with [Short] automatically.</summary>
    public static void Write(string message) => Queue.Enqueue($"{DateTime.Now:HH:mm:ss.fff} [{Short}] {message}");

    public static void Flush()
    {
        lock (WriterLock)
        {
            while (Queue.TryDequeue(out var line))
            {
                try { _writer?.WriteLine(line); }
                catch { }
            }
        }
    }

    public static void Close()
    {
        Flush();
        lock (WriterLock)
        {
            try { _writer?.Dispose(); } catch { }
            _writer = null;
        }
    }
}
