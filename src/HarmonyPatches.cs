using System.Collections.Generic;
using HarmonyLib;

namespace SnapBounty
{
    // Server-seitige Hooks fuer Nicht-Kill-Bounties.

    // Blockaenderungen durch Spieler (Platzieren / Abbauen). SetBlocksRPC ist der Server-Eintritt
    // fuer ueber Netzwerk eintreffende Blockedits; BlockChangeInfo.changedByEntityId ordnet sie zu.
    [HarmonyPatch(typeof(GameManager), "SetBlocksRPC")]
    public static class Patch_SetBlocksRPC
    {
        static void Postfix(List<BlockChangeInfo> _changes)
        {
            if (_changes == null) return;
            foreach (var c in _changes)
            {
                if (!c.bChangeBlockValue) continue;       // nur echte Blockwert-Aenderungen, kein reiner Schaden
                if (c.changedByEntityId <= 0) continue;    // nur spielerverursachte Aenderungen
                bool air = c.blockValue.isair;
                string name = air ? null : (c.blockValue.Block != null ? c.blockValue.Block.GetBlockName() : null);
                BountyManager.OnBlockChange(c.changedByEntityId, name, placed: !air);
            }
        }
    }

    // Crafting an Werkstationen (Schmiede, Werkbank, Chemiestation, Lagerfeuer ...).
    // Laeuft server-seitig, da Werkstationen ihre Warteschlange auf dem Server abarbeiten.
    [HarmonyPatch(typeof(TileEntityWorkstation), "AddCraftComplete")]
    public static class Patch_AddCraftComplete
    {
        static void Postfix(int crafterEntityID, string recipeName, int craftedCount)
        {
            if (craftedCount <= 0 || string.IsNullOrEmpty(recipeName)) return; // Scrap/leere Faelle ignorieren
            BountyManager.OnCraft(crafterEntityID, recipeName, craftedCount);
        }
    }
}
