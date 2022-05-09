using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// District selection panel main class.
    /// </summary>
    internal class DistrictSelectionPanel : UIPanel
    {
        // Panel components.
        protected readonly UIDistrictFastList districtList;

        // Current selection.
        internal int selectedDistrict;


        /// <summary>
        /// Performs initial setup.
        /// </summary>
        internal DistrictSelectionPanel()
        {
            try
            {
                // Basic setup.
                name = "DistrictSelectionPanel";
                autoLayout = false;
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = TransferPanel.ColumnWidth;
                height = TransferPanel.ListHeight;

                // District selection list.
                districtList = UIDistrictFastList.Create<DistrictRow, UIDistrictFastList>(this);
                districtList.backgroundSprite = "UnlockingPanel";
                districtList.width = TransferPanel.ColumnWidth;
                districtList.height = TransferPanel.ListHeight;
                districtList.canSelect = true;
                districtList.rowHeight = DistrictRow.RowHeight;
                districtList.autoHideScrollbar = true;
                districtList.relativePosition = Vector2.zero;
                districtList.rowsData = new FastList<object>();
                districtList.selectedIndex = -1;

            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up building panel");
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
            districtList.FindDistrict(selectedDistrict);
        }


        /// <summary>
        /// Populates the list.
        /// </summary>
        protected virtual void PopulateList()
        {
            // Local references.
            List<DistrictItem> districtRecords = new List<DistrictItem>();
            DistrictManager districtManager = Singleton<DistrictManager>.instance;

            // Generic districts.
            District[] districtBuffer = districtManager.m_districts.m_buffer;
            for (int i = 1; i < districtBuffer.Length; ++i)
            {
                if ((districtBuffer[i].m_flags & District.Flags.Created) != District.Flags.None)
                {
                    districtRecords.Add(new DistrictItem(i));
                }
            }

            // Park districts (park/industry/campus).
            DistrictPark[] parkBuffer = districtManager.m_parks.m_buffer;
            for (int i = 1; i < parkBuffer.Length; ++i)
            {
                if ((parkBuffer[i].m_flags & DistrictPark.Flags.Created) != DistrictPark.Flags.None)
                {
                    districtRecords.Add(new DistrictItem(-i));
                }
            }

            // Set fastlist items.
            districtList.rowsData = new FastList<object>
            {
                m_buffer = districtRecords.OrderBy(x => x.name).ToArray(),
                m_size = districtRecords.Count
            };
        }
    }
}