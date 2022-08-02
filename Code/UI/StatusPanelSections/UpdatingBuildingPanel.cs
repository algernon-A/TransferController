// <copyright file="UpdatingBuildingPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// UI panel which regenerates content every second.
    /// </summary>
    public abstract class UpdatingBuildingPanel : UIPanel
    {
        /// <summary>
        /// Current buildiing selection.
        /// </summary>
        private ushort _currentBuilding;

        // Timer.
        private float ticks;

        /// <summary>
        /// Gets the current building ID.
        /// </summary>
        public ushort CurrentBuilding => _currentBuilding;

        /// <summary>
        /// Called by Unity every update.
        /// Used to refresh the list periodically.
        /// </summary>
        public override void Update()
        {
            base.Update();

            ticks += Time.deltaTime;

            // Refresh every second.
            if (ticks > 1)
            {
                UpdateContent();
                ticks = 0f;
            }
        }

        /// <summary>
        /// Sets the target to the selected building.
        /// </summary>
        /// <param name="buildingID">New building ID.</param>
        internal virtual void SetTarget(ushort buildingID)
        {
            // Set target building and regenerate the list.
            _currentBuilding = buildingID;
            UpdateContent();

            // Reset timer.
            ticks = 0f;
        }

        /// <summary>
        /// Updates panel content.
        /// </summary>
        protected abstract void UpdateContent();
    }
}