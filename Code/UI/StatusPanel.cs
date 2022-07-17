using System;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Panel to show current building status.
    /// </summary>
    internal class StatusPanel : UIPanel
    {
        // Layout constants.
        private const float Margin = 5f;
        private const float PanelWidth = GuestVehiclesPanel.PanelWidth;
        private const float OffersY = 45f;
        private const float GuestVehiclesY = OffersY + OffersPanel.PanelHeight + Margin;
        private const float OwnedVehiclesY = GuestVehiclesY + GuestVehiclesPanel.PanelHeight + Margin;
        private const float PanelHeight = OwnedVehiclesY + OwnedVehiclesPanel.PanelHeight + Margin;


        // Components.
        private OffersPanel offersPanel;
        private OwnedVehiclesPanel ownedVehiclesPanel;
        private GuestVehiclesPanel guestVehiclesPanel;

        // Current selection.
        protected ushort currentBuilding;


        /// <summary>
        /// Constructor - performs initial setup.
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
                UILabel titleLabel = UIControls.AddLabel(this, 0f, 10f, Translations.Translate("TFC_STA_TIT"), PanelWidth, 1.2f);
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
                offersPanel = this.AddUIComponent<OffersPanel>();
                offersPanel.relativePosition = new Vector2(0f, OffersY);
                guestVehiclesPanel = this.AddUIComponent<GuestVehiclesPanel>();
                guestVehiclesPanel.relativePosition = new Vector2(0f, GuestVehiclesY);
                ownedVehiclesPanel = this.AddUIComponent<OwnedVehiclesPanel>();
                ownedVehiclesPanel.relativePosition = new Vector2(0f, OwnedVehiclesY);
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up status panel");
            }
        }


        /// <summary>
        /// Sets the target to the selected building.
        /// </summary>
        /// <param name="buildingID">New building ID</param>
        internal void SetTarget(ushort buildingID)
        {
            // Set target building.
            currentBuilding = buildingID;
            offersPanel.SetTarget(buildingID);
            guestVehiclesPanel.SetTarget(buildingID);

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
                (buildingAI is DisasterResponseBuildingAI disasterAI && disasterAI.m_vehicleCount > 0)
                )
            {
                // Has outgoing vehicles - show vehicle status panel.
                this.height = PanelHeight;
                ownedVehiclesPanel.SetTarget(buildingID);
                ownedVehiclesPanel.Show();
            }
            else
            {
                // No supported outgoing vehicles - hide vehicle status panel.
                ownedVehiclesPanel.Hide();
                this.height = OwnedVehiclesY;
            }
        }
    }
}