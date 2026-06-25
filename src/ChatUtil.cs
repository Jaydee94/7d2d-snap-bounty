namespace SnapBounty
{
    public static class ChatUtil
    {
        // Sendet eine Chat-Zeile an genau EINEN Spieler (direkt via NetPackage an dessen Client).
        public static void Send(ClientInfo ci, string msg)
        {
            if (ci == null) return;
            try
            {
                ci.SendPackage(NetPackageManager.GetPackage<NetPackageChat>()
                    .Setup(EChatType.Global, -1, "[SnapBounty] " + msg, null,
                        EMessageSender.Server, GeneratedTextManager.BbCodeSupportMode.Supported));
            }
            catch (System.Exception e)
            {
                Log.Error("[SnapBounty] Chat-Send fehlgeschlagen: " + e.Message);
            }
        }
    }
}
