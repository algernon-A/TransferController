// <copyright file="PathFailRow.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using AlgernonCommons.Translation;
    using ColossalFramework.UI;

    /// <summary>
    /// UI list item for pathfind failures.
    /// </summary>
    public class PathFailRow : StatusRow
    {
        /// <summary>
        /// Row width.
        /// </summary>
        internal const float RowWidth = BuildingX + BuildingWidth + Margin;

        // Layout constants - private.
        private const float BuildingX = ReasonX;
        private const float BuildingWidth = 200f;

        // Components.
        private UILabel _directionLabel;
        private UILabel _buildingLabel;

        /// <summary>
        /// Generates and displays a row.
        /// </summary>
        /// <param name="data">Object data to display.</param>
        /// <param name="rowIndex">Row index number (for background banding).</param>
        public override void Display(object data, int rowIndex)
        {
            // Perform initial setup for new rows.
            if (_directionLabel == null)
            {
                // Add text labels.
                _directionLabel = AddLabel(DirectionX, DirectionWidth);
                _buildingLabel = AddLabel(BuildingX, BuildingWidth);

                // Add mouse down event to zoom to building.
                this.eventClicked += (c, p) => BuildingPanel.ZoomToBuilding(BuildingID);
            }

            // Check for valid data.
            if (data is PathFailItem pathFail)
            {
                // Set building ID.
                BuildingID = pathFail.BuildingID;

                // Set text.
                _directionLabel.text = Translations.Translate(pathFail.IsIncoming ? "TFC_LOG_IN" : "TFC_LOG_OU");
                _buildingLabel.text = pathFail.BuildingName;
            }
            else
            {
                // Just in case (no valid offer record).
                BuildingID = 0;
                _directionLabel.text = string.Empty;
                _buildingLabel.text = string.Empty;
            }

            // Set initial background as deselected state.
            Deselect(rowIndex);
        }
    }
}