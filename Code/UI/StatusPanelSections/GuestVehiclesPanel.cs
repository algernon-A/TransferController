using AlgernonCommons;
using AlgernonCommons.Translation;
using AlgernonCommons.UI;
using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace TransferController
{
    /// <summary>
    /// Panel to show building guest vehicles.
    /// </summary>
    internal class GuestVehiclesPanel : StatusPanelSection
    {
        // Layout constants.
        internal const float PanelWidth = ListWidth + Margin + Margin;
        internal const float PanelHeight = ListY + ListHeight + Margin;
        private const float ListWidth = VehicleStatusRow.RowWidth + ScrollBarWidth;
        private const float ListHeight = StatusRow.RowHeight * 4f;


        // Vehicle list.
        private readonly UIFastList vehiclesList;


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        internal GuestVehiclesPanel()
        {
            try
            {
                // Basic setup.
                size = new Vector2(PanelWidth, PanelHeight);

                // Title label.
                UILabel titleLabel = UILabels.AddLabel(this, 0f, Margin, Translations.Translate("TFC_STA_TIG"), PanelWidth, 1f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Header labels.
                UILabels.AddLabel(this, VehicleStatusRow.VehicleNameX + Margin, ListY - 15f, Translations.Translate("TFC_STA_VEH"), VehicleStatusRow.VehicleNameWidth, 0.7f);
                UILabels.AddLabel(this, VehicleStatusRow.TargetBuildingX + Margin, ListY - 15f, Translations.Translate("TFC_STA_ORG"), VehicleStatusRow.TargetBuildingWidth, 0.7f);
                UILabels.AddLabel(this, VehicleStatusRow.TransferReasonX + Margin, ListY - 15f, Translations.Translate("TFC_STA_MAT"), VehicleStatusRow.TransferReasonWidth, 0.7f);
                UILabel amountLabel = UILabels.AddLabel(this, VehicleStatusRow.TransferAmountX, ListY - 15f, Translations.Translate("TFC_STA_AMT"), VehicleStatusRow.TransferAmountWidth + Margin, 0.7f);
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
            List<VehicleStatusItem> guestVehicles = new List<VehicleStatusItem>();

            // Iterate through building vehicles and add to owned vehicle list.
            uint vehicleID = building.m_guestVehicles;
            while (vehicleID != 0)
            {
                ref Vehicle thisVehicle = ref vehicleBuffer[vehicleID];

                // Skip vehicles without defined source building (imports that haven't yet been properly allocated).
                if (thisVehicle.m_sourceBuilding != 0)
                {
                    guestVehicles.Add(new VehicleStatusItem((ushort)vehicleID, thisVehicle.Info, thisVehicle.m_sourceBuilding, thisVehicle.m_transferType, thisVehicle.m_transferSize));
                }
                vehicleID = vehicleBuffer[vehicleID].m_nextGuestVehicle;
            }

            // Set fastlist items, without changing the display.
            vehiclesList.rowsData.m_buffer = guestVehicles.ToArray();
            vehiclesList.rowsData.m_size = guestVehicles.Count;
            vehiclesList.Refresh();
        }
    }
}