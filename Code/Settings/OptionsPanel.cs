// <copyright file="OptionsPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using AlgernonCommons.Keybinding;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

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
        /// Initializes a new instance of the <see cref="OptionsPanel"/> class.
        /// </summary>
        public OptionsPanel()
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
            OptionsKeymapping keyMapping = languageDropDown.parent.parent.gameObject.AddComponent<UUIKeymapping>();
            keyMapping.Panel.relativePosition = new Vector2(LeftMargin, currentY);
            currentY += keyMapping.Panel.height + GroupMargin;

            // New algorithm checkbox.
            UICheckBox newAlgorithmCheck = UICheckBoxes.AddPlainCheckBox(this, LeftMargin, currentY, Translations.Translate("TFC_OPT_NEW"));
            newAlgorithmCheck.tooltip = Translations.Translate("TFC_OPT_NEW_TIP");
            newAlgorithmCheck.tooltipBox = UIToolTips.WordWrapToolTip;
            newAlgorithmCheck.isChecked = Patcher.UseNewAlgorithm;
            newAlgorithmCheck.eventCheckChanged += (control, isChecked) =>
            {
                Patcher.UseNewAlgorithm = isChecked;
                distanceSlider.parent.isVisible = !isChecked;
            };
            currentY += newAlgorithmCheck.height + Margin;

            // Distance multiplier slider.
            distanceSlider = UISliders.AddPlainSliderWithValue(this, LeftMargin, currentY, Translations.Translate("TFC_OPT_DIS"), 0f, 100f, 1f, OldMatching.DistancePercentage);
            distanceSlider.eventValueChanged += (control, value) => OldMatching.DistancePercentage = (int)value.RoundToNearest(1f);
            distanceSlider.tooltip = Translations.Translate("TFC_OPT_DIS_TIP");
            distanceSlider.tooltipBox = UIToolTips.WordWrapToolTip;
            distanceSlider.parent.isVisible = !newAlgorithmCheck.isChecked;
            currentY += distanceSlider.parent.height + GroupMargin;

            // Warehouse priority slider.
            UISlider warehouseSlider = UISliders.AddPlainSliderWithValue(this, LeftMargin, currentY, Translations.Translate("TFC_OPT_WAR"), 0f, 4f, 1f, Matching.WarehousePriority);
            warehouseSlider.eventValueChanged += (control, value) => Matching.WarehousePriority = (int)value.RoundToNearest(1f);
            warehouseSlider.tooltip = Translations.Translate("TFC_OPT_WAR_TIP");
            warehouseSlider.tooltipBox = UIToolTips.WordWrapToolTip;
            currentY += distanceSlider.parent.height + GroupMargin;

            // Outside rail connection priority slider.
            UISlider railSlider = UISliders.AddPlainSliderWithValue(this, LeftMargin, currentY, Translations.Translate("TFC_OPT_ORP"), 0f, 4f, 1f, Matching.OutsideRailPriority);
            railSlider.eventValueChanged += (control, value) => Matching.OutsideRailPriority = (int)value.RoundToNearest(1f);
            railSlider.tooltip = Translations.Translate("TFC_OPT_ORP_TIP");
            railSlider.tooltipBox = UIToolTips.WordWrapToolTip;
            currentY += railSlider.parent.height + GroupMargin;

            // Outside shipping connection priority slider.
            UISlider shippingSlider = UISliders.AddPlainSliderWithValue(this, LeftMargin, currentY, Translations.Translate("TFC_OPT_OSP"), 0f, 4f, 1f, Matching.OutsideShipPriority);
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