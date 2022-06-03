using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// RON options panel.
    /// </summary>
    public class TCOptionsPanel : UIPanel
    {
        // Layout constants.
        private const float Margin = 5f;
        private const float LeftMargin = 24f;
        private const float GroupMargin = 40f;


        /// <summary>
        /// Performs initial setup for the panel; we don't use Start() as that's not sufficiently reliable (race conditions), and is not needed with the dynamic create/destroy process.
        /// </summary>
        internal void Setup()
        {
            // Manual layout.
            this.autoLayout = false;

            // Y position indicator.
            float currentY = Margin;

            // Language selection.
            UIDropDown languageDropDown = UIControls.AddPlainDropDown(this, Translations.Translate("TRN_CHOICE"), Translations.LanguageList, Translations.Index);
            languageDropDown.eventSelectedIndexChanged += (control, index) =>
            {
                Translations.Index = index;
                OptionsPanelManager.LocaleChanged();
            };
            languageDropDown.parent.relativePosition = new Vector2(LeftMargin, currentY);
            currentY += languageDropDown.parent.height + GroupMargin;

            // Hotkey control.
            OptionsKeymapping keyMapping = languageDropDown.parent.parent.gameObject.AddComponent<OptionsKeymapping>();
            keyMapping.uIPanel.relativePosition = new Vector2(LeftMargin, currentY);
            currentY += keyMapping.uIPanel.height + GroupMargin;

            // Distance multiplier slider.
            UISlider distanceSlider = UIControls.AddSliderWithValue(this, Translations.Translate("TFC_OPT_DIS"), 0f, 100f, 1f, TransferManagerPatches.distancePercentage, (value) => { TransferManagerPatches.distancePercentage = (int)value.RoundToNearest(1f); });
            distanceSlider.parent.relativePosition = new Vector2(LeftMargin, currentY);
            currentY += distanceSlider.parent.height + GroupMargin;

            // Warehouse priority slider.
            UISlider warehouseSlider = UIControls.AddSliderWithValue(this, Translations.Translate("TFC_OPT_WAR"), 0f, 4f, 1f, AddOffers.warehousePriority, (value) => { AddOffers.warehousePriority = (int)value.RoundToNearest(1f); });
            warehouseSlider.parent.relativePosition = new Vector2(LeftMargin, currentY);
            currentY += distanceSlider.parent.height + GroupMargin;

            // Pathfind failure montioring checkbox.
            UICheckBox blockPathfindCheck = UIControls.AddPlainCheckBox(this, LeftMargin, currentY, Translations.Translate("TFC_OPT_PAT"));
            blockPathfindCheck.tooltip = Translations.Translate("TFC_OPT_PAT_TIP");
            blockPathfindCheck.isChecked = PathFindFailure.EnableFailTracking;
            blockPathfindCheck.eventCheckChanged += (control, isChecked) => PathFindFailure.EnableFailTracking = isChecked;
        }
    }
}