using ICities;
using CitiesHarmony.API;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// The base mod class for instantiation by the game.
    /// </summary>
    public class TransferControllerMod : IUserMod
    {
        public static string ModName => "Transfer Controller";
        public static string Version => "0.2";

        public string Name => ModName + " " + Version;
        public string Description => Translations.Translate("TFC_DESC");


        /// <summary>
        /// Called by the game when the mod is enabled.
        /// </summary>
        public void OnEnabled()
        {
            // Apply Harmony patches via Cities Harmony.
            // Called here instead of OnCreated to allow the auto-downloader to do its work prior to launch.
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());

            // Load the settings file.
            ModSettings.Load();
        }


        /// <summary>
        /// Called by the game when the mod is disabled.
        /// </summary>
        public void OnDisabled()
        {
            // Unapply Harmony patches via Cities Harmony.
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                Patcher.UnpatchAll();
            }
        }


        /// <summary>
        /// Called by the game when the mod options panel is setup.
        /// </summary>
        public void OnSettingsUI(UIHelperBase helper)
        {
            // Language options.
            UIHelperBase languageGroup = helper.AddGroup(Translations.Translate("TRN_CHOICE"));
            UIDropDown languageDropDown = (UIDropDown)languageGroup.AddDropdown(Translations.Translate("TRN_CHOICE"), Translations.LanguageList, Translations.Index, (value) => { Translations.Index = value; ModSettings.Save(); });
            languageDropDown.autoSize = false;
            languageDropDown.width = 270f;

            // Distance multiplier slider.
            UISlider distanceSlider = helper.AddSlider(Translations.Translate("TFC_OPT_DIS"), 0f, 100f, 1f, TransferManagerPatches.distancePercentage, (value) => { TransferManagerPatches.distancePercentage = (int)value.RoundToNearest(1f); }) as UISlider;
            UILabel distanceLabel = distanceSlider.parent.AddUIComponent<UILabel>();
            distanceLabel.text = TransferManagerPatches.distancePercentage.ToString() + "%";
            distanceSlider.eventValueChanged += (control,value) => { distanceLabel.text = TransferManagerPatches.distancePercentage.ToString() + "%"; ModSettings.Save(); };
            distanceSlider.parent.height += 20f;
            distanceSlider.parent.width += 100f;

            // Warehouse priority slider.
            UISlider warehouseSlider = helper.AddSlider(Translations.Translate("TFC_OPT_WAR"), 0f, 4f, 1f, AddOffers.warehousePriority, (value) => { AddOffers.warehousePriority = (int)value.RoundToNearest(1f); }) as UISlider;
            UILabel warehouseLabel = warehouseSlider.parent.AddUIComponent<UILabel>();
            warehouseLabel.autoSize = true;
            warehouseLabel.wordWrap = false;
            warehouseLabel.text = AddOffers.warehousePriority.ToString(); ;
            warehouseSlider.eventValueChanged += (control, value) => { warehouseLabel.text = AddOffers.warehousePriority.ToString(); ModSettings.Save(); };
            warehouseSlider.parent.width += 100f;
        }
    }
}
