using System;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Vehicles tab for buildings.
    /// </summary>
    internal class BuildingVehiclesTab : BuildingPanelTab
    {
        // Layout constants.
        private const float VehicleListY = 5f;
        private const float SecondaryHeight = VehicleListY + VehicleSelection.PanelHeight + 20f;

        // Panel components.
        private VehicleSelection vehicleSelection, secondaryVehicleSelection;
        private WarehouseControls warehouseControls;


        /// <summary>
        /// Whether or not a secondary vehicle selection is required.
        /// </summary>
        private bool HasSecondVehicleType
        {
            get
            {
                // Post offices have secondary vehile type.
                if (TransferReason == TransferManager.TransferReason.Mail)
                {
                    return true;
                }

                // Check for Prison Helicopter big (central) police stations - these send police cars and prison vans.
                // Big stations are marked by the 'downgrading' flag being clear.
                ref Building thisBuilding = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[CurrentBuilding];
                return thisBuilding.Info.m_buildingAI.GetType().Name.Equals("PrisonCopterPoliceStationAI") && (thisBuilding.m_flags & Building.Flags.Downgrading) == Building.Flags.None;
            }
        }


        /// <summary>
        /// Current content height.
        /// </summary>
        internal override float ContentHeight
        {
            get
            {
                // Panel height depends on which secondary compenent is visible (if any).
                if (HasSecondVehicleType)
                {
                    return secondaryVehicleSelection.relativePosition.y + secondaryVehicleSelection.height + Margin;
                }
                else if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[CurrentBuilding].Info.m_buildingAI is WarehouseAI)
                {
                    return warehouseControls.relativePosition.y + warehouseControls.height + Margin;
                }

                // Default is just high enough to cover the primary vehicle selection.
                return vehicleSelection.relativePosition.y + vehicleSelection.height + Margin;
            }
        }


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        /// <param name="tabSprite">Tab status sprite</param>
        /// <param name="parentPanel">Containing UI panel</param>
        internal BuildingVehiclesTab(UIPanel parentPanel, UISprite tabSprite) : base(parentPanel, tabSprite)
        {
            try
            {
                // Vehicle selection panels.
                vehicleSelection = parentPanel.AddUIComponent<VehicleSelection>();
                vehicleSelection.ParentPanel = this;
                vehicleSelection.relativePosition = new Vector2(0f, VehicleListY);
                secondaryVehicleSelection = parentPanel.AddUIComponent<VehicleSelection>();
                secondaryVehicleSelection.ParentPanel = this;
                secondaryVehicleSelection.relativePosition = new Vector2(0f, SecondaryHeight);

                // Warehouse vehicle controls panel.
                warehouseControls = parentPanel.AddUIComponent<WarehouseControls>();
                warehouseControls.ParentPanel = this;
                warehouseControls.relativePosition = new Vector2(0f, SecondaryHeight);

            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up BuildingVehiclesTab");
            }
        }


        /// <summary>
        /// Sets the tab status sprite to the correct state according to current settings.
        /// </summary>
        internal void SetSprite()
        {
            bool hasRecord = VehicleControl.HasRecord(CurrentBuilding, TransferReason) ||
                WarehouseControl.HasRecord(CurrentBuilding) ||
                (HasSecondVehicleType &&
                    (VehicleControl.HasRecord(CurrentBuilding, TransferManager.TransferReason.None) ||
                    VehicleControl.HasRecord(CurrentBuilding, (TransferManager.TransferReason)125)));

            statusSprite.spriteName = hasRecord ? "AchievementCheckedTrue" : "AchievementCheckedFalse";
        }


        /// <summary>
        /// Refreshes the controls with current data.
        /// </summary>
        protected override void Refresh()
        {
            // Show/hide warehouse panel as appropriate.
            ref Building building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[CurrentBuilding];
            if (building.Info.m_buildingAI is WarehouseAI warehouseAI)
            {
                // Update warehouse panel before showing.
                warehouseControls.SetTarget(CurrentBuilding);

                // Also update material to reflect warehouses' current setting.
                TransferReason = warehouseAI.GetActualTransferReason(CurrentBuilding, ref building);

                warehouseControls.Show();
            }
            else
            {
                warehouseControls.Hide();
            }

            // Set vehicle selection.
            vehicleSelection.SetTarget(CurrentBuilding, TransferReason);

            // Activate secondary vehicle selection if the primary reason is mail.
            if (HasSecondVehicleType)
            {
                // Secondary transfer reason is 125 (if Prison Helicopter big police station), or otherwise 'none'.
                secondaryVehicleSelection.SetTarget(CurrentBuilding,
                    TransferReason == TransferManager.TransferReason.Crime ? (TransferManager.TransferReason)125 : TransferManager.TransferReason.None);
                secondaryVehicleSelection.Show();
            }
            else
            {
                secondaryVehicleSelection.Hide();
            }

            // Resize panel.
            panel.height = ContentHeight;

            // Set sprite status.
            SetSprite();
        }
    }
}