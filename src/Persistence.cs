using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SnapBounty
{
    // Einfache, abhaengigkeitsfreie Textpersistenz im Savegame-Ordner.
    // Zeilenformat:  <combinedKey>|<defId>:<progress>,<defId>:<progress>,...
    public static class Persistence
    {
        private static string FilePath()
        {
            string dir = Path.Combine(GameIO.GetSaveGameDir(), "SnapBounty");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "bounties.txt");
        }

        public static void Load()
        {
            try
            {
                string path = FilePath();
                if (!File.Exists(path)) return;
                lock (typeof(Persistence))
                {
                    BountyManager.States.Clear();
                    foreach (var raw in File.ReadAllLines(path))
                    {
                        string line = raw.Trim();
                        if (line.Length == 0) continue;
                        int bar = line.IndexOf('|');
                        if (bar <= 0) continue;
                        string key = line.Substring(0, bar);
                        string rest = line.Substring(bar + 1);
                        var st = new PlayerState();
                        if (rest.Length > 0)
                        {
                            foreach (var part in rest.Split(','))
                            {
                                int colon = part.IndexOf(':');
                                if (colon <= 0) continue;
                                string defId = part.Substring(0, colon);
                                if (BountyCatalog.ById(defId) == null) continue; // veraltete IDs ignorieren
                                int prog;
                                int.TryParse(part.Substring(colon + 1), out prog);
                                st.Active.Add(new ActiveBounty { DefId = defId, Progress = prog });
                            }
                        }
                        BountyManager.States[key] = st;
                    }
                }
                Log.Out("[SnapBounty] Persistenz geladen (" + BountyManager.States.Count + " Spieler).");
            }
            catch (Exception e)
            {
                Log.Error("[SnapBounty] Laden fehlgeschlagen: " + e.Message);
            }
        }

        public static void Save()
        {
            try
            {
                string path = FilePath();
                var sb = new StringBuilder();
                lock (typeof(Persistence))
                {
                    foreach (var kv in BountyManager.States)
                    {
                        sb.Append(kv.Key).Append('|');
                        bool first = true;
                        foreach (var ab in kv.Value.Active)
                        {
                            if (!first) sb.Append(',');
                            sb.Append(ab.DefId).Append(':').Append(ab.Progress);
                            first = false;
                        }
                        sb.Append('\n');
                    }
                    File.WriteAllText(path, sb.ToString());
                }
            }
            catch (Exception e)
            {
                Log.Error("[SnapBounty] Speichern fehlgeschlagen: " + e.Message);
            }
        }
    }
}
