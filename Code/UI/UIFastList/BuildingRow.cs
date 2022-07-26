using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
	/// <summary>
	/// Building list item record.
	/// </summary>
	public class BuildingItem
	{
		private ushort id;
		public string name;


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id">Building ID for this item</param>
		public BuildingItem(ushort id)
		{
			ID = id;
		}


		/// <summary>
		/// Building ID for this record.
		/// </summary>
		public ushort ID
		{
			get => id;

			set
			{
				id = value;

				// Local reference.
				ref Building building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[id];

				// Check for valid entry.
				if (value != 0 && (building.m_flags & Building.Flags.Created) != 0 && building.Info != null)
				{
					name = Singleton<BuildingManager>.instance.GetBuildingName(id, InstanceID.Empty);
				}
				else
				{
					// Invalid building.
					name = string.Empty;
				}
			}
		}
	}

	/// <summary>
	/// UI fastlist item for buildings.
	/// </summary>
	public class BuildingRow : UIBasicRow
	{
		// Building name label.
		private UILabel buildingNameLabel;

		// Building ID.
		protected ushort buildingID;

		/// <summary>
		/// Generates and displays a list row.
		/// </summary>
		/// <param name="data">Object to list</param>
		/// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
		public override void Display(object data, bool isRowOdd)
		{
			/// Perform initial setup for new rows.
			if (buildingNameLabel == null)
			{
				isVisible = true;
				canFocus = true;
				isInteractive = true;
				width = parent.width;
				height = rowHeight;

				// Add building name label.
				buildingNameLabel = AddLabel(Margin, parent.width - Margin - Margin);
			}

			// Get building ID and set name label.
			if (data is BuildingItem thisItem)
			{
				buildingID = thisItem.ID;
				buildingNameLabel.text = thisItem.name;
			}
			else
			{
				// Just in case (no valid building record).
				buildingNameLabel.text = string.Empty;
			}

			// Set initial background as deselected state.
			Deselect(isRowOdd);
		}


		/// Updates current replacement selection when this item is selected.
		/// </summary>
		protected override void Selected()
		{
			UIPanel parentPanel = this.parent as UIPanel;
			UIFastList parentFastList = parentPanel?.parent as UIFastList;
			SelectedBuildingPanel parentSelectionPanel = parentFastList?.parent as SelectedBuildingPanel;

			if (parentSelectionPanel != null)
			{
				parentSelectionPanel.SelectedBuilding = buildingID;
			}
			else
			{
				Logging.Error("couldn't find parent selection panel");
			}
		}
	}
}