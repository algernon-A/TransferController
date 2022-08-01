// <copyright file="SelectedDistrictPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Selected district panel.
    /// </summary>
    internal class SelectedDistrictPanel : DistrictSelectionPanel
    {
        /// <summary>
        /// Populates the district list.
        /// </summary>
        protected override void PopulateList()
        {
            // Get district hashset for this building.
            HashSet<int> hashSet = DistrictSettingsList;

            // If no district hashset was recovered, clear list and selection and exit.
            if (hashSet == null)
            {
                DistrictList.Data = new FastList<object>
                {
                    m_buffer = new DistrictItem[0],
                    m_size = 0,
                };
                DistrictList.SelectedIndex = -1;
                return;
            }

            // Validate hashset before continuting.
            TransferDataUtils.ValidateDistricts(hashSet);

            // Recreate UI district list from hashset.
            DistrictItem[] items = new DistrictItem[hashSet.Count];
            int i = 0;
            foreach (int id in hashSet)
            {
                items[i++] = new DistrictItem(id);
            }

            // Set display list items, without changing the display.
            DistrictList.Data = new FastList<object>
            {
                m_buffer = items.OrderBy(x => x.Name).ToArray(),
                m_size = hashSet.Count,
            };
        }
    }
}