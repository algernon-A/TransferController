using System;
using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Transfer panel (setting restrictions for the given transfer).
    /// </summary>
    internal class TransferVehicleTab : TransferPanelTab
    {
        // Layout constants.
        private const float VehicleListY = 25f;
        internal const float VehicleListHeight = 240f;


        // Panel components.
        private readonly UIButton addVehicleButton, removeVehicleButton;
        private VehicleSelectionPanel vehicleSelectionPanel;
        private SelectedVehiclePanel buildingVehicleSelectionPanel;


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        /// <param name="parentPanel">Containing UI panel</param>
        internal TransferVehicleTab(UIPanel parentPanel)
        {
            try
            {
                // 'Add vehicle' button.
                addVehicleButton = AddIconButton(parentPanel, MidControlX, ListY, ArrowSize, "TFC_VEH_ADD", TextureUtils.LoadSpriteAtlas("TC-ArrowPlus"));
                addVehicleButton.isEnabled = false;
                addVehicleButton.eventClicked += (control, clickEvent) => AddVehicle(vehicleSelectionPanel.SelectedVehicle);

                // Remove vehicle button.
                removeVehicleButton = AddIconButton(parentPanel, MidControlX, ListY + ArrowSize, ArrowSize, "TFC_VEH_SUB", TextureUtils.LoadSpriteAtlas("TC-ArrowMinus"));
                removeVehicleButton.isEnabled = false;
                removeVehicleButton.eventClicked += (control, clickEvent) => RemoveVehicle();

                // Vehicle selection panels.
                buildingVehicleSelectionPanel = parentPanel.AddUIComponent<SelectedVehiclePanel>();
                buildingVehicleSelectionPanel.relativePosition = new Vector2(Margin, VehicleListY);
                buildingVehicleSelectionPanel.ParentPanel = this;
                vehicleSelectionPanel = parentPanel.AddUIComponent<VehicleSelectionPanel>();
                vehicleSelectionPanel.ParentPanel = this;
                vehicleSelectionPanel.relativePosition = new Vector2(RightColumnX, VehicleListY);

                // Vehicle selection panel labels.
                UILabel vehicleSelectionLabel = UIControls.AddLabel(vehicleSelectionPanel, 0f, -15f, Translations.Translate("TFC_VEH_AVA"), ColumnWidth, 0.8f);
                vehicleSelectionLabel.textAlignment = UIHorizontalAlignment.Center;
                UILabel buildingDistrictSelectionLabel = UIControls.AddLabel(buildingVehicleSelectionPanel, 0f, -15f, Translations.Translate("TFC_VEH_SEL"), ColumnWidth, 0.8f);
                buildingDistrictSelectionLabel.textAlignment = UIHorizontalAlignment.Center;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up TransferVehicleTab");
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
        /// Refreshes the controls with current data.
        /// </summary>
        protected override void Refresh()
        {
            buildingVehicleSelectionPanel.RefreshList();
            vehicleSelectionPanel.RefreshList();
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