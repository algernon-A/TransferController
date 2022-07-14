using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
	/// <summary>
	/// Class to hold match data for logging offers.
	/// </summary>
	public class MatchData
    {
		public TransferManager.TransferReason reason;
		public byte incomingPriority, outgoingPriority;
		public bool incoming, incomingExcluded, outgoingExcluded;
		public ushort buildingID, incomingBuildingID, outgoingBuildingID;
		public Vector3 incomingPos, outgoingPos;
		public bool allowed;


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="reason">Transfer reason</param
		/// <param name="buildingID">Building ID of this building</param>
		/// <param name="incomingPriority">Incoming offer priority</param>
		/// <param name="outgoingPriority">Outgoing offer priority</param>
		/// <param name="incomingExcluded">Incoming offer exclusion status</param>
		/// <param name="outgoingExcluded">Outgoing offer exclusion status</param>
		/// <param name="incomingBuildingID">Incoming building ID</param>
		/// <param name="outgoingBuildingID">Incoming building ID</param>
		/// <param name="incomingPos">Incoming offer position</param>
		/// <param name="outgoingPos">Incoming offer position</param>
		/// <param name="allowed">True if match was permitted, false otherwise</param>
		public MatchData(ushort buildingID, TransferManager.TransferReason reason, byte incomingPriority, byte outgoingPriority, bool incomingExcluded, bool outgoingExcluded, ushort incomingBuildingID, ushort outgoingBuildingID, Vector3 incomingPos, Vector3 outgoingPos, bool allowed)
        {
			this.buildingID = buildingID;
            this.reason = reason;
            this.incomingPriority = incomingPriority;
            this.outgoingPriority = outgoingPriority;
			this.incomingExcluded = incomingExcluded;
			this.outgoingExcluded = outgoingExcluded;
			this.incomingBuildingID = incomingBuildingID;
            this.outgoingBuildingID = outgoingBuildingID;
			this.incomingPos = incomingPos;
			this.outgoingPos = outgoingPos;
			this.allowed = allowed;
        }
    }


	/// <summary>
	/// UI fastlist item for districts.
	/// </summary>
	public class MatchRow : StatusRow
	{
		// Layout constants.
		internal const float TargetWidth = 150f;
		internal const float AllowedWidth = 60f;
		internal const float ThisPriorityX = ReasonX + ReasonWidth + Margin;
		internal const float OtherPriorityX = ThisPriorityX + PriorityWidth + Margin;
		internal const float TargetX = OtherPriorityX + PriorityWidth + Margin;
		internal const float AllowedX = TargetX + TargetWidth + Margin;
		internal const float RowWidth = AllowedX + AllowedWidth + Margin;


		// Components.
		private UILabel directionLabel, reasonLabel, thisPriorityLabel, otherPriorityLabel, targetLabel, allowedLabel;

		// Transfer position.
		private Vector3 transferPos;


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

				// Add text labels
				directionLabel = AddLabel(DirectionX, DirectionWidth);
				reasonLabel = AddLabel(ReasonX, ReasonWidth);
				thisPriorityLabel = AddLabel(ThisPriorityX, PriorityWidth);
				otherPriorityLabel = AddLabel(OtherPriorityX, PriorityWidth);
				targetLabel = AddLabel(TargetX, TargetWidth);
                allowedLabel = AddLabel(AllowedX, AllowedWidth);
				allowedLabel.textAlignment = UIHorizontalAlignment.Center;
            }

            // Check for valid data.
            if (data is MatchData thisMatch)
			{
				// Set ID records.
				if (thisMatch.buildingID == thisMatch.incomingBuildingID)
				{
					// This was the incoming building; target position is the outgoing building.
					buildingID = thisMatch.outgoingBuildingID;
					transferPos = thisMatch.outgoingPos;

					// Set labels.
					directionLabel.text = "In";
					thisPriorityLabel.text = thisMatch.incomingPriority.ToString();
					otherPriorityLabel.text = thisMatch.outgoingPriority.ToString();
					targetLabel.text = thisMatch.outgoingBuildingID == 0 ? string.Empty : Singleton<BuildingManager>.instance.GetBuildingName(thisMatch.outgoingBuildingID, InstanceID.Empty);
					
					// Add warehouse tages.
					if (thisMatch.incomingExcluded)
					{
						thisPriorityLabel.text += "W";
					}
					if (thisMatch.outgoingExcluded)
					{
						otherPriorityLabel.text += "W";
					}
				}
				else
				{
					// This was the outgoing building; target position is the incoming building.
					buildingID = thisMatch.incomingBuildingID;
					transferPos = thisMatch.incomingPos;

					// Set labels.
					directionLabel.text = "Out";
					thisPriorityLabel.text = thisMatch.outgoingPriority.ToString();
					otherPriorityLabel.text = thisMatch.incomingPriority.ToString();
					targetLabel.text = thisMatch.incomingBuildingID == 0 ? string.Empty : Singleton<BuildingManager>.instance.GetBuildingName(thisMatch.incomingBuildingID, InstanceID.Empty);

					// Add warehouse tages.
					if (thisMatch.outgoingExcluded)
					{
						thisPriorityLabel.text += "W";
					}
					if (thisMatch.incomingExcluded)
					{
						otherPriorityLabel.text += "W";
					}
				}

				// Set other text.
				reasonLabel.text = thisMatch.reason.ToString();
				allowedLabel.text = thisMatch.allowed ? "Y" : "N";
			}
			else
			{
				// Just in case (no valid offer record).
				reasonLabel.text = string.Empty;
				directionLabel.text = string.Empty;
				thisPriorityLabel.text = string.Empty;
				otherPriorityLabel.text = string.Empty;
				targetLabel.text = string.Empty;
				allowedLabel.text = string.Empty;
			}

			// Set initial background as deselected state.
			Deselect(isRowOdd);
		}



		/// <summary>
		/// Zooms to offer target when this row is selected.
		/// </summary>
		protected override void Selected()
		{
			base.Selected();

			if (buildingID != 0)
			{
				BuildingPanel.ZoomToBuilding(buildingID);
			}
			else if (transferPos != Vector3.zero)
			{
				// No valid building - move camera target position (clearing any existing target).
				ToolsModifierControl.cameraController.ClearTarget();
				ToolsModifierControl.cameraController.m_targetPosition = transferPos;
			}
		}
    }
}