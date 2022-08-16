// <copyright file="StatusRow.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// Base row for status row.
    /// </summary>
    public abstract class StatusRow : UIListRow
    {
        /// <summary>
        /// Default row height.
        /// </summary>
        internal const float DefaultRowHeight = UIList.DefaultRowHeight;

        /// <summary>
        /// Transfer direction column relative X position.
        /// </summary>
        internal const float DirectionX = Margin;

        /// <summary>
        /// Transfer reason column relative X position.
        /// </summary>
        internal const float ReasonX = DirectionX + DirectionWidth + Margin;

        /// <summary>
        /// Transfer priority column relative X position.
        /// </summary>
        internal const float PriorityX = ReasonX + ReasonWidth + Margin;

        /// <summary>
        /// Transfer direction column width.
        /// </summary>
        internal const float DirectionWidth = 30f;

        /// <summary>
        /// Transfer reason column width.
        /// </summary>
        internal const float ReasonWidth = 120f;

        /// <summary>
        /// Transfer priority column width.
        /// </summary>
        internal const float PriorityWidth = 20f;

        /// <summary>
        /// Zoom button size.
        /// </summary>
        protected const float ButtonSize = 16f;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusRow"/> class.
        /// </summary>
        public StatusRow()
        {
            height = RowHeight;
        }

        /// <summary>
        /// Gets or sets the row's target building ID.
        /// </summary>
        protected ushort BuildingID { get; set; }

        /// <summary>
        /// Adds a zoom icon button.
        /// </summary>
        /// <param name="parent">Parent UIComponent.</param>
        /// <param name="xPos">Relative X position.</param>
        /// <param name="tooltipKey">Tooltip translation key.</param>
        /// <returns>New UIButton.</returns>
        protected UIButton AddZoomButton(UIComponent parent, float xPos, string tooltipKey) => BuildingPanel.AddZoomButton(parent, xPos, (RowHeight - ButtonSize) / 2f, ButtonSize, tooltipKey);
    }
}