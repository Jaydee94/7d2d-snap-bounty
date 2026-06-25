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
        // Helfer fuer kompakte Listen
        private static HashSet<string> N(params string[] v) => new HashSet<string>(v);

        // Alle Entity-/Block-/Biom-/Recipe-Namen sind gegen die echten vanilla-XMLs (V2.6) verifiziert.
        public static readonly List<BountyDef> All = new List<BountyDef>
        {
            // ===================== Kills: beliebige Zombies/Tiere =====================
            new BountyDef("z_any_10", 1, 10, "Toete 10 Zombies", BountyKind.KillAnyZombie),
            new BountyDef("z_any_50", 2, 50, "Toete 50 Zombies", BountyKind.KillAnyZombie),
            new BountyDef("z_any_100", 3, 100, "Toete 100 Zombies", BountyKind.KillAnyZombie),
            new BountyDef("a_any_5", 1, 5, "Toete 5 Tiere", BountyKind.KillAnyAnimal),
            new BountyDef("a_any_20", 2, 20, "Toete 20 Tiere", BountyKind.KillAnyAnimal),

            // ===================== Kills: spezielle Zombie-Typen =====================
            new BountyDef("z_cop_5", 2, 5, "Toete 5 Cop-Zombies", BountyKind.KillNamed,
                N("zombieFatCop", "zombieFatCopFeral", "zombieFatCopRadiated", "zombieFatCopInfernal")),
            new BountyDef("z_demo_3", 3, 3, "Toete 3 Demolisher", BountyKind.KillNamed,
                N("zombieDemolition")),
            new BountyDef("z_vulture_20", 1, 20, "Toete 20 Geier", BountyKind.KillNamed,
                N("animalZombieVulture", "animalZombieVultureRadiated")),
            new BountyDef("z_spider_10", 2, 10, "Toete 10 Spinnen-Zombies", BountyKind.KillNamed,
                N("zombieSpider", "zombieSpiderFeral", "zombieSpiderRadiated", "zombieSpiderCharged", "zombieSpiderInfernal")),
            new BountyDef("z_lumberjack_10", 2, 10, "Toete 10 Holzfaeller", BountyKind.KillNamed,
                N("zombieLumberjack", "zombieLumberjackFeral", "zombieLumberjackRadiated", "zombieLumberjackInfernal")),
            new BountyDef("z_screamer_5", 2, 5, "Toete 5 Screamer", BountyKind.KillNamed,
                N("zombieScreamer", "zombieScreamerFeral", "zombieScreamerRadiated")),
            new BountyDef("z_mutated_10", 2, 10, "Toete 10 Mutierte", BountyKind.KillNamed,
                N("zombieMutated", "zombieMutatedFeral", "zombieMutatedRadiated", "zombieMutatedCharged", "zombieMutatedInfernal")),
            new BountyDef("z_wight_5", 3, 5, "Toete 5 Wights", BountyKind.KillNamed,
                N("zombieWightFeral", "zombieWightRadiated", "zombieWightCharged", "zombieWightInfernal")),

            // ===================== Kills: Tiere / Jagd =====================
            new BountyDef("a_pred_10", 2, 10, "Toete 10 Raubtiere", BountyKind.KillNamed,
                N("animalWolf", "animalDireWolf", "animalBear", "animalMountainLion")),
            new BountyDef("a_bear_3", 2, 3, "Toete 3 Baeren", BountyKind.KillNamed,
                N("animalBear", "animalBearSmall", "animalZombieBear")),
            new BountyDef("a_dog_15", 2, 15, "Toete 15 Zombie-Hunde", BountyKind.KillNamed,
                N("animalZombieDog")),
            new BountyDef("a_snake_10", 1, 10, "Toete 10 Schlangen", BountyKind.KillNamed,
                N("animalSnake")),
            new BountyDef("a_boar_10", 1, 10, "Toete 10 Wildschweine", BountyKind.KillNamed,
                N("animalBoar", "animalZombieBoar")),

            // ===================== Sammeln / Abbauen (Bloecke abbauen) =====================
            new BountyDef("mine_60", 1, 60, "Baue 60 Bloecke ab", BountyKind.MineBlock),
            new BountyDef("mine_250", 2, 250, "Baue 250 Bloecke ab", BountyKind.MineBlock),
            new BountyDef("mine_500", 3, 500, "Baue 500 Bloecke ab", BountyKind.MineBlock),

            // ===================== Bauen (Bloecke platzieren) =====================
            new BountyDef("place_50", 1, 50, "Platziere 50 Bloecke", BountyKind.PlaceBlock),
            new BountyDef("place_150", 2, 150, "Platziere 150 Bloecke", BountyKind.PlaceBlock),
            new BountyDef("place_300", 3, 300, "Platziere 300 Bloecke", BountyKind.PlaceBlock),

            // ===================== Crafting (an Werkstationen) =====================
            new BountyDef("craft_any_20", 2, 20, "Stelle 20 Dinge an Werkstationen her", BountyKind.Craft),
            new BountyDef("craft_any_50", 3, 50, "Stelle 50 Dinge an Werkstationen her", BountyKind.Craft),
            new BountyDef("craft_iron_50", 2, 50, "Schmiede 50 geschmiedetes Eisen", BountyKind.Craft,
                N("resourceForgedIron")),
            new BountyDef("craft_steel_100", 3, 100, "Schmiede 100 geschmiedeten Stahl", BountyKind.Craft,
                N("resourceForgedSteel")),
            new BountyDef("craft_concrete_100", 2, 100, "Mische 100 Betonmischung", BountyKind.Craft,
                N("resourceConcreteMix")),

            // ===================== Erkunden (Biome) =====================
            new BountyDef("biome_desert", 1, 1, "Betritt die Wueste", BountyKind.EnterBiome, N("desert")),
            new BountyDef("biome_burnt", 2, 1, "Betritt den verbrannten Wald", BountyKind.EnterBiome, N("burnt_forest")),
            new BountyDef("biome_snow", 2, 1, "Betritt den Schnee", BountyKind.EnterBiome, N("snow")),
            new BountyDef("biome_wasteland", 3, 1, "Betritt das Oedland", BountyKind.EnterBiome, N("wasteland")),
        };

        public static BountyDef ById(string id)
        {
            foreach (var d in All) if (d.Id == id) return d;
            return null;
        }
    }
}
