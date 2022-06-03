using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Warehouse info panel.
    /// </summary>
    internal class WarehouseInfoPanel : BuildingInfoPanel
    {
        // Layout constants.
        private const float DoubleMargin = Margin + Margin;
        private const float CheckHeight = 25f;
        private const float Check1Y = ControlY;
        private const float Check2Y = Check1Y + CheckHeight;
        private const float Check3Y = Check2Y + CheckHeight;
        private const float SliderY = Check3Y + CheckHeight;
        private const float SliderWidth = PanelWidth - DoubleMargin - DoubleMargin;

        // Default vehicle slider maximum.
        private const int MaxReservedVehicles = 16;


        // Panel components.
        private readonly UICheckBox reserveUniqueCheck, reserveOutsideCheck, reserveCityCheck;
        private readonly UISlider reservedVehiclesSlider;

        // Status flag.
        private bool ignoreEvents = false;


        // Layout constants.
        protected override float PanelHeight => base.PanelHeight + (CheckHeight * 3f) + 40f + Margin;


        /// <summary>
        /// Constructor.
        /// </summary>
        internal WarehouseInfoPanel()
        {
            // Add reserve vehicle checkboxes.
            reserveCityCheck = UIControls.LabelledCheckBox(this, Margin, Check1Y, Translations.Translate("TFC_WAR_RVI"), tooltip: Translations.Translate("TFC_WAR_RVI_TIP"));
            reserveCityCheck.tooltipBox = TooltipUtils.TooltipBox;
            reserveUniqueCheck = UIControls.LabelledCheckBox(this, Margin, Check2Y, Translations.Translate("TFC_WAR_RVU"), tooltip: Translations.Translate("TFC_WAR_RVU_TIP"));
            reserveUniqueCheck.tooltipBox = TooltipUtils.TooltipBox;
            reserveOutsideCheck = UIControls.LabelledCheckBox(this, Margin, Check3Y, Translations.Translate("TFC_WAR_RVO"), tooltip: Translations.Translate("TFC_WAR_RVO_TIP"));
            reserveOutsideCheck.tooltipBox = TooltipUtils.TooltipBox;
            reserveUniqueCheck.eventCheckChanged += ReserveUniqueCheckChanged;
            reserveOutsideCheck.eventCheckChanged += ReserveOutsideCheckChanged;
            reserveCityCheck.eventCheckChanged += ReserveCityCheckChanged;

            // Reserved vehicles slider.
            reservedVehiclesSlider = AddVehicleSlider(this, DoubleMargin, SliderY, SliderWidth, WarehouseControl.GetReservedVehicles(CurrentBuilding));
        }


        /// <summary>
        /// Sets/changes the currently selected building.
        /// </summary>
        /// <param name="buildingID">New building ID</param>
        internal override void SetTarget(ushort buildingID)
        {
            base.SetTarget(buildingID);

            // Set checkbox states with event processing suspended.
            ignoreEvents = true;

            // Ensure valid building.
            if (buildingID != 0)
            {
                // Set checkboxes.
                reserveCityCheck.isChecked = WarehouseControl.GetReserveCity(CurrentBuilding);
                reserveUniqueCheck.isChecked = WarehouseControl.GetReserveUnique(CurrentBuilding);
                reserveOutsideCheck.isChecked = WarehouseControl.GetReserveOutside(CurrentBuilding);

                // Set reserved vehicles slider.
                reservedVehiclesSlider.value = WarehouseControl.GetReservedVehicles(CurrentBuilding);

                // Set vehicle slider maximum.
                if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[CurrentBuilding].Info.m_buildingAI is WarehouseAI warehouseAI)
                {
                    reservedVehiclesSlider.maxValue = System.Math.Min(warehouseAI.m_truckCount, MaxReservedVehicles);
                }
                else
                {
                    // Shouldn't ever get here, but just in case...
                    reservedVehiclesSlider.maxValue = MaxReservedVehicles;
                }
            }

            // Resume event processing.
            ignoreEvents = false;
        }


        /// <summary>
        /// Reserve vehicles for unique factories checkbox event handler.
        /// </summary>
        /// <param name="component">Calling component</param>
        /// <param name="isChecked">New checked status</param>
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
                // If this is checked, unchek the reserve vehicles for outside connections checkbox.
                reserveOutsideCheck.isChecked = false;
                reserveCityCheck.isChecked = false;
                WarehouseControl.SetReserveUnique(CurrentBuilding);
            }
            else
            {
                WarehouseControl.ClearReserve(CurrentBuilding);
            }

            // Resume event processing.
            ignoreEvents = false;
        }


        /// <summary>
        /// Reserve vehicles for outside connections checkbox event handler.
        /// </summary>
        /// <param name="component">Calling component</param>
        /// <param name="isChecked">New checked status</param>
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
                // If this is checked, unchek the reserve vehicles for unique factories checkbox.
                reserveUniqueCheck.isChecked = false;
                reserveCityCheck.isChecked = false;
                WarehouseControl.SetReserveOutside(CurrentBuilding);
            }
            else
            {
                WarehouseControl.ClearReserve(CurrentBuilding);
            }

            // Resume event processing.
            ignoreEvents = false;
        }


        /// <summary>
        /// Reserve vehicles for local service checkbox event handler.
        /// </summary>
        /// <param name="component">Calling component</param>
        /// <param name="isChecked">New checked status</param>
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
                // If this is checked, unchek the reserve vehicles for unique factories checkbox.
                reserveOutsideCheck.isChecked = false;
                reserveUniqueCheck.isChecked = false;
                WarehouseControl.SetReserveCity(CurrentBuilding);
            }
            else
            {
                WarehouseControl.ClearReserve(CurrentBuilding);
            }

            // Resume event processing.
            ignoreEvents = false;
        }


        /// <summary>
        /// Reserve vehicles slider change event handler.
        /// </summary>
        /// <param name="component">Calling component</param>
        /// <param name="value">New value</param>
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

            WarehouseControl.SetReservedVehicles(CurrentBuilding, (byte)(value.RoundToNearest(1f)));
        }


        /// <summary>
        /// Adds a slider with integer value display to the specified component.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="xPos">Relative X position</param
        /// <param name="yPos">Relative Y position</param
        /// <param name="width">Slider width</param>
        /// <param name="width">Slider width</param>
        /// <returns>New UISlider</returns>
        private UISlider AddVehicleSlider(UIComponent parent, float xPos, float yPos, float width, float maxValue)
        {
            const float SliderY = 18f;
            const float LabelY = -13f;
            const float SliderHeight = 18f;

            // Slider control.
            UISlider newSlider = parent.AddUIComponent<UISlider>();
            newSlider.size = new Vector2(width, SliderHeight);
            newSlider.relativePosition = new Vector2(xPos, yPos + SliderY);
            newSlider.name = name;

            // Tooltip.
            newSlider.tooltip = Translations.Translate("TFC_WAR_RVC_TIP");

            // Title label.
            UILabel titleLabel = UIControls.AddLabel(newSlider, 0f, LabelY, Translations.Translate("TFC_WAR_RVC") + ": ", textScale: 0.7f);

            // Value label.
            UILabel valueLabel = UIControls.AddLabel(newSlider, titleLabel.width, LabelY, "0", textScale: 0.7f);
            newSlider.objectUserData = valueLabel;

            // Slider track.
            UISlicedSprite sliderSprite = newSlider.AddUIComponent<UISlicedSprite>();
            sliderSprite.atlas = TextureUtils.InGameAtlas;
            sliderSprite.spriteName = "BudgetSlider";
            sliderSprite.size = new Vector2(newSlider.width, 9f);
            sliderSprite.relativePosition = new Vector2(0f, 4f);

            // Slider thumb.
            UISlicedSprite sliderThumb = newSlider.AddUIComponent<UISlicedSprite>();
            sliderThumb.atlas = TextureUtils.InGameAtlas;
            sliderThumb.spriteName = "SliderBudget";
            newSlider.thumbObject = sliderThumb;

            // Add value changed event handler.
            newSlider.eventValueChanged += ReservedVehiclesSliderChanged;

            // Set initial values.
            newSlider.stepSize = 1f;
            newSlider.minValue = 0f;

            return newSlider;
        }
    }
}