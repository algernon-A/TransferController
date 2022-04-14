using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
	/// <summary>
	/// Class to hold offer data for logging offers.
	/// </summary>
	public class OfferData
    {
		public string text;
		public ushort buildingID;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="displayText">Text to display</param>
		/// <param name="building">Target building ID (0 for none)</param>
		public OfferData(string displayText, ushort building)
        {
			text = displayText;
			buildingID = building;
        }
    }


	/// <summary>
	/// UI fastlist item for districts.
	/// </summary>
	public class OfferRow : UIBasicRow
	{
		// Layout constants.
		private const float TextScale = 0.8f;
		private const float LeftMargin = 5f;
		private const float PaddingY = 3f;

		// District name label.
		private UILabel logLine;

		// Building ID.
		private ushort buildingID;

		/// <summary>
		/// Generates and displays a list row.
		/// </summary>
		/// <param name="data">Object to list</param>
		/// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
		public override void Display(object data, bool isRowOdd)
		{
			/// Perform initial setup for new rows.
			if (logLine == null)
			{
				isVisible = true;
				canFocus = true;
				isInteractive = true;
				width = parent.width;
				height = RowHeight;

				// Add logging name label.
				logLine = AddUIComponent<UILabel>();
				logLine.width = this.width - 10f;
				logLine.textScale = TextScale;
				logLine.font = FontUtils.Regular;
			}

			// Update text and building ID.
			if (data is OfferData offerData)
			{
				logLine.text = offerData.text;
				buildingID = offerData.buildingID;
			}
			else
            {
				// Clear building ID and text if no valid data.
				logLine.text = string.Empty;
				buildingID = 0;
            }

			// Call OnSizeChanged to set label position.
			OnSizeChanged();

			// Set initial background as deselected state.
			Deselect(isRowOdd);
		}


		/// <summary>
		/// Called when dimensions are changed, including as part of initial setup (required to set correct relative position of label).
		/// </summary>
		protected override void OnSizeChanged()
		{
			base.OnSizeChanged();

			if (logLine != null)
			{
				logLine.relativePosition = new Vector2(LeftMargin, PaddingY);
			}
		}


		/// <summary>
		/// Updates current replacement selection when this item is selected.
		/// </summary>
		protected override void UpdateSelection()
		{
			Logging.Message("updateselection");

			// Got to target building.
			if (buildingID != 0)
            {
				InstanceID instance = default;
				instance.Building = buildingID;
				ToolsModifierControl.cameraController.SetTarget(instance, Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].m_position, zoomIn: true);
			}
		}
	}
}