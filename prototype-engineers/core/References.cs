// SE2Core - shared prototype core. VENDORED: edit the original in the prototype-engineers skill (core\),
// then run scripts\update-core.ps1. Do not edit a project's copy.
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace SE2Core;

/// <summary>
/// Finds the game types/members this plugin's COMPILED code refers to that the running game build
/// doesn't have - e.g. a field whose type changed, or a method whose signature changed. Plain C#
/// that compiled against the old build fails on those only when it first runs (MissingFieldException,
/// MissingMethodException), so a feature can look fine at startup and break when used. This resolves
/// every such reference up front, exactly as the runtime would, and reports which code uses it.
/// </summary>
internal static class References
{
    /// <summary>One broken reference: what it is, and the methods of this plugin that use it.</summary>
    internal sealed record Broken(string Member, List<MethodBase> UsedIn);

    /// <summary>What the last Check() looked at (for compat-check output).</summary>
    internal static string Stats = "not run";

    internal static List<Broken> Check()
    {
        var result = new List<Broken>();
        var asm = typeof(References).Assembly;
        var module = asm.ManifestModule;
        string path = asm.Location;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Stats = "skipped: plugin file not found";
            return result;
        }

        var badTokens = new Dictionary<int, string>();
        int checkedTypes = 0, checkedMembers = 0, unchecked_ = 0;
        using (var pe = new PEReader(File.OpenRead(path)))
        {
            var md = pe.GetMetadataReader();

            foreach (var h in md.TypeReferences)
            {
                var tr = md.GetTypeReference(h);
                if (!IsGameAssembly(md, tr.ResolutionScope))
                    continue;
                int token = MetadataTokens.GetToken(h);
                checkedTypes++;
                try { module.ResolveType(token); }
                catch (Exception ex) when (IsMissing(ex)) { badTokens[token] = TypeName(md, h); }
                catch (ArgumentException) { unchecked_++; } // needs a generic context - can't resolve standalone
            }

            foreach (var h in md.MemberReferences)
            {
                var mr = md.GetMemberReference(h);
                if (mr.Parent.Kind != HandleKind.TypeReference)
                    continue; // members of generic instantiations / own types: not checked
                var parent = (TypeReferenceHandle)mr.Parent;
                if (!IsGameAssembly(md, md.GetTypeReference(parent).ResolutionScope))
                    continue;
                int token = MetadataTokens.GetToken(h);
                checkedMembers++;
                try { module.ResolveMember(token); }
                catch (Exception ex) when (IsMissing(ex))
                {
                    badTokens[token] = $"{TypeName(md, parent)}.{md.GetString(mr.Name)}";
                }
                catch (ArgumentException)
                {
                    // The parent is a plain (non-generic) type, so no generic context is involved: the
                    // runtime couldn't match the reference's signature. For a field that means its type
                    // changed (e.g. Entity? -> NullOnReplicationFail<Entity>); for a method, only when no
                    // method of that name exists any more - otherwise it can't be judged standalone.
                    string name = md.GetString(mr.Name);
                    bool changed = mr.GetKind() == MemberReferenceKind.Field || !MethodNameExists(module, parent, name);
                    if (changed)
                        badTokens[token] = $"{TypeName(md, parent)}.{name}";
                    else
                        unchecked_++;
                }
            }
        }
        Stats = $"checked {checkedTypes} game types and {checkedMembers} game members, {badTokens.Count} changed ({unchecked_} not checkable standalone)";
        if (badTokens.Count == 0)
            return result;

        // Which of our methods use them: look for the token in each method body's IL.
        var users = badTokens.Keys.ToDictionary(t => t, _ => new List<MethodBase>());
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        Type[] types;
        try { types = asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray()!; }
        foreach (var type in types)
        {
            IEnumerable<MethodBase> methods;
            try { methods = type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all)).ToList(); }
            catch { continue; }
            foreach (var m in methods)
            {
                byte[]? il;
                try { il = m.GetMethodBody()?.GetILAsByteArray(); }
                catch { continue; }
                if (il == null)
                    continue;
                foreach (var token in badTokens.Keys)
                    if (ContainsToken(il, token))
                        users[token].Add(m);
            }
        }
        foreach (var (token, member) in badTokens)
            result.Add(new Broken(member, users[token]));
        return result;
    }

    private static bool MethodNameExists(Module module, TypeReferenceHandle parent, string name)
    {
        try
        {
            var type = module.ResolveType(MetadataTokens.GetToken(parent));
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            return name == ".ctor" ? type.GetConstructors(all).Length > 0 : type.GetMethods(all).Any(m => m.Name == name);
        }
        catch { return false; }
    }

    private static bool IsMissing(Exception ex) =>
        ex is MissingMemberException or TypeLoadException or FileNotFoundException or BadImageFormatException
        || ex.InnerException is MissingMemberException or TypeLoadException;

    private static bool IsGameAssembly(MetadataReader md, EntityHandle scope)
    {
        while (scope.Kind == HandleKind.TypeReference) // nested type: walk out to its declaring type
            scope = md.GetTypeReference((TypeReferenceHandle)scope).ResolutionScope;
        if (scope.Kind != HandleKind.AssemblyReference)
            return false;
        string name = md.GetString(md.GetAssemblyReference((AssemblyReferenceHandle)scope).Name);
        return name.StartsWith("VRage", StringComparison.Ordinal) || name.StartsWith("Game2", StringComparison.Ordinal)
               || name.StartsWith("Keen", StringComparison.Ordinal) || name == "SpaceEngineers2";
    }

    private static string TypeName(MetadataReader md, TypeReferenceHandle h)
    {
        var tr = md.GetTypeReference(h);
        string name = md.GetString(tr.Name);
        if (tr.ResolutionScope.Kind == HandleKind.TypeReference)
            return TypeName(md, (TypeReferenceHandle)tr.ResolutionScope) + "." + name;
        return name; // short name reads better in a log line
    }

    private static bool ContainsToken(byte[] il, int token)
    {
        byte b0 = (byte)token, b1 = (byte)(token >> 8), b2 = (byte)(token >> 16), b3 = (byte)(token >> 24);
        for (int i = 0; i + 3 < il.Length; i++)
            if (il[i] == b0 && il[i + 1] == b1 && il[i + 2] == b2 && il[i + 3] == b3)
                return true;
        return false;
    }
}
