using AlgernonCommons;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;


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

				// Add district name label.
				districtNameLabel = AddLabel(Margin, parent.width - Margin - Margin);
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
			}
			else
            {
				// Just in case (no valid district record).
				districtNameLabel.text = string.Empty;
            }

			// Set initial background as deselected state.
			Deselect(isRowOdd);
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