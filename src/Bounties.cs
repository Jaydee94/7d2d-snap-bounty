using System.Collections.Generic;

namespace SnapBounty
{
    // Kategorien, je nach Fortschritts-Quelle (Server-seitig hookbar).
    public enum BountyKind
    {
        KillAnyZombie,  // ModEvents.EntityKilled
        KillAnyAnimal,
        KillNamed,      // entityClassName in Names
        MineBlock,      // SetBlocksRPC: Block -> Luft (Names leer = beliebig, sonst Blockname)
        PlaceBlock,     // SetBlocksRPC: Block gesetzt (nicht Luft)
        Craft,          // TileEntityWorkstation.AddCraftComplete (Names leer = beliebig, sonst recipeName)
        EnterBiome      // Polling biomeStandingOn (Names = Biomname, z.B. "wasteland")
    }

    public sealed class BountyDef
    {
        public readonly string Id;
        public readonly int Tier;          // 1..3 -> reward_event + Loot-Tier
        public readonly int TargetCount;
        public readonly string Title;
        public readonly BountyKind Kind;
        public readonly HashSet<string> Names; // Bedeutung je nach Kind; null/leer = "beliebig"

        public BountyDef(string id, int tier, int target, string title, BountyKind kind, HashSet<string> names = null)
        {
            Id = id; Tier = tier; TargetCount = target; Title = title; Kind = kind; Names = names;
        }

        public string RewardEvent => "snapBountyReward_t" + Tier;

        public bool NameMatches(string name)
            => Names == null || Names.Count == 0 || (name != null && Names.Contains(name));
    }

    public static class BountyCatalog
    {
        // Alle Entity-/Block-/Biom-/Recipe-Namen sind gegen vanilla XML verifiziert.
        public static readonly List<BountyDef> All = new List<BountyDef>
        {
            // ---- Kills ----
            new BountyDef("z_any_10", 1, 10, "Toete 10 Zombies", BountyKind.KillAnyZombie),
            new BountyDef("z_any_50", 2, 50, "Toete 50 Zombies", BountyKind.KillAnyZombie),
            new BountyDef("z_any_100", 3, 100, "Toete 100 Zombies", BountyKind.KillAnyZombie),
            new BountyDef("a_any_5", 1, 5, "Toete 5 Tiere", BountyKind.KillAnyAnimal),
            new BountyDef("z_cop_5", 2, 5, "Toete 5 Cop-Zombies", BountyKind.KillNamed,
                new HashSet<string>{ "zombieFatCop", "zombieFatCopFeral", "zombieFatCopRadiated", "zombieFatCopInfernal" }),
            new BountyDef("z_demo_3", 3, 3, "Toete 3 Demolisher", BountyKind.KillNamed,
                new HashSet<string>{ "zombieDemolition" }),

            // ---- Sammeln / Abbauen (Bloecke abbauen) ----
            new BountyDef("mine_60", 1, 60, "Baue 60 Bloecke ab", BountyKind.MineBlock),
            new BountyDef("mine_250", 2, 250, "Baue 250 Bloecke ab", BountyKind.MineBlock),

            // ---- Bauen (Bloecke platzieren) ----
            new BountyDef("place_50", 1, 50, "Platziere 50 Bloecke", BountyKind.PlaceBlock),
            new BountyDef("place_150", 2, 150, "Platziere 150 Bloecke", BountyKind.PlaceBlock),

            // ---- Crafting (an Werkstationen) ----
            new BountyDef("craft_any_20", 2, 20, "Stelle 20 Dinge an Werkstationen her", BountyKind.Craft),
            new BountyDef("craft_iron_50", 2, 50, "Schmiede 50 geschmiedetes Eisen", BountyKind.Craft,
                new HashSet<string>{ "resourceForgedIron" }),

            // ---- Erkunden (Biome) ----
            new BountyDef("biome_desert", 1, 1, "Betritt die Wueste", BountyKind.EnterBiome,
                new HashSet<string>{ "desert" }),
            new BountyDef("biome_snow", 2, 1, "Betritt den Schnee", BountyKind.EnterBiome,
                new HashSet<string>{ "snow" }),
            new BountyDef("biome_wasteland", 3, 1, "Betritt das Oedland", BountyKind.EnterBiome,
                new HashSet<string>{ "wasteland" }),
        };

        public static BountyDef ById(string id)
        {
            foreach (var d in All) if (d.Id == id) return d;
            return null;
        }
    }
}
