using AlgernonCommons;
using AlgernonCommons.UI;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace TransferController
{
    /// <summary>
    /// Selected building panel.
    /// </summary>
    internal class SelectedBuildingPanel : UIPanel
    {
        // Panel components.
        protected readonly UIList buildingList;

        // Current selection.
        private ushort selectedBuilding;

        // Parent reference.
        internal BuildingRestrictionsTab ParentPanel { get; set; }


        /// <summary>
        /// Currently selected building.
        /// </summary>
        internal ushort SelectedBuilding
        {
            get => selectedBuilding;

            set
            {
                selectedBuilding = value;

                // Refresh parent panel button states.
                ParentPanel.SelectionUpdated();
            }
        }


        /// <summary>
        /// Performs initial setup.
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
                buildingList = UIList.AddUIList<BuildingRow>(this);
                buildingList.BackgroundSprite = "UnlockingPanel";
                buildingList.width = BuildingRestrictionsTab.BuildingColumnWidth;
                buildingList.height = BuildingRestrictionsTab.ListHeight;
                //buildingList.canSelect = true;
                //buildingList.rowHeight = DistrictRow.DefaultRowHeight;
                //buildingList.autoHideScrollbar = true;
                buildingList.relativePosition = Vector2.zero;
                buildingList.Data = new FastList<object>();
                buildingList.SelectedIndex = -1;

                buildingList.EventSelectionChanged += (control, selectedItem) =>
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
        /// Refreshes the list with current information.
        /// </summary>
        internal void RefreshList()
        {
            // Repopulate the list.
            PopulateList();

            // (Re)select currently-selected building to ensure list selection matches.
            buildingList.FindItem<ushort>(selectedBuilding);
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
                buildingList.Data = new FastList<object>
                {
                    m_buffer = new DistrictItem[0],
                    m_size = 0
                };
                buildingList.SelectedIndex = -1;
                return;
            }

            // Recreate UI building list from hashset.
            BuildingItem[] items = new BuildingItem[hashSet.Count];
            int i = 0;
            foreach (uint id in hashSet)
            {
                items[i++] = new BuildingItem((ushort)id);
            }

            buildingList.Data = new FastList<object>
            {
                m_buffer = items.OrderBy(x => x.Name).ToArray(),
                m_size = hashSet.Count,
            };
        }
    }
}