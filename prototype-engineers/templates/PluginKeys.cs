// Rebindable keys for this plugin.
using System.Text.Json;
using Keen.VRage.Core.Input;

namespace __NAME__;

/// <summary>
/// Keys a player can rebind without rebuilding. Resolve each key ONCE and poll the result:
/// <code>
///   static readonly DigitalInput ToggleKey = PluginKeys.Get("toggle", "Keyboard::Numpad5");
///   ...
///   bool down = kb.GetDigitalState(ToggleKey);
///   Toast($"Press {PluginKeys.Label("toggle", "Keyboard::Numpad5")} to close");
/// </code>
/// To rebind, the player puts a keys.json next to the plugin DLL: {"toggle": "Keyboard::F6"}.
/// Without the file, or for an id it doesn't mention or can't parse, the default is used. What was
/// resolved is written to keys.log beside it.
/// </summary>
internal static class PluginKeys
{
    private static readonly object Lock = new();
    private static Dictionary<string, string>? _overrides;
    private static readonly SortedDictionary<string, string> Resolved = new(StringComparer.Ordinal);

    /// <summary>A keyboard or mouse button. <paramref name="defaultInput"/> uses the game's names: "Keyboard::Numpad5", "Mouse::Middle".</summary>
    public static DigitalInput Get(string id, string defaultInput)
    {
        string name = Resolve(id, defaultInput);
        if (TryDigital(name, out var input))
            return input;
        Note(id, $"{name} is not a valid key - using {defaultInput}");
        return TryDigital(defaultInput, out input) ? input : default;
    }

    /// <summary>For plugins that poll Windows directly (GetAsyncKeyState): the Win32 virtual-key code.
    /// Only keyboard keys - the game's keyboard input index IS the virtual-key code.</summary>
    public static int GetVirtualKey(string id, string defaultInput)
    {
        string name = Resolve(id, defaultInput);
        var parsed = name.StartsWith("Keyboard::", StringComparison.Ordinal) ? KeyboardInputs.TryGetInputFromName(name) : null;
        if (parsed is InputId k)
            return k.Index;
        Note(id, $"{name} is not a keyboard key - using {defaultInput}");
        return KeyboardInputs.TryGetInputFromName(defaultInput) is InputId d ? d.Index : 0;
    }

    /// <summary>The current key's short name for messages ("Numpad5", "F6", "Middle mouse").</summary>
    public static string Label(string id, string defaultInput)
    {
        string name = Resolve(id, defaultInput);
        int sep = name.IndexOf("::", StringComparison.Ordinal);
        string key = sep < 0 ? name : name[(sep + 2)..];
        return name.StartsWith("Mouse::", StringComparison.Ordinal) ? $"{key} mouse" : key;
    }

    private static bool TryDigital(string name, out DigitalInput input)
    {
        input = default;
        InputId? id = name.StartsWith("Mouse::", StringComparison.Ordinal)
            ? MouseInputs.TryGetInputFromName(name)
            : KeyboardInputs.TryGetInputFromName(name);
        if (id is not InputId value || value.Type != InputType.Digital)
            return false;
        input = new DigitalInput(value);
        return true;
    }

    private static string Resolve(string id, string defaultInput)
    {
        lock (Lock)
        {
            _overrides ??= Load();
            string name = _overrides.TryGetValue(id, out var o) && o.Length > 0 ? o : defaultInput;
            if (!Resolved.TryGetValue(id, out var prev) || prev != name)
            {
                Resolved[id] = name;
                WriteLog();
            }
            return name;
        }
    }

    private static string Folder => Path.GetDirectoryName(typeof(PluginKeys).Assembly.Location) ?? AppContext.BaseDirectory;

    private static Dictionary<string, string> Load()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            string path = Path.Combine(Folder, "keys.json");
            if (!File.Exists(path))
                return map;
            using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            foreach (var p in doc.RootElement.EnumerateObject())
                if (p.Value.ValueKind == JsonValueKind.String)
                    map[p.Name] = p.Value.GetString()!;
        }
        catch (Exception ex)
        {
            Resolved["(error)"] = $"keys.json unreadable, using defaults: {ex.Message}";
        }
        return map;
    }

    private static void Note(string id, string message)
    {
        lock (Lock)
        {
            Resolved[id] = message;
            WriteLog();
        }
    }

    private static void WriteLog()
    {
        try { File.WriteAllLines(Path.Combine(Folder, "keys.log"), Resolved.Select(kv => $"{kv.Key} = {kv.Value}")); }
        catch { } // read-only install folder: the log is only a debugging aid
    }
}
