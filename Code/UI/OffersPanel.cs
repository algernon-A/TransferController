using System;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


using System.Reflection;

namespace TransferController
{
    /// <summary>
    /// Panel to show current building offers.
    /// </summary>
    internal class OffersPanel : UIPanel
    {
        // Layout constants.
        private const float Margin = 5f;
        private const float PanelWidth = BuildingVehiclesTab.PanelWidth;
        private const float ListHeaderY = 45f;
        private const float ListY = ListHeaderY + 15f;
        private const float ListHeight = StatusRow.RowHeight * 8f;
        private const float PanelHeight = ListY + ListHeight + Margin;


        // Offer list.
        private readonly UIFastList offersList;

        // Current selection.
        private ushort currentBuilding;

        // Timer.
        private float ticks;
        VehicleStatusPanel vehicleStatusPanel;

        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        internal OffersPanel()
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
                size = new Vector2(PanelWidth, PanelHeight);

                // Title label.
                UILabel titleLabel = UIControls.AddLabel(this, 0f, 10f, Translations.Translate("TFC_OFF_TIT"), PanelWidth, 1.2f);
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

                // Header labels.
                UIControls.AddLabel(this, OfferRow.ReasonX + Margin, ListHeaderY, Translations.Translate("TFC_LOG_MAT"), textScale: 0.7f);
                UIControls.AddLabel(this, OfferRow.PriorityX + Margin, ListHeaderY, Translations.Translate("TFC_LOG_PRI"), textScale: 0.7f);

                // Offers list.
                offersList = UIFastList.Create<OfferRow>(this);
                offersList.backgroundSprite = "UnlockingPanel";
                offersList.width = width - 10f;
                offersList.height = ListHeight;
                offersList.canSelect = true;
                offersList.rowHeight = DistrictRow.DefaultRowHeight;
                offersList.autoHideScrollbar = true;
                offersList.relativePosition = new Vector2(Margin, ListY);
                offersList.rowsData = new FastList<object>();
                offersList.selectedIndex = -1;

                // Vehicle status panel.
                vehicleStatusPanel = this.AddUIComponent<VehicleStatusPanel>();
                vehicleStatusPanel.relativePosition = new Vector2(0f, PanelHeight + Margin);
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up offers panel");
            }
        }


        /// <summary>
        /// Called by Unity every update.
        /// Used to refresh the list periodically.
        /// </summary>
        public override void Update()
        {
            base.Update();

            ticks += Time.deltaTime;

            // Refresh every second - maybe too much?
            if (ticks > 1)
            {
                PopulateList();
                ticks = 0f;
            }
        }


        /// <summary>
        /// Sets the target to the selected building.
        /// </summary>
        /// <param name="buildingID">New building ID</param>
        internal void SetTarget(ushort buildingID)
        {
            // Set target building and regenerate the list.
            currentBuilding = buildingID;
            PopulateList();

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
                vehicleStatusPanel.SetTarget(buildingID);
                vehicleStatusPanel.Show();
            }
            else
            {
                // No supported outgoing vehicles - hide vehicle status panel.
                vehicleStatusPanel.Hide();
            }
        }


        /// <summary>
        /// Populates the panel with a list of current transfers.
        /// </summary>
        private void PopulateList()
        {
            List<OfferData> offerList = new List<OfferData>();

            TransferManager tManager = Singleton<TransferManager>.instance;

            // Reflect transfer offer fields.
            FieldInfo incomingOfferField = typeof(TransferManager).GetField("m_incomingOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo outgoingOfferField = typeof(TransferManager).GetField("m_outgoingOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            TransferManager.TransferOffer[] incomingOffers = incomingOfferField.GetValue(tManager) as TransferManager.TransferOffer[];
            TransferManager.TransferOffer[] outgoingOffers = outgoingOfferField.GetValue(tManager) as TransferManager.TransferOffer[];

            // Find offers to this building.
            for (int i = 0; i < incomingOffers.Length; ++i)
            {
                // Calculate reason and priority blocks.
                TransferManager.TransferReason thisReason = (TransferManager.TransferReason)((i & 0xFFFFF800) >> 11);
                byte priority =(byte)((i & 0x0700) >> 8);

                // Incoming offers.
                if (incomingOffers[i].Building == currentBuilding)
                {
                    // Add to list.
                    offerList.Add(new OfferData(thisReason, priority, true));
                }

                // Outgoing offers.
                if (outgoingOffers[i].Building == currentBuilding)
                {
                    // Add to list.
                    offerList.Add(new OfferData(thisReason, priority, false));
                }
            }

            // Set fastlist items.
            offersList.rowsData = new FastList<object>
            {
                m_buffer = offerList.ToArray(),
                m_size = offerList.Count
            };
        }
    }
}