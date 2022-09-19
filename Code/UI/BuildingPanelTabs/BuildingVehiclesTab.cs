// <copyright file="BuildingVehiclesTab.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using AlgernonCommons;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Vehicles tab for buildings.
    /// </summary>
    internal class BuildingVehiclesTab : BuildingPanelTab
    {
        // Layout constants.
        private const float VehicleListY = 5f;
        private const float SecondaryHeight = VehicleListY + VehicleSelection.PanelHeight + 20f;

        // Panel components.
        private VehicleSelection _vehicleSelection;
        private VehicleSelection _secondaryVehicleSelection;
        private WarehouseControls _warehouseControls;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildingVehiclesTab"/> class.
        /// </summary>
        /// <param name="tabSprite">Tab status sprite.</param>
        /// <param name="parentPanel">Containing UI panel.</param>
        internal BuildingVehiclesTab(UIPanel parentPanel, UISprite tabSprite)
            : base(parentPanel, tabSprite)
        {
            try
            {
                // Vehicle selection panels.
                _vehicleSelection = parentPanel.AddUIComponent<VehicleSelection>();
                _vehicleSelection.ParentPanel = this;
                _vehicleSelection.relativePosition = new Vector2(0f, VehicleListY);
                _secondaryVehicleSelection = parentPanel.AddUIComponent<VehicleSelection>();
                _secondaryVehicleSelection.ParentPanel = this;
                _secondaryVehicleSelection.relativePosition = new Vector2(0f, SecondaryHeight);

                // Warehouse vehicle controls panel.
                _warehouseControls = parentPanel.AddUIComponent<WarehouseControls>();
                _warehouseControls.ParentPanel = this;
                _warehouseControls.relativePosition = new Vector2(0f, SecondaryHeight);
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up BuildingVehiclesTab");
            }
        }

        /// <summary>
        /// Gets the current content height.
        /// </summary>
        internal override float ContentHeight
        {
            get
            {
                // Panel height depends on which secondary compenent is visible (if any).
                if (HasSecondVehicleType)
                {
                    return _secondaryVehicleSelection.relativePosition.y + _secondaryVehicleSelection.height + Margin;
                }
                else if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[CurrentBuilding].Info.m_buildingAI is WarehouseAI)
                {
                    return _warehouseControls.relativePosition.y + _warehouseControls.height + Margin;
                }

                // Default is just high enough to cover the primary vehicle selection.
                return _vehicleSelection.relativePosition.y + _vehicleSelection.height + Margin;
            }
        }

        /// <summary>
        /// Gets a value indicating whether (true) or not (false) a secondary vehicle selection is required.
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
                if (thisBuilding.Info.m_buildingAI.GetType().Name.Equals("PrisonCopterPoliceStationAI") && (thisBuilding.m_flags & Building.Flags.Downgrading) == Building.Flags.None)
                {
                     return true;
                }

                // Check for Prison Helicopter police helicopter depots - these send police helicopters and prison helicopters.
                // Police helicopter depots are marked by the 'downgrading' flag being clear.
                if (thisBuilding.Info.m_buildingAI is HelicopterDepotAI && (thisBuilding.m_flags & Building.Flags.Downgrading) == Building.Flags.None)
                {
                     return true;
                }

                return false;
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
                    VehicleControl.HasRecord(CurrentBuilding, (TransferManager.TransferReason)120) ||
                    VehicleControl.HasRecord(CurrentBuilding, (TransferManager.TransferReason)121)));

            StatusSprite.spriteName = hasRecord ? "AchievementCheckedTrue" : "AchievementCheckedFalse";
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
                _warehouseControls.SetTarget(CurrentBuilding);

                // Also update material to reflect warehouses' current setting.
                TransferReason = warehouseAI.GetActualTransferReason(CurrentBuilding, ref building);

                _warehouseControls.Show();
            }
            else
            {
                _warehouseControls.Hide();
            }

            // Set vehicle selection.
            _vehicleSelection.SetTarget(CurrentBuilding, TransferReason);

            // Activate secondary vehicle selection if the primary reason is mail.
            if (HasSecondVehicleType)
            {
                // Secondary transfer reason is 120 (if Prison Helicopter big police station), or otherwise 'none'.
                _secondaryVehicleSelection.SetTarget(
                    CurrentBuilding,
                    TransferReason == TransferManager.TransferReason.Crime ? (TransferManager.TransferReason)120 : TransferManager.TransferReason.None);
                _secondaryVehicleSelection.Show();
            }
            else
            {
                _secondaryVehicleSelection.Hide();
            }

            // Resize panel.
            Panel.height = ContentHeight;

            // Set sprite status.
            SetSprite();
        }
    }
}