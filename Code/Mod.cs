using AlgernonCommons;
using AlgernonCommons.Patching;
using AlgernonCommons.Translation;
using AlgernonCommons.UI;
using ColossalFramework.UI;
using ICities;


namespace TransferController
{
    /// <summary>
    /// The base mod class for instantiation by the game.
    /// </summary>
    public class Mod : PatcherMod, IUserMod
    {
        public static string ModName => "Transfer Controller";
        public override string LogName => ModName;
        public override string HarmonyID => "com.github.algernon-A.csl.tc";

        public string Name => ModName + ' ' + AssemblyUtils.TrimmedCurrentVersion;
        public string Description => Translations.Translate("TFC_DESC");


        /// <summary>
        /// Called by the game when the mod is enabled.
        /// </summary>
        public override void OnEnabled()
        {
            base.OnEnabled();

            // Apply additional TransferManager patches via Cities Harmony.
            //HarmonyHelper.DoOnHarmonyReady(() => TransferPatcher.ApplyPatches());

            // Add the options panel event handler for the start screen (to enable/disable options panel based on visibility).
            // First, check to see if UIView is ready.
            if (UIView.GetAView() != null)
            {
                // It's ready - attach the hook now.
                OptionsPanelManager<OptionsPanel>.OptionsEventHook();
            }
            else
            {
                // Otherwise, queue the hook for when the intro's finished loading.
                LoadingManager.instance.m_introLoaded += OptionsPanelManager<OptionsPanel>.OptionsEventHook;
            }
        }


        /// <summary>
        /// Called by the game when the mod options panel is setup.
        /// </summary>
        public void OnSettingsUI(UIHelperBase helper)
        {
            // Create options panel.
            OptionsPanelManager<OptionsPanel>.Setup(helper);
        }


        /// <summary>
        /// Saves settings file.
        /// </summary>
        public override void SaveSettings() => ModSettings.Save();


        /// <summary>
        /// Loads settings file.
        /// </summary>
        public override void LoadSettings() => ModSettings.Load();


        /// <summary>
        /// Apply Harmony patches.
        /// </summary>
        protected override void ApplyPatches() => Patcher.PatchAll();


        /// <summary>
        /// Remove Harmony patches.
        /// </summary>
        protected override void RemovePatches() => Patcher.UnpatchAll();
    }
}
