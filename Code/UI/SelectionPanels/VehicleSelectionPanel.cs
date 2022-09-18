// <copyright file="VehicleSelectionPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AlgernonCommons;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;

    /// <summary>
    /// Vehicle selection panel main class.
    /// </summary>
    internal class VehicleSelectionPanel : UIPanel
    {
        // Vehicle selection list.
        private readonly UIList _vehicleList;

        // Currently selected vehicle.
        private VehicleInfo _selectedVehicle;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleSelectionPanel"/> class.
        /// </summary>
        internal VehicleSelectionPanel()
        {
            try
            {
                // Basic setup.
                name = "VehicleSelectionPanel";
                autoLayout = false;
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = BuildingVehiclesTab.ColumnWidth;
                height = VehicleSelection.VehicleListHeight;

                // Vehicle selection list.
                _vehicleList = UIList.AddUIList<VehicleSelectionRow>(this, 0f, 0f, BuildingVehiclesTab.ColumnWidth, VehicleSelection.VehicleListHeight, VehicleSelectionRow.VehicleRowHeight);
                _vehicleList.EventSelectionChanged += (control, selectedItem) => SelectedVehicle = (selectedItem as VehicleItem)?.Info;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up vehicle selection panel");
            }
        }

        /// <summary>
        /// Gets or sets the parent reference.
        /// </summary>
        internal VehicleSelection ParentPanel { get; set; }

        /// <summary>
        /// Gets or sets the currently selected vehicle.
        /// </summary>
        internal VehicleInfo SelectedVehicle
        {
            get => _selectedVehicle;

            set
            {
                _selectedVehicle = value;

                // Refresh parent panel button states.
                ParentPanel.SelectionUpdated();
            }
        }

        /// <summary>
        /// Gets the vehicle selection list.
        /// </summary>
        protected internal UIList VehicleList => _vehicleList;

        /// <summary>
        /// Refreshes the list with current information.
        /// </summary>
        internal void RefreshList()
        {
            // Clear selected index.
            _vehicleList.SelectedIndex = -1;

            // Repopulate the list.
            PopulateList();
        }

        /// <summary>
        /// Populates the list with available vehicles.
        /// </summary>
        protected virtual void PopulateList()
        {
            // Ensure valid building selection.
            ushort currentBuilding = ParentPanel.CurrentBuilding;
            if (currentBuilding == 0)
            {
                return;
            }

            // Local references.
            Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            BuildingInfo buildingInfo = buildingBuffer[currentBuilding].Info;
            ItemClass buildingClass = buildingInfo.m_class;
            ItemClass.Service buildingService = buildingClass.m_service;
            ItemClass.SubService buildingSubService = buildingClass.m_subService;
            ItemClass.Level buildingLevel = buildingClass.m_level;

            List<VehicleItem> items = new List<VehicleItem>();

            // Player industry requires some translation into private industry equivalents.
            if (buildingService == ItemClass.Service.PlayerIndustry)
            {
                buildingService = ItemClass.Service.Industrial;
                buildingSubService = ItemClass.SubService.None;

                // Get building transfer type for those buildings with variable types.
                TransferManager.TransferReason variableTransferReason = TransferManager.TransferReason.None;
                switch (buildingInfo.m_buildingAI)
                {
                    case WarehouseAI warehouseAI:
                        variableTransferReason = warehouseAI.GetTransferReason(currentBuilding, ref buildingBuffer[currentBuilding]);
                        break;
                    case ExtractingFacilityAI extractorAI:
                        variableTransferReason = extractorAI.m_outputResource;
                        break;
                    case ProcessingFacilityAI processorAI:
                        variableTransferReason = processorAI.m_outputResource;
                        break;
                }

                // Translate into private industry equivalents - conversions are from WarehouseAI.GetTransferVehicleService.
                switch (variableTransferReason)
                {
                    // Ore.
                    case TransferManager.TransferReason.Ore:
                    case TransferManager.TransferReason.Coal:
                    case TransferManager.TransferReason.Glass:
                    case TransferManager.TransferReason.Metals:
                        buildingSubService = ItemClass.SubService.IndustrialOre;
                        break;

                    // Forestry.
                    case TransferManager.TransferReason.Logs:
                    case TransferManager.TransferReason.Lumber:
                    case TransferManager.TransferReason.Paper:
                    case TransferManager.TransferReason.PlanedTimber:
                        buildingSubService = ItemClass.SubService.IndustrialForestry;
                        break;

                    // Oil.
                    case TransferManager.TransferReason.Oil:
                    case TransferManager.TransferReason.Petrol:
                    case TransferManager.TransferReason.Petroleum:
                    case TransferManager.TransferReason.Plastics:
                        buildingSubService = ItemClass.SubService.IndustrialOil;
                        break;

                    // Farming.
                    case TransferManager.TransferReason.Grain:
                    case TransferManager.TransferReason.Food:
                    case TransferManager.TransferReason.Flours:
                        buildingSubService = ItemClass.SubService.IndustrialFarming;
                        break;

                    // Animal products have their own category.
                    case TransferManager.TransferReason.AnimalProducts:
                        buildingService = ItemClass.Service.PlayerIndustry;
                        buildingSubService = ItemClass.SubService.PlayerIndustryFarming;
                        break;

                    // Generic goods.
                    case TransferManager.TransferReason.Goods:
                        buildingSubService = ItemClass.SubService.IndustrialGeneric;
                        break;

                    // Luxury products.
                    case TransferManager.TransferReason.LuxuryProducts:
                        buildingService = ItemClass.Service.PlayerIndustry;
                        break;
                }
            }
            else if (buildingSubService == ItemClass.SubService.PublicTransportPost)
            {
                // Special treatement for post offices - post vans have level 2, others level 5.
                buildingLevel = ParentPanel.TransferReason == TransferManager.TransferReason.Mail ? ItemClass.Level.Level2 : ItemClass.Level.Level5;
            }
            else if (ParentPanel.TransferReason == (TransferManager.TransferReason)128)
            {
                // Prison helicopter mod big police station sends prison vans.
                buildingLevel = ItemClass.Level.Level4;
            }
            else if (ParentPanel.TransferReason == (TransferManager.TransferReason)129)
            {
                // Prison helicopter mod police helicopter depot sends prison helicopters.
                buildingLevel = ItemClass.Level.Level4;
            }

            // Get list of already-selected vehicles.
            List<VehicleInfo> selectedList = VehicleControl.GetVehicles(currentBuilding, ParentPanel.TransferReason);

            // Iterate through all loaded vehicles.
            for (uint i = 0; i < PrefabCollection<VehicleInfo>.LoadedCount(); ++i)
            {
                if (PrefabCollection<VehicleInfo>.GetLoaded(i) is VehicleInfo vehicle)
                {
                    // Looking for service, sub-service and level match.
                    // Level match is ignored if the service is PlayerIndustry, to let general Industries DLC cargo vehicles (level 0) also transfer luxury products (level 1).
                    // Ignore any trailer vehicles.
                    // Ignore any procedural vehicles (e.g. fire helicopter buckets).
                    if (vehicle.m_class.m_service == buildingService &&
                        vehicle.m_class.m_subService == buildingSubService &&
                        (vehicle.m_class.m_level == buildingLevel || buildingService == ItemClass.Service.PlayerIndustry) &&
                        !(vehicle.m_vehicleAI is CarTrailerAI) &&
                        !(vehicle.m_placementStyle == ItemClass.Placement.Procedural) &&
                        (selectedList == null || !selectedList.Contains(vehicle)))
                    {
                        // Check vehicle type, if applicable.
                        if (buildingInfo.m_buildingAI is PlayerBuildingAI playerBuildingAI)
                        {
                            VehicleInfo.VehicleType vehicleType = playerBuildingAI.GetVehicleType();
                            if (vehicleType != VehicleInfo.VehicleType.None && vehicleType != vehicle.m_vehicleType)
                            {
                                continue;
                            }
                        }

                        // All filters passed - add to available list.
                        items.Add(new VehicleItem(vehicle));
                    }
                }
            }

            // Set display list items, without changing the display.
            _vehicleList.Data = new FastList<object>
            {
                m_buffer = items.OrderBy(x => x.Name).ToArray(),
                m_size = items.Count,
            };
        }
    }
}