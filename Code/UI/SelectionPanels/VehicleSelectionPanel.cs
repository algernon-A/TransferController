using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Vehicle selection panel main class.
    /// </summary>
    internal class VehicleSelectionPanel : UIPanel
    {
        // Panel components.
        protected readonly UIVehicleFastList vehicleList;

        // Current selection.
        protected VehicleInfo selectedVehicle;

        // Parent reference.
        internal VehicleSelection ParentPanel { get; set; }


        /// <summary>
        /// Currently selected vehicle.
        /// </summary>
        internal VehicleInfo SelectedVehicle
        {
            get => selectedVehicle;

            set
            {
                selectedVehicle = value;

                // Refresh parent panel button states.
                ParentPanel.SelectionUpdated();
            }
        }


        /// <summary>
        /// Performs initial setup.
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
                vehicleList = UIVehicleFastList.Create<VehicleRow, UIVehicleFastList>(this);
                vehicleList.backgroundSprite = "UnlockingPanel";
                vehicleList.width = BuildingVehiclesTab.ColumnWidth;
                vehicleList.height = VehicleSelection.VehicleListHeight;
                vehicleList.canSelect = true;
                vehicleList.rowHeight = VehicleRow.VehicleRowHeight;
                vehicleList.autoHideScrollbar = true;
                vehicleList.relativePosition = Vector2.zero;
                vehicleList.rowsData = new FastList<object>();
                vehicleList.selectedIndex = -1;

            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up vehicle selection panel");
            }
        }


        /// <summary>
        /// Refreshes the list with current information.
        /// </summary>
        internal void RefreshList()
        {
            // Repopulate the list.
            PopulateList();

            // (Re)select currently-selected vehicle to ensure list selection matches.
            vehicleList.FindVehicle(selectedVehicle);
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

            // Set fastlist items.
            vehicleList.rowsData = new FastList<object>
            {
                m_buffer = items.OrderBy(x => x.name).ToArray(),
                m_size = items.Count
            };
        }
    }
}