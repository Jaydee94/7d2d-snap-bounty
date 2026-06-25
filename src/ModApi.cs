using System.Reflection;
using HarmonyLib;

namespace SnapBounty
{
    // Einstiegspunkt des C#-Server-Mods (IModApi). Verdrahtet ModEvents + Harmony mit dem BountyManager.
    public class ModApi : IModApi
    {
        private int _biomeTick;

        public void InitMod(Mod _modInstance)
        {
            Log.Out("[SnapBounty] InitMod – registriere Handler & Harmony-Patches");

            Config.Load(_modInstance != null ? _modInstance.Path : null);

            var harmony = new Harmony("com.jaydee.snapbounty");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
            ModEvents.GameShutdown.RegisterHandler(OnGameShutdown);
            ModEvents.PlayerSpawnedInWorld.RegisterHandler(OnPlayerSpawned);
            ModEvents.PlayerDisconnected.RegisterHandler(OnPlayerDisconnected);
            ModEvents.EntityKilled.RegisterHandler(OnEntityKilled);
            ModEvents.ChatMessage.RegisterHandler(OnChatMessage);
            ModEvents.GameUpdate.RegisterHandler(OnGameUpdate);
        }

        private void OnGameStartDone(ref ModEvents.SGameStartDoneData _d) => Persistence.Load();
        private void OnGameShutdown(ref ModEvents.SGameShutdownData _d) => Persistence.Save();

        private void OnPlayerSpawned(ref ModEvents.SPlayerSpawnedInWorldData _d)
            => BountyManager.OnPlayerSpawned(_d.ClientInfo);

        private void OnPlayerDisconnected(ref ModEvents.SPlayerDisconnectedData _d)
            => BountyManager.OnPlayerDisconnected(_d.ClientInfo);

        private void OnEntityKilled(ref ModEvents.SEntityKilledData _d)
        {
            if (_d.KillingEntity is EntityPlayer killer && _d.KilledEntitiy != null)
                BountyManager.OnKill(killer.entityId, _d.KilledEntitiy);
        }

        // Biom-Bounties: gedrosseltes Server-Polling (kein Client-Code noetig).
        private void OnGameUpdate(ref ModEvents.SGameUpdateData _d)
        {
            if (++_biomeTick < 200) return;
            _biomeTick = 0;
            BountyManager.PollBiomes();
        }

        // Chat-Commands: /bounty [help], /skip <Nr>
        private ModEvents.EModEventResult OnChatMessage(ref ModEvents.SChatMessageData _d)
        {
            var ci = _d.ClientInfo;
            string msg = _d.Message;
            if (ci == null || string.IsNullOrEmpty(msg) || msg[0] != '/')
                return ModEvents.EModEventResult.Continue;

            string[] parts = msg.Trim().Split(' ');
            string cmd = parts[0].ToLowerInvariant();

            if (cmd == "/bounty" || cmd == "/bounties")
            {
                if (parts.Length > 1 && parts[1].ToLowerInvariant() == "help")
                    ChatUtil.Send(ci, "Commands: /bounty (show bounties), /skip <n> (reroll a bounty)");
                else
                    BountyManager.SendList(ci);
                return ModEvents.EModEventResult.StopHandlersAndVanilla;
            }

            if (cmd == "/skip")
            {
                int idx = 1;
                if (parts.Length > 1 && !int.TryParse(parts[1], out idx))
                {
                    ChatUtil.Send(ci, "Usage: /skip <n>  (e.g. /skip 2)");
                    return ModEvents.EModEventResult.StopHandlersAndVanilla;
                }
                BountyManager.Skip(ci, idx);
                return ModEvents.EModEventResult.StopHandlersAndVanilla;
            }

            return ModEvents.EModEventResult.Continue;
        }
    }
}
