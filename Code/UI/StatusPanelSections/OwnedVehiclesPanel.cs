// <copyright file="OwnedVehiclesPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using System.Collections.Generic;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Panel to show building owned vehicles.
    /// </summary>
    internal class OwnedVehiclesPanel : StatusPanelSection
    {
        /// <summary>
        /// Panel width.
        /// </summary>
        internal const float PanelWidth = ListWidth + Margin + Margin;

        /// <summary>
        /// Panel height.
        /// </summary>
        internal const float PanelHeight = ListY + ListHeight + Margin;

        // Layout constants - private.
        private const float ListWidth = VehicleStatusRow.RowWidth + ScrollbarWidth;
        private const float ListHeight = UIList.DefaultRowHeight * 6f;

        // Vehicle list.
        private readonly UIList _vehiclesList;

        /// <summary>
        /// Initializes a new instance of the <see cref="OwnedVehiclesPanel"/> class.
        /// </summary>
        internal OwnedVehiclesPanel()
        {
            try
            {
                // Basic setup.
                size = new Vector2(PanelWidth, PanelHeight);

                // Title label.
                UILabel titleLabel = UILabels.AddLabel(this, 0f, Margin, Translations.Translate("TFC_STA_TIO"), PanelWidth, 1f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Header labels.
                UILabels.AddLabel(this, VehicleStatusRow.VehicleNameX + Margin, ListHeaderY, Translations.Translate("TFC_STA_VEH"), VehicleStatusRow.VehicleNameWidth, 0.7f);
                UILabels.AddLabel(this, VehicleStatusRow.TargetBuildingX + Margin, ListHeaderY, Translations.Translate("TFC_LOG_TAR"), VehicleStatusRow.TargetBuildingWidth, 0.7f);
                UILabels.AddLabel(this, VehicleStatusRow.TransferReasonX + Margin, ListHeaderY, Translations.Translate("TFC_STA_MAT"), VehicleStatusRow.TransferReasonWidth, 0.7f);
                UILabel amountLabel = UILabels.AddLabel(this, VehicleStatusRow.TransferAmountX, ListY - 15f, Translations.Translate("TFC_STA_AMT"), VehicleStatusRow.TransferAmountWidth + Margin, 0.7f);
                amountLabel.textAlignment = UIHorizontalAlignment.Right;

                // Vehicle list.
                _vehiclesList = UIList.AddUIList<VehicleStatusRow>(this, Margin, ListY, ListWidth, ListHeight);
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
            ref Building building = ref buildingBuffer[CurrentBuilding];

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
            _vehiclesList.Data = new FastList<object>
            {
                m_buffer = ownedVehicles.ToArray(),
                m_size = ownedVehicles.Count,
            };
        }
    }
}