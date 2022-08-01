// <copyright file="SelectedVehiclePanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System.Collections.Generic;
    using System.Linq;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Selected vehicle panel.
    /// </summary>
    internal class SelectedVehiclePanel : VehicleSelectionPanel
    {
        // Panel to display when no item is selected.
        private UIPanel _randomPanel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectedVehiclePanel"/> class.
        /// </summary>
        internal SelectedVehiclePanel()
        {
            // Panel setup.
            _randomPanel = this.AddUIComponent<UIPanel>();
            _randomPanel.width = this.width;
            _randomPanel.height = this.height;
            _randomPanel.relativePosition = new Vector2(0f, 0f);

            // Random sprite.
            UISprite randomSprite = _randomPanel.AddUIComponent<UISprite>();
            randomSprite.atlas = UITextures.InGameAtlas;
            randomSprite.spriteName = "Random";

            // Label.
            UILabel randomLabel = randomSprite.AddUIComponent<UILabel>();
            randomLabel.textScale = 0.8f;
            randomLabel.text = Translations.Translate("TFC_VEH_ANY");

            // Size is 56x33, so offset -8 from left and 3.5 from top to match normal row sizing.
            randomSprite.size = new Vector2(56f, 33f);
            randomSprite.relativePosition = new Vector2(-8, (40f - randomSprite.height) / 2f);
            randomLabel.relativePosition = new Vector2(48f, (randomSprite.height - randomLabel.height) / 2f);
        }

        /// <summary>
        /// Populates the list.
        /// </summary>
        protected override void PopulateList()
        {
            List<VehicleItem> items = new List<VehicleItem>();
            List<VehicleInfo> buildingVehicles = VehicleControl.GetVehicles(ParentPanel.CurrentBuilding, ParentPanel.TransferReason);
            if (buildingVehicles != null)
            {
                foreach (VehicleInfo vehicle in buildingVehicles)
                {
                    items.Add(new VehicleItem(vehicle));
                }
            }

            // If list is empty, show random item panel (and hide otherwise).
            _randomPanel.isVisible = items.Count == 0;

            // Set display list items, without changing the display.
            VehicleList.Data = new FastList<object>
            {
                m_buffer = items.OrderBy(x => x.Name).ToArray(),
                m_size = items.Count,
            };
        }
    }
}