// SE2Core - shared prototype core. VENDORED from the prototype-engineers skill (core\). It changes
// only with a skill update; scripts\update-core.ps1 then refreshes this copy. Do not edit either.
using System.Text;

namespace SE2Core;

/// <summary>
/// Entry point for scripts\compat-check.ps1. Its small host loads this plugin plus a game build's
/// DLLs (without starting the game), calls <see cref="RunOffline"/> by reflection, and prints the
/// result. Runs the same feature declarations and checks the game would, but patches nothing.
/// </summary>
public static class Compat
{
    public const string CoreVersion = "1.2.1";

    /// <summary>Returns one line per feature: "OK|name" or "OFF|name|reason". First line is "GAME|version".</summary>
    public static string RunOffline()
    {
        Feature.Offline = true;
        Feature.DeclareAndCheck();
        var sb = new StringBuilder();
        sb.AppendLine($"GAME|{GameVersion.Running}|built {GameVersion.BuiltAgainst}|core {CoreVersion}");
        foreach (var f in Feature.All)
            sb.AppendLine(f.Passes ? $"OK|{f.Name}" : $"OFF|{f.Name}|{f.Report()}");
        sb.AppendLine($"REFS|{References.Stats}");
        foreach (var u in Feature.Unattributed)
            sb.AppendLine($"WARN|{u}|changed in this build, used by code outside any one feature");
        if (Feature.All.Count == 0)
            sb.AppendLine("NOFEATURES|no [FeatureDeclarations] found");
        return sb.ToString();
    }
}
