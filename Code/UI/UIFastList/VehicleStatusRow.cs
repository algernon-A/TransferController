using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
	/// <summary>
	/// Vehicle list item record.
	/// </summary>
	public class VehicleStatusItem
	{
		public ushort vehicleID;
		public string vehicleName;
		public ushort targetBuildingID;
		public TransferManager.TransferReason material;
		public ushort amount;


		/// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="prefab">Vehicle ID</param>
        public VehicleStatusItem(ushort vehicleID, VehicleInfo vehicleInfo, ushort targetBuildingID, byte material, ushort amount)
        {
            this.vehicleID = vehicleID;
            this.vehicleName = TextUtils.GetDisplayName(vehicleInfo);
            this.targetBuildingID = targetBuildingID;
            this.material = (TransferManager.TransferReason)material;
			this.amount = amount;
        }
    }


	/// <summary>
	/// UI fastlist item for vehicles.
	/// </summary>
	public class VehicleStatusRow : StatusRow
	{
		// Layout constants.
		internal const float VehicleNameWidth = 150f;
		internal const float TargetBuildingWidth = 160f;
		internal const float TransferReasonWidth = 115f;
		internal const float TransferAmountWidth = 50f;
		internal const float VehicleNameX = VehicleZoomX + ButtonSize + Margin;
		internal const float TargetBuildingX = BuildingZoomX + ButtonSize + Margin;
		internal const float TransferReasonX = TargetBuildingX + TargetBuildingWidth + Margin;
		internal const float TransferAmountX = TransferReasonX + TransferReasonWidth + Margin;
		internal const float RowWidth = TransferAmountX + TransferAmountWidth + Margin;
		private const float VehicleZoomX = Margin;
		private const float BuildingZoomX = VehicleNameX + VehicleNameWidth + Margin;


		// Components.
		private UILabel vehicleNameLabel, targetBuildingLabel, transferReasonLabel, transferAmountLabel;
		private UIButton vehicleZoomButton, buildingZoomButton;

		// Target IDs.
		private ushort vehicleID;


		/// <summary>
		/// Constructor.
		/// </summary>
		public VehicleStatusRow()
		{
			rowHeight = RowHeight;
		}


		/// <summary>
		/// Generates and displays a list row.
		/// </summary>
		/// <param name="data">Object to list</param>
		/// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
		public override void Display(object data, bool isRowOdd)
		{
			/// Perform initial setup for new rows.
			if (vehicleNameLabel == null)
			{
				isVisible = true;
				canFocus = true;
				isInteractive = true;
				width = RowWidth;
				height = rowHeight;

				// Add text labels.
				vehicleNameLabel = AddLabel(VehicleNameX, VehicleNameWidth);
				targetBuildingLabel = AddLabel(TargetBuildingX, TargetBuildingWidth);
				transferReasonLabel = AddLabel(TransferReasonX, TransferReasonWidth);
				transferAmountLabel = AddLabel(TransferAmountX, TransferAmountWidth);
				transferAmountLabel.textAlignment = UIHorizontalAlignment.Right;

				// Add zoom buttons.
				vehicleZoomButton = AddZoomButton(this, VehicleZoomX, "TFC_VSP_ZTV");
				buildingZoomButton = AddZoomButton(this, BuildingZoomX, "TFC_VSP_ZTB");
				vehicleZoomButton.eventClicked += ZoomToVehicle;
				buildingZoomButton.eventClicked += (c, p) => ZoomToBuilding();
			}

			// Check for valid data.
			if (data is VehicleStatusItem thisItem)
			{
				// Set ID records.
				vehicleID = thisItem.vehicleID;
				buildingID = thisItem.targetBuildingID;

				// Set text.
				vehicleNameLabel.text = thisItem.vehicleName;
				targetBuildingLabel.text = buildingID == 0 ? Translations.Translate("TFC_VSP_RET") : Singleton<BuildingManager>.instance.GetBuildingName(buildingID, InstanceID.Empty);
				transferReasonLabel.text = thisItem.material.ToString();
				transferAmountLabel.text = thisItem.amount.ToString("N0");

				// Set button visibility.
				vehicleZoomButton.Show();
				buildingZoomButton.isVisible = buildingID != 0;
			}
			else
			{
				// Just in case (no valid vehicle record).
				vehicleNameLabel.text = string.Empty;
				targetBuildingLabel.text = string.Empty;
				transferReasonLabel.text = string.Empty;
				transferAmountLabel.text = string.Empty;

				// Hide buttons.
				vehicleZoomButton.Hide();
				buildingZoomButton.Hide();
			}

			// Set initial background as deselected state.
			Deselect(isRowOdd);
		}


		/// <summary>
		/// Zoom to vehicle button event handler.
		/// </summary>
		/// <param name="control">Calling component</param>
		/// <param name="clickEvent">Event parameter</param>
		private void ZoomToVehicle(UIComponent control, UIMouseEventParameter clickEvent)
		{
			if (vehicleID != 0)
			{
				// Go to target building if available.
				InstanceID instance = default;
				instance.Vehicle = vehicleID;
				ToolsModifierControl.cameraController.SetTarget(instance, Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleID].GetLastFramePosition(), zoomIn: true);
			}
		}
	}
}