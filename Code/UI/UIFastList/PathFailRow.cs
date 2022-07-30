using AlgernonCommons.Translation;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
	/// <summary>
	/// Class to hold match data for logging offers.
	/// </summary>
	public class PathFailData
	{
		public ushort buildingID;
		public bool incoming;
		public string buildingName;


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="buildingID">Building ID</param
		/// <param name="incoming">True if the failure was from incoming to outgoing, false otherwise</param>
		public PathFailData(ushort buildingID, bool incoming)
		{
			this.buildingID = buildingID;
			this.incoming = incoming;

			// Get target building name.
			ref Building building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
			if (buildingID != 0 && (building.m_flags & Building.Flags.Created) != 0 && building.Info != null)
			{
				buildingName = Singleton<BuildingManager>.instance.GetBuildingName(buildingID, InstanceID.Empty);
			}
			else
			{
				// Invalid building.
				buildingName = string.Empty;
			}
		}
	}


	/// <summary>
	/// UI fastlist item for pathfind failures.
	/// </summary>
	public class PathFailRow : StatusRow
	{
		// Layout constants.
		internal const float RowWidth = BuildingX + BuildingWidth + Margin;
		private const float BuildingX = ReasonX;
		private const float BuildingWidth = 200f;


		// Components.
		private UILabel directionLabel, buildingLabel;


		/// <summary>
		/// Generates and displays a list row.
		/// </summary>
		/// <param name="data">Object to list</param>
		/// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
		public override void Display(object data, bool isRowOdd)
		{
			/// Perform initial setup for new rows.
			if (directionLabel == null)
			{
				isVisible = true;
				canFocus = true;
				isInteractive = true;
				width = RowWidth;
				height = rowHeight;

				// Add text labels.
				directionLabel = AddLabel(DirectionX, DirectionWidth);
				buildingLabel = AddLabel(BuildingX, BuildingWidth);
			}

			// Check for valid data.
			if (data is PathFailData pathFail)
			{
				// Set building ID.
				buildingID = pathFail.buildingID;

				// Set text.
				directionLabel.text = Translations.Translate(pathFail.incoming ? "TFC_LOG_IN" : "TFC_LOG_OU");
				buildingLabel.text = pathFail.buildingName;
			}
			else
			{
				// Just in case (no valid offer record).
				buildingID = 0;
				directionLabel.text = string.Empty;
				buildingLabel.text = string.Empty;
			}

			// Set initial background as deselected state.
			Deselect(isRowOdd);
		}


		/// <summary>
		/// Zooms to pathfind fail target when this row is selected.
		/// </summary>
		protected override void Selected()
		{
			base.Selected();

			if (buildingID != 0)
			{
				BuildingPanel.ZoomToBuilding(buildingID);
			}
		}
	}
}