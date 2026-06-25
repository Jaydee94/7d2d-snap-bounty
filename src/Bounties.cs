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

        // Effektiver Katalog (von Config.Load ggf. mit Counts/Tiers/enabled ueberschrieben).
        public static List<BountyDef> All = BuildDefaults();

        // Eingebaute Standard-Bounties. Alle Entity-/Block-/Biom-/Recipe-Namen sind gegen die
        // echten vanilla-XMLs (V2.6) verifiziert.
        public static List<BountyDef> BuildDefaults() => new List<BountyDef>
        {
            // ===================== Kills: any zombies/animals =====================
            new BountyDef("z_any_10", 1, 10, "Kill 10 zombies", BountyKind.KillAnyZombie),
            new BountyDef("z_any_50", 2, 50, "Kill 50 zombies", BountyKind.KillAnyZombie),
            new BountyDef("z_any_100", 3, 100, "Kill 100 zombies", BountyKind.KillAnyZombie),
            new BountyDef("a_any_5", 1, 5, "Kill 5 animals", BountyKind.KillAnyAnimal),
            new BountyDef("a_any_20", 2, 20, "Kill 20 animals", BountyKind.KillAnyAnimal),

            // ===================== Kills: specific zombie types =====================
            new BountyDef("z_cop_5", 2, 5, "Kill 5 cop zombies", BountyKind.KillNamed,
                N("zombieFatCop", "zombieFatCopFeral", "zombieFatCopRadiated", "zombieFatCopInfernal")),
            new BountyDef("z_demo_3", 3, 3, "Kill 3 demolishers", BountyKind.KillNamed,
                N("zombieDemolition")),
            new BountyDef("z_vulture_20", 1, 20, "Kill 20 vultures", BountyKind.KillNamed,
                N("animalZombieVulture", "animalZombieVultureRadiated")),
            new BountyDef("z_spider_10", 2, 10, "Kill 10 spider zombies", BountyKind.KillNamed,
                N("zombieSpider", "zombieSpiderFeral", "zombieSpiderRadiated", "zombieSpiderCharged", "zombieSpiderInfernal")),
            new BountyDef("z_lumberjack_10", 2, 10, "Kill 10 lumberjacks", BountyKind.KillNamed,
                N("zombieLumberjack", "zombieLumberjackFeral", "zombieLumberjackRadiated", "zombieLumberjackInfernal")),
            new BountyDef("z_screamer_5", 2, 5, "Kill 5 screamers", BountyKind.KillNamed,
                N("zombieScreamer", "zombieScreamerFeral", "zombieScreamerRadiated")),
            new BountyDef("z_mutated_10", 2, 10, "Kill 10 mutated zombies", BountyKind.KillNamed,
                N("zombieMutated", "zombieMutatedFeral", "zombieMutatedRadiated", "zombieMutatedCharged", "zombieMutatedInfernal")),
            new BountyDef("z_wight_5", 3, 5, "Kill 5 wights", BountyKind.KillNamed,
                N("zombieWightFeral", "zombieWightRadiated", "zombieWightCharged", "zombieWightInfernal")),

            // ===================== Kills: animals / hunting =====================
            new BountyDef("a_pred_10", 2, 10, "Kill 10 predators", BountyKind.KillNamed,
                N("animalWolf", "animalDireWolf", "animalBear", "animalMountainLion")),
            new BountyDef("a_bear_3", 2, 3, "Kill 3 bears", BountyKind.KillNamed,
                N("animalBear", "animalBearSmall", "animalZombieBear")),
            new BountyDef("a_dog_15", 2, 15, "Kill 15 zombie dogs", BountyKind.KillNamed,
                N("animalZombieDog")),
            new BountyDef("a_snake_10", 1, 10, "Kill 10 snakes", BountyKind.KillNamed,
                N("animalSnake")),
            new BountyDef("a_boar_10", 1, 10, "Kill 10 boars", BountyKind.KillNamed,
                N("animalBoar", "animalZombieBoar")),

            // ===================== Mine blocks =====================
            new BountyDef("mine_60", 1, 60, "Mine 60 blocks", BountyKind.MineBlock),
            new BountyDef("mine_250", 2, 250, "Mine 250 blocks", BountyKind.MineBlock),
            new BountyDef("mine_500", 3, 500, "Mine 500 blocks", BountyKind.MineBlock),

            // ===================== Build (place blocks) =====================
            new BountyDef("place_50", 1, 50, "Place 50 blocks", BountyKind.PlaceBlock),
            new BountyDef("place_150", 2, 150, "Place 150 blocks", BountyKind.PlaceBlock),
            new BountyDef("place_300", 3, 300, "Place 300 blocks", BountyKind.PlaceBlock),

            // ===================== Craft (at workstations) =====================
            new BountyDef("craft_any_20", 2, 20, "Craft 20 items at workstations", BountyKind.Craft),
            new BountyDef("craft_any_50", 3, 50, "Craft 50 items at workstations", BountyKind.Craft),
            new BountyDef("craft_iron_50", 2, 50, "Forge 50 forged iron", BountyKind.Craft,
                N("resourceForgedIron")),
            new BountyDef("craft_steel_100", 3, 100, "Forge 100 forged steel", BountyKind.Craft,
                N("resourceForgedSteel")),
            new BountyDef("craft_concrete_100", 2, 100, "Mix 100 concrete mix", BountyKind.Craft,
                N("resourceConcreteMix")),

            // ===================== Explore (biomes) =====================
            new BountyDef("biome_desert", 1, 1, "Enter the desert", BountyKind.EnterBiome, N("desert")),
            new BountyDef("biome_burnt", 2, 1, "Enter the burnt forest", BountyKind.EnterBiome, N("burnt_forest")),
            new BountyDef("biome_snow", 2, 1, "Enter the snow", BountyKind.EnterBiome, N("snow")),
            new BountyDef("biome_wasteland", 3, 1, "Enter the wasteland", BountyKind.EnterBiome, N("wasteland")),
        };

        public static BountyDef ById(string id)
        {
            foreach (var d in All) if (d.Id == id) return d;
            return null;
        }
    }
}
