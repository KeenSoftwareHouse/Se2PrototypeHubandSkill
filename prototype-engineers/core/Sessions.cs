// SE2Core - shared prototype core. VENDORED: edit the original in the prototype-engineers skill (core\),
// then run scripts\update-core.ps1. Do not edit a project's copy.
using Keen.Game2.Game.SessionComponents;
using Keen.Game2.Simulation.RuntimeSystems.CoreScenes;
using Keen.VRage.Core;
using Keen.VRage.Core.Game.Systems;
using Keen.VRage.DCS.Components;
using Keen.VRage.Library.Utils;

namespace SE2Core;

/// <summary>
/// Finds the client and server world sessions. Call <see cref="Refresh"/> on the engine thread.
///
/// Single player runs BOTH in one process: the client session (what you see, UI, particles,
/// planets, input) and an in-process server session (authoritative simulation: grid movement,
/// thrust, resources, production, ownership). Most [ServerOnly] gameplay state only exists on the
/// server copy. In single player <c>GameCoreScene.GameServer</c> is null, so the server world is
/// found by scanning the core scene for the other WorldSessionComponent.
/// </summary>
internal static class Sessions
{
    public static Session? Client { get; private set; }
    public static Session? Server { get; private set; }

    public static void Refresh()
    {
        Session? server = null, client = null;
        Entity? engine = Singleton<VRageCore>.Instance.Engine;
        if (engine?.Scene?.UserObject is GameCoreScene coreScene)
        {
            Entity? clientEntity = coreScene.GameClient;
            client = clientEntity?.TryGet<WorldSessionComponent>()?.OwnedSession;
            server = coreScene.GameServer?.TryGet<WorldSessionComponent>()?.OwnedSession;
            if (server == null)
            {
                foreach (Entity e in coreScene.Entities)
                {
                    if (ReferenceEquals(e, clientEntity))
                        continue;
                    var s = e.TryGet<WorldSessionComponent>()?.OwnedSession;
                    if (s != null && !ReferenceEquals(s, client))
                    {
                        server = s;
                        break;
                    }
                }
            }
        }

        if (!ReferenceEquals(client, Client))
            Log.Write(client != null ? "Client session attached." : "Client session gone.");
        if (!ReferenceEquals(server, Server))
            Log.Write(server != null ? "Server session attached." : "No server session (menu, or joined someone else's game).");
        Client = client;
        Server = server;
    }
}
