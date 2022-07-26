using System;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Panel to show building owned vehicles.
    /// </summary>
    internal class OwnedVehiclesPanel : StatusPanelSection
    {
        // Layout constants.
        internal const float PanelWidth = ListWidth + Margin + Margin;
        internal const float PanelHeight = ListY + ListHeight + Margin;
        private const float ListWidth = VehicleStatusRow.RowWidth + ScrollBarWidth;
        private const float ListHeight = StatusRow.RowHeight * 6f;


        // Vehicle list.
        private readonly UIFastList vehiclesList;


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        internal OwnedVehiclesPanel()
        {
            try
            {
                // Basic setup.
                size = new Vector2(PanelWidth, PanelHeight);

                // Title label.
                UILabel titleLabel = UIControls.AddLabel(this, 0f, Margin, Translations.Translate("TFC_STA_TIO"), PanelWidth, 1f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Header labels.
                UIControls.AddLabel(this, VehicleStatusRow.VehicleNameX + Margin, ListHeaderY, Translations.Translate("TFC_STA_VEH"), VehicleStatusRow.VehicleNameWidth, 0.7f);
                UIControls.AddLabel(this, VehicleStatusRow.TargetBuildingX + Margin, ListHeaderY, Translations.Translate("TFC_LOG_TAR"), VehicleStatusRow.TargetBuildingWidth, 0.7f);
                UIControls.AddLabel(this, VehicleStatusRow.TransferReasonX + Margin, ListHeaderY, Translations.Translate("TFC_STA_MAT"), VehicleStatusRow.TransferReasonWidth, 0.7f);
                UILabel amountLabel = UIControls.AddLabel(this, VehicleStatusRow.TransferAmountX, ListY - 15f, Translations.Translate("TFC_STA_AMT"), VehicleStatusRow.TransferAmountWidth + Margin, 0.7f);
                amountLabel.textAlignment = UIHorizontalAlignment.Right;

                // Vehicle list.
                vehiclesList = AddList<VehicleStatusRow>(ListY, ListWidth, ListHeight);
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up vehicle status panel");
            }
        }


        /// <summary>
        /// Updates panel content.
        /// </summary>
        protected override void UpdateContent()
        {
            // Local references.
            Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            Vehicle[] vehicleBuffer = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
            ref Building building = ref buildingBuffer[currentBuilding];

            // List of owned vehicles
            List<VehicleStatusItem> ownedVehicles = new List<VehicleStatusItem>();

            // Iterate through building vehicles and add to owned vehicle list.
            ushort vehicleID = building.m_ownVehicles;
            while (vehicleID != 0)
            {
                ref Vehicle thisVehicle = ref vehicleBuffer[vehicleID];
                ownedVehicles.Add(new VehicleStatusItem(vehicleID, thisVehicle.Info, thisVehicle.m_targetBuilding, thisVehicle.m_transferType, thisVehicle.m_transferSize));
                vehicleID = vehicleBuffer[vehicleID].m_nextOwnVehicle;
            }

            // Set fastlist items, without changing the display.
            vehiclesList.rowsData.m_buffer = ownedVehicles.ToArray();
            vehiclesList.rowsData.m_size = ownedVehicles.Count;
            vehiclesList.Refresh();
        }
    }
}