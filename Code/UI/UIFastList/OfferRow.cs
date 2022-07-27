using ColossalFramework.UI;


namespace TransferController
{
	/// <summary>
	/// Class to hold match data for logging offers.
	/// </summary>
	public class OfferData
	{
		public TransferManager.TransferReason reason;
		public byte priority;
		public bool incoming;


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="reason">Transfer reason</param>
		/// <param name="priority">Offer priority</param>
		/// <param name="incoming">Incoming status</param>
		public OfferData(TransferManager.TransferReason reason, byte priority, bool incoming)
		{
			this.reason = reason;
			this.priority = priority;
			this.incoming = incoming;
		}
	}


	/// <summary>
	/// UI fastlist item for open offers.
	/// </summary>
	public class OfferRow : StatusRow
	{
		// Layout constants.
		internal const float RowWidth = PriorityX + PriorityWidth + Margin;


		// Components.
		private UILabel reasonLabel, directionLabel, priorityLabel;


		/// <summary>
		/// Generates and displays a list row.
		/// </summary>
		/// <param name="data">Object to list</param>
		/// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
		public override void Display(object data, bool isRowOdd)
		{
			/// Perform initial setup for new rows.
			if (reasonLabel == null)
			{
				isVisible = true;
				canFocus = true;
				isInteractive = true;
				width = RowWidth;
				height = rowHeight;

				// Add text labels.
				directionLabel = AddLabel(DirectionX, DirectionWidth);
				reasonLabel = AddLabel(ReasonX, ReasonWidth);
				priorityLabel = AddLabel(PriorityX, PriorityWidth);
				priorityLabel.textAlignment = UIHorizontalAlignment.Center;
			}

			// Check for valid data.
			if (data is OfferData thisOffer)
			{
				// Set text.
				directionLabel.text = Translations.Translate(thisOffer.incoming ? "TFC_LOG_IN" : "TFC_LOG_OU");
				reasonLabel.text = thisOffer.reason.ToString();
				priorityLabel.text = thisOffer.priority.ToString();
			}
			else
			{
				// Just in case (no valid offer record).
				directionLabel.text = string.Empty;
				reasonLabel.text = string.Empty;
				priorityLabel.text = string.Empty;
			}

			// Set initial background as deselected state.
			Deselect(isRowOdd);
		}
	}
}