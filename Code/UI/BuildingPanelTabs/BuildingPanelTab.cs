// <copyright file="BuildingPanelTab.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using AlgernonCommons.Translation;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Building panel tab panel.
    /// </summary>
    internal abstract class BuildingPanelTab
    {
        /// <summary>
        /// Layout column width.
        /// </summary>
        internal const float ColumnWidth = 210f;

        /// <summary>
        /// Arrow button size.
        /// </summary>
        internal const float ArrowSize = 32f;

        /// <summary>
        /// Midpoint controls relative X position.
        /// </summary>
        internal const float MidControlX = Margin + ColumnWidth + Margin;

        /// <summary>
        /// Right column relative X position.
        /// </summary>
        internal const float RightColumnX = MidControlX + ArrowSize + Margin;

        /// <summary>
        /// Column width.
        /// </summary>
        internal const float BuildingColumnWidth = ColumnWidth * 2f;

        /// <summary>
        /// Panel width.
        /// </summary>
        internal const float PanelWidth = RightColumnX + ColumnWidth + Margin;

        /// <summary>
        /// Layout margin.
        /// </summary>
        protected const float Margin = 5f;

        /// <summary>
        /// Checkbox margin.
        /// </summary>
        protected const float CheckMargin = 20f;

        /// <summary>
        /// Checkbox height.
        /// </summary>
        protected const float CheckHeight = 20f;

        /// <summary>
        /// Button height.
        /// </summary>
        protected const float ButtonHeight = 28f;

        // Current selection.
        private ushort _currentBuilding;

        // UI components.
        private UIPanel _panel;
        private UISprite _statusSprite;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildingPanelTab"/> class.
        /// </summary>
        /// <param name="parentPanel">Parent UI panel.</param>
        /// <param name="tabSprite">Tab status sprite.</param>
        internal BuildingPanelTab(UIPanel parentPanel, UISprite tabSprite)
        {
            // Set references.
            _panel = parentPanel;
            _statusSprite = tabSprite;
        }

        /// <summary>
        /// Gets the current content height.
        /// </summary>
        internal abstract float ContentHeight { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this is an incoming (true) or outgoing (false) transfer.
        /// </summary>
        internal bool IsIncoming { get; set; }

        /// <summary>
        /// Gets or sets the current transfer reason.
        /// </summary>
        internal TransferManager.TransferReason TransferReason { get; set; }

        /// <summary>
        /// Gets or sets the currently selected building.
        /// </summary>
        internal ushort CurrentBuilding
        {
            get => _currentBuilding;

            set
            {
                _currentBuilding = value;
                Refresh();
            }
        }

        /// <summary>
        /// Gets the tab's active UI panel.
        /// </summary>
        protected UIPanel Panel => _panel;

        /// <summary>
        /// Gets the tab's status sprite.
        /// </summary>
        protected UISprite StatusSprite => _statusSprite;

        /// <summary>
        /// Refreshes the controls with current data.
        /// </summary>
        protected abstract void Refresh();
    }
}