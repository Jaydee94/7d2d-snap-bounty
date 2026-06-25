using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace SnapBounty
{
    // Liest die optionale snapbounty.xml aus dem Mod-Ordner und ueberschreibt damit
    // globale Settings (maxActive, skipCooldownSeconds) sowie Count/Tier/enabled der Bounties.
    // Fehlt die Datei oder ist sie fehlerhaft, gelten die eingebauten Defaults.
    public static class Config
    {
        public static int MaxActive = 3;
        public static int SkipCooldownSeconds = 300;

        public static void Load(string modDir)
        {
            MaxActive = 3;
            SkipCooldownSeconds = 300;
            var bounties = BountyCatalog.BuildDefaults();

            try
            {
                string path = string.IsNullOrEmpty(modDir) ? null : Path.Combine(modDir, "snapbounty.xml");
                if (path == null || !File.Exists(path))
                {
                    Log.Out("[SnapBounty] no snapbounty.xml found; using defaults (" + bounties.Count + " bounties).");
                }
                else
                {
                    var root = XDocument.Load(path).Root;
                    if (root != null)
                    {
                        MaxActive = GetInt(root.Element("maxActive"), MaxActive);
                        SkipCooldownSeconds = GetInt(root.Element("skipCooldownSeconds"), SkipCooldownSeconds);

                        var list = root.Element("bounties");
                        if (list != null)
                        {
                            var byId = new Dictionary<string, BountyDef>();
                            foreach (var d in bounties) byId[d.Id] = d;

                            var result = new List<BountyDef>();
                            foreach (var e in list.Elements("bounty"))
                            {
                                string id = (string)e.Attribute("id");
                                if (string.IsNullOrEmpty(id) || !byId.TryGetValue(id, out var def))
                                {
                                    Log.Warning("[SnapBounty] config: unknown bounty id '" + id + "' - ignored.");
                                    continue;
                                }
                                if (!ParseBool((string)e.Attribute("enabled"), true)) continue; // disabled
                                int count = Math.Max(1, GetIntAttr(e, "count", def.TargetCount));
                                int tier = Math.Min(3, Math.Max(1, GetIntAttr(e, "tier", def.Tier)));
                                result.Add(new BountyDef(def.Id, tier, count, def.Title, def.Kind, def.Names));
                            }

                            if (result.Count > 0) bounties = result;
                            else Log.Warning("[SnapBounty] config: no enabled bounties listed - keeping defaults.");
                        }
                    }
                    Log.Out("[SnapBounty] config loaded: maxActive=" + MaxActive
                        + ", skipCooldown=" + SkipCooldownSeconds + "s, bounties=" + bounties.Count);
                }
            }
            catch (Exception ex)
            {
                Log.Error("[SnapBounty] config load failed, using defaults: " + ex.Message);
                bounties = BountyCatalog.BuildDefaults();
                MaxActive = 3;
                SkipCooldownSeconds = 300;
            }

            if (MaxActive < 1) MaxActive = 1;
            if (SkipCooldownSeconds < 0) SkipCooldownSeconds = 0;
            BountyCatalog.All = bounties;
        }

        private static int GetInt(XElement e, int fallback)
            => e != null && int.TryParse(e.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;

        private static int GetIntAttr(XElement e, string name, int fallback)
        {
            var a = e.Attribute(name);
            return a != null && int.TryParse(a.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;
        }

        private static bool ParseBool(string s, bool fallback)
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            s = s.Trim().ToLowerInvariant();
            return s == "true" || s == "1" || s == "yes";
        }
    }
}
