// <copyright file="DistrictRow.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// UIListRow for districts.
    /// </summary>
    public class DistrictRow : UIListRow
    {
        // District name label.
        private UILabel _districtNameLabel;

        /// <summary>
        /// Generates and displays a list row.
        /// </summary>
        /// <param name="data">Object data to display.</param>
        /// <param name="rowIndex">Row index number (for background banding).</param>
        public override void Display(object data, int rowIndex)
        {
            // Perform initial setup for new rows.
            if (_districtNameLabel == null)
            {
                // Add district name label.
                _districtNameLabel = AddLabel(Margin, parent.width - Margin - Margin);
            }

            // Get district ID and set name label according to district type.
            if (data is DistrictItem thisItem)
            {
                if (thisItem.ID < 0)
                {
                    // Park area is negative district ID.
                    _districtNameLabel.text = "[p] " + thisItem.Name;
                }
                else
                {
                    // Generic district is positive district ID.
                    _districtNameLabel.text = thisItem.Name;
                }

                // Set label color.
                _districtNameLabel.textColor = thisItem.Color;
            }
            else
            {
                // Just in case (no valid district record).
                _districtNameLabel.text = string.Empty;
            }

            // Set initial background as deselected state.
            Deselect(rowIndex);
        }
    }
}