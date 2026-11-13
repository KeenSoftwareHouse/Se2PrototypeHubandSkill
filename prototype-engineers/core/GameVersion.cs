// SE2Core - shared prototype core. VENDORED: edit the original in the prototype-engineers skill (core\),
// then run scripts\update-core.ps1. Do not edit a project's copy.
using System.Reflection;

namespace SE2Core;

/// <summary>
/// Which game build this plugin was compiled against, and which one is running. Both come from
/// VRage.Core's assembly version.
///
/// Only one direction matters: a game NEWER than the build is the normal case after an update
/// (the plugin loads, and the feature checks decide what still works). A game OLDER than the build
/// never gets this far - the loader refuses the plugin ("Plugin NOT loaded" in the game log) - and
/// that is deliberately left alone.
/// </summary>
internal static class GameVersion
{
    public static Version? BuiltAgainst { get; } =
        typeof(GameVersion).Assembly.GetReferencedAssemblies().FirstOrDefault(a => a.Name == "VRage.Core")?.Version;

    /// <summary>The VRage.Core loaded in this process (the running game, or the build compat-check points at).</summary>
    public static Version? Running =>
        AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetName())
            .FirstOrDefault(n => n.Name == "VRage.Core")?.Version;

    /// <summary>True when the running game is a newer build than the one this plugin was tested on.</summary>
    public static bool IsNewerThanBuilt => Running != null && BuiltAgainst != null && Running > BuiltAgainst;

    public static string Describe() =>
        IsNewerThanBuilt
            ? $"built for game {BuiltAgainst}, running on newer {Running} (untested on this build)"
            : $"game {Running?.ToString() ?? "?"}";
}
