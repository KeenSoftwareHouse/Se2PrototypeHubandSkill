// SE2Core - shared prototype core. VENDORED: edit the original in the prototype-engineers skill (core\),
// then run scripts\update-core.ps1. Do not edit a project's copy.
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using Keen.VRage.Core;
using Keen.VRage.Library.Utils;

namespace SE2Core;

/// <summary>Marks the class whose static <c>Declare()</c> defines the plugin's features.</summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class FeatureDeclarationsAttribute : Attribute { }

/// <summary>
/// One player-visible piece of a plugin ("Hold P to dock", "Ship class tab") together with every
/// game hook and game internal it needs. This is what lets a plugin survive game updates:
///
/// 1. CHECK BEFORE PATCHING. Every target and member is resolved before anything is patched. If any
///    is missing, that feature stays off and the log names exactly what's gone; nothing is
///    half-patched, and the plugin's other features still run.
/// 2. GUARDED HOOKS. Every patched game method gets a finalizer that catches exceptions thrown by
///    THIS plugin's patch code (never the game's own), so a bug or an API change can't crash the
///    game. A throwing prefix turns its feature off at once; other hooks after 5 errors.
/// 3. OFFLINE CHECK. scripts\compat-check.ps1 runs the same declarations against a new game build
///    without launching it.
///
/// <code>
/// [FeatureDeclarations]
/// internal static class Hooks
/// {
///     public static Feature Thrust = null!;
///     public static void Declare()
///     {
///         Thrust = Feature.Define("Flight feel", f =>
///         {
///             f.Postfix(typeof(ThrustComponent), "ComputeThrust", typeof(Hooks), nameof(ComputePostfix));
///             f.Prefix("Keen.Game2.Client.Foo", "Bar|BarOld", typeof(Hooks), nameof(BarPrefix));
///             ObservedBlock = f.NeedField(typeof(CockpitInputHandlerComponent), "_observedBlock");
///         });
///     }
/// }
/// </code>
/// Write targets as LITERALS (typeof / "Full.Type.Name" plus a "Method" string, patch method via
/// nameof), so a rename shows up as a clear OFF reason instead of a wrong patch.
/// Put each feature's game access inside its Define lambda or in Game\ adapter code it calls: a
/// lambda that hits a missing game type throws, and only that feature is switched off.
/// </summary>
internal sealed class Feature
{
    private static readonly List<Feature> Defined = new();
    private static readonly Dictionary<(Module, int), (Feature Owner, bool IsPrefix)> PatchOwners = new();
    private static readonly HashSet<MethodBase> Guarded = new();
    private static Harmony? _harmony;

    /// <summary>Raised for things a player should see (a feature switched off, untested game build).
    /// Default: log only. A plugin with a toast/HUD can subscribe.</summary>
    public static event Action<string>? Notice;

    /// <summary>True while compat-check runs declarations outside the game: nothing is patched.</summary>
    public static bool Offline { get; internal set; }

    public static IReadOnlyList<Feature> All => Defined;

    public string Name { get; }
    /// <summary>True once installed with every requirement met, until a guarded error trips it.</summary>
    public bool On { get; private set; }
    public string? OffReason { get; private set; }

    private readonly Action<Feature> _declare;
    private readonly Feature[] _dependsOn;
    private readonly List<string> _missing = new();
    private readonly List<PatchSpec> _specs = new();
    private readonly List<(MethodBase Target, MethodInfo Patch)> _applied = new();
    private int _errors;

    private Feature(string name, Action<Feature> declare, Feature[] dependsOn)
    {
        Name = name;
        _declare = declare;
        _dependsOn = dependsOn;
    }

    public static Feature Define(string name, Action<Feature> declare, params Feature[] dependsOn)
    {
        var f = new Feature(name, declare, dependsOn);
        Defined.Add(f);
        return f;
    }

    // ---------------------------------------------------------------- patches

    // allOverloads: patch EVERY overload of the name (e.g. all ".ctor"s) instead of requiring exactly one.
    public void Prefix(Type target, string method, Type hooks, string patch, Type[]? args = null, Func<MethodBase, bool>? pick = null, bool allOverloads = false)
        => Add(target, null, method, hooks, patch, PatchKind.Prefix, args, pick, allOverloads);
    public void Prefix(string targetType, string method, Type hooks, string patch, Type[]? args = null, Func<MethodBase, bool>? pick = null, bool allOverloads = false)
        => Add(null, targetType, method, hooks, patch, PatchKind.Prefix, args, pick, allOverloads);
    public void Postfix(Type target, string method, Type hooks, string patch, Type[]? args = null, Func<MethodBase, bool>? pick = null, bool allOverloads = false)
        => Add(target, null, method, hooks, patch, PatchKind.Postfix, args, pick, allOverloads);
    public void Postfix(string targetType, string method, Type hooks, string patch, Type[]? args = null, Func<MethodBase, bool>? pick = null, bool allOverloads = false)
        => Add(null, targetType, method, hooks, patch, PatchKind.Postfix, args, pick, allOverloads);

    // ---------------------------------------------------------------- requirements (no patch)

    public Type? NeedType(string names) => Track(Bind.Type(names, out var why), why);
    public FieldInfo? NeedField(Type? type, string names) => Track(Bind.Field(type, names, out var why), why);
    public FieldInfo? NeedField(string typeNames, string names) => NeedField(NeedType(typeNames), names);
    public PropertyInfo? NeedProperty(Type? type, string names) => Track(Bind.Property(type, names, out var why), why);
    public PropertyInfo? NeedProperty(string typeNames, string names) => NeedProperty(NeedType(typeNames), names);
    public MethodBase? NeedMethod(Type? type, string names, Type[]? args = null, Func<MethodBase, bool>? pick = null)
        => Track(Bind.Method(type, names, args, pick, out var why), why);
    public MethodBase? NeedMethod(string typeNames, string names, Type[]? args = null, Func<MethodBase, bool>? pick = null)
        => NeedMethod(NeedType(typeNames), names, args, pick);

    /// <summary>Any other condition, e.g. a definition GUID that must exist. Must be pure reflection /
    /// lookup - it also runs in compat-check, outside the game.</summary>
    public void Need(string what, Func<bool> check)
    {
        try { if (!check()) _missing.Add(what); }
        catch (Exception ex) { _missing.Add($"{what}: {Describe(ex)}"); }
    }

    private T? Track<T>(T? found, string? why) where T : class
    {
        if (found == null && why != null)
            _missing.Add(why);
        return found;
    }

    // ---------------------------------------------------------------- running feature code

    /// <summary>Runs feature code (ticks, UI, timers) only while the feature is on; an exception counts
    /// toward switching it off instead of escaping.</summary>
    public void Run(Action action)
    {
        if (!On) return;
        try { action(); }
        catch (Exception ex) { Trip(ex, immediate: false); }
    }

    public T Run<T>(Func<T> action, T fallback)
    {
        if (!On) return fallback;
        try { return action(); }
        catch (Exception ex) { Trip(ex, immediate: false); return fallback; }
    }

    // ---------------------------------------------------------------- install

    /// <summary>Declare, check and patch every feature. Call once, from the pump, after VRageCore.HasValue.</summary>
    public static void InstallAll(Harmony harmony)
    {
        _harmony = harmony;
        DeclareAndCheck();

        // In definition order, so a dependency is applied (or has failed) before what depends on it.
        foreach (var f in Defined)
            if (f._missing.Count == 0 && f._dependsOn.All(d => d.On))
                f.Apply();
        foreach (var f in Defined.Where(f => !f.On && f.OffReason == null))
            f.OffReason = f._missing.Count > 0 ? string.Join("; ", f._missing) : f.DepReason();

        Log.Write(GameVersion.Describe());
        foreach (var f in Defined)
            Log.Write(f.On ? $"Feature \"{f.Name}\": ON ({f._applied.Count} hooks)" : $"Feature \"{f.Name}\": OFF - {f.OffReason}");

        foreach (var u in Unattributed)
            Log.Write($"WARNING: uses {u}, which changed in this game build - that code will fail when it runs; rebuild the plugin for this build.");

        var off = Defined.Where(f => !f.On).ToList();
        if (Unattributed.Count > 0)
            RaiseNotice($"{Log.Short}: parts of it use game code that changed in this build - rebuild it for this game version. See the plugin log.");
        if (off.Count > 0)
            RaiseNotice($"{Log.Short}: {off.Count} of {Defined.Count} features unavailable on this game build ({string.Join(", ", off.Select(f => f.Name))}). See the plugin log.");
        else if (GameVersion.IsNewerThanBuilt)
            RaiseNotice($"{Log.Short}: running on a newer game build than it was made for - all checks passed.");
    }

    /// <summary>Runs every [FeatureDeclarations] Declare() and resolves all requirements. No patching.</summary>
    internal static void DeclareAndCheck()
    {
        if (Defined.Count == 0)
        {
            foreach (var t in typeof(Feature).Assembly.GetTypes().Where(t => t.GetCustomAttribute<FeatureDeclarationsAttribute>() != null))
            {
                var declare = t.GetMethod("Declare", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (declare == null) { Log.Write($"{t.Name} has [FeatureDeclarations] but no static Declare()"); continue; }
                try { declare.Invoke(null, null); }
                catch (TargetInvocationException ex) { Log.Write($"{t.Name}.Declare() failed: {ex.InnerException}"); }
            }
        }

        foreach (var f in Defined)
        {
            try { f._declare(f); }
            catch (Exception ex) { f._missing.Add(Describe(ex)); }
            foreach (var spec in f._specs)
                spec.Resolve(f._missing);
        }

        // Game members our compiled code uses that this build changed (would fail on first use).
        List<References.Broken> broken;
        try { broken = References.Check(); }
        catch (Exception ex) { References.Stats = $"failed: {ex.GetType().Name}: {ex.Message}"; Log.Write($"Reference check skipped: {ex.Message}"); broken = new(); }
        Log.Write($"Reference check: {References.Stats}");
        foreach (var b in broken)
        {
            var owners = b.UsedIn.Select(OwnerOfMethod).ToList();
            foreach (var f in owners.OfType<Feature>().Distinct())
                f._missing.Add($"uses {b.Member}, which changed in this game build (rebuild the plugin for it)");
            if (b.UsedIn.Count == 0 || owners.Any(o => o == null))
                Unattributed.Add($"{b.Member} (used in {string.Join(", ", b.UsedIn.Select(m => $"{m.DeclaringType?.Name}.{m.Name}").Distinct())})");
        }
    }

    /// <summary>Changed game members used by code no single feature owns - reported, can't be switched off.</summary>
    internal static readonly List<string> Unattributed = new();

    /// <summary>The feature a method belongs to: one of its patch/declare methods, or any method of a class
    /// only that feature registers methods in (e.g. a helper next to its hooks). Null when shared or unknown.</summary>
    private static Feature? OwnerOfMethod(MethodBase m)
    {
        foreach (var f in Defined)
            if (f.RegisteredMethods().Any(r => SameMethod(r, m)))
                return f;
        var type = Outermost(m.DeclaringType);
        var owners = Defined.Where(f => f.RegisteredMethods().Any(r => Outermost(r.DeclaringType) == type)).ToList();
        return owners.Count == 1 ? owners[0] : null;
    }

    private IEnumerable<MethodBase> RegisteredMethods()
    {
        yield return _declare.Method;
        foreach (var s in _specs)
            if (s.PatchMethod != null)
                yield return s.PatchMethod;
    }

    private static bool SameMethod(MethodBase a, MethodBase b) => a.Module == b.Module && a.MetadataToken == b.MetadataToken;

    private static Type? Outermost(Type? t)
    {
        while (t?.DeclaringType != null)
            t = t.DeclaringType;
        return t;
    }

    private void Apply()
    {
        if (Offline || _harmony == null)
            return;
        try
        {
            foreach (var spec in _specs)
            {
                var hm = new HarmonyMethod(spec.PatchMethod);
                foreach (var target in spec.Targets)
                {
                    _harmony.Patch(target,
                        prefix: spec.Kind == PatchKind.Prefix ? hm : null,
                        postfix: spec.Kind == PatchKind.Postfix ? hm : null);
                    _applied.Add((target, spec.PatchMethod!));
                    AddGuard(target);
                }
                lock (PatchOwners)
                    PatchOwners[(spec.PatchMethod!.Module, spec.PatchMethod.MetadataToken)] = (this, spec.Kind == PatchKind.Prefix);
            }
            On = true;
        }
        catch (Exception ex)
        {
            OffReason = $"patching failed: {Describe(ex)}";
            Unpatch();
        }
    }

    private static void AddGuard(MethodBase target)
    {
        lock (Guarded)
        {
            if (!Guarded.Add(target))
                return;
        }
        var guard = typeof(Feature).GetMethod(nameof(Guard), BindingFlags.NonPublic | BindingFlags.Static)!;
        _harmony!.Patch(target, finalizer: new HarmonyMethod(guard));
    }

    // ---------------------------------------------------------------- guard + circuit breaker

    /// <summary>Harmony finalizer on every target we patch. Swallows exceptions that came from THIS
    /// plugin's patch methods; anything thrown by the game (or another plugin) passes through untouched.</summary>
    private static Exception? Guard(Exception? __exception)
    {
        if (__exception == null)
            return null;
        var owner = OwnerOf(__exception);
        if (owner == null)
            return __exception;
        owner.Value.Owner.Trip(__exception, immediate: owner.Value.IsPrefix);
        return null;
    }

    private static (Feature Owner, bool IsPrefix)? OwnerOf(Exception ex)
    {
        var frames = new StackTrace(ex, false).GetFrames();
        lock (PatchOwners)
        {
            foreach (var frame in frames)
            {
                var m = frame.GetMethod();
                if (m != null && PatchOwners.TryGetValue((m.Module, m.MetadataToken), out var owner))
                    return owner;
            }
        }
        return null;
    }

    private void Trip(Exception ex, bool immediate)
    {
        int n = Interlocked.Increment(ref _errors);
        if (n == 1)
            Log.Write($"Feature \"{Name}\" error: {ex}");
        // A throwing prefix may have stopped the game's own method from running - stop at once.
        if (On && (immediate || n >= 5))
            TurnOff(immediate ? "a hook that runs before the game's code failed" : $"{n} errors");
    }

    private void TurnOff(string why)
    {
        On = false;
        OffReason = why;
        Log.Write($"Feature \"{Name}\" switched OFF: {why}. The rest of the plugin keeps running.");
        RaiseNotice($"{Log.Short}: \"{Name}\" switched off after an error. See the plugin log.");
        // Unpatch on the engine thread, never from inside a running hook.
        if (VRageCore.HasValue)
            Singleton<VRageCore>.Instance.UpdateQueue.Enqueue(Unpatch);
    }

    private void Unpatch()
    {
        if (_harmony == null)
            return;
        foreach (var (target, patch) in _applied)
        {
            try { _harmony.Unpatch(target, patch); }
            catch (Exception ex) { Log.Write($"Unpatch {target.Name} failed: {ex.Message}"); }
        }
        _applied.Clear();
    }

    private static void RaiseNotice(string message)
    {
        Log.Write("NOTICE: " + message);
        try { Notice?.Invoke(message); }
        catch (Exception ex) { Log.Write($"Notice handler failed: {ex.Message}"); }
    }

    // ---------------------------------------------------------------- helpers

    private bool DepsOn() => _dependsOn.All(d => d._missing.Count == 0 && d.DepsOn());
    private string DepReason() => "needs " + string.Join(", ", _dependsOn.Where(d => !d.On).Select(d => $"\"{d.Name}\""));
    private string OfflineDepReason() => "needs " + string.Join(", ", _dependsOn.Where(d => d._missing.Count > 0 || !d.DepsOn()).Select(d => $"\"{d.Name}\""));

    internal string Report() => _missing.Count == 0 && DepsOn() ? "OK" : (_missing.Count > 0 ? string.Join("; ", _missing) : OfflineDepReason());
    internal bool Passes => _missing.Count == 0 && DepsOn();

    internal static string Describe(Exception ex)
    {
        ex = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
        return ex switch
        {
            TypeLoadException tle => $"game type {tle.TypeName} is gone",
            MissingMemberException mme => $"game member missing ({mme.Message})",
            FileNotFoundException fnf => $"game assembly missing ({fnf.FileName})",
            _ => $"{ex.GetType().Name}: {ex.Message}",
        };
    }

    private void Add(Type? target, string? targetTypeName, string method, Type hooks, string patch, PatchKind kind,
        Type[]? args, Func<MethodBase, bool>? pick, bool allOverloads)
        => _specs.Add(new PatchSpec(target, targetTypeName, method, hooks, patch, kind, args, pick, allOverloads));

    private enum PatchKind { Prefix, Postfix }

    private sealed class PatchSpec(Type? target, string? targetTypeName, string method, Type hooks, string patch,
        PatchKind kind, Type[]? args, Func<MethodBase, bool>? pick, bool allOverloads)
    {
        public PatchKind Kind { get; } = kind;
        public List<MethodBase> Targets { get; } = new();
        public MethodInfo? PatchMethod { get; private set; }

        public void Resolve(List<string> missing)
        {
            var type = target;
            if (type == null)
            {
                type = Bind.Type(targetTypeName!, out var whyType);
                if (type == null) { missing.Add(whyType!); return; }
            }
            if (allOverloads)
            {
                var all = Bind.Methods(type, method, pick);
                if (all.Count == 0) { missing.Add($"{Bind.Short(type)}.{method} not found"); return; }
                Targets.AddRange(all);
            }
            else
            {
                var one = Bind.Method(type, method, args, pick, out var why);
                if (one == null) { missing.Add(why!); return; }
                Targets.Add(one);
            }

            PatchMethod = hooks.GetMethod(patch, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (PatchMethod == null) { missing.Add($"patch method {hooks.Name}.{patch} not found (plugin bug)"); return; }

            // Harmony matches patch parameters to the target BY NAME. A renamed game parameter
            // makes Patch() throw at runtime; catch it here so compat-check sees it too.
            foreach (var t in Targets)
            {
                var targetParams = t.GetParameters().Select(p => p.Name).ToHashSet();
                foreach (var p in PatchMethod.GetParameters())
                {
                    string name = p.Name ?? "";
                    if (name.StartsWith("___"))
                    {
                        if (AccessTools.Field(t.DeclaringType, name[3..]) == null)
                            missing.Add($"{Bind.Short(type)}.{t.Name}: field {name[3..]} (used by {patch}) is gone");
                    }
                    else if (name.StartsWith("__"))
                    {
                        continue; // __instance, __result, __state, __args, __0 ...
                    }
                    else if (!targetParams.Contains(name))
                    {
                        missing.Add($"{Bind.Short(type)}.{t.Name} no longer has parameter '{name}' (used by {patch})");
                    }
                }
            }
        }
    }
}
