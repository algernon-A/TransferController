using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Selected building panel.
    /// </summary>
    internal class SelectedBuildingPanel : UIPanel
    {
        // Panel components.
        protected readonly UIBuildingFastList buildingList;

        // Current selection.
        private ushort selectedBuilding;

        // Parent reference.
        internal TransferBuildingTab ParentPanel { get; set; }


        /// <summary>
        /// Currently selected district.
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
                width = TransferBuildingTab.BuildingColumnWidth;
                height = TransferBuildingTab.ListHeight;

                // District selection list.
                buildingList = UIBuildingFastList.Create<BuildingRow, UIBuildingFastList>(this);
                buildingList.backgroundSprite = "UnlockingPanel";
                buildingList.width = TransferBuildingTab.BuildingColumnWidth;
                buildingList.height = TransferBuildingTab.ListHeight;
                buildingList.canSelect = true;
                buildingList.rowHeight = DistrictRow.RowHeight;
                buildingList.autoHideScrollbar = true;
                buildingList.relativePosition = Vector2.zero;
                buildingList.rowsData = new FastList<object>();
                buildingList.selectedIndex = -1;

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

            // (Re)select currently-selected district to ensure list selection matches.
            buildingList.FindBuilding(selectedBuilding);
        }


        /// <summary>
        /// Populates the list.
        /// </summary>
        private void PopulateList()
        {
            // Get building hashset for this building.
            HashSet<uint> hashSet = BuildingControl.GetBuildings(ParentPanel.CurrentBuilding, ParentPanel.RecordNumber);

            // If no building hashset was recovered, clear list and selection and exit.
            if (hashSet == null)
            {
                buildingList.rowsData = new FastList<object>
                {
                    m_buffer = new DistrictItem[0],
                    m_size = 0
                };
                buildingList.selectedIndex = -1;
                return;
            }

            // Recreate UI building list from hashset.
            BuildingItem[] items = new BuildingItem[hashSet.Count];
            int i = 0;
            foreach (uint id in hashSet)
            {
                items[i++] = new BuildingItem((ushort)id);
            }

            buildingList.rowsData = new FastList<object>
            {
                m_buffer = items.OrderBy(x => x.name).ToArray(),
                m_size = hashSet.Count
            };
        }
    }
}