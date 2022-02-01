using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
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
				height = RowHeight;

				// Add object name label.
				districtNameLabel = AddUIComponent<UILabel>();
				districtNameLabel.width = this.width - 10f;
				districtNameLabel.textScale = TextScale;
				districtNameLabel.font = FontUtils.Regular;
			}

			// Get district ID and set name label according to district type.
			districtID = (int)data;
			if (districtID < 0)
			{
				// Park area is negative district ID.
				districtNameLabel.text = "[p] " + Singleton<DistrictManager>.instance.GetParkName(-districtID);
			}
			else
			{
				// Generic district is positive district ID.
				districtNameLabel.text = Singleton<DistrictManager>.instance.GetDistrictName(districtID);
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

			if (districtNameLabel != null)
			{
				districtNameLabel.relativePosition = new Vector2(LeftMargin, PaddingY);
			}
		}


		/// Updates current replacement selection when this item is selected.
		/// </summary>
		protected override void UpdateSelection()
		{
			UIPanel parentPanel = this.parent as UIPanel;
			UIFastList parentFastList = parentPanel?.parent as UIFastList;
			DistrictSelectionPanel parentSelectionPanel = parentFastList?.parent as DistrictSelectionPanel;

			if (parentSelectionPanel != null)
			{
				Logging.Message("found parent selection panel");
				parentSelectionPanel.selectedDistrict = districtID;
			}
			else
            {
				Logging.Error("couldn't find parent selection panel");
            }
		}
	}
}