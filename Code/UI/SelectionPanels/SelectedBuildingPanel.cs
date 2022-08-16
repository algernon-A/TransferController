// <copyright file="SelectedBuildingPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AlgernonCommons;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Selected building panel.
    /// </summary>
    internal class SelectedBuildingPanel : UIPanel
    {
        // Panel components.
        private readonly UIList _buildingList;

        // Current selection.
        private ushort _selectedBuilding;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectedBuildingPanel"/> class.
        /// </summary>
        internal SelectedBuildingPanel()
        {
            try
            {
                // Basic setup.
                name = "SelectedBuildingPanel";
                autoLayout = false;
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = BuildingRestrictionsTab.BuildingColumnWidth;
                height = BuildingRestrictionsTab.ListHeight;

                // District selection list.
                _buildingList = UIList.AddUIList<BuildingRow>(this, 0f, 0f, BuildingRestrictionsTab.BuildingColumnWidth, BuildingRestrictionsTab.ListHeight);
                _buildingList.EventSelectionChanged += (control, selectedItem) =>
                {
                    if (selectedItem is BuildingItem buildingItem)
                    {
                        SelectedBuilding = buildingItem.ID;
                    }
                    else
                    {
                        SelectedBuilding = 0;
                    }
                };
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up selected building panel");
            }
        }

        /// <summary>
        /// Gets or sets the parent panel reference.
        /// </summary>
        internal BuildingRestrictionsTab ParentPanel { get; set; }

        /// <summary>
        /// Gets or sets the currently selected building.
        /// </summary>
        internal ushort SelectedBuilding
        {
            get => _selectedBuilding;

            set
            {
                _selectedBuilding = value;

                // Refresh parent panel button states.
                ParentPanel.SelectionUpdated();
            }
        }

        /// <summary>
        /// Refreshes the list with current information.
        /// </summary>
        internal void RefreshList()
        {
            // Repopulate the list.
            PopulateList();

            // (Re)select currently-selected building to ensure list selection matches.
            _buildingList.FindItem<ushort>(_selectedBuilding);
        }

        /// <summary>
        /// Populates the list.
        /// </summary>
        private void PopulateList()
        {
            // Get building hashset for this building.
            HashSet<uint> hashSet = BuildingControl.GetBuildings(ParentPanel.CurrentBuilding, ParentPanel.IsIncoming, ParentPanel.TransferReason);

            // If no building hashset was recovered, clear list and selection and exit.
            if (hashSet == null)
            {
                _buildingList.Data = new FastList<object>
                {
                    m_buffer = new DistrictItem[0],
                    m_size = 0,
                };
                _buildingList.SelectedIndex = -1;
                return;
            }

            // Recreate UI building list from hashset.
            BuildingItem[] items = new BuildingItem[hashSet.Count];
            int i = 0;
            foreach (uint id in hashSet)
            {
                items[i++] = new BuildingItem((ushort)id);
            }

            // Set display list items, without changing the display.
            _buildingList.Data = new FastList<object>
            {
                m_buffer = items.OrderBy(x => x.Name).ToArray(),
                m_size = hashSet.Count,
            };
        }
    }
}