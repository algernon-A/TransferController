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
        private const float SecondaryHeight = VehicleListY + VehicleSelection.PanelHeight + 20f;

        // Panel components.
        private VehicleSelection vehicleSelection, secondaryVehicleSelection;
        private WarehouseControls warehouseControls;


        /// <summary>
        /// Whether or not a secondary vehicle selection is required.
        /// </summary>
        private bool HasSecondVehicleType => TransferReason == TransferManager.TransferReason.Mail;


        /// <summary>
        /// Current content height.
        /// </summary>
        internal override float ContentHeight
        {
            get
            {
                // Panel height depends on which secondary compenent is visible (if any).
                if (HasSecondVehicleType)
                {
                    return secondaryVehicleSelection.relativePosition.y + secondaryVehicleSelection.height + Margin;
                }
                else if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[CurrentBuilding].Info.m_buildingAI is WarehouseAI)
                {
                    return warehouseControls.relativePosition.y + warehouseControls.height + Margin;
                }

                // Default is just high enough to cover the primary vehicle selection.
                return vehicleSelection.relativePosition.y + vehicleSelection.height + Margin;
            }
        }


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        /// <param name="parentPanel">Containing UI panel</param>
        internal BuildingVehiclesTab(UIPanel parentPanel) : base(parentPanel)
        {
            try
            {
                vehicleSelection = parentPanel.AddUIComponent<VehicleSelection>();
                vehicleSelection.relativePosition = new Vector2(0f, VehicleListY);
                secondaryVehicleSelection = parentPanel.AddUIComponent<VehicleSelection>();
                secondaryVehicleSelection.relativePosition = new Vector2(0f, SecondaryHeight);

                // Warehouse vehicle controls panel.
                warehouseControls = parentPanel.AddUIComponent<WarehouseControls>();
                warehouseControls.relativePosition = new Vector2(0f, SecondaryHeight);

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
            if (HasSecondVehicleType)
            {
                secondaryVehicleSelection.SetTarget(CurrentBuilding, TransferManager.TransferReason.None);
                secondaryVehicleSelection.Show();
            }
            else
            {
                secondaryVehicleSelection.Hide();
            }

            // Resize panel.
            panel.height = ContentHeight;
        }
    }
}