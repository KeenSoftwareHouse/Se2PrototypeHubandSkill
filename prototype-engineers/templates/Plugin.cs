using Keen.VRage.Core;
using Keen.VRage.Core.Plugins;
using Keen.VRage.Library.Utils;
using SE2Core;

namespace __NAME__;

/// <summary>
/// SE2 prototype: __SHORT__.
///
/// TODO: one paragraph on what the player experiences and how the prototype does it.
///
/// Layout (keeps game updates cheap - see the skill's "Surviving game updates"):
///   Game\      the ONLY code that touches the game: feature declarations + hooks (Game\Hooks.cs)
///              and reads/writes of game state. A game update means fixing this folder.
///   *.cs       the prototype's own logic, on plain data. No Keen.* namespaces except math.
///   SE2Core\   shared core, vendored from the skill (log, sessions, features, compat). Don't edit.
///
/// Load: SpaceEngineers2.exe "-plugins:C:\path\to\__NAME__.dll"
/// Log:  %APPDATA%\SpaceEngineers2\__SHORT__\__SHORT__*.log
/// </summary>
public class __SHORT__Plugin : IPlugin, IDisposable
{
    private readonly Timer _timer;
    private bool _installed;
    private int _tickQueued;
    private volatile bool _disposed;

    public __SHORT__Plugin(PluginHost host)
    {
        // Keep the constructor tiny. The game relaunches itself once at startup, so this runs in
        // a bootstrapper process too, long before the engine or Avalonia exist. Never patch,
        // bind ports or touch game/Avalonia types here.
        Log.Init("__SHORT__");
        Log.Write("Plugin loaded.");
        _timer = new Timer(_ => Pump(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1));
    }

    private void Pump()
    {
        try
        {
            // Only the process that brings up the engine gets past this check.
            if (!_disposed && VRageCore.HasValue)
            {
                if (!_installed)
                {
                    _installed = true;
                    // Declares every feature, checks all its game hooks/members exist, then patches
                    // only the features that are complete. Logs one ON/OFF line per feature.
                    try { Feature.InstallAll(new HarmonyLib.Harmony("se2.__SHORT__".ToLowerInvariant())); }
                    catch (Exception ex) { Log.Write($"Install failed: {ex}"); }
                }

                // Game state is only safe to touch on the engine thread: queue a 1 Hz tick there.
                if (Interlocked.CompareExchange(ref _tickQueued, 1, 0) == 0)
                {
                    Singleton<VRageCore>.Instance.UpdateQueue.Enqueue(() =>
                    {
                        Interlocked.Exchange(ref _tickQueued, 0);
                        if (_disposed)
                            return;
                        try { Sessions.Refresh(); }
                        catch (Exception ex) { Log.Write($"Session refresh failed: {ex.Message}"); }
                        global::__NAME__.Game.Hooks.Tick();
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Log.Write($"Pump error: {ex.Message}");
        }
        Log.Flush();
    }

    public void Dispose()
    {
        _disposed = true;
        _timer.Dispose();
        Log.Close();
    }
}
