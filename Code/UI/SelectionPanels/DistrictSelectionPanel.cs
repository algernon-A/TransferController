// <copyright file="DistrictSelectionPanel.cs" company="algernon (K. Algernon A. Sheppard)">
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
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// District selection panel main class.
    /// </summary>
    internal class DistrictSelectionPanel : UIPanel
    {
        // The district selection list.
        private readonly UIList _districtList;

        // Current selection.
        private int _selectedDistrict;

        /// <summary>
        /// Initializes a new instance of the <see cref="DistrictSelectionPanel"/> class.
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
                width = BuildingRestrictionsTab.ColumnWidth;
                height = BuildingRestrictionsTab.ListHeight;

                // District selection list.
                _districtList = UIList.AddUIList<DistrictRow>(this);
                _districtList.BackgroundSprite = "UnlockingPanel";
                _districtList.width = BuildingRestrictionsTab.ColumnWidth;
                _districtList.height = BuildingRestrictionsTab.ListHeight;
                _districtList.relativePosition = Vector2.zero;
                _districtList.Data = new FastList<object>();
                _districtList.SelectedIndex = -1;

                _districtList.EventSelectionChanged += (c, value) =>
                {
                    if (value is DistrictItem districtItem)
                    {
                        SelectedDistrict = districtItem.ID;
                    }
                };
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up district selection panel");
            }
        }

        /// <summary>
        /// Gets or sets the parent panel reference.
        /// </summary>
        internal BuildingRestrictionsTab ParentPanel { get; set; }

        /// <summary>
        /// Gets or sets the currently selected district.
        /// </summary>
        internal int SelectedDistrict
        {
            get => _selectedDistrict;

            set
            {
                _selectedDistrict = value;

                // Refresh parent panel button states.
                ParentPanel.SelectionUpdated();
            }
        }

        /// <summary>
        /// Gets the district selection list reference.
        /// </summary>
        protected internal UIList DistrictList => _districtList;

        /// <summary>
        /// Gets the HashSet of currently selected districts for the currently selected building.
        /// </summary>
        protected internal HashSet<int> DistrictSettingsList => BuildingControl.GetDistricts(ParentPanel.CurrentBuilding, ParentPanel.IsIncoming, ParentPanel.TransferReason);

        /// <summary>
        /// Refreshes the list with current information.
        /// </summary>
        internal void RefreshList()
        {
            // Repopulate the list.
            PopulateList();

            // (Re)select currently-selected district to ensure list selection matches.
            DistrictList.FindItem<int>(_selectedDistrict);
        }

        /// <summary>
        /// Populates the list.
        /// </summary>
        protected virtual void PopulateList()
        {
            // Local reference.
            HashSet<int> selectedDistricts = DistrictSettingsList;

            // Local references.
            List<DistrictItem> districtRecords = new List<DistrictItem>();
            DistrictManager districtManager = Singleton<DistrictManager>.instance;

            // Generic districts.
            District[] districtBuffer = districtManager.m_districts.m_buffer;
            for (int i = 1; i < districtBuffer.Length; ++i)
            {
                if ((districtBuffer[i].m_flags & District.Flags.Created) != District.Flags.None)
                {
                    // Skip any existing records.
                    if (selectedDistricts == null || !selectedDistricts.Contains(i))
                    {
                        districtRecords.Add(new DistrictItem(i));
                    }
                }
            }

            // Park districts (park/industry/campus).
            DistrictPark[] parkBuffer = districtManager.m_parks.m_buffer;
            for (int i = 1; i < parkBuffer.Length; ++i)
            {
                if ((parkBuffer[i].m_flags & DistrictPark.Flags.Created) != DistrictPark.Flags.None)
                {
                    // Skip any existing records.
                    if (selectedDistricts == null || !selectedDistricts.Contains(-i))
                    {
                        districtRecords.Add(new DistrictItem(-i));
                    }
                }
            }

            // Set display list items, without changing the display.
            DistrictList.Data = new FastList<object>
            {
                m_buffer = districtRecords.OrderBy(x => x.Name).ToArray(),
                m_size = districtRecords.Count,
            };
        }
    }
}