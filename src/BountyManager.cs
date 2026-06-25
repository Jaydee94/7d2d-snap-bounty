using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SnapBounty
{
    public sealed class ActiveBounty
    {
        public string DefId;
        public int Progress;
        public BountyDef Def => BountyCatalog.ById(DefId);
    }

    public sealed class PlayerState
    {
        public readonly List<ActiveBounty> Active = new List<ActiveBounty>();
    }

    public static class BountyManager
    {
        public const int MaxActive = 3;

        internal static readonly Dictionary<string, PlayerState> States = new Dictionary<string, PlayerState>();
        private static readonly Dictionary<int, ClientInfo> Online = new Dictionary<int, ClientInfo>();
        private static readonly Dictionary<int, string> LastBiome = new Dictionary<int, string>();

        private static readonly System.Random Rng = new System.Random();
        private static readonly object Lock = new object();

        private static string Key(ClientInfo ci)
        {
            if (ci != null && ci.PlatformId != null) return ci.PlatformId.CombinedString;
            return ci != null ? ("name_" + ci.playerName) : "unknown";
        }

        private static ClientInfo OnlineById(int entityId)
        {
            lock (Lock) { return Online.TryGetValue(entityId, out var ci) ? ci : null; }
        }

        // ===================== Lifecycle =====================
        public static void OnPlayerSpawned(ClientInfo ci)
        {
            if (ci == null) return;
            bool isLogin;
            lock (Lock)
            {
                isLogin = !Online.ContainsKey(ci.entityId);
                Online[ci.entityId] = ci;
                string key = Key(ci);
                if (!States.TryGetValue(key, out var st)) { st = new PlayerState(); States[key] = st; }
                FillToMax(st);
            }
            SeedBiome(ci.entityId);
            Persistence.Save();
            if (isLogin)
            {
                ChatUtil.Send(ci, "Willkommen! /bounty zeigt deine Auftraege, /skip <Nr> wuerfelt neu.");
                SendList(ci);
            }
        }

        public static void OnPlayerDisconnected(ClientInfo ci)
        {
            if (ci == null) return;
            lock (Lock) { Online.Remove(ci.entityId); LastBiome.Remove(ci.entityId); }
            Persistence.Save();
        }

        // ===================== Fortschritts-Quellen =====================
        public static void OnKill(int killerEntityId, Entity victim)
        {
            var ci = OnlineById(killerEntityId);
            if (ci == null || victim == null) return;
            string cname = victim.EntityClass != null ? victim.EntityClass.entityClassName : null;
            Award(ci, def =>
            {
                switch (def.Kind)
                {
                    case BountyKind.KillAnyZombie: return victim is EntityZombie ? 1 : 0;
                    case BountyKind.KillAnyAnimal: return victim is EntityAnimal ? 1 : 0;
                    case BountyKind.KillNamed: return def.NameMatches(cname) && def.Names != null && def.Names.Count > 0 ? 1 : 0;
                    default: return 0;
                }
            });
        }

        public static void OnBlockChange(int entityId, string blockName, bool placed)
        {
            var ci = OnlineById(entityId);
            if (ci == null) return;
            Award(ci, def =>
            {
                if (placed && def.Kind == BountyKind.PlaceBlock && def.NameMatches(blockName)) return 1;
                if (!placed && def.Kind == BountyKind.MineBlock && def.NameMatches(blockName)) return 1;
                return 0;
            });
        }

        public static void OnCraft(int crafterEntityId, string recipeName, int count)
        {
            var ci = OnlineById(crafterEntityId);
            if (ci == null || count <= 0) return;
            Award(ci, def => def.Kind == BountyKind.Craft && def.NameMatches(recipeName) ? count : 0);
        }

        private static void OnBiome(ClientInfo ci, string biomeName)
        {
            Award(ci, def => def.Kind == BountyKind.EnterBiome && def.NameMatches(biomeName)
                && def.Names != null && def.Names.Count > 0 ? def.TargetCount : 0);
        }

        // ===================== Biom-Polling (vom GameUpdate gedrosselt aufgerufen) =====================
        private static void SeedBiome(int entityId)
        {
            try
            {
                var p = GameManager.Instance?.World?.GetEntity(entityId) as EntityPlayer;
                string name = p != null && p.biomeStandingOn != null ? p.biomeStandingOn.m_sBiomeName : null;
                lock (Lock) { if (name != null) LastBiome[entityId] = name; }
            }
            catch { }
        }

        public static void PollBiomes()
        {
            List<KeyValuePair<int, ClientInfo>> snapshot;
            lock (Lock) { snapshot = Online.ToList(); }
            var world = GameManager.Instance != null ? GameManager.Instance.World : null;
            if (world == null) return;

            foreach (var kv in snapshot)
            {
                EntityPlayer p;
                try { p = world.GetEntity(kv.Key) as EntityPlayer; } catch { continue; }
                if (p == null || p.biomeStandingOn == null) continue;
                string name = p.biomeStandingOn.m_sBiomeName;
                if (string.IsNullOrEmpty(name)) continue;

                bool changed;
                lock (Lock)
                {
                    LastBiome.TryGetValue(kv.Key, out var prev);
                    changed = name != prev;
                    if (changed) LastBiome[kv.Key] = name;
                }
                if (changed) OnBiome(kv.Value, name);
            }
        }

        // ===================== Chat-Commands =====================
        public static void SendList(ClientInfo ci)
        {
            if (ci == null) return;
            lock (Lock)
            {
                if (!States.TryGetValue(Key(ci), out var st) || st.Active.Count == 0)
                {
                    ChatUtil.Send(ci, "Du hast aktuell keine Auftraege.");
                    return;
                }
                var sb = new StringBuilder("Deine Auftraege:");
                int i = 1;
                foreach (var ab in st.Active)
                {
                    var def = ab.Def;
                    if (def == null) continue;
                    sb.Append("\n  ").Append(i).Append(") ").Append(def.Title)
                      .Append("  [").Append(ab.Progress).Append('/').Append(def.TargetCount)
                      .Append("]  (Tier ").Append(def.Tier).Append(')');
                    i++;
                }
                ChatUtil.Send(ci, sb.ToString());
            }
        }

        public static void Skip(ClientInfo ci, int index1Based)
        {
            if (ci == null) return;
            BountyDef newDef = null;
            lock (Lock)
            {
                if (!States.TryGetValue(Key(ci), out var st) || st.Active.Count == 0)
                {
                    ChatUtil.Send(ci, "Du hast keine Auftraege zum Skippen.");
                    return;
                }
                if (index1Based < 1 || index1Based > st.Active.Count)
                {
                    ChatUtil.Send(ci, "Ungueltige Nummer. Nutze /bounty (1-" + st.Active.Count + ").");
                    return;
                }
                var current = new HashSet<string>(st.Active.Select(a => a.DefId));
                var replacement = PickRandom(current);
                if (replacement == null) { ChatUtil.Send(ci, "Kein anderer Auftrag verfuegbar."); return; }
                st.Active[index1Based - 1] = new ActiveBounty { DefId = replacement.Id, Progress = 0 };
                newDef = replacement;
                Persistence.Save();
            }
            ChatUtil.Send(ci, "Auftrag neu gewuerfelt: " + newDef.Title + " (Tier " + newDef.Tier + ")");
            SendList(ci);
        }

        // ===================== Kern =====================
        private static void Award(ClientInfo ci, Func<BountyDef, int> incFor)
        {
            if (ci == null) return;
            var completed = new List<BountyDef>();
            lock (Lock)
            {
                if (!States.TryGetValue(Key(ci), out var st)) return;
                bool changed = false;
                foreach (var ab in st.Active.ToArray())
                {
                    var def = ab.Def;
                    if (def == null) continue;
                    int inc = incFor(def);
                    if (inc <= 0) continue;
                    ab.Progress += inc;
                    changed = true;
                    if (ab.Progress >= def.TargetCount)
                    {
                        st.Active.Remove(ab);
                        completed.Add(def);
                    }
                }
                if (completed.Count > 0) FillToMax(st);
                if (changed) Persistence.Save();
            }
            foreach (var def in completed)
            {
                ChatUtil.Send(ci, "Auftrag erfuellt: " + def.Title + " -> Loot-Bag (Tier " + def.Tier + ") wird gedroppt!");
                GrantReward(ci, def);
            }
        }

        private static void GrantReward(ClientInfo ci, BountyDef def)
        {
            try
            {
                var player = GameManager.Instance.World.GetEntity(ci.entityId) as EntityPlayer;
                if (player == null) { Log.Warning("[SnapBounty] Reward: Spieler-Entity nicht gefunden."); return; }
                if (GameEventManager.Current == null) { Log.Warning("[SnapBounty] Reward: GameEventManager null."); return; }
                GameEventManager.Current.HandleAction(def.RewardEvent, player, null, false, "", "", false, false, "", null);
            }
            catch (Exception e)
            {
                Log.Error("[SnapBounty] Reward-Trigger fehlgeschlagen: " + e.Message);
            }
        }

        private static void FillToMax(PlayerState st)
        {
            int guard = 0;
            while (st.Active.Count < MaxActive && guard++ < 50)
            {
                var current = new HashSet<string>(st.Active.Select(a => a.DefId));
                var def = PickRandom(current);
                if (def == null) break;
                st.Active.Add(new ActiveBounty { DefId = def.Id, Progress = 0 });
            }
        }

        private static BountyDef PickRandom(HashSet<string> excludeIds)
        {
            var pool = BountyCatalog.All.Where(d => !excludeIds.Contains(d.Id)).ToList();
            return pool.Count == 0 ? null : pool[Rng.Next(pool.Count)];
        }
    }
}
