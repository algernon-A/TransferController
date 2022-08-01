// <copyright file="BuildingRow.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// UIListRow for buildings.
    /// </summary>
    public class BuildingRow : UIListRow
    {
        // Building name label.
        private UILabel _buildingNameLabel;

        // Building ID.
        private ushort _buildingID;

        /// <summary>
        /// Generates and displays a list row.
        /// </summary>
        /// <param name="data">Object data to display.</param>
        /// <param name="rowIndex">Row index number (for background banding).</param>
        public override void Display(object data, int rowIndex)
        {
            // Perform initial setup for new rows.
            if (_buildingNameLabel == null)
            {
                // Add building name label.
                _buildingNameLabel = AddLabel(Margin, parent.width - Margin - Margin);
            }

            // Get building ID and set name label.
            if (data is BuildingItem thisItem)
            {
                _buildingID = thisItem.ID;
                _buildingNameLabel.text = thisItem.Name;
            }
            else
            {
                // Just in case (no valid building record).
                _buildingNameLabel.text = string.Empty;
            }

            // Set initial background as deselected state.
            Deselect(rowIndex);
        }
    }
}