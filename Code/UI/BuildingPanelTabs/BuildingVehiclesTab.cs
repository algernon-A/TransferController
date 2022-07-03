using System;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Vehicles tab for buildings.
    /// </summary>
    internal class BuildingVehiclesTab : BuildingPanelTab
    {
        // Layout constants.
        private const float VehicleListY = 5f;
        private const float ControlsY = VehicleListY + VehicleSelection.PanelHeight + 20f;

        // Panel components.
        private VehicleSelection vehicleSelection, secondaryVehicleSelection;
        private WarehouseControls warehouseControls;


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        /// <param name="parentPanel">Containing UI panel</param>
        internal BuildingVehiclesTab(UIPanel parentPanel)
        {
            try
            {
                vehicleSelection = parentPanel.AddUIComponent<VehicleSelection>();
                vehicleSelection.relativePosition = new Vector2(0f, VehicleListY);
                secondaryVehicleSelection = parentPanel.AddUIComponent<VehicleSelection>();
                secondaryVehicleSelection.relativePosition = new Vector2(0f, ControlsY);

                // Warehouse vehicle controls panel.
                warehouseControls = parentPanel.AddUIComponent<WarehouseControls>();
                warehouseControls.relativePosition = new Vector2(0f, ControlsY);

            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up BuildingVehiclesTab");
            }
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
                warehouseControls.SetTarget(CurrentBuilding);

                // Also update material to reflect warehouses' current setting.
                TransferReason = warehouseAI.GetActualTransferReason(CurrentBuilding, ref building);

                warehouseControls.Show();
            }
            else
            {
                warehouseControls.Hide();
            }

            // Set vehicle selection.
            vehicleSelection.SetTarget(CurrentBuilding, TransferReason);

            // Activate secondary vehicle selection if the primary reason is mail.
            if (TransferReason == TransferManager.TransferReason.Mail)
            {
                secondaryVehicleSelection.SetTarget(CurrentBuilding, TransferManager.TransferReason.None);
                secondaryVehicleSelection.Show();
            }
            else
            {
                secondaryVehicleSelection.Hide();
            }
        }
    }
}