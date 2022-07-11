using System;
using System.Text;
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
    internal class VehicleStatusPanel : UIPanel
    {
        // Layout constants.
        private const float Margin = 5f;
        private const float PanelWidth = VehicleStatusRow.RowWidth + Margin + Margin;
        private const float ListTitleY = 45f;
        private const float ListY = ListTitleY + 15f;
        private const float ListHeight = DistrictRow.DefaultRowHeight * 10f;
        private const float PanelHeight = ListY + ListHeight + Margin;


        // Offer list.
        private readonly UIFastList vehiclesList;

        // Current selection.
        private ushort currentBuilding;

        // Timer.
        private float ticks;


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        internal VehicleStatusPanel()
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
                UILabel titleLabel = UIControls.AddLabel(this, 0f, 10f, Translations.Translate("TFC_VSP_TIT"), PanelWidth, 1.2f);
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

                // Vehicle list.
                UIControls.AddLabel(this, VehicleStatusRow.VehicleNameX, ListY - 15f, Translations.Translate("TFC_VSP_VEH"), VehicleStatusRow.VehicleNameWidth + Margin, 0.7f);
                UIControls.AddLabel(this, VehicleStatusRow.TargetBuildingX, ListY - 15f, Translations.Translate("TFC_VSP_TAR"), VehicleStatusRow.TargetBuildingWidth + Margin, 0.7f);
                UIControls.AddLabel(this, VehicleStatusRow.TransferReasonX, ListY - 15f, Translations.Translate("TFC_VSP_MAT"), VehicleStatusRow.TransferReasonWidth + Margin, 0.7f);
                UILabel amountLabel = UIControls.AddLabel(this, VehicleStatusRow.TransferAmountX, ListY - 15f, Translations.Translate("TFC_VSP_AMT"), VehicleStatusRow.TransferAmountWidth + Margin, 0.7f);
                amountLabel.textAlignment = UIHorizontalAlignment.Right;

                vehiclesList = UIFastList.Create<VehicleStatusRow>(this);
                vehiclesList.backgroundSprite = "UnlockingPanel";
                vehiclesList.width = VehicleStatusRow.RowWidth;
                vehiclesList.height = ListHeight;
                vehiclesList.canSelect = true;
                vehiclesList.rowHeight = DistrictRow.DefaultRowHeight;
                vehiclesList.autoHideScrollbar = true;
                vehiclesList.relativePosition = new Vector2(Margin, ListY);
                vehiclesList.rowsData = new FastList<object>();
                vehiclesList.selectedIndex = -1;

            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up vehicle status panel");
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
        }


        /// <summary>
        /// Populates the panel with a list of current transfers.
        /// </summary>
        private void PopulateList()
        {
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;
            Building[] buildingBuffer = buildingManager.m_buildings.m_buffer;
            Vehicle[] vehicleBuffer = vehicleManager.m_vehicles.m_buffer;
            ref Building building = ref buildingBuffer[currentBuilding];

            List<VehicleStatusItem> vehicleList = new List<VehicleStatusItem>();

            ushort vehicleID = building.m_ownVehicles;
            while (vehicleID != 0)
            {
                ref Vehicle thisVehicle = ref vehicleBuffer[vehicleID];

                vehicleList.Add(new VehicleStatusItem(vehicleID, thisVehicle.Info, thisVehicle.m_targetBuilding, thisVehicle.m_transferType, thisVehicle.m_transferSize));

                vehicleID = vehicleBuffer[vehicleID].m_nextOwnVehicle;
            }


            // Set fastlist items.
            vehiclesList.rowsData = new FastList<object>
            {
                m_buffer = vehicleList.ToArray(),
                m_size = vehicleList.Count
            };
        }
    }
}