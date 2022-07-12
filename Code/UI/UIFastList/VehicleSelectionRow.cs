using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
	/// <summary>
	/// Vehicle list item record.
	/// </summary>
	public class VehicleItem
	{
		private VehicleInfo vehicleInfo;
		public string name;


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="prefab">Vehicle prefab for this item</param>
		public VehicleItem(VehicleInfo prefab)
		{
			Info = prefab;
		}


		/// <summary>
		/// Vehicle ID for this record.
		/// </summary>
		public VehicleInfo Info
		{
			get => vehicleInfo;

			set
			{
				vehicleInfo = value;

				// Set display name.
				name = TextUtils.GetDisplayName(value);
			}
		}
	}


	/// <summary>
	/// UI fastlist item for vehicles.
	/// </summary>
	public class VehicleSelectionRow : UIBasicRow
	{
		// Layout constants.
		public const float VehicleRowHeight = 40f;
		private const float TextScale = 0.8f;
		private const float VehicleSpriteSize = 40f;

		// Vehicle name label.
		private UILabel vehicleNameLabel;

		// Preview image.
		private UISprite vehicleSprite;

		// Vehicle prefab info.
		protected VehicleInfo info;


		/// <summary>
		/// Constructor.
		/// </summary>
		public VehicleSelectionRow()
		{
			rowHeight = VehicleRowHeight;
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
				width = parent.width;
				height = VehicleRowHeight;

				// Add object name label.
				vehicleNameLabel = AddLabel(VehicleSpriteSize + Margin, parent.width - Margin - Margin, wordWrap: true);

				// Add preview sprite image.
				vehicleSprite = AddUIComponent<UISprite>();
				vehicleSprite.height = VehicleSpriteSize;
				vehicleSprite.width = VehicleSpriteSize;
				vehicleSprite.relativePosition = Vector2.zero;
			}

			// Get building ID and set name label.
			if (data is VehicleItem thisItem)
			{
				info = thisItem.Info;
				vehicleNameLabel.text = thisItem.name;

				vehicleSprite.atlas = info?.m_Atlas;
				vehicleSprite.spriteName = info?.m_Thumbnail;
			}
			else
			{
				// Just in case (no valid district record).
				vehicleNameLabel.text = string.Empty;
			}

			// Set initial background as deselected state.
			Deselect(isRowOdd);
		}

		
		/// <summary>
		/// Updates current replacement selection when this item is selected.
		/// </summary>
		protected override void Selected()
		{
			UIPanel parentPanel = this.parent as UIPanel;
			UIFastList parentFastList = parentPanel?.parent as UIFastList;
			VehicleSelectionPanel parentSelectionPanel = parentFastList?.parent as VehicleSelectionPanel;

			if (parentSelectionPanel != null)
			{
				parentSelectionPanel.SelectedVehicle = info;
			}
			else
			{
				Logging.Error("couldn't find parent selection panel");
			}
		}
	}
}