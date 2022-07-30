using AlgernonCommons.Keybinding;
using AlgernonCommons.Translation;
using AlgernonCommons.UI;
using ColossalFramework.UI;
using UnityEngine;


namespace TransferController
{
    /// <summary>
    /// Transfer Controller options panel.
    /// </summary>
    public class OptionsPanel : UIPanel
    {
        // Layout constants.
        private const float Margin = 5f;
        private const float LeftMargin = 24f;
        private const float GroupMargin = 40f;


        // Components.
        private UISlider distanceSlider;

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
            UIDropDown languageDropDown = UIDropDowns.AddPlainDropDown(this, LeftMargin, currentY, Translations.Translate("TRN_CHOICE"), Translations.LanguageList, Translations.Index);
            languageDropDown.eventSelectedIndexChanged += (control, index) =>
            {
                Translations.Index = index;
                OptionsPanelManager<OptionsPanel>.LocaleChanged();
            };
            currentY += languageDropDown.parent.height + GroupMargin;

            // Hotkey control.
            OptionsKeymapping keyMapping = languageDropDown.parent.parent.gameObject.AddComponent<OptionsKeymapping>();
            keyMapping.uIPanel.relativePosition = new Vector2(LeftMargin, currentY);
            currentY += keyMapping.uIPanel.height + GroupMargin;

            // New algorithm checkbox.
            UICheckBox newAlgorithmCheck = UICheckBoxes.AddPlainCheckBox(this, LeftMargin, currentY, Translations.Translate("TFC_OPT_NEW"));
            newAlgorithmCheck.tooltip = Translations.Translate("TFC_OPT_NEW_TIP");
            newAlgorithmCheck.tooltipBox = UIToolTips.WordWrapToolTip;
            newAlgorithmCheck.isChecked = Patcher.UseNewAlgorithm;
            newAlgorithmCheck.eventCheckChanged += (control, isChecked) => { Patcher.UseNewAlgorithm = isChecked; distanceSlider.parent.isVisible = !isChecked; };
            currentY += newAlgorithmCheck.height + Margin;

            // Distance multiplier slider.
            distanceSlider = UISliders.AddSliderWithValue(this, LeftMargin, currentY, Translations.Translate("TFC_OPT_DIS"), 0f, 100f, 1f, OldMatching.distancePercentage);
            distanceSlider.eventValueChanged += (control, value) => OldMatching.distancePercentage = (int)value.RoundToNearest(1f);
            distanceSlider.tooltip = Translations.Translate("TFC_OPT_DIS_TIP");
            distanceSlider.tooltipBox = UIToolTips.WordWrapToolTip;
            distanceSlider.parent.isVisible = !newAlgorithmCheck.isChecked;
            currentY += distanceSlider.parent.height + GroupMargin;

            // Warehouse priority slider.
            UISlider warehouseSlider = UISliders.AddSliderWithValue(this, LeftMargin, currentY, Translations.Translate("TFC_OPT_WAR"), 0f, 4f, 1f, Matching.WarehousePriority);
            warehouseSlider.eventValueChanged += (control, value) => Matching.WarehousePriority = (int)value.RoundToNearest(1f);
            warehouseSlider.tooltip = Translations.Translate("TFC_OPT_WAR_TIP"); ;
            warehouseSlider.tooltipBox = UIToolTips.WordWrapToolTip;
            currentY += distanceSlider.parent.height + GroupMargin;

            // Outside rail connection priority slider.
            UISlider railSlider = UISliders.AddSliderWithValue(this, LeftMargin, currentY, Translations.Translate("TFC_OPT_ORP"), 0f, 4f, 1f, Matching.OutsideRailPriority);
            railSlider.eventValueChanged += (control, value) => Matching.OutsideRailPriority = (int)value.RoundToNearest(1f);
            railSlider.tooltip = Translations.Translate("TFC_OPT_ORP_TIP");
            railSlider.tooltipBox = UIToolTips.WordWrapToolTip;
            currentY += railSlider.parent.height + GroupMargin;

            // Outside shipping connection priority slider.
            UISlider shippingSlider = UISliders.AddSliderWithValue(this, LeftMargin, currentY, Translations.Translate("TFC_OPT_OSP"), 0f, 4f, 1f, Matching.OutsideShipPriority);
            shippingSlider.eventValueChanged += (control, value) => Matching.OutsideShipPriority = (int)value.RoundToNearest(1f);
            shippingSlider.tooltip = Translations.Translate("TFC_OPT_OSP_TIP");
            shippingSlider.tooltipBox = UIToolTips.WordWrapToolTip;
            currentY += shippingSlider.parent.height + GroupMargin;

            // Pathfind failure montioring checkbox.
            UICheckBox blockPathfindCheck = UICheckBoxes.AddPlainCheckBox(this, LeftMargin, currentY, Translations.Translate("TFC_OPT_PAT"));
            blockPathfindCheck.tooltip = Translations.Translate("TFC_OPT_PAT_TIP");
            blockPathfindCheck.tooltipBox = UIToolTips.WordWrapToolTip;
            blockPathfindCheck.isChecked = PathFindFailure.EnableFailTracking;
            blockPathfindCheck.eventCheckChanged += (control, isChecked) => PathFindFailure.EnableFailTracking = isChecked;
        }
    }
}