// SE2Core - shared prototype core. VENDORED from the prototype-engineers skill (core\). It changes
// only with a skill update; scripts\update-core.ps1 then refreshes this copy. Do not edit either.
using System.Reflection;
using HarmonyLib;

namespace SE2Core;

/// <summary>
/// Late, tolerant lookup of game members by name - for the game's PRIVATE or unstable parts,
/// which are what change between builds. Names may list alternatives separated by '|'
/// ("_observedBlock|_observedEntity"): the first that exists wins, so a rename only needs the new
/// name added, and the plugin keeps working on both builds.
///
/// Methods are matched by name; pass <c>args</c> or <c>pick</c> only when the name is overloaded.
/// A name that is overloaded and not disambiguated FAILS rather than guessing.
///
/// Stable PUBLIC API should stay compile-time bound (plain C#): a break there shows up as a
/// compile error in compat-check, which is what you want.
/// </summary>
internal static class Bind
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                     BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static IEnumerable<string> Names(string names) =>
        names.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    public static Type? Type(string names, out string? why)
    {
        foreach (var n in Names(names))
            if (AccessTools.TypeByName(n) is Type t) { why = null; return t; }
        why = $"type {names} not found";
        return null;
    }

    public static MethodBase? Method(Type? type, string names, Type[]? args, Func<MethodBase, bool>? pick, out string? why)
    {
        if (type == null) { why = $"{names}: declaring type missing"; return null; }
        foreach (var name in Names(names))
        {
            var found = Candidates(type, name).ToList();
            if (args != null)
                found = found.Where(m => m.GetParameters().Select(p => p.ParameterType).SequenceEqual(args)).ToList();
            if (pick != null)
                found = found.Where(pick).ToList();
            if (found.Count == 1) { why = null; return found[0]; }
            if (found.Count > 1)
            {
                why = $"{Short(type)}.{name} has {found.Count} overloads - pass args or pick";
                return null;
            }
        }
        why = $"{Short(type)}.{names} not found";
        return null;
    }

    /// <summary>Every overload of the first name that exists (for patching all constructors etc.).</summary>
    public static List<MethodBase> Methods(Type? type, string names, Func<MethodBase, bool>? pick)
    {
        if (type == null) return new();
        foreach (var name in Names(names))
        {
            var found = Candidates(type, name).Where(m => pick == null || pick(m)).ToList();
            if (found.Count > 0) return found;
        }
        return new();
    }

    public static FieldInfo? Field(Type? type, string names, out string? why)
    {
        if (type == null) { why = $"{names}: declaring type missing"; return null; }
        foreach (var n in Names(names))
            if (AccessTools.Field(type, n) is FieldInfo f) { why = null; return f; }
        why = $"field {Short(type)}.{names} not found";
        return null;
    }

    public static PropertyInfo? Property(Type? type, string names, out string? why)
    {
        if (type == null) { why = $"{names}: declaring type missing"; return null; }
        foreach (var n in Names(names))
            if (AccessTools.Property(type, n) is PropertyInfo p) { why = null; return p; }
        why = $"property {Short(type)}.{names} not found";
        return null;
    }

    /// <summary>Methods (or ".ctor" constructors) with this name, from the most-derived type that declares any.</summary>
    private static IEnumerable<MethodBase> Candidates(Type type, string name)
    {
        if (name == ".ctor")
            return type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        for (var t = type; t != null; t = t.BaseType)
        {
            var here = t.GetMethods(All).Where(m => m.Name == name).ToList();
            if (here.Count > 0)
                return here;
        }
        return Array.Empty<MethodBase>();
    }

    /// <summary>
    /// Reads a field or property by name and returns it as <typeparamref name="T"/>, surviving the
    /// common "type changed" update: field became property (or back), or the value got wrapped (e.g.
    /// <c>Entity?</c> became <c>NullOnReplicationFail&lt;Entity&gt;</c>) - a wrapper is unwrapped through an
    /// implicit conversion or a member of type T. Use instead of plain C# member access for game
    /// members that change between builds; plain access fails with MissingFieldException there.
    /// Returns default when the member is gone or the value can't be turned into T.
    /// </summary>
    public static T? ReadAs<T>(object? instance, string names)
    {
        if (instance == null)
            return default;
        var type = instance.GetType();
        foreach (var n in Names(names))
        {
            object? raw;
            if (AccessTools.Field(type, n) is FieldInfo f) raw = f.GetValue(instance);
            else if (AccessTools.Property(type, n) is PropertyInfo p && p.GetIndexParameters().Length == 0) raw = p.GetValue(instance);
            else continue;
            return Unwrap<T>(raw);
        }
        return default;
    }

    private static T? Unwrap<T>(object? raw, int depth = 0)
    {
        if (raw is T t)
            return t;
        if (raw == null || depth > 2)
            return default;
        var type = raw.GetType();
        // implicit/explicit conversion operator to T
        foreach (var op in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            if (op.Name is "op_Implicit" or "op_Explicit" && op.ReturnType == typeof(T) && op.GetParameters().Length == 1)
                return (T?)op.Invoke(null, new[] { raw });
        // a single member holding the wrapped value (Value, Item, ...)
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            if (p.GetIndexParameters().Length == 0 && (typeof(T).IsAssignableFrom(p.PropertyType) || Nullable.GetUnderlyingType(p.PropertyType) == typeof(T)))
                return Unwrap<T>(p.GetValue(raw), depth + 1);
        foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            if (typeof(T).IsAssignableFrom(f.FieldType) || Nullable.GetUnderlyingType(f.FieldType) == typeof(T))
                return Unwrap<T>(f.GetValue(raw), depth + 1);
        return default;
    }

    public static string Short(Type? t) => t == null ? "?" : t.Name;
}
