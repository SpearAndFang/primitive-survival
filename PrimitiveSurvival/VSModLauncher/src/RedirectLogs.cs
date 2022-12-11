namespace VSModLauncher
{
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Server;
    /// <summary>
    /// Redirects all log entries into the visual studio output window. Only for your convenience during development and testing.
    /// </summary>
    public class RedirectLogs : ModSystem
    {

        public override bool ShouldLoad(EnumAppSide side)
        {
            return true;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Server.Logger.EntryAdded += this.OnServerLogEntry;
        }

        private void OnServerLogEntry(EnumLogType logType, string message, params object[] args)
        {
            if (logType == EnumLogType.VerboseDebug)  return;
            System.Diagnostics.Debug.WriteLine("[Server " + logType + "] " + message, args);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.World.Logger.EntryAdded += this.OnClientLogEntry;
        }

        private void OnClientLogEntry(EnumLogType logType, string message, params object[] args)
        {
            if (logType == EnumLogType.VerboseDebug) return;
            System.Diagnostics.Debug.WriteLine("[Client " + logType + "] " + message, args);
        }
    }
}
