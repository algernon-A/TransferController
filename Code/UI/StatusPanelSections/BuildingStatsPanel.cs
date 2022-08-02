// <copyright file="BuildingStatsPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Building stats panel.
    /// </summary>
    internal class BuildingStatsPanel : StatusPanelSection
    {
        /// <summary>
        /// Panel width.
        /// </summary>
        internal const float PanelWidth = Margin + StatsPanel.PanelWidth + Margin;

        /// <summary>
        /// Panel hieght.
        /// </summary>
        internal const float PanelHeight = OffersPanel.PanelHeight;

        // Panel components.
        private StatsPanel _statsPanel;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildingStatsPanel"/> class.
        /// </summary>
        internal BuildingStatsPanel()
        {
            try
            {
                // Basic setup.
                size = new Vector2(PanelWidth, PanelHeight);

                // Title label.
                UILabel titleLabel = UILabels.AddLabel(this, 0f, Margin, Translations.Translate("TFC_BST_TIT"), PanelWidth, 1f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Stats panel.
                _statsPanel = this.AddUIComponent<StatsPanel>();

                // Align bottom with bottom of offers panel.
                _statsPanel.relativePosition = new Vector2(Margin, 27f);
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up offers panel");
            }
        }

        /// <summary>
        /// Updates panel content.
        /// </summary>
        protected override void UpdateContent()
        {
            _statsPanel.UpdateContent(CurrentBuilding);
        }
    }
}
