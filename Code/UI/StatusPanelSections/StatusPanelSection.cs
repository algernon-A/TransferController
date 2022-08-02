// <copyright file="StatusPanelSection.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using AlgernonCommons;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Detail section of building status panel.
    /// </summary>
    internal abstract class StatusPanelSection : UpdatingBuildingPanel
    {
        /// <summary>
        /// Layout margin.
        /// </summary>
        protected const float Margin = 5f;

        /// <summary>
        /// Scrollbar width.
        /// </summary>
        protected const float ScrollbarWidth = 20f;

        /// <summary>
        /// List header relative Y-position.
        /// </summary>
        protected const float ListHeaderY = 25f;

        /// <summary>
        /// List relative Y-position.
        /// </summary>
        protected const float ListY = ListHeaderY + 15f;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusPanelSection"/> class.
        /// </summary>
        internal StatusPanelSection()
        {
            try
            {
                // Basic setup.
                autoLayout = false;
                isVisible = true;
                canFocus = true;
                isInteractive = true;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up status panel section");
            }
        }

        /// <summary>
        /// Adds a UI list of the given row type.
        /// </summary>
        /// <typeparam name="TRow">Row type.</typeparam>
        /// <param name="yPos">Relative Y position.</param>
        /// <param name="listWidth">List width.</param>
        /// <param name="listHeight">List height.</param>
        /// <returns>New UIFastList.</returns>
        protected UIList AddList<TRow>(float yPos, float listWidth, float listHeight)
            where TRow : UIListRow
        {
            // List setup.
            UIList newList = UIList.AddUIList<TRow>(this);
            newList.BackgroundSprite = "UnlockingPanel";
            newList.width = listWidth;
            newList.height = listHeight;
            newList.relativePosition = new Vector2(Margin, yPos);
            newList.Data = new FastList<object>();

            return newList;
        }
    }
}