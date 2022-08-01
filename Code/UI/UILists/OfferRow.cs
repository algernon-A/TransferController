// <copyright file="OfferRow.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using AlgernonCommons.Translation;
    using ColossalFramework.UI;

    /// <summary>
    /// UI list row for open offers.
    /// </summary>
    public class OfferRow : StatusRow
    {
        /// <summary>
        /// Row width.
        /// </summary>
        internal const float RowWidth = PriorityX + PriorityWidth + Margin;

        // Components.
        private UILabel _reasonLabel;
        private UILabel _directionLabel;
        private UILabel _priorityLabel;

        /// <summary>
        /// Generates and displays a row.
        /// </summary>
        /// <param name="data">Object data to display.</param>
        /// <param name="rowIndex">Row index number (for background banding).</param>
        public override void Display(object data, int rowIndex)
        {
            // Perform initial setup for new rows.
            if (_reasonLabel == null)
            {
                // Add text labels.
                _directionLabel = AddLabel(DirectionX, DirectionWidth);
                _reasonLabel = AddLabel(ReasonX, ReasonWidth);
                _priorityLabel = AddLabel(PriorityX, PriorityWidth);
                _priorityLabel.textAlignment = UIHorizontalAlignment.Center;
            }

            // Check for valid data.
            if (data is OfferItem thisOffer)
            {
                // Set text.
                _directionLabel.text = Translations.Translate(thisOffer.IsIncoming ? "TFC_LOG_IN" : "TFC_LOG_OU");
                _reasonLabel.text = thisOffer.Reason.ToString();
                _priorityLabel.text = thisOffer.Priority.ToString();
            }
            else
            {
                // Just in case (no valid offer record).
                _directionLabel.text = string.Empty;
                _reasonLabel.text = string.Empty;
                _priorityLabel.text = string.Empty;
            }

            // Set initial background as deselected state.
            Deselect(rowIndex);
        }
    }
}