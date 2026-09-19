using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace GleamFarmer
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; } = null!;

        private readonly Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            Instance = this;

            var enabled = Config.Bind(
                "General",
                "GleamMode",
                true,
                "When enabled, the in-game editor is compiled as Gleam instead of the built-in Python.");

            var externalProject = Config.Bind(
                "General",
                "ExternalProject",
                true,
                "Mirror the code windows to <save>/gleam-project/ so they can be edited in an " +
                "external editor with full Gleam LSP support, and compile from that project.");

            GleamHost.Init(Logger, enabled, externalProject);
            _harmony.PatchAll();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded.");
        }

        /// <summary>Run on the Unity main thread each frame: drains game actions queued by the Gleam worker.</summary>
        private void Update()
        {
            GleamHost.Instance?.PumpDispatcher();
            GleamHost.Instance?.PumpProjectSync();
        }
    }
}