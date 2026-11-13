using HarmonyLib;
using SE2Core;

namespace __NAME__.Game;

/// <summary>
/// The plugin's features and every game hook/member each one needs. SE2Core runs Declare() once
/// the engine is up, checks EVERYTHING exists before patching anything, then patches only the
/// features that are complete. A game update that removes something switches off just the feature
/// that needed it, with a log line naming what's gone, and scripts\compat-check.ps1 finds it
/// before anyone launches the new build.
///
/// Rules (they're what make this survive updates):
/// - Write targets as literals: typeof(T) or "Full.Type.Name", a "Method" string (use "New|Old"
///   for a renamed method), the patch via nameof.
/// - Patch methods take only the parameters they use. Harmony matches them BY NAME.
/// - Prefer a postfix; a prefix that returns bool can skip the game's code (and other plugins').
/// - Private members: f.NeedField / NeedMethod / NeedProperty (tolerant, checked up front).
///   Public API: plain C# is fine - a break there is a compile error in compat-check.
/// - Feature code outside hooks (ticks, UI): wrap in feature.Run(() => ...) so it stops cleanly
///   if the feature is off and can't crash the game.
/// </summary>
[FeatureDeclarations]
internal static class Hooks
{
    public static Feature Main = null!;

    public static void Declare()
    {
        Main = Feature.Define("__SHORT__", f =>
        {
            // Examples - replace with the real targets found via api-search -Decompile:
            //
            // f.Postfix(typeof(ThrustComponent), "ComputeThrust", typeof(Hooks), nameof(ComputeThrustPostfix));
            // f.Prefix("Keen.Game2.Client.UI.TerminalScreen.TerminalScreenViewModel", ".ctor", typeof(Hooks), nameof(TerminalCtor));
            // ObservedBlock = f.NeedField(typeof(CockpitInputHandlerComponent), "_observedBlock|_observedEntity");
        });
    }

    /// <summary>Engine thread, 1 Hz (called by the plugin's pump).</summary>
    public static void Tick() => Main.Run(() =>
    {
        // TODO: prototype logic that polls game state.
    });
}
