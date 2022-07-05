using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Warehouse vehicle controls.
    /// </summary>
    internal class VehicleSelection : UIPanel
    {
        // Layout constants.
        internal const float PanelHeight = VehicleListY + VehicleListHeight + Margin;
        internal const float VehicleListHeight = 240f;
        private const float Margin = 5f;
        private const float VehicleListY = 25f;

        // Panel components.
        private readonly UIButton addVehicleButton, removeVehicleButton;
        private VehicleSelectionPanel vehicleSelectionPanel;
        private SelectedVehiclePanel buildingVehicleSelectionPanel;


        /// <summary>
        /// Current transfer reason.
        /// </summary>
        internal TransferManager.TransferReason TransferReason { get; private set; }


        /// <summary>
        /// Currently selected building
        /// </summary>
        internal ushort CurrentBuilding { get; private set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        internal VehicleSelection()
        {
            // Set size.
            this.height = PanelHeight;
            this.width = BuildingPanel.PanelWidth;

            // 'Add vehicle' button.
            addVehicleButton = BuildingPanelTab.AddIconButton(this, BuildingPanelTab.MidControlX, VehicleListY, BuildingPanelTab.ArrowSize, "TFC_VEH_ADD", TextureUtils.LoadSpriteAtlas("TC-ArrowPlus"));
            addVehicleButton.isEnabled = false;
            addVehicleButton.eventClicked += (control, clickEvent) => AddVehicle(vehicleSelectionPanel.SelectedVehicle);

            // Remove vehicle button.
            removeVehicleButton = BuildingPanelTab.AddIconButton(this, BuildingPanelTab.MidControlX, VehicleListY + BuildingPanelTab.ArrowSize, BuildingPanelTab.ArrowSize, "TFC_VEH_SUB", TextureUtils.LoadSpriteAtlas("TC-ArrowMinus"));
            removeVehicleButton.isEnabled = false;
            removeVehicleButton.eventClicked += (control, clickEvent) => RemoveVehicle();

            // Vehicle selection panels.
            buildingVehicleSelectionPanel = this.AddUIComponent<SelectedVehiclePanel>();
            buildingVehicleSelectionPanel.relativePosition = new Vector2(Margin, VehicleListY);
            buildingVehicleSelectionPanel.ParentPanel = this;
            vehicleSelectionPanel = this.AddUIComponent<VehicleSelectionPanel>();
            vehicleSelectionPanel.ParentPanel = this;
            vehicleSelectionPanel.relativePosition = new Vector2(BuildingPanelTab.RightColumnX, VehicleListY);

            // Vehicle selection panel labels.
            UILabel vehicleSelectionLabel = UIControls.AddLabel(vehicleSelectionPanel, 0f, -15f, Translations.Translate("TFC_VEH_AVA"), BuildingPanelTab.ColumnWidth, 0.8f);
            vehicleSelectionLabel.textAlignment = UIHorizontalAlignment.Center;
            UILabel buildingDistrictSelectionLabel = UIControls.AddLabel(buildingVehicleSelectionPanel, 0f, -15f, Translations.Translate("TFC_VEH_SEL"), BuildingPanelTab.ColumnWidth, 0.8f);
            buildingDistrictSelectionLabel.textAlignment = UIHorizontalAlignment.Center;
        }


        /// <summary>
        /// Sets/changes the currently selected building.
        /// </summary>
        /// <param name="buildingID">New building ID</param>
        /// <param name="reason">Transfer reason for this vehicle selection</param>
        internal void SetTarget(ushort buildingID, TransferManager.TransferReason reason)
        {
            // Ensure valid building.
            if (buildingID != 0)
            {
                CurrentBuilding = buildingID;
                TransferReason = reason;

                buildingVehicleSelectionPanel.RefreshList();
                vehicleSelectionPanel.RefreshList();
            }
        }


        /// <summary>
        /// Update button states when district selections are updated.
        /// </summary>
        internal void SelectionUpdated()
        {
            addVehicleButton.isEnabled = vehicleSelectionPanel.SelectedVehicle != null;
            removeVehicleButton.isEnabled = buildingVehicleSelectionPanel.SelectedVehicle != null;
        }


        /// <summary>
        /// Adds a vehicle to the list for this transfer.
        /// </summary>
        /// <param name="vehicle">Vehicle prefab to add</param>
        private void AddVehicle(VehicleInfo vehicle)
        {
            // Add vehicle to building.
            VehicleControl.AddVehicle(CurrentBuilding, TransferReason, vehicle);

            // Update current selection.
            buildingVehicleSelectionPanel.SelectedVehicle = vehicle;

            // Update district lists.
            buildingVehicleSelectionPanel.RefreshList();
            vehicleSelectionPanel.RefreshList();
        }


        /// <summary>
        /// Removes the currently selected district from the list for this building.
        /// Should be called as base after district has been updated by child class.
        /// </summary>
        private void RemoveVehicle()
        {
            // Remove selected vehicle from building.
            VehicleControl.RemoveVehicle(CurrentBuilding, TransferReason, buildingVehicleSelectionPanel.SelectedVehicle);

            // Clear current selection.
            buildingVehicleSelectionPanel.SelectedVehicle = null;

            // Update vehicle lists.
            buildingVehicleSelectionPanel.RefreshList();
            vehicleSelectionPanel.RefreshList();
        }
    }
}