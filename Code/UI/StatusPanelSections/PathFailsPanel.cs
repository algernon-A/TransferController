// <copyright file="PathFailsPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using System.Collections.Generic;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Panel to show pathfinding failures.
    /// </summary>
    internal class PathFailsPanel : StatusPanelSection
    {
        /// <summary>
        /// Panel width.
        /// </summary>
        internal const float PanelWidth = PathFailRow.RowWidth + ScrollbarWidth + Margin + Margin;

        /// <summary>
        /// Panel height.
        /// </summary>
        internal const float PanelHeight = PathFailListY + ListHeight + Margin;

        // Layout constants - private.
        private const float PathFailListY = ListHeaderY;
        private const float ListHeight = StatusRow.DefaultRowHeight * 5f;
        private const float ListWidth = PanelWidth - Margin - Margin;

        // Panel components.
        private readonly UIList _pathfindList;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathFailsPanel"/> class.
        /// </summary>
        internal PathFailsPanel()
        {
            try
            {
                // Basic setup.
                size = new Vector2(PanelWidth, PanelHeight);

                // Title label.
                UILabel titleLabel = UILabels.AddLabel(this, 0f, Margin, Translations.Translate("TFC_PFF_TIT"), PanelWidth, 1f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Pathfind failure list.
                _pathfindList = UIList.AddUIList<PathFailRow>(this, Margin, PathFailListY, ListWidth, ListHeight);

                // Populate initial data.
                UpdateContent();
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up pathfinding failures panel");
            }
        }

        /// <summary>
        /// Updates panel content.
        /// </summary>
        protected override void UpdateContent()
        {
            // Get filtered log list.
            List<PathFailItem> displayList = PathFindFailure.GetFails(CurrentBuilding);

            // Set fastlist items, without changing the display.
            _pathfindList.Data = new FastList<object>
            {
                m_buffer = displayList.ToArray(),
                m_size = displayList.Count,
            };
        }
    }
}