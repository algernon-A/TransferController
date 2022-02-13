using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
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

			// Display logging line.
			logLine.text = data as string;

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


		/// Updates current replacement selection when this item is selected.
		/// </summary>
		protected override void UpdateSelection()
		{
			// No selection action.
		}
	}
}