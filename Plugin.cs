using System;
using BepInEx;
using HarmonyLib;

namespace Shock12.Client
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("EscapeFromTarkov.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.bensburnedwaffles.shock12.client";
        public const string PluginName = "12/70 Shock-12 Client";
        public const string PluginVersion = "1.0.2";

        private Harmony _harmony;

        private void Awake()
        {
            try
            {
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll(typeof(Plugin).Assembly);
                Logger.LogInfo("Shock-12 loaded: trauma effects and dark-blue cartridge visuals are active.");
            }
            catch (Exception exception)
            {
                Logger.LogError("Shock-12 failed to initialize: " + exception);
            }
        }

        private void OnDestroy()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
            }
        }
    }
}
