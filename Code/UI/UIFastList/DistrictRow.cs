using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
	/// <summary>
	/// District list item record.
	/// </summary>
	public class DistrictItem
    {
		private int id;
		public Color32 displayColor;
		public string name;


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id">District ID for this item</param>
		public DistrictItem(int id)
        {
			ID = id;
        }


		/// <summary>
		/// District ID for this record.  Negative values represent park districts.
		/// </summary>
		public int ID
        {
			get => id;

			set
			{
				id = value;

				// Local reference.
				DistrictManager districtManager = Singleton<DistrictManager>.instance;

				// Default color is white.
				displayColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

				if (value < 0)
				{
					// Park area.
					name = districtManager.GetParkName(-value);

					// Set park display color if applicable.
					ref DistrictPark park = ref districtManager.m_parks.m_buffer[-value];
					if (park.IsIndustry)
					{
						displayColor = new Color32(255, 230, 160, 255);
					}
					else if (park.IsPark)
                    {
						displayColor = new Color32(140, 255, 200, 255);
					}
				}
				else
                {
					// District.
					name = districtManager.GetDistrictName(value);
				}
			}
        }
    }

	/// <summary>
	/// UI fastlist item for districts.
	/// </summary>
	public class DistrictRow : UIBasicRow
	{
		// Layout constants.
		private const float TextScale = 0.8f;
		private const float LeftMargin = 5f;
		private const float PaddingY = 3f;

		// District name label.
		private UILabel districtNameLabel;

		// District ID.
		protected int districtID;

		/// <summary>
		/// Generates and displays a list row.
		/// </summary>
		/// <param name="data">Object to list</param>
		/// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
		public override void Display(object data, bool isRowOdd)
		{
			/// Perform initial setup for new rows.
			if (districtNameLabel == null)
			{
				isVisible = true;
				canFocus = true;
				isInteractive = true;
				width = parent.width;
				height = rowHeight;

				// Add object name label.
				districtNameLabel = AddUIComponent<UILabel>();
				districtNameLabel.width = this.width - 10f;
				districtNameLabel.textScale = TextScale;
				districtNameLabel.font = FontUtils.Regular;
			}

			// Get district ID and set name label according to district type.
			if (data is DistrictItem thisItem)
			{
				districtID = thisItem.ID;

				if (thisItem.ID < 0)
				{
					// Park area is negative district ID.
					districtNameLabel.text = "[p] " + thisItem.name;
				}
				else
				{
					// Generic district is positive district ID.
					districtNameLabel.text = thisItem.name;
				}
				
				// Set label color.
				districtNameLabel.textColor = thisItem.displayColor;

				// Call OnSizeChanged to set label position.
				OnSizeChanged();
			}
			else
            {
				// Just in case (no valid district record).
				districtNameLabel.text = string.Empty;
            }

			// Set initial background as deselected state.
			Deselect(isRowOdd);
		}


		/// <summary>
		/// Called when dimensions are changed, including as part of initial setup (required to set correct relative position of label).
		/// </summary>
		protected override void OnSizeChanged()
		{
			base.OnSizeChanged();

			if (districtNameLabel != null)
			{
				districtNameLabel.relativePosition = new Vector2(LeftMargin, PaddingY);
			}
		}


		/// Updates current district selection when this item is selected.
		/// </summary>
		protected override void Selected()
		{
			UIPanel parentPanel = this.parent as UIPanel;
			UIFastList parentFastList = parentPanel?.parent as UIFastList;
			DistrictSelectionPanel parentSelectionPanel = parentFastList?.parent as DistrictSelectionPanel;

			if (parentSelectionPanel != null)
			{
				parentSelectionPanel.SelectedDistrict = districtID;
			}
			else
            {
				Logging.Error("couldn't find parent selection panel");
            }
		}
	}
}