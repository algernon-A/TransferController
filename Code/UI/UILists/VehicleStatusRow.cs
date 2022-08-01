// <copyright file="VehicleStatusRow.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using AlgernonCommons.Translation;
    using ColossalFramework;
    using ColossalFramework.UI;

    /// <summary>
    /// UI fastlist item for vehicles.
    /// </summary>
    public class VehicleStatusRow : StatusRow
    {
        /// <summary>
        /// Vehicle name column width.
        /// </summary>
        internal const float VehicleNameWidth = 150f;

        /// <summary>
        /// Target building name column width.
        /// </summary>
        internal const float TargetBuildingWidth = 160f;

        /// <summary>
        /// Transfer reason column width.
        /// </summary>
        internal const float TransferReasonWidth = 115f;

        /// <summary>
        /// Transfer amount column width.
        /// </summary>
        internal const float TransferAmountWidth = 50f;

        /// <summary>
        /// Vehicle name column relative X position.
        /// </summary>
        internal const float VehicleNameX = VehicleZoomX + ButtonSize + Margin;

        /// <summary>
        /// Target building name column relative X position.
        /// </summary>
        internal const float TargetBuildingX = BuildingZoomX + ButtonSize + Margin;

        /// <summary>
        /// Transfer reason column relative X position.
        /// </summary>
        internal const float TransferReasonX = TargetBuildingX + TargetBuildingWidth + Margin;

        /// <summary>
        /// Transfer amount column relative X position.
        /// </summary>
        internal const float TransferAmountX = TransferReasonX + TransferReasonWidth + Margin;

        /// <summary>
        /// Row width.
        /// </summary>
        internal const float RowWidth = TransferAmountX + TransferAmountWidth + Margin;

        // Layout constants - private.
        private const float VehicleZoomX = Margin;
        private const float BuildingZoomX = VehicleNameX + VehicleNameWidth + Margin;

        // Components.
        private UILabel _vehicleNameLabel;
        private UILabel _targetBuildingLabel;
        private UILabel _transferReasonLabel;
        private UILabel _transferAmountLabel;
        private UIButton _vehicleZoomButton;
        private UIButton _buildingZoomButton;

        // Target IDs.
        private ushort _vehicleID;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleStatusRow"/> class.
        /// </summary>
        public VehicleStatusRow()
        {
            height = RowHeight;
        }

        /// <summary>
        /// Generates and displays a row.
        /// </summary>
        /// <param name="data">Object data to display.</param>
        /// <param name="rowIndex">Row index number (for background banding).</param>
        public override void Display(object data, int rowIndex)
        {
            // Perform initial setup for new rows.
            if (_vehicleNameLabel == null)
            {
                // Add text labels.
                _vehicleNameLabel = AddLabel(VehicleNameX, VehicleNameWidth);
                _targetBuildingLabel = AddLabel(TargetBuildingX, TargetBuildingWidth);
                _transferReasonLabel = AddLabel(TransferReasonX, TransferReasonWidth);
                _transferAmountLabel = AddLabel(TransferAmountX, TransferAmountWidth);
                _transferAmountLabel.textAlignment = UIHorizontalAlignment.Right;

                // Add zoom buttons.
                _vehicleZoomButton = AddZoomButton(this, VehicleZoomX, "TFC_STA_ZTV");
                _buildingZoomButton = AddZoomButton(this, BuildingZoomX, "TFC_STA_ZTB");
                _vehicleZoomButton.eventClicked += ZoomToVehicle;
                _buildingZoomButton.eventClicked += (c, p) => BuildingPanel.ZoomToBuilding(BuildingID);
            }

            // Check for valid data.
            if (data is VehicleStatusItem thisItem)
            {
                // Set ID records.
                _vehicleID = thisItem.VehicleID;
                BuildingID = thisItem.TargetBuildingID;

                // Set text.
                _vehicleNameLabel.text = thisItem.Name;
                _targetBuildingLabel.text = BuildingID == 0 ? Translations.Translate("TFC_STA_RET") : Singleton<BuildingManager>.instance.GetBuildingName(BuildingID, InstanceID.Empty);
                _transferReasonLabel.text = thisItem.Material.ToString();
                _transferAmountLabel.text = thisItem.Amount.ToString("N0");

                // Set button visibility.
                _vehicleZoomButton.Show();
                _buildingZoomButton.isVisible = BuildingID != 0;
            }
            else
            {
                // Just in case (no valid vehicle record).
                _vehicleNameLabel.text = string.Empty;
                _targetBuildingLabel.text = string.Empty;
                _transferReasonLabel.text = string.Empty;
                _transferAmountLabel.text = string.Empty;

                // Hide buttons.
                _vehicleZoomButton.Hide();
                _buildingZoomButton.Hide();
            }

            // Set initial background as deselected state.
            Deselect(rowIndex);
        }

        /// <summary>
        /// Zoom to vehicle button event handler.
        /// </summary>
        /// <param name="control">Calling component.</param>
        /// <param name="clickEvent">Event parameter.</param>
        private void ZoomToVehicle(UIComponent control, UIMouseEventParameter clickEvent)
        {
            if (_vehicleID != 0)
            {
                // Go to target building if available.
                InstanceID instance = default;
                instance.Vehicle = _vehicleID;
                ToolsModifierControl.cameraController.SetTarget(instance, Singleton<VehicleManager>.instance.m_vehicles.m_buffer[_vehicleID].GetLastFramePosition(), zoomIn: true);
            }
        }
    }
}