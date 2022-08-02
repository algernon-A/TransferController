// <copyright file="StatusPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Panel to show current building status.
    /// </summary>
    internal class StatusPanel : UpdatingBuildingPanel
    {
        // Layout constants.
        private const float Margin = 5f;
        private const float PanelWidth = GuestVehiclesPanel.PanelWidth;
        private const float OffersY = 45f;
        private const float GuestVehiclesY = OffersY + OffersPanel.PanelHeight + Margin;
        private const float OwnedVehiclesY = GuestVehiclesY + GuestVehiclesPanel.PanelHeight + Margin;
        private const float PanelHeight = OwnedVehiclesY + OwnedVehiclesPanel.PanelHeight;

        // Components.
        private OffersPanel _offersPanel;
        private BuildingStatsPanel _statsPanel;
        private OwnedVehiclesPanel _ownedVehiclesPanel;
        private GuestVehiclesPanel _guestVehiclesPanel;
        private PathFailsPanel _pathFailspanel;

        // Panel height without pathfails panel.
        private float _baseHeight;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusPanel"/> class.
        /// </summary>
        internal StatusPanel()
        {
            try
            {
                // Basic setup.
                autoLayout = false;
                backgroundSprite = "MenuPanel2";
                opacity = 0.95f;
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = PanelWidth;
                height = PanelHeight;

                // Title.
                UILabel titleLabel = UILabels.AddLabel(this, 0f, 10f, Translations.Translate("TFC_STA_TIT"), PanelWidth, 1.2f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Close button.
                UIButton closeButton = AddUIComponent<UIButton>();
                closeButton.relativePosition = new Vector2(width - 35, 2);
                closeButton.normalBgSprite = "buttonclose";
                closeButton.hoveredBgSprite = "buttonclosehover";
                closeButton.pressedBgSprite = "buttonclosepressed";

                // Close button event handler.
                closeButton.eventClick += (component, clickEvent) =>
                {
                    BuildingPanelManager.Panel?.ResetButtons();
                    Hide();
                };

                // Status panels.
                _offersPanel = this.AddUIComponent<OffersPanel>();
                _offersPanel.relativePosition = new Vector2(0f, OffersY);
                _statsPanel = this.AddUIComponent<BuildingStatsPanel>();
                _statsPanel.relativePosition = new Vector2(PanelWidth - BuildingStatsPanel.PanelWidth, OffersY);
                _guestVehiclesPanel = this.AddUIComponent<GuestVehiclesPanel>();
                _guestVehiclesPanel.relativePosition = new Vector2(0f, GuestVehiclesY);
                _ownedVehiclesPanel = this.AddUIComponent<OwnedVehiclesPanel>();
                _ownedVehiclesPanel.relativePosition = new Vector2(0f, OwnedVehiclesY);
                _pathFailspanel = this.AddUIComponent<PathFailsPanel>();
                _pathFailspanel.isVisible = false;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up status panel");
            }
        }

        /// <summary>
        /// Sets the target to the selected building.
        /// </summary>
        /// <param name="buildingID">New building ID.</param>
        internal override void SetTarget(ushort buildingID)
        {
            // Update child panels.
            _offersPanel.SetTarget(buildingID);
            _statsPanel.SetTarget(buildingID);
            _guestVehiclesPanel.SetTarget(buildingID);
            _pathFailspanel.SetTarget(buildingID);

            // Set vehicle status panel visibility based on building type and vehicle count.
            BuildingAI buildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info.m_buildingAI;
            if (buildingAI is IndustrialBuildingAI ||
                buildingAI is IndustrialExtractorAI ||
                (buildingAI is ExtractingFacilityAI extractingAI && extractingAI.m_outputVehicleCount > 0) ||
                (buildingAI is ProcessingFacilityAI processingAI && processingAI.m_outputVehicleCount > 0) ||
                (buildingAI is WarehouseAI warehouseAI && warehouseAI.m_truckCount > 0) ||
                (buildingAI is FishFarmAI fishFarmAI && fishFarmAI.m_outputVehicleCount > 0) ||
                (buildingAI is FishingHarborAI fishHarborAI && fishHarborAI.m_outputVehicleCount > 0) ||
                (buildingAI is FireStationAI fireStationAI && fireStationAI.m_fireTruckCount > 0) ||
                (buildingAI is PoliceStationAI policeStationAI && policeStationAI.m_policeCarCount > 0) ||
                (buildingAI is HospitalAI hospitalAI && hospitalAI.m_ambulanceCount > 0) ||
                (buildingAI is CemeteryAI cemeteryAI && cemeteryAI.m_hearseCount > 0) ||
                (buildingAI is LandfillSiteAI landfillSiteAI && landfillSiteAI.m_garbageTruckCount > 0) ||
                (buildingAI is PostOfficeAI postOfficeAI && postOfficeAI.m_postTruckCount + postOfficeAI.m_postVanCount > 0) ||
                (buildingAI is MaintenanceDepotAI depotAI && depotAI.m_maintenanceTruckCount > 0) ||
                (buildingAI is HelicopterDepotAI heliAI && heliAI.m_helicopterCount > 0) ||
                (buildingAI is SnowDumpAI snowDumpAI && snowDumpAI.m_snowTruckCount > 0) ||
                (buildingAI is DisasterResponseBuildingAI disasterAI && disasterAI.m_vehicleCount > 0) ||
                buildingAI.GetType().Name.Equals("PrisonCopterPoliceStationAI"))
            {
                // Has outgoing vehicles - show vehicle status panel.
                this.height = PanelHeight;
                _ownedVehiclesPanel.SetTarget(buildingID);
                _ownedVehiclesPanel.Show();
            }
            else
            {
                // No supported outgoing vehicles - hide vehicle status panel.
                _ownedVehiclesPanel.Hide();
                this.height = OwnedVehiclesY - Margin;
            }

            // Record base panel height without pathfinding failures panel.
            _baseHeight = this.height;

            // Perform base actions.
            base.SetTarget(buildingID);
        }

        /// <summary>
        /// Updates panel content.
        /// Checks for current path fail status and adusts visibility accordingly.
        /// </summary>
        protected override void UpdateContent()
        {
            // Check for pathfinding failures relating to the current building.
            if (PathFindFailure.HasFailure(CurrentBuilding))
            {
                // Found at least one - show the pathfinding failures panel if we're not already doing so.
                this.height = _baseHeight + PathFailsPanel.PanelHeight;
                _pathFailspanel.relativePosition = new Vector2(0f, _baseHeight);
                _pathFailspanel.Show();
            }
            else
            {
                // No pathfinding failures - hide the pathfinding failures panel.
                this.height = _baseHeight;
                _pathFailspanel.Hide();
            }
        }
    }
}