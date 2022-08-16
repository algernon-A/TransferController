// <copyright file="StatusPanelSection.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using AlgernonCommons;

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
    }
}