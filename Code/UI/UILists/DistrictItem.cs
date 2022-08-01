// <copyright file="DistrictItem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using ColossalFramework;
    using UnityEngine;

    /// <summary>
    /// District list item record.
    /// </summary>
    public class DistrictItem
    {
        // District data.
        private int _districtID;
        private string _districtName;
        private Color32 _districtColor;

        /// <summary>
        /// Initializes a new instance of the <see cref="DistrictItem"/> class.
        /// </summary>
        /// <param name="id">District ID for this item.</param>
        public DistrictItem(int id)
        {
            ID = id;
        }

        /// <summary>
        /// Gets the district's name (empty string if none).
        /// </summary>
        public string Name => _districtName;

        /// <summary>
        /// Gets the district's text display color.
        /// </summary>
        public Color32 Color => _districtColor;

        /// <summary>
        /// Gets or sets the district ID for this record.  Negative values represent park districts.
        /// </summary>
        public int ID
        {
            get => _districtID;

            set
            {
                _districtID = value;

                // Local reference.
                DistrictManager districtManager = Singleton<DistrictManager>.instance;

                // Default color is white.
                _districtColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

                if (value < 0)
                {
                    // Park area.
                    _districtName = districtManager.GetParkName(-value);

                    // Set park display color if applicable.
                    ref DistrictPark park = ref districtManager.m_parks.m_buffer[-value];
                    if (park.IsIndustry)
                    {
                        _districtColor = new Color32(255, 230, 160, 255);
                    }
                    else if (park.IsPark)
                    {
                        _districtColor = new Color32(140, 255, 200, 255);
                    }
                }
                else
                {
                    // District.
                    _districtName = districtManager.GetDistrictName(value);
                }
            }
        }
    }
}