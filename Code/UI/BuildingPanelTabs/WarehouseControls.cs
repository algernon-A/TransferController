// <copyright file="WarehouseControls.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;

    /// <summary>
    /// Warehouse vehicle controls.
    /// </summary>
    internal class WarehouseControls : UIPanel
    {
        /// <summary>
        /// Panel height.
        /// </summary>
        internal const float PanelHeight = SliderY + 45f;

        // Layout constants - private.
        private const float Margin = 5;
        private const float DoubleMargin = Margin + Margin;
        private const float CheckHeight = 25f;
        private const float Check1Y = Margin;
        private const float Check2Y = Check1Y + CheckHeight;
        private const float Check3Y = Check2Y + CheckHeight;
        private const float SliderY = Check3Y + CheckHeight;
        private const float SliderWidth = BuildingPanel.PanelWidth - DoubleMargin - DoubleMargin;

        // Default vehicle slider maximum.
        private const int MaxReservedVehicles = 16;

        // Panel components.
        private readonly UICheckBox _reserveUniqueCheck;
        private readonly UICheckBox _reserveOutsideCheck;
        private readonly UICheckBox _reserveCityCheck;
        private readonly UISlider _reservedVehiclesSlider;

        // References.
        private ushort currentBuilding;

        // Status flag.
        private bool ignoreEvents = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="WarehouseControls"/> class.
        /// </summary>
        internal WarehouseControls()
        {
            // Set size.
            this.height = PanelHeight;
            this.width = BuildingPanel.PanelWidth;

            // Add reserve vehicle checkboxes.
            _reserveCityCheck = UICheckBoxes.AddLabelledCheckBox(this, Margin, Check1Y, Translations.Translate("TFC_WAR_RVI"), tooltip: Translations.Translate("TFC_WAR_RVI_TIP"));
            _reserveCityCheck.tooltipBox = UIToolTips.WordWrapToolTip;
            _reserveUniqueCheck = UICheckBoxes.AddLabelledCheckBox(this, Margin, Check2Y, Translations.Translate("TFC_WAR_RVU"), tooltip: Translations.Translate("TFC_WAR_RVU_TIP"));
            _reserveUniqueCheck.tooltipBox = UIToolTips.WordWrapToolTip;
            _reserveOutsideCheck = UICheckBoxes.AddLabelledCheckBox(this, Margin, Check3Y, Translations.Translate("TFC_WAR_RVO"), tooltip: Translations.Translate("TFC_WAR_RVO_TIP"));
            _reserveOutsideCheck.tooltipBox = UIToolTips.WordWrapToolTip;
            _reserveUniqueCheck.eventCheckChanged += ReserveUniqueCheckChanged;
            _reserveOutsideCheck.eventCheckChanged += ReserveOutsideCheckChanged;
            _reserveCityCheck.eventCheckChanged += ReserveCityCheckChanged;

            // Reserved vehicles slider.
            _reservedVehiclesSlider = AddVehicleSlider(this, DoubleMargin, SliderY, SliderWidth, WarehouseControl.GetReservedVehicles(currentBuilding));
        }

        /// <summary>
        /// Gets or sets the parent instance reference.
        /// </summary>
        internal BuildingVehiclesTab ParentPanel { get; set; }

        /// <summary>
        /// Sets/changes the currently selected building.
        /// </summary>
        /// <param name="buildingID">New building ID.</param>
        internal void SetTarget(ushort buildingID)
        {
            // Set checkbox states with event processing suspended.
            ignoreEvents = true;

            // Ensure valid building.
            if (buildingID != 0)
            {
                currentBuilding = buildingID;

                // Set checkboxes.
                _reserveCityCheck.isChecked = WarehouseControl.GetReserveCity(buildingID);
                _reserveUniqueCheck.isChecked = WarehouseControl.GetReserveUnique(buildingID);
                _reserveOutsideCheck.isChecked = WarehouseControl.GetReserveOutside(buildingID);

                // Set reserved vehicle slider maximum.
                if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info.m_buildingAI is WarehouseAI warehouseAI)
                {
                    _reservedVehiclesSlider.maxValue = System.Math.Min(warehouseAI.m_truckCount, MaxReservedVehicles);
                }
                else
                {
                    // Shouldn't ever get here, but just in case...
                    _reservedVehiclesSlider.maxValue = MaxReservedVehicles;
                }

                // Set reserved vehicles slider value.
                _reservedVehiclesSlider.value = WarehouseControl.GetReservedVehicles(buildingID);
            }

            // Update component visibility.
            SetVisibility();

            // Resume event processing.
            ignoreEvents = false;
        }

        /// <summary>
        /// Reserve vehicles for unique factories checkbox event handler.
        /// </summary>
        /// <param name="component">Calling component.</param>
        /// <param name="isChecked">New checked status.</param>
        private void ReserveUniqueCheckChanged(UIComponent component, bool isChecked)
        {
            // Don't do anything if we're ignoring events.
            if (ignoreEvents)
            {
                return;
            }

            // Suspend event processing.
            ignoreEvents = true;

            // Set warehouse status to reflect current state.
            if (isChecked)
            {
                // If this is checked, unchek the other checkboxes.
                _reserveOutsideCheck.isChecked = false;
                _reserveCityCheck.isChecked = false;
                WarehouseControl.SetReserveUnique(currentBuilding);
            }
            else
            {
                WarehouseControl.ClearReserve(currentBuilding);
            }

            // Update component visibility.
            SetVisibility();

            // Resume event processing.
            ignoreEvents = false;
        }

        /// <summary>
        /// Reserve vehicles for outside connections checkbox event handler.
        /// </summary>
        /// <param name="component">Calling component.</param>
        /// <param name="isChecked">New checked status.</param>
        private void ReserveOutsideCheckChanged(UIComponent component, bool isChecked)
        {
            // Don't do anything if we're ignoring events.
            if (ignoreEvents)
            {
                return;
            }

            // Suspend event processing.
            ignoreEvents = true;

            // Set warehouse status to reflect current state.
            if (isChecked)
            {
                // If this is checked, unchek the other checkboxes.
                _reserveUniqueCheck.isChecked = false;
                _reserveCityCheck.isChecked = false;
                WarehouseControl.SetReserveOutside(currentBuilding);
            }
            else
            {
                WarehouseControl.ClearReserve(currentBuilding);
            }

            // Update component visibility.
            SetVisibility();

            // Resume event processing.
            ignoreEvents = false;
        }

        /// <summary>
        /// Reserve vehicles for local service checkbox event handler.
        /// </summary>
        /// <param name="component">Calling component.</param>
        /// <param name="isChecked">New checked status.</param>
        private void ReserveCityCheckChanged(UIComponent component, bool isChecked)
        {
            // Don't do anything if we're ignoring events.
            if (ignoreEvents)
            {
                return;
            }

            // Suspend event processing.
            ignoreEvents = true;

            // Set warehouse status to reflect current state.
            if (isChecked)
            {
                // If this is checked, unchek the other checkboxes.
                _reserveOutsideCheck.isChecked = false;
                _reserveUniqueCheck.isChecked = false;
                WarehouseControl.SetReserveCity(currentBuilding);
            }
            else
            {
                WarehouseControl.ClearReserve(currentBuilding);
            }

            // Update component visibility.
            SetVisibility();

            // Resume event processing.
            ignoreEvents = false;
        }

        /// <summary>
        /// Reserve vehicles slider change event handler.
        /// </summary>
        /// <param name="component">Calling component.</param>
        /// <param name="value">New value.</param>
        private void ReservedVehiclesSliderChanged(UIComponent component, float value)
        {
            // Update value label.
            if (component.objectUserData is UILabel valueLabel)
            {
                valueLabel.text = value.RoundToNearest(1f).ToString("N0");
            }

            // Don't do anything further if we're ignoring events.
            if (ignoreEvents)
            {
                return;
            }

            WarehouseControl.SetReservedVehicles(currentBuilding, (byte)value.RoundToNearest(1f));

            // Update component visibility.
            SetVisibility();
        }

        /// <summary>
        /// Sets component visibilities based on current status.
        /// </summary>
        private void SetVisibility()
        {
            _reservedVehiclesSlider.isVisible = _reserveUniqueCheck.isChecked ||
                _reserveOutsideCheck.isChecked ||
                _reserveCityCheck.isChecked;

            // Update tab sprite.
            ParentPanel.SetSprite();
        }

        /// <summary>
        /// Adds a slider with integer value display to the specified component.
        /// </summary>
        /// <param name="parent">Parent component.</param>
        /// <param name="xPos">Relative X position.</param>
        /// <param name="yPos">Relative Y position.</param>
        /// <param name="width">Slider width.</param>
        /// <param name="maxValue">Slider maximum value.</param>
        /// <returns>New UISlider.</returns>
        private UISlider AddVehicleSlider(UIComponent parent, float xPos, float yPos, float width, float maxValue)
        {
            const float LabelY = -13f;

            // Slider control.
            UISlider newSlider = UISliders.AddBudgetSlider(parent, xPos, yPos, width, maxValue, Translations.Translate("TFC_WAR_RVC_TIP"));
            newSlider.name = name;

            // Value label.
            UILabel titleLabel = UILabels.AddLabel(newSlider, 0f, LabelY, Translations.Translate("TFC_WAR_RVC"), textScale: 0.7f);
            UILabel valueLabel = UILabels.AddLabel(newSlider, titleLabel.width, LabelY, "0", textScale: 0.7f);
            newSlider.objectUserData = valueLabel;

            // Add value changed event handler.
            newSlider.eventValueChanged += ReservedVehiclesSliderChanged;

            return newSlider;
        }
    }
}